using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;

namespace kcp2k
{
    public class KcpClientConnection : KcpConnection
    {
        // IMPORTANT: raw receive buffer always needs to be of 'MTU' size, even
        //            if MaxMessageSize is larger. kcp always sends in MTU
        //            segments and having a buffer smaller than MTU would
        //            silently drop excess data.
        //            => we need the MTU to fit channel + message!
        readonly byte[] rawReceiveBuffer = new byte[Kcp.MTU_DEF];
        
        public double DnsResolveTime { get; private set; }
        public double SocketConnectTime { get; private set; }
        public double SendHandshakeTime { get; private set; }

        // helper function to resolve host to IPAddress
        public static bool ResolveHostname(string hostname, out IPAddress[] addresses)
        {
            try
            {
                addresses = Dns.GetHostAddresses(hostname);
                return addresses.Length >= 1;
            }
            catch (SocketException)
            {
                Log.Info($"Failed to resolve host: {hostname}");
                addresses = null;
                return false;
            }
        }
        
        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=netframework-4.8#examples
        protected bool ConnectSocket(IPAddress[] addresses, int port)
        {
            socket = null;
            
            // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
            // an exception that occurs when the host IP Address is not compatible with the address family
            // (typical in the IPv6 case).
            foreach (IPAddress address in addresses)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                tempSocket.Connect(ipe);

                if (tempSocket.Connected)
                {
                    socket = tempSocket;
                    remoteEndPoint = ipe;
                    return true;
                }
            }
            return false;
        }

        // EndPoint & Receive functions can be overwritten for where-allocation:
        // https://github.com/vis2k/where-allocation
        // NOTE: Client's SendTo doesn't allocate, don't need a virtual.
        //protected virtual void CreateRemoteEndPoint(IPAddress[] addresses, ushort port) =>
        //    remoteEndPoint = new IPEndPoint(addresses[0], port);

        protected virtual int ReceiveFrom(byte[] buffer) =>
            //socket.ReceiveFrom(buffer, ref remoteEndPoint);
            socket.Receive(buffer);

        public void Connect(string host, ushort port, bool noDelay, uint interval = Kcp.INTERVAL, int fastResend = 0, bool congestionWindow = true, uint sendWindowSize = Kcp.WND_SND, uint receiveWindowSize = Kcp.WND_RCV, int timeout = DEFAULT_TIMEOUT, uint maxRetransmits = Kcp.DEADLINK)
        {
            Log.Info($"KcpClient: connect to {host}:{port}");

            // try resolve host name
            var stopwatch = Stopwatch.StartNew();
            if (ResolveHostname(host, out IPAddress[] addresses))
            {
                stopwatch.Stop();
                DnsResolveTime = stopwatch.Elapsed.TotalMilliseconds;
                
                stopwatch.Restart();
                ConnectSocket(addresses, port);
                stopwatch.Stop();
                SocketConnectTime = stopwatch.Elapsed.TotalMilliseconds;
                
                if (socket == null)
                {
                    OnDisconnected();
                    return;
                }

                // set up kcp
                SetupKcp(noDelay, interval, fastResend, congestionWindow, sendWindowSize, receiveWindowSize, timeout, maxRetransmits);

                // client should send handshake to server as very first message
                stopwatch.Restart();
                SendHandshake();
                stopwatch.Stop();
                SendHandshakeTime = stopwatch.Elapsed.TotalMilliseconds;

                RawReceive();
            }
            // otherwise call OnDisconnected to let the user know.
            else
            {
                OnDisconnected();
            }
        }

        // call from transport update
        public void RawReceive()
        {
            try
            {
                if (socket != null)
                {
                    while (socket.Poll(0, SelectMode.SelectRead))
                    {
                        int msgLength = ReceiveFrom(rawReceiveBuffer);
                        
                        // Log.Info($"RAW RECV {msgLength} bytes = {BitConverter.ToString(rawReceiveBuffer, 0, msgLength)}");
                        
                        // IMPORTANT: detect if buffer was too small for the
                        //            received msgLength. otherwise the excess
                        //            data would be silently lost.
                        //            (see ReceiveFrom documentation)
                        if (msgLength <= rawReceiveBuffer.Length)
                        {
                            //Log.Debug($"KCP: client raw recv {msgLength} bytes = {BitConverter.ToString(buffer, 0, msgLength)}");
                            RawInput(rawReceiveBuffer, msgLength);
                        }
                        else
                        {
                            Log.Error($"KCP ClientConnection: message of size {msgLength} does not fit into buffer of size {rawReceiveBuffer.Length}. The excess was silently dropped. Disconnecting.");
                            Disconnect();
                        }
                    }
                }
            }
            // this is fine, the socket might have been closed in the other end
            catch (SocketException) {}
        }

        protected override void Dispose()
        {
            socket.Close();
            socket = null;
        }

        protected override void RawSend(byte[] data, int length)
        {
            socket.Send(data, length, SocketFlags.None);
        }
    }
}
