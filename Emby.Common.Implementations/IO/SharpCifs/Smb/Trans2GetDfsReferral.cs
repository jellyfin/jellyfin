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
namespace SharpCifs.Smb
{
	internal class Trans2GetDfsReferral : SmbComTransaction
	{
		private int _maxReferralLevel = 3;

		internal Trans2GetDfsReferral(string filename)
		{
			Path = filename;
			Command = SmbComTransaction2;
			SubCommand = Trans2GetDfsReferral;
			TotalDataCount = 0;
			MaxParameterCount = 0;
			MaxDataCount = 4096;
			MaxSetupCount = unchecked(unchecked(0x00));
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = SubCommand;
			dst[dstIndex++] = unchecked(unchecked(0x00));
			return 2;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(_maxReferralLevel, dst, dstIndex);
			dstIndex += 2;
			dstIndex += WriteString(Path, dst, dstIndex);
			return dstIndex - start;
		}

		internal override int WriteDataWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadSetupWireFormat(byte[] buffer, int bufferIndex, int len
			)
		{
			return 0;
		}

		internal override int ReadParametersWireFormat(byte[] buffer, int bufferIndex, int
			 len)
		{
			return 0;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			return 0;
		}

		public override string ToString()
		{
			return "Trans2GetDfsReferral[" + base.ToString() + ",maxReferralLevel=0x"
				 + _maxReferralLevel + ",filename=" + Path + "]";
		}
	}
}
