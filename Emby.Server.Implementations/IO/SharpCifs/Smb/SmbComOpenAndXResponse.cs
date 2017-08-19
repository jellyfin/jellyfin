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
	internal class SmbComOpenAndXResponse : AndXServerMessageBlock
	{
		internal int Fid;

		internal int FileAttributes;

		internal int DataSize;

		internal int GrantedAccess;

		internal int FileType;

		internal int DeviceState;

		internal int Action;

		internal int ServerFid;

		internal long LastWriteTime;

	    internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			Fid = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			FileAttributes = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			LastWriteTime = ReadUTime(buffer, bufferIndex);
			bufferIndex += 4;
			DataSize = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			GrantedAccess = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			FileType = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			DeviceState = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			Action = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			ServerFid = ReadInt4(buffer, bufferIndex);
			bufferIndex += 6;
			return bufferIndex - start;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			return "SmbComOpenAndXResponse[" + base.ToString() + ",fid=" + Fid + ",fileAttributes="
				 + FileAttributes + ",lastWriteTime=" + LastWriteTime + ",dataSize=" + DataSize 
				+ ",grantedAccess=" + GrantedAccess + ",fileType=" + FileType + ",deviceState=" 
				+ DeviceState + ",action=" + Action + ",serverFid=" + ServerFid + "]";
		}
	}
}
