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
	internal class Trans2SetFileInformation : SmbComTransaction
	{
		internal const int SmbFileBasicInfo = unchecked(0x101);

		private int _fid;

		private int _attributes;

		private long _createTime;

		private long _lastWriteTime;

		internal Trans2SetFileInformation(int fid, int attributes, long createTime, long 
			lastWriteTime)
		{
			this._fid = fid;
			this._attributes = attributes;
			this._createTime = createTime;
			this._lastWriteTime = lastWriteTime;
			Command = SmbComTransaction2;
			SubCommand = Trans2SetFileInformation;
			MaxParameterCount = 6;
			MaxDataCount = 0;
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
			WriteInt2(_fid, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(SmbFileBasicInfo, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(0, dst, dstIndex);
			dstIndex += 2;
			return dstIndex - start;
		}

		internal override int WriteDataWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteTime(_createTime, dst, dstIndex);
			dstIndex += 8;
			WriteInt8(0L, dst, dstIndex);
			dstIndex += 8;
			WriteTime(_lastWriteTime, dst, dstIndex);
			dstIndex += 8;
			WriteInt8(0L, dst, dstIndex);
			dstIndex += 8;
			WriteInt2(unchecked(0x80) | _attributes, dst, dstIndex);
			dstIndex += 2;
			WriteInt8(0L, dst, dstIndex);
			dstIndex += 6;
			return dstIndex - start;
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
			return "Trans2SetFileInformation[" + base.ToString() + ",fid=" + _fid +
				 "]";
		}
	}
}
