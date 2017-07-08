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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	public class Name
	{
		private const int TypeOffset = 31;

		private const int ScopeOffset = 33;

		private static readonly string DefaultScope = Config.GetProperty("jcifs.netbios.scope"
			);

		internal static readonly string OemEncoding = Config.GetProperty("jcifs.encoding"
			, Runtime.GetProperty("file.encoding"));

		public string name;

		public string Scope;

		public int HexCode;

		internal int SrcHashCode;

		public Name()
		{
		}

		public Name(string name, int hexCode, string scope)
		{
			if (name.Length > 15)
			{
				name = Runtime.Substring(name, 0, 15);
			}
			this.name = name.ToUpper();
			this.HexCode = hexCode;
			this.Scope = !string.IsNullOrEmpty(scope) ? scope : DefaultScope;
			SrcHashCode = 0;
		}

		internal virtual int WriteWireFormat(byte[] dst, int dstIndex)
		{
			// write 0x20 in first byte
			dst[dstIndex] = unchecked(0x20);
			// write name
			try
			{
				byte[] tmp = Runtime.GetBytesForString(name, OemEncoding
					);
				int i;
				for (i = 0; i < tmp.Length; i++)
				{
					dst[dstIndex + (2 * i + 1)] = unchecked((byte)(((tmp[i] & unchecked(0xF0))
						 >> 4) + unchecked(0x41)));
					dst[dstIndex + (2 * i + 2)] = unchecked((byte)((tmp[i] & unchecked(0x0F)) 
						+ unchecked(0x41)));
				}
				for (; i < 15; i++)
				{
					dst[dstIndex + (2 * i + 1)] = unchecked(unchecked(0x43));
					dst[dstIndex + (2 * i + 2)] = unchecked(unchecked(0x41));
				}
				dst[dstIndex + TypeOffset] = unchecked((byte)(((HexCode & unchecked(0xF0)
					) >> 4) + unchecked(0x41)));
				dst[dstIndex + TypeOffset + 1] = unchecked((byte)((HexCode & unchecked(0x0F)) + unchecked(0x41)));
			}
			catch (UnsupportedEncodingException)
			{
			}
			return ScopeOffset + WriteScopeWireFormat(dst, dstIndex + ScopeOffset);
		}

		internal virtual int ReadWireFormat(byte[] src, int srcIndex)
		{
			byte[] tmp = new byte[ScopeOffset];
			int length = 15;
			for (int i = 0; i < 15; i++)
			{
				tmp[i] = unchecked((byte)(((src[srcIndex + (2 * i + 1)] & unchecked(0xFF))
					 - unchecked(0x41)) << 4));
				tmp[i] |= unchecked((byte)(((src[srcIndex + (2 * i + 2)] & unchecked(0xFF)
					) - unchecked(0x41)) & unchecked(0x0F)));
				if (tmp[i] != unchecked((byte)' '))
				{
					length = i + 1;
				}
			}
			try
			{
				name = Runtime.GetStringForBytes(tmp, 0, length, OemEncoding
					);
			}
			catch (UnsupportedEncodingException)
			{
			}
			HexCode = ((src[srcIndex + TypeOffset] & unchecked(0xFF)) - unchecked(0x41)) << 4;
			HexCode |= ((src[srcIndex + TypeOffset + 1] & unchecked(0xFF)) - unchecked(
				0x41)) & unchecked(0x0F);
			return ScopeOffset + ReadScopeWireFormat(src, srcIndex + ScopeOffset);
		}

        internal int ReadWireFormatDos(byte[] src, int srcIndex)
        {

            int length = 15;
            byte[] tmp = new byte[length];

            Array.Copy(src, srcIndex, tmp, 0, length);

            try
            {
                name = Runtime.GetStringForBytes(tmp, 0, length).Trim();
            }
            catch (Exception ex)
            {

            }

            HexCode = src[srcIndex + length];

            return length + 1;
        }


		internal virtual int WriteScopeWireFormat(byte[] dst, int dstIndex)
		{
			if (Scope == null)
			{
				dst[dstIndex] = unchecked(unchecked(0x00));
				return 1;
			}
			// copy new scope in
			dst[dstIndex++] = unchecked((byte)('.'));
			try
			{
				Array.Copy(Runtime.GetBytesForString(Scope, OemEncoding
					), 0, dst, dstIndex, Scope.Length);
			}
			catch (UnsupportedEncodingException)
			{
			}
			dstIndex += Scope.Length;
			dst[dstIndex++] = unchecked(unchecked(0x00));
			// now go over scope backwards converting '.' to label length
			int i = dstIndex - 2;
			int e = i - Scope.Length;
			int c = 0;
			do
			{
				if (dst[i] == '.')
				{
					dst[i] = unchecked((byte)c);
					c = 0;
				}
				else
				{
					c++;
				}
			}
			while (i-- > e);
			return Scope.Length + 2;
		}

		internal virtual int ReadScopeWireFormat(byte[] src, int srcIndex)
		{
			int start = srcIndex;
			int n;
			StringBuilder sb;
			if ((n = src[srcIndex++] & unchecked(0xFF)) == 0)
			{
				Scope = null;
				return 1;
			}
			try
			{
				sb = new StringBuilder(Runtime.GetStringForBytes(src, srcIndex, n, OemEncoding));
				srcIndex += n;
				while ((n = src[srcIndex++] & unchecked(0xFF)) != 0)
				{
					sb.Append('.').Append(Runtime.GetStringForBytes(src, srcIndex, n, OemEncoding));
					srcIndex += n;
				}
				Scope = sb.ToString();
			}
			catch (UnsupportedEncodingException)
			{
			}
			return srcIndex - start;
		}

		public override int GetHashCode()
		{
			int result;
			result = name.GetHashCode();
			result += 65599 * HexCode;
			result += 65599 * SrcHashCode;
			if (Scope != null && Scope.Length != 0)
			{
				result += Scope.GetHashCode();
			}
			return result;
		}

		public override bool Equals(object obj)
		{
			Name n;
			if (!(obj is Name))
			{
				return false;
			}
			n = (Name)obj;
			if (Scope == null && n.Scope == null)
			{
				return name.Equals(n.name) && HexCode == n.HexCode;
			}
			return name.Equals(n.name) && HexCode == n.HexCode && Scope.Equals(n.Scope);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

		    //return "";

			string n = name;
			// fix MSBROWSE name
			if (n == null)
			{
				n = "null";
			}
			else
			{
				if (n[0] == unchecked(0x01))
				{
					char[] c = n.ToCharArray();
					c[0] = '.';
					c[1] = '.';
					c[14] = '.';
					n = new string(c);
				}
			}
			sb.Append(n).Append("<").Append(Hexdump.ToHexString(HexCode, 2)).Append(">");
			if (Scope != null)
			{
				sb.Append(".").Append(Scope);
			}
			return sb.ToString();
		}
	}
}
