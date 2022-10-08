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
using System.IO;
using System.Text;

namespace MiniIT.MessagePack
{
	public static class MessagePackSerializer
	{
		public static byte[] Serialize(Dictionary<string, object> data)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				Serialize(ms, data);
				return ms.ToArray();
			}
		}

		public static void Serialize(Stream ms, object val)
		{
			if (val == null)
			{
				ms.WriteByte((byte)0xC0);
			}
			else if (val is string str)
			{
				WriteString(ms, str);
			}
			else if (val is ISnipeObjectConvertable expando)
			{
				Serialize(ms, expando.ConvertToSnipeObject());
			}
			else if (val is IDictionary map)
			{
				WriteMap(ms, map);
			}
			else if (val is IList list)
			{
				WirteArray(ms, list);
			}
			else if (val is byte[] data)
			{
				WriteBinary(ms, data);
			}
			else
			{
				switch (Type.GetTypeCode(val.GetType()))
				{
					case TypeCode.UInt64:
						WriteInteger(ms, (ulong)val);
						break;

					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.Char:
						WriteInteger(ms, Convert.ToInt64(val));
						break;

					case TypeCode.Single:
						ms.WriteByte((byte)0xCA);
						ms.Write(EndianBitConverter.Big.GetBytes((float)val), 0, 4);
						break;

					case TypeCode.Double:
					case TypeCode.Decimal:
						ms.WriteByte((byte)0xCB);
						ms.Write(EndianBitConverter.Big.GetBytes(Convert.ToDouble(val)), 0, 8);
						break;

					case TypeCode.Boolean:
						ms.WriteByte((bool)val ? (byte)0xC3 : (byte)0xC2);
						break;
				}
			}
		}

		private static void WriteString(Stream ms, string str)
		{
			byte[] raw_bytes = Encoding.UTF8.GetBytes(str);
			int len = raw_bytes.Length;

			if (len <= 31)
			{
				ms.WriteByte((byte)(0xA0 | len));
			}
			else if (len <= 0xFF)
			{
				ms.WriteByte((byte)0xD9);
				ms.WriteByte((byte)len);
			}
			else if (len <= 0xFFFF)
			{
				ms.WriteByte((byte)0xDA);
				ms.Write(EndianBitConverter.Big.GetBytes(Convert.ToUInt16(len)), 0, 2);
			}
			else
			{
				ms.WriteByte((byte)0xDB);
				ms.Write(EndianBitConverter.Big.GetBytes(Convert.ToUInt32(len)), 0, 4);
			}

			ms.Write(raw_bytes, 0, raw_bytes.Length);
		}

		private static void WriteMap(Stream ms, IDictionary map)
		{
			int len = map.Count;
			if (len <= 0x0F)
			{
				ms.WriteByte((byte)(0x80 | len));
			}
			else if (len <= 0xFFFF)
			{
				ms.WriteByte((byte)0xDE);
				ms.Write(EndianBitConverter.Big.GetBytes((UInt16)len), 0, 2);
			}
			else
			{
				ms.WriteByte((byte)0xDF);
				ms.Write(EndianBitConverter.Big.GetBytes((UInt32)len), 0, 4);
			}

			foreach (DictionaryEntry item in map)
			{
				Serialize(ms, item.Key);
				Serialize(ms, item.Value);
			}
		}

		private static void WirteArray(Stream ms, IList list)
		{
			int len = list.Count;
			if (len <= 0x0F)
			{
				ms.WriteByte((byte)(0x90 | len));
			}
			else if (len <= 0xFFFF)
			{
				ms.WriteByte((byte)0xDC);
				ms.Write(EndianBitConverter.Big.GetBytes((UInt16)len), 0, 2);
			}
			else
			{
				ms.WriteByte((byte)0xDD);
				ms.Write(EndianBitConverter.Big.GetBytes((UInt32)len), 0, 4);
			}

			for (int i = 0; i < len; i++)
			{
				Serialize(ms, list[i]);
			}
		}

		private static void WriteBinary(Stream ms, byte[] data)
		{
			int len = data.Length;
			if (len <= 0xFF)
			{
				ms.WriteByte((byte)0xC4);
				ms.WriteByte((byte)len);
			}
			else if (len <= 0xFFFF)
			{
				ms.WriteByte((byte)0xC5);
				ms.Write(EndianBitConverter.Big.GetBytes(Convert.ToUInt16(len)), 0, 2);
			}
			else
			{
				ms.WriteByte((byte)0xC6);
				ms.Write(EndianBitConverter.Big.GetBytes(Convert.ToUInt32(len)), 0, 4);
			}

			ms.Write(data, 0, data.Length);
		}

		private static void WriteInteger(Stream ms, ulong val) // uint 64
		{
			ms.WriteByte(0xCF);

			byte[] bytes = EndianBitConverter.Big.GetBytes(val);
			ms.Write(bytes, 0, bytes.Length);
		}

		private static void WriteInteger(Stream ms, long val)
		{
			if (val >= 0)
			{
				if (val <= 0x7F)  // positive fixint
				{
					ms.WriteByte((byte)val);
				}
				else if (val <= 0xFF)  // uint 8
				{
					ms.WriteByte((byte)0xCC);
					ms.WriteByte((byte)val);
				}
				else if (val <= 0xFFFF)  // uint 16
				{
					ms.WriteByte((byte)0xCD);
					ms.Write(EndianBitConverter.Big.GetBytes((UInt16)val), 0, 2);
				}
				else if (val <= 0xFFFFFFFF)  // uint 32
				{
					ms.WriteByte((byte)0xCE);
					ms.Write(EndianBitConverter.Big.GetBytes((UInt32)val), 0, 4);
				}
				else // signed int 64
				{
					ms.WriteByte((byte)0xD3);
					ms.Write(EndianBitConverter.Big.GetBytes(val), 0, 8);
				}
			}
			else
			{
				if (val <= Int32.MinValue)  // int 64
				{
					ms.WriteByte((byte)0xD3);
					ms.Write(EndianBitConverter.Big.GetBytes(val), 0, 8);
				}
				else if (val <= Int16.MinValue)  // int 32
				{
					ms.WriteByte((byte)0xD2);
					ms.Write(EndianBitConverter.Big.GetBytes((Int32)val), 0, 4);
				}
				else if (val <= -128)  // int 16
				{
					ms.WriteByte((byte)0xD1);
					ms.Write(EndianBitConverter.Big.GetBytes((Int16)val), 0, 2);
				}
				else if (val <= -32)  // int 8
				{
					ms.WriteByte((byte)0xD0);
					ms.WriteByte((byte)val);
				}
				else  // negative fixint (5-bit)
				{
					ms.WriteByte((byte)val);
				}
			}
		}
	}
}