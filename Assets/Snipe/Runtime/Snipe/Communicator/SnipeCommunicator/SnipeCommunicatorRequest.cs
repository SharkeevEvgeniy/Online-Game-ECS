using System;
using System.Threading.Tasks;
using MiniIT;

namespace MiniIT.Snipe
{
	public class SnipeCommunicatorRequest : IDisposable
	{
		private const int RETRIES_COUNT = 3;
		private const int RETRY_DELAY = 1000; // milliseconds
		
		private static readonly SnipeObject EMPTY_DATA = new SnipeObject();
		
		public string MessageType { get; private set; }
		public SnipeObject Data { get; set; }
		
		public bool WaitingForRoomJoined { get; private set; } = false;
		
		public delegate void ResponseHandler(string error_code, SnipeObject data);

		private SnipeCommunicator mCommunicator;
		private ResponseHandler mCallback;

		private int mRequestId;
		private int mRetriesLeft = RETRIES_COUNT;
		
		private bool mSent = false;
		private bool mWaitingForResponse = false;
		private bool mAuthorization = false;

		public SnipeCommunicatorRequest(SnipeCommunicator communicator, string message_type = null)
		{
			mCommunicator = communicator;
			MessageType = message_type;
			
			if (mCommunicator != null)
			{	
				mCommunicator.Requests.Add(this);
			}
		}

		public void Request(SnipeObject data, ResponseHandler callback = null)
		{
			Data = data;
			Request(callback);
		}

		public virtual void Request(ResponseHandler callback = null)
		{
			if (mSent)
				return;
				
			mCallback = callback;
			SendRequest();
		}
		
		internal void RequestAuth(SnipeObject data, ResponseHandler callback = null)
		{
			mAuthorization = true;
			Data = data;
			Request(callback);
		}
		
		private void SendRequest()
		{
			mSent = true;
			
			if (mCommunicator == null || mCommunicator.RoomJoined == false && MessageType == SnipeMessageTypes.ROOM_LEAVE)
			{
				InvokeCallback(SnipeErrorCodes.NOT_READY, EMPTY_DATA);
				return;
			}
			
			if (string.IsNullOrEmpty(MessageType))
				MessageType = Data?.SafeGetString("t");

			if (string.IsNullOrEmpty(MessageType))
			{
				InvokeCallback(SnipeErrorCodes.INVALIND_DATA, EMPTY_DATA);
				return;
			}
			
			if (mCommunicator.LoggedIn || (mAuthorization && mCommunicator.Connected))
			{
				OnCommunicatorReady();
			}
			else
			{
				OnConnectionClosed(true);
			}
		}

		private void OnCommunicatorReady()
		{
			if (mCommunicator.RoomJoined != true &&
				MessageType.StartsWith(SnipeMessageTypes.PREFIX_ROOM) &&
				MessageType != SnipeMessageTypes.ROOM_JOIN &&
				MessageType != SnipeMessageTypes.ROOM_LEAVE)
			{
				WaitingForRoomJoined = true;
			}
			
			mCommunicator.ConnectionFailed -= OnConnectionClosed;
			mCommunicator.ConnectionFailed += OnConnectionClosed;
			
			if ((mCallback != null || WaitingForRoomJoined) && MessageType != SnipeMessageTypes.ROOM_LEAVE)
			{
				mWaitingForResponse = true;
				mCommunicator.MessageReceived -= OnMessageReceived;
				mCommunicator.MessageReceived += OnMessageReceived;
			}
			
			if (!WaitingForRoomJoined)
			{
				DoSendRequest();
			}
		}
		
