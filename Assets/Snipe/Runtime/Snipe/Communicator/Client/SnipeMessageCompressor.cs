using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace MiniIT.Snipe
{
	public class SnipeMessageCompressor
	{
		private byte[] mDecompressionBuffer;
		
		public ArraySegment<byte> Compress(ArraySegment<byte> msg_data)
		{
			using (var stream = new MemoryStream())
			{
				using (var deflate = new DeflateStream(stream, CompressionLevel.Fastest))
				{
					deflate.Write(msg_data.Array, msg_data.Offset, msg_data.Count);
				}

				return new ArraySegment<byte>(stream.ToArray());
			}
		}

		public ArraySegment<byte> Decompress(ArraySegment<byte> compressed)
		{
			int length = 0;

			using (var stream = new MemoryStream(compressed.Array, compressed.Offset, compressed.Count))
			{
				using (var deflate = new DeflateStream(stream, CompressionMode.Decompress))
				{
					const int portion_size = 1024;
					while (deflate.CanRead)
					{
						if (mDecompressionBuffer == null)
							mDecompressionBuffer = new byte[portion_size];
						else if (mDecompressionBuffer.Length < length + portion_size)
							Array.Resize(ref mDecompressionBuffer, length + portion_size);

						int bytes_read = deflate.Read(mDecompressionBuffer, length, portion_size);
						if (bytes_read == 0)
							break;

						length += bytes_read;
					}
				}
			}

			return new ArraySegment<byte>(mDecompressionBuffer, 0, length);
		}
	}
}