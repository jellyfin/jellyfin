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
using System;
using System.Text;

namespace SharpCifs.Util
{
	public class Base64
	{
		private static readonly string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

		/// <summary>Base-64 encodes the supplied block of data.</summary>
		/// <remarks>
		/// Base-64 encodes the supplied block of data.  Line wrapping is not
		/// applied on output.
		/// </remarks>
		/// <param name="bytes">The block of data that is to be Base-64 encoded.</param>
		/// <returns>A <code>String</code> containing the encoded data.</returns>
		public static string Encode(byte[] bytes)
		{
			int length = bytes.Length;
			if (length == 0)
			{
				return string.Empty;
			}
			StringBuilder buffer = new StringBuilder((int)Math.Ceiling(length / 3d) * 4);
			int remainder = length % 3;
			length -= remainder;
			int block;
			int i = 0;
			while (i < length)
			{
				block = ((bytes[i++] & unchecked(0xff)) << 16) | ((bytes[i++] & unchecked(
					0xff)) << 8) | (bytes[i++] & unchecked(0xff));
				buffer.Append(Alphabet[(int)(((uint)block) >> 18)]);
				buffer.Append(Alphabet[((int)(((uint)block) >> 12)) & unchecked(0x3f)]);
				buffer.Append(Alphabet[((int)(((uint)block) >> 6)) & unchecked(0x3f)]);
				buffer.Append(Alphabet[block & unchecked(0x3f)]);
			}
			if (remainder == 0)
			{
				return buffer.ToString();
			}
			if (remainder == 1)
			{
				block = (bytes[i] & unchecked(0xff)) << 4;
				buffer.Append(Alphabet[(int)(((uint)block) >> 6)]);
				buffer.Append(Alphabet[block & unchecked(0x3f)]);
				buffer.Append("==");
				return buffer.ToString();
			}
			block = (((bytes[i++] & unchecked(0xff)) << 8) | ((bytes[i]) & unchecked(0xff))) << 2;
			buffer.Append(Alphabet[(int)(((uint)block) >> 12)]);
			buffer.Append(Alphabet[((int)(((uint)block) >> 6)) & unchecked(0x3f)]);
			buffer.Append(Alphabet[block & unchecked(0x3f)]);
			buffer.Append("=");
			return buffer.ToString();
		}

		/// <summary>Decodes the supplied Base-64 encoded string.</summary>
		/// <remarks>Decodes the supplied Base-64 encoded string.</remarks>
		/// <param name="string">The Base-64 encoded string that is to be decoded.</param>
		/// <returns>A <code>byte[]</code> containing the decoded data block.</returns>
		public static byte[] Decode(string @string)
		{
			int length = @string.Length;
			if (length == 0)
			{
				return new byte[0];
			}
			int pad = (@string[length - 2] == '=') ? 2 : (@string[length - 1] == '=') ? 1 : 0;
			int size = length * 3 / 4 - pad;
			byte[] buffer = new byte[size];
			int block;
			int i = 0;
			int index = 0;
			while (i < length)
			{
				block = (Alphabet.IndexOf(@string[i++]) & unchecked(0xff)) << 18 | (Alphabet
					.IndexOf(@string[i++]) & unchecked(0xff)) << 12 | (Alphabet.IndexOf(@string
					[i++]) & unchecked(0xff)) << 6 | (Alphabet.IndexOf(@string[i++]) & unchecked(
					0xff));
				buffer[index++] = unchecked((byte)((int)(((uint)block) >> 16)));
				if (index < size)
				{
					buffer[index++] = unchecked((byte)(((int)(((uint)block) >> 8)) & unchecked(0xff)));
				}
				if (index < size)
				{
					buffer[index++] = unchecked((byte)(block & unchecked(0xff)));
				}
			}
			return buffer;
		}
	}
}
