//
//  MessagePack serialization format specification can be found here:
//  https://github.com/msgpack/msgpack/blob/master/spec.md
//
//  This implementation is inspired by
//  https://github.com/ymofen/SimpleMsgPack.Net
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace MiniIT.MessagePack
{
	public class MessagePackSerializerNonAlloc
	{
		public static int MaxUsedBufferSize { get; private set; } = 2048;
		
		private static Stack<MessagePackSerializerNonAlloc> mInstances;
		
		private int mPosition;
		
		public static ArraySegment<byte> Serialize(ref byte[] buffer, Dictionary<string, object> data)
		{
			return Serialize(ref buffer, 0, data);
		}
		
		public static ArraySegment<byte> Serialize(ref byte[] buffer, int position, Dictionary<string, object> data)
		{
			if (mInstances == null)
				mInstances = new Stack<MiniIT.MessagePack.MessagePackSerializerNonAlloc>(1);
			
			var serializer = mInstances.Count > 0 ? mInstances.Pop() : new MessagePackSerializerNonAlloc();
			var result = serializer.DoSerialize(ref buffer, position, data);
			mInstances.Push(serializer);
			
			return result;
		}
		
		private ArraySegment<byte> DoSerialize(ref byte[] buffer, int position, Dictionary<string, object> data)
		{
			mPosition = position;
			DoSerialize(ref buffer, data);
			return new ArraySegment<byte>(buffer, 0, mPosition);
		}
		
		private void DoSerialize(ref byte[] buffer, object val)
		{
			EnsureCapacity(ref buffer, 1);
			
			if (val == null)
			{
				buffer[mPosition++] = (byte)0xC0;
			}
			else if (val is string str)
			{
				WriteString(ref buffer, str);
			}
			else if (val is ISnipeObjectConvertable soc)
			{
				DoSerialize(ref buffer, soc.ConvertToSnipeObject());
			}
			else if (val is IDictionary map)
			{
				WriteMap(ref buffer, map);
			}
			else if (val is IList list)
			{
				WirteArray(ref buffer, list);
			}
			else if (val is byte[] data)
			{
				CopyBytes(ref buffer, data);
			}
			else
			{
				switch (Type.GetTypeCode(val.GetType()))
				{
					case TypeCode.UInt64:
						WriteInteger(ref buffer, (ulong)val);
						break;

					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.Char:
						WriteInteger(ref buffer, Convert.ToInt64(val));
						break;

					case TypeCode.Single:
						buffer[mPosition++] = (byte)0xCA;
						CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((float)val), 4);
						break;

					case TypeCode.Double:
					case TypeCode.Decimal:
						buffer[mPosition++] = (byte)0xCB;
						CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(Convert.ToDouble(val)), 8);
						break;

					case TypeCode.Boolean:
						buffer[mPosition++] = (bool)val ? (byte)0xC3 : (byte)0xC2;
						break;
				}
			}
		}

		private void WriteString(ref byte[] buffer, string str)
		{
			byte[] raw_bytes = Encoding.UTF8.GetBytes(str);
			int len = raw_bytes.Length;
			
			EnsureCapacity(ref buffer, len + 5);

			if (len <= 31)
			{
				buffer[mPosition++] = (byte)(0xA0 | len);
			}
			else if (len <= 0xFF)
			{
				buffer[mPosition++] = (byte)0xD9;
				buffer[mPosition++] = (byte)len;
			}
			else if (len <= 0xFFFF)
			{
				buffer[mPosition++] = (byte)0xDA;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(Convert.ToUInt16(len)), 2);
			}
			else
			{
				buffer[mPosition++] = (byte)0xDB;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(Convert.ToUInt32(len)), 4);
			}
			
			CopyBytes(ref buffer, raw_bytes, len);
		}

		private void WriteMap(ref byte[] buffer, IDictionary map)
		{
			int len = map.Count;
			
			EnsureCapacity(ref buffer, len + 5);
			
			if (len <= 0x0F)
			{
				buffer[mPosition++] = (byte)(0x80 | len);
			}
			else if (len <= 0xFFFF)
			{
				buffer[mPosition++] = (byte)0xDE;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt16)len), 2);
			}
			else
			{
				buffer[mPosition] = (byte)0xDF;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt32)len), 4);
			}

			foreach (DictionaryEntry item in map)
			{
				DoSerialize(ref buffer, item.Key);
				DoSerialize(ref buffer, item.Value);
			}
		}

		private void WirteArray(ref byte[] buffer, IList list)
		{
			int len = list.Count;
			
			EnsureCapacity(ref buffer, len + 5);
			
			if (len <= 0x0F)
			{
				buffer[mPosition++] = (byte)(0x90 | len);
			}
			else if (len <= 0xFFFF)
			{	
				buffer[mPosition++] = (byte)0xDC;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt16)len), 2);
			}
			else
			{
				buffer[mPosition++] = (byte)0xDD;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt32)len), 4);
			}

			for (int i = 0; i < len; i++)
			{
				DoSerialize(ref buffer, list[i]);
			}
		}

		private void WriteBinary(ref byte[] buffer, byte[] data)
		{
			int len = data.Length;
			
			EnsureCapacity(ref buffer, len + 5);
			
			if (len <= 0xFF)
			{
				buffer[mPosition++] = (byte)0xC4;
				buffer[mPosition++] = (byte)len;
			}
			else if (len <= 0xFFFF)
			{
				buffer[mPosition++] = (byte)0xC5;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(Convert.ToUInt16(len)), 2);
			}
			else
			{
				buffer[mPosition++] = (byte)0xC6;
				CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(Convert.ToUInt32(len)), 4);
			}
			
			CopyBytes(ref buffer, data, len);
		}

		private void WriteInteger(ref byte[] buffer, ulong val) // uint 64
		{
			EnsureCapacity(ref buffer, 9);
			
			buffer[mPosition++] = (byte)0xCF;
			CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(val));
		}

		private void WriteInteger(ref byte[] buffer, long val)
		{
			EnsureCapacity(ref buffer, 9);
			
			if (val >= 0)
			{
				if (val <= 0x7F)  // positive fixint
				{
					buffer[mPosition++] = (byte)val;
				}
				else if (val <= 0xFF)  // uint 8
				{
					buffer[mPosition++] = (byte)0xCC;
					buffer[mPosition++] = (byte)val;
				}
				else if (val <= 0xFFFF)  // uint 16
				{
					buffer[mPosition++] = (byte)0xCD;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt16)val), 2);
				}
				else if (val <= 0xFFFFFFFF)  // uint 32
				{
					buffer[mPosition++] = (byte)0xCE;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((UInt32)val), 4);
				}
				else // signed int 64
				{
					buffer[mPosition++] = (byte)0xD3;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(val), 8);
				}
			}
			else
			{
				if (val <= Int32.MinValue)  // int 64
				{
					buffer[mPosition++] = (byte)0xD3;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes(val), 8);
				}
				else if (val <= Int16.MinValue)  // int 32
				{
					buffer[mPosition++] = (byte)0xD2;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((Int32)val), 4);
				}
				else if (val <= -128)  // int 16
				{
					buffer[mPosition++] = (byte)0xD1;
					CopyBytes(ref buffer, EndianBitConverter.Big.GetBytes((Int16)val), 2);
				}
				else if (val <= -32)  // int 8
				{
					buffer[mPosition++] = (byte)0xD0;
					buffer[mPosition++] = (byte)val;
				}
				else  // negative fixint (5-bit)
				{
					buffer[mPosition++] = (byte)val;
				}
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CopyBytes(ref byte[] buffer, byte[] data)
		{
			CopyBytes(ref buffer, data, data.Length);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CopyBytes(ref byte[] buffer, byte[] data, int length)
		{
			EnsureCapacity(ref buffer, length);
			
			Array.ConstrainedCopy(data, 0, buffer, mPosition, length);
			mPosition += length;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void EnsureCapacity(ref byte[] buffer, int additional_lenght)
		{
			int length = mPosition + additional_lenght;
			if (buffer.Length < length)
			{
				int capacity = Math.Max(length, buffer.Length * 2);
				Array.Resize(ref buffer, capacity);
				
				if (MaxUsedBufferSize < capacity)
					MaxUsedBufferSize = capacity;
			}
		}
	}
}