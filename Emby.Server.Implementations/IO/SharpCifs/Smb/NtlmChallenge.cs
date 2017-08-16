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
using SharpCifs.Util;

namespace SharpCifs.Smb
{
	
	public sealed class NtlmChallenge
	{
		public byte[] Challenge;

		public UniAddress Dc;

		internal NtlmChallenge(byte[] challenge, UniAddress dc)
		{
			this.Challenge = challenge;
			this.Dc = dc;
		}

		public override string ToString()
		{
			return "NtlmChallenge[challenge=0x" + Hexdump.ToHexString(Challenge, 0, Challenge
				.Length * 2) + ",dc=" + Dc + "]";
		}
	}
}
