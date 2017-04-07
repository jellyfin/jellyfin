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
namespace SharpCifs.Util
{
	public class Rc4
	{
		internal byte[] S;

		internal int I;

		internal int J;

		public Rc4()
		{
		}

		public Rc4(byte[] key)
		{
			Init(key, 0, key.Length);
		}

		public virtual void Init(byte[] key, int ki, int klen)
		{
			S = new byte[256];
			for (I = 0; I < 256; I++)
			{
				S[I] = unchecked((byte)I);
			}
			for (I = J = 0; I < 256; I++)
			{
				J = (J + key[ki + I % klen] + S[I]) & unchecked(0xff);
				byte t = S[I];
				S[I] = S[J];
				S[J] = t;
			}
			I = J = 0;
		}

		public virtual void Update(byte[] src, int soff, int slen, byte[] dst, int doff)
		{
			int slim;
			slim = soff + slen;
			while (soff < slim)
			{
				I = (I + 1) & unchecked(0xff);
				J = (J + S[I]) & unchecked(0xff);
				byte t = S[I];
				S[I] = S[J];
				S[J] = t;
				dst[doff++] = unchecked((byte)(src[soff++] ^ S[(S[I] + S[J]) & unchecked(0xff)]));
			}
		}
	}
}
