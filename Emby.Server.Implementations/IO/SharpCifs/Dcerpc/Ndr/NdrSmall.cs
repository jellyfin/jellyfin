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
namespace SharpCifs.Dcerpc.Ndr
{
	public class NdrSmall : NdrObject
	{
		public int Value;

		public NdrSmall(int value)
		{
			this.Value = value & unchecked(0xFF);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Encode(NdrBuffer dst)
		{
			dst.Enc_ndr_small(Value);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Decode(NdrBuffer src)
		{
			Value = src.Dec_ndr_small();
		}
	}
}
