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
	internal class SmbComDelete : ServerMessageBlock
	{
		private int _searchAttributes;

		internal SmbComDelete(string fileName)
		{
			Path = fileName;
			Command = SmbComDelete;
            _searchAttributes = SmbConstants.AttrHidden | SmbConstants.AttrHidden | SmbConstants.AttrSystem;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			WriteInt2(_searchAttributes, dst, dstIndex);
			return 2;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dst[dstIndex++] = unchecked(unchecked(0x04));
			dstIndex += WriteString(Path, dst, dstIndex);
			return dstIndex - start;
		}

		internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			return 0;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			return "SmbComDelete[" + base.ToString() + ",searchAttributes=0x" + Hexdump
				.ToHexString(_searchAttributes, 4) + ",fileName=" + Path + "]";
		}
	}
}