		private void DoSendRequest()
		{
			mRequestId = 0;
			
			bool check_duplication = false;
			if (mCommunicator.mMergeableRequestTypes != null)
			{
				for (int i = 0; i < mCommunicator.mMergeableRequestTypes.Count; i++)
				{
					SnipeRequestDescriptor descriptor = mCommunicator.mMergeableRequestTypes[i];
					string mergeble_type = descriptor?.MessageType;
					
					if (mergeble_type != null && string.Equals(mergeble_type, this.MessageType, StringComparison.Ordinal))
					{
						bool matched = true;
						
						if (descriptor.Data != null && this.Data != null)
						{
							foreach (var pair in descriptor.Data)
							{
								if (this.Data[pair.Key] != pair.Value)
								{
									matched = false;
									break;
								}
							}
						}
						
						if (matched)
						{
							check_duplication = true;
							break;
						}
					}
				}
			}
			
			if (check_duplication)
			{
				for (int i = 0; i < mCommunicator.Requests.Count; i++)
				{
					var request = mCommunicator.Requests[i];
					
					if (request == null)
						continue;
					
					if (request == this)
						break;
					
					if (request.mAuthorization == this.mAuthorization &&
						string.Equals(request.MessageType, this.MessageType, StringComparison.Ordinal) &&
						SnipeObject.ContentEquals(request.Data, this.Data))
					{
						mRequestId = request.mRequestId;
						break;
					}
				}
			}
			
			if (mRequestId != 0)
			{
				DebugLogger.Log($"[SnipeCommunicatorRequest] DoSendRequest - Same request found: {MessageType}, id = {mRequestId}, mWaitingForResponse = {mWaitingForResponse}");
				
				if (!mWaitingForResponse)
				{
					Dispose();
				}
				return;
			}
			
			if (mCommunicator.LoggedIn || mAuthorization)
			{
				mRequestId = mCommunicator.Client.SendRequest(this.MessageType, this.Data);
			}
			
			if (mRequestId == 0)
			{
				InvokeCallback(SnipeErrorCodes.NOT_READY, EMPTY_DATA);
			}
			
			if (!mWaitingForResponse)
			{
				// keep this instance for a while to prevent duplicate requests
				DelayedDispose();
			}
		}

		private void OnConnectionClosed(bool will_retry = false)
		{
			if (will_retry)
			{
				mWaitingForResponse = false;

				mCommunicator.ConnectionSucceeded -= OnCommunicatorReady;
				mCommunicator.LoginSucceeded -= OnCommunicatorReady;
				mCommunicator.MessageReceived -= OnMessageReceived;
				
				if (mAuthorization)
				{
					DebugLogger.Log($"[SnipeCommunicatorRequest] Waiting for connection - {MessageType}");
					
					mCommunicator.ConnectionSucceeded += OnCommunicatorReady;
				}
				else if (mCommunicator.AllowRequestsToWaitForLogin)
				{
					DebugLogger.Log($"[SnipeCommunicatorRequest] Waiting for login - {MessageType} - {Data?.ToJSONString()}");
					
					mCommunicator.LoginSucceeded += OnCommunicatorReady;
				}
				else
				{
					InvokeCallback(SnipeErrorCodes.NOT_READY, EMPTY_DATA);
				}
			}
			else
			{
				Dispose();
			}
		}

		private void OnMessageReceived(string message_type, string error_code, SnipeObject response_data, int request_id)
		{
			if (mCommunicator == null)
				return;
			
			if (WaitingForRoomJoined && mCommunicator.RoomJoined == true)
			{
				DebugLogger.Log($"[SnipeCommunicatorRequest] OnMessageReceived - Room joined. Send {MessageType}, id = {mRequestId}");
				
				WaitingForRoomJoined = false;
				DoSendRequest();
				return;
			}
			
			if ((request_id == 0 || request_id == mRequestId) && message_type == MessageType)
			{
				if (error_code == SnipeErrorCodes.SERVICE_OFFLINE && mRetriesLeft > 0)
				{
					mRetriesLeft--;
					DelayedRetryRequest();
					return;
				}

				InvokeCallback(error_code, response_data);
			}
		}
		
		private void InvokeCallback(string error_code, SnipeObject response_data)
		{
			var callback = mCallback;
			
			Dispose();
			
			if (callback != null)
			{
				try
				{
					callback.Invoke(error_code, response_data);
				}
				catch (Exception e)
				{
					DebugLogger.Log($"[SnipeCommunicatorRequest] {MessageType} Callback invokation error: {e}");
				}
			}
		}
		
		private async void DelayedRetryRequest()
		{
			await Task.Delay(RETRY_DELAY);
			
			if (mCommunicator != null)
			{
				Request(mCallback);
			}
		}
		
		private async void DelayedDispose()
		{
			await Task.Yield();
			Dispose();
		}

		public void Dispose()
		{
			if (mCommunicator != null)
			{
				if (mCommunicator.Requests != null)
				{
					mCommunicator.Requests.Remove(this);
				}
				
				mCommunicator.LoginSucceeded -= OnCommunicatorReady;
				mCommunicator.ConnectionSucceeded -= OnCommunicatorReady;
				mCommunicator.ConnectionFailed -= OnConnectionClosed;
				mCommunicator.MessageReceived -= OnMessageReceived;
				mCommunicator = null;
			}
			
			mCallback = null;
			mWaitingForResponse = false;
		}
	}
}