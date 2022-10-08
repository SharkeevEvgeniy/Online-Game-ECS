//
//  MessagePack serialization format specification can be found here:
//  https://github.com/msgpack/msgpack/blob/master/spec.md
//
//  This implementation is inspired by
//  https://github.com/ymofen/SimpleMsgPack.Net
//


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniIT.MessagePack
{
	public static class MessagePackDeserializer
	{
		// cached encoding
		private static readonly Encoding ENCODING_UTF8 = Encoding.UTF8;

		public static object Parse(byte[] array)
		{
			using (MemoryStream ms = new MemoryStream(array))
			{
				return Parse(ms);
			}
		}
		
		public static object Parse(ArraySegment<byte> buffer)
		{
			using (MemoryStream ms = new MemoryStream(buffer.Array))
			{
				ms.Position = buffer.Offset;
				return Parse(ms);
			}
		}

		public static object Parse(Stream ms)
		{
			byte format_byte = (byte)ms.ReadByte();

			if (format_byte <= 0x7F)  // positive fixint	0xxxxxxx	0x00 - 0x7f
			{
				return Convert.ToInt32(format_byte);
			}
			else if ((format_byte >= 0x80) && (format_byte <= 0x8F))  // fixmap	1000xxxx	0x80 - 0x8f
			{
				int len = format_byte & 0b00001111;
				return ReadMap(ms, len);
			}
			else if ((format_byte >= 0x90) && (format_byte <= 0x9F))  // fixarray	1001xxxx	0x90 - 0x9f
			{
				int len = format_byte & 0b00001111;
				return ReadArray(ms, len);
			}
			else if ((format_byte >= 0xA0) && (format_byte <= 0xBF))  // fixstr	101xxxxx	0xa0 - 0xbf
			{
				int len = format_byte & 0b00011111;
				return ReadString(ms, len);
			}
			else if ((format_byte >= 0xE0) && (format_byte <= 0xFF)) // negative fixint	111xxxxx	0xe0 - 0xff (5-bit negative integer)
			{
				return Convert.ToInt32((sbyte)format_byte);
			}
			else if (format_byte == 0xC0)
			{
				return null;
			}
			//else if (format_byte == 0xC1)
			//{
			//    throw new ArgumentException("(never used) 0xc1");
			//}
			//else if ((format_byte == 0xC7) || (format_byte == 0xC8) || (format_byte == 0xC9))
			//{
			//    throw new ArgumentException("(ext8, ext16, ex32) type 0xc7, 0xc8, 0xc9");
			//}
			else if (format_byte == 0xC2)
			{
				return false;
			}
			else if (format_byte == 0xC3)
			{
				return true;
			}
			else if (format_byte == 0xC4)  // bin 8
			{
				int len = ms.ReadByte();
				var raw_bytes = new byte[len];
				ms.Read(raw_bytes, 0, len);
				return Convert.ToInt32(raw_bytes);
			}
			else if (format_byte == 0xC5)  // bin 16
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				int len = EndianBitConverter.Big.ToInt16(raw_bytes, 0);

				raw_bytes = new byte[len];
				ms.Read(raw_bytes, 0, len);
				return raw_bytes;
			}
			else if (format_byte == 0xC6)  // bin 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				int len = Convert.ToInt32(EndianBitConverter.Big.ToUInt32(raw_bytes, 0));

				raw_bytes = new byte[len];
				ms.Read(raw_bytes, 0, len);
				return raw_bytes;
			}
			else if (format_byte == 0xCA)  // float 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				return EndianBitConverter.Big.ToSingle(raw_bytes, 0);
			}
			else if (format_byte == 0xCB)  // float 64
			{
				var raw_bytes = new byte[8];
				ms.Read(raw_bytes, 0, 8);
				return EndianBitConverter.Big.ToDouble(raw_bytes, 0);
			}
			else if (format_byte == 0xCC)  // uint8
			{
				return Convert.ToInt32(ms.ReadByte());
			}
			else if (format_byte == 0xCD)  // uint16
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				return EndianBitConverter.Big.ToUInt16(raw_bytes, 0);
			}
			else if (format_byte == 0xCE)  // uint 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				return EndianBitConverter.Big.ToUInt32(raw_bytes, 0);
			}
			else if (format_byte == 0xCF)  // uint 64
			{
				var raw_bytes = new byte[8];
				ms.Read(raw_bytes, 0, 8);
				return EndianBitConverter.Big.ToUInt64(raw_bytes, 0);
			}
			else if (format_byte == 0xD9 || format_byte == 0xDA || format_byte == 0xDB)  // str 8, str 16, str 32
			{
				return ReadString(format_byte, ms);
			}
			else if (format_byte == 0xDC)  // array 16
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				var len = EndianBitConverter.Big.ToInt16(raw_bytes, 0);
				return ReadArray(ms, len);
			}
			else if (format_byte == 0xDD)  // array 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				int len = EndianBitConverter.Big.ToInt32(raw_bytes, 0);
				return ReadArray(ms, len);
			}
			else if (format_byte == 0xDE)  // map 16
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				var len = EndianBitConverter.Big.ToInt16(raw_bytes, 0);
				return ReadMap(ms, len);
			}
			else if (format_byte == 0xDF)  // map 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				int len = EndianBitConverter.Big.ToInt32(raw_bytes, 0);
				return ReadMap(ms, len);
			}
			else if (format_byte == 0xD0)  // int 8
			{
				return Convert.ToInt32((sbyte)ms.ReadByte());
			}
			else if (format_byte == 0xD1)  // int 16
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				return Convert.ToInt32(EndianBitConverter.Big.ToInt16(raw_bytes, 0));
			}
			else if (format_byte == 0xD2)  // int 32
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				return Convert.ToInt32(EndianBitConverter.Big.ToInt32(raw_bytes, 0));
			}
			else if (format_byte == 0xD3)  // int 64
			{
				var raw_bytes = new byte[8];
				ms.Read(raw_bytes, 0, 8);
				return EndianBitConverter.Big.ToInt64(raw_bytes, 0);
			}

			return null;
		}

		private static List<object> ReadArray(Stream ms, int len)
		{
			var data = new List<object>(len);
			for (int i = 0; i < len; i++)
			{
				data.Add(Parse(ms));
			}
			return data;
		}

		private static SnipeObject ReadMap(Stream ms, int len)
		{
			var data = new SnipeObject();
			for (int i = 0; i < len; i++)
			{
				string key = ReadString(ms);
				var value = Parse(ms);
				data[key] = value;
			}
			return data;
		}

		private static string ReadString(Stream ms, int len)
		{
			byte[] data = new byte[len];
			ms.Read(data, 0, len);
			return ENCODING_UTF8.GetString(data);
		}

		private static string ReadString(Stream ms)
		{
			byte[] data = new byte[1];
			ms.Read(data, 0, 1);
			return ReadString(data[0], ms);
		}

		private static string ReadString(byte flag, Stream ms)
		{
			int len = 0;
			if ((flag >= 0xA0) && (flag <= 0xBF))  // fixstr stores a byte array whose length is upto 31 bytes:
			{
				len = flag & 0b00011111;
			}
			else if (flag == 0xD9)                 // str 8 stores a byte array whose length is upto (2^8)-1 bytes:
			{
				len = ms.ReadByte();
			}
			else if (flag == 0xDA)                 // str 16 stores a byte array whose length is upto (2^16)-1 bytes:
			{
				var raw_bytes = new byte[2];
				ms.Read(raw_bytes, 0, 2);
				len = Convert.ToInt32(EndianBitConverter.Big.ToInt16(raw_bytes, 0));
			}
			else if (flag == 0xDB)                 // str 32 stores a byte array whose length is upto (2^32)-1 bytes:
			{
				var raw_bytes = new byte[4];
				ms.Read(raw_bytes, 0, 4);
				len = Convert.ToInt32(EndianBitConverter.Big.ToUInt32(raw_bytes, 0));
			}

			var string_bytes = new byte[len];
			ms.Read(string_bytes, 0, len);

			return ENCODING_UTF8.GetString(string_bytes);
		}
	}
}