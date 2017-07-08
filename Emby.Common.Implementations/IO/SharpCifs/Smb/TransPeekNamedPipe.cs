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
	internal class TransPeekNamedPipe : SmbComTransaction
	{
		private int _fid;

		internal TransPeekNamedPipe(string pipeName, int fid)
		{
			Name = pipeName;
			this._fid = fid;
			Command = SmbComTransaction;
			SubCommand = TransPeekNamedPipe;
			Timeout = unchecked((int)(0xFFFFFFFF));
			MaxParameterCount = 6;
			MaxDataCount = 1;
			MaxSetupCount = unchecked(unchecked(0x00));
			SetupCount = 2;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = SubCommand;
			dst[dstIndex++] = unchecked(unchecked(0x00));
			// this says "Transaction priority" in netmon
			WriteInt2(_fid, dst, dstIndex);
			return 4;
		}

		internal override int ReadSetupWireFormat(byte[] buffer, int bufferIndex, int len
			)
		{
			return 0;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteDataWireFormat(byte[] dst, int dstIndex)
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
			return "TransPeekNamedPipe[" + base.ToString() + ",pipeName=" + Name +
				 "]";
		}
	}
}
