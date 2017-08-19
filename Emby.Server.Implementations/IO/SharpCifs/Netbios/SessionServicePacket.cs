// This code is derived from jcifs smb client library <jcifs at samba dot org>
// Ported by J. Arturo <webmaster at komodosoft dot net>
//  
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System.IO;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	public abstract class SessionServicePacket
	{
		internal const int SessionMessage = unchecked(0x00);

		internal const int SessionRequest = unchecked(0x81);

		public const int PositiveSessionResponse = unchecked(0x82);

		public const int NegativeSessionResponse = unchecked(0x83);

		internal const int SessionRetargetResponse = unchecked(0x84);

		internal const int SessionKeepAlive = unchecked(0x85);

		internal const int MaxMessageSize = unchecked(0x0001FFFF);

		internal const int HeaderLength = 4;

		// session service packet types 
		internal static void WriteInt2(int val, byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = unchecked((byte)((val >> 8) & unchecked(0xFF)));
			dst[dstIndex] = unchecked((byte)(val & unchecked(0xFF)));
		}

		internal static void WriteInt4(int val, byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = unchecked((byte)((val >> 24) & unchecked(0xFF)));
			dst[dstIndex++] = unchecked((byte)((val >> 16) & unchecked(0xFF)));
			dst[dstIndex++] = unchecked((byte)((val >> 8) & unchecked(0xFF)));
			dst[dstIndex] = unchecked((byte)(val & unchecked(0xFF)));
		}

		internal static int ReadInt2(byte[] src, int srcIndex)
		{
			return ((src[srcIndex] & unchecked(0xFF)) << 8) + (src[srcIndex + 1] & unchecked(
				0xFF));
		}

		internal static int ReadInt4(byte[] src, int srcIndex)
		{
			return ((src[srcIndex] & unchecked(0xFF)) << 24) + ((src[srcIndex + 1] & unchecked(
				0xFF)) << 16) + ((src[srcIndex + 2] & unchecked(0xFF)) << 8) + (src
				[srcIndex + 3] & unchecked(0xFF));
		}

		internal static int ReadLength(byte[] src, int srcIndex)
		{
			srcIndex++;
			return ((src[srcIndex++] & unchecked(0x01)) << 16) + ((src[srcIndex++] & unchecked(
				0xFF)) << 8) + (src[srcIndex++] & unchecked(0xFF));
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static int Readn(InputStream @in, byte[] b, int off, int len)
		{
			int i = 0;
			int n;
			while (i < len)
			{
				n = @in.Read(b, off + i, len - i);
				if (n <= 0)
				{
					break;
				}
				i += n;
			}
			return i;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static int ReadPacketType(InputStream @in, byte[] buffer, int bufferIndex
			)
		{
			int n;
			if ((n = Readn(@in, buffer, bufferIndex, HeaderLength)) != HeaderLength)
			{
				if (n == -1)
				{
					return -1;
				}
				throw new IOException("unexpected EOF reading netbios session header");
			}
			int t = buffer[bufferIndex] & unchecked(0xFF);
			return t;
		}

		internal int Type;

		internal int Length;

		public virtual int WriteWireFormat(byte[] dst, int dstIndex)
		{
			Length = WriteTrailerWireFormat(dst, dstIndex + HeaderLength);
			WriteHeaderWireFormat(dst, dstIndex);
			return HeaderLength + Length;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int ReadWireFormat(InputStream @in, byte[] buffer, int bufferIndex
			)
		{
			ReadHeaderWireFormat(@in, buffer, bufferIndex);
			return HeaderLength + ReadTrailerWireFormat(@in, buffer, bufferIndex);
		}

		internal virtual int WriteHeaderWireFormat(byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = unchecked((byte)Type);
			if (Length > unchecked(0x0000FFFF))
			{
				dst[dstIndex] = unchecked(unchecked(0x01));
			}
			dstIndex++;
			WriteInt2(Length, dst, dstIndex);
			return HeaderLength;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int ReadHeaderWireFormat(InputStream @in, byte[] buffer, int bufferIndex
			)
		{
			Type = buffer[bufferIndex++] & unchecked(0xFF);
			Length = ((buffer[bufferIndex] & unchecked(0x01)) << 16) + ReadInt2(buffer
				, bufferIndex + 1);
			return HeaderLength;
		}

		internal abstract int WriteTrailerWireFormat(byte[] dst, int dstIndex);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract int ReadTrailerWireFormat(InputStream @in, byte[] buffer, int bufferIndex
			);
	}
}
