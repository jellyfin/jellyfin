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
	internal class Trans2FindNext2 : SmbComTransaction
	{
		private int _sid;

		private int _informationLevel;

		private int _resumeKey;

		private int _flags;

		private string _filename;

		internal Trans2FindNext2(int sid, int resumeKey, string filename)
		{
			this._sid = sid;
			this._resumeKey = resumeKey;
			this._filename = filename;
			Command = SmbComTransaction2;
			SubCommand = Trans2FindNext2;
			_informationLevel = Smb.Trans2FindFirst2.SmbFileBothDirectoryInfo;
			_flags = unchecked(0x00);
			MaxParameterCount = 8;
			MaxDataCount = Smb.Trans2FindFirst2.ListSize;
			MaxSetupCount = 0;
		}

		internal override void Reset(int resumeKey, string lastName)
		{
			base.Reset();
			this._resumeKey = resumeKey;
			_filename = lastName;
			Flags2 = 0;
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
			WriteInt2(_sid, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(Smb.Trans2FindFirst2.ListCount, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_informationLevel, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_resumeKey, dst, dstIndex);
			dstIndex += 4;
			WriteInt2(_flags, dst, dstIndex);
			dstIndex += 2;
			dstIndex += WriteString(_filename, dst, dstIndex);
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
			return "Trans2FindNext2[" + base.ToString() + ",sid=" + _sid + ",searchCount="
				 + Smb.Trans2FindFirst2.ListSize + ",informationLevel=0x" + Hexdump.ToHexString(_informationLevel
				, 3) + ",resumeKey=0x" + Hexdump.ToHexString(_resumeKey, 4) + ",flags=0x" + Hexdump
				.ToHexString(_flags, 2) + ",filename=" + _filename + "]";
		}
	}
}
