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
namespace SharpCifs.Dcerpc
{
	public class UnicodeString : Rpc.Unicode_string
	{
		internal bool Zterm;

		public UnicodeString(bool zterm)
		{
			this.Zterm = zterm;
		}

		public UnicodeString(Rpc.Unicode_string rus, bool zterm)
		{
			Length = rus.Length;
			MaximumLength = rus.MaximumLength;
			Buffer = rus.Buffer;
			this.Zterm = zterm;
		}

		public UnicodeString(string str, bool zterm)
		{
			this.Zterm = zterm;
			int len = str.Length;
			int zt = zterm ? 1 : 0;
			Length = MaximumLength = (short)((len + zt) * 2);
			Buffer = new short[len + zt];
			int i;
			for (i = 0; i < len; i++)
			{
				Buffer[i] = (short)str[i];
			}
			if (zterm)
			{
				Buffer[i] = 0;
			}
		}

		public override string ToString()
		{
			int len = Length / 2 - (Zterm ? 1 : 0);
			char[] ca = new char[len];
			for (int i = 0; i < len; i++)
			{
				ca[i] = (char)Buffer[i];
			}
			return new string(ca, 0, len);
		}
	}
}
