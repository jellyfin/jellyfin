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
using System.IO;

namespace SharpCifs.Util
{
	public class Hexdump
	{
		private static readonly string Nl = @"\r\n"; //Runtime.GetProperty("line.separator");

		private static readonly int NlLength = Nl.Length;

		private static readonly char[] SpaceChars = { ' ', ' ', ' ', ' ', ' '
			, ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' '
			, ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' '
			, ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

		public static readonly char[] HexDigits = { '0', '1', '2', '3', '4', 
			'5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };


	    private static bool IsIsoControl(char c)
	    {
	        return (c >= '\u0000' && c <= '\u001F') || (c >= '\u007F' && c <= '\u009F');
	    }
        
        /// <summary>
		/// Generate "hexdump" output of the buffer at src like the following:
		/// <p><blockquote><pre>
		/// 00000: 04 d2 29 00 00 01 00 00 00 00 00 01 20 45 47 46  |..).........
		/// </summary>
		/// <remarks>
		/// Generate "hexdump" output of the buffer at src like the following:
		/// <p><blockquote><pre>
		/// 00000: 04 d2 29 00 00 01 00 00 00 00 00 01 20 45 47 46  |..)......... EGF|
		/// 00010: 43 45 46 45 45 43 41 43 41 43 41 43 41 43 41 43  |CEFEECACACACACAC|
		/// 00020: 41 43 41 43 41 43 41 43 41 43 41 41 44 00 00 20  |ACACACACACAAD.. |
		/// 00030: 00 01 c0 0c 00 20 00 01 00 00 00 00 00 06 20 00  |..... ........ .|
		/// 00040: ac 22 22 e1                                      |."".            |
		/// </blockquote></pre>
		/// </remarks>
		public static void ToHexdump(TextWriter ps, byte[] src, int srcIndex, int length)
		{
			if (length == 0)
			{
				return;
			}
			int s = length % 16;
			int r = (s == 0) ? length / 16 : length / 16 + 1;
			char[] c = new char[r * (74 + NlLength)];
			char[] d = new char[16];
			int i;
			int si = 0;
			int ci = 0;
			do
			{
				ToHexChars(si, c, ci, 5);
				ci += 5;
				c[ci++] = ':';
				do
				{
					if (si == length)
					{
						int n = 16 - s;
						Array.Copy(SpaceChars, 0, c, ci, n * 3);
						ci += n * 3;
						Array.Copy(SpaceChars, 0, d, s, n);
						break;
					}
					c[ci++] = ' ';
					i = src[srcIndex + si] & 0xFF;
					ToHexChars(i, c, ci, 2);
					ci += 2;
					if (i < 0 || IsIsoControl((char)i))
					{
						d[si % 16] = '.';
					}
					else
					{
						d[si % 16] = (char)i;
					}
				}
				while ((++si % 16) != 0);
				c[ci++] = ' ';
				c[ci++] = ' ';
				c[ci++] = '|';
				Array.Copy(d, 0, c, ci, 16);
				ci += 16;
				c[ci++] = '|';
				//Sharpen.Runtime.GetCharsForString(NL, 0, NL_LENGTH, c, ci);
			    c = Nl.ToCharArray(0, NlLength);
				ci += NlLength;
			}
			while (si < length);
			ps.WriteLine(c);
		}

		/// <summary>
		/// This is an alternative to the <code>java.lang.Integer.toHexString</cod>
		/// method.
		/// </summary>
		/// <remarks>
		/// This is an alternative to the <code>java.lang.Integer.toHexString</cod>
		/// method. It is an efficient relative that also will pad the left side so
		/// that the result is <code>size</code> digits.
		/// </remarks>
		public static string ToHexString(int val, int size)
		{
			char[] c = new char[size];
			ToHexChars(val, c, 0, size);
			return new string(c);
		}

		public static string ToHexString(long val, int size)
		{
			char[] c = new char[size];
			ToHexChars(val, c, 0, size);
			return new string(c);
		}

		public static string ToHexString(byte[] src, int srcIndex, int size)
		{
			char[] c = new char[size];
			size = (size % 2 == 0) ? size / 2 : size / 2 + 1;
			for (int i = 0, j = 0; i < size; i++)
			{
				c[j++] = HexDigits[(src[i] >> 4) & 0x0F];
				if (j == c.Length)
				{
					break;
				}
				c[j++] = HexDigits[src[i] & 0x0F];
			}
			return new string(c);
		}

		/// <summary>
		/// This is the same as
		/// <see cref="ToHexString(int, int)">ToHexString(int, int)</see>
		/// but provides a more practical form when trying to avoid
		/// <see cref="string">string</see>
		/// concatenation and
		/// <see cref="System.Text.StringBuilder">System.Text.StringBuilder</see>
		/// .
		/// </summary>
		public static void ToHexChars(int val, char[] dst, int dstIndex, int size)
		{
			while (size > 0)
			{
				int i = dstIndex + size - 1;
				if (i < dst.Length)
				{
					dst[i] = HexDigits[val & 0x000F];
				}
				if (val != 0)
				{
					val = (int)(((uint)val) >> 4);
				}
				size--;
			}
		}

		public static void ToHexChars(long val, char[] dst, int dstIndex, int size)
		{
			while (size > 0)
			{
				dst[dstIndex + size - 1] = HexDigits[(int)(val & 0x000FL)];
				if (val != 0)
				{
					val = (long)(((ulong)val) >> 4);
				}
				size--;
			}
		}
	}
}
