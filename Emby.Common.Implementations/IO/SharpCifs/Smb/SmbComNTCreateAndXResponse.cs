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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComNtCreateAndXResponse : AndXServerMessageBlock
	{
		internal const int ExclusiveOplockGranted = 1;

		internal const int BatchOplockGranted = 2;

		internal const int LevelIiOplockGranted = 3;

		internal byte OplockLevel;

		internal int Fid;

		internal int CreateAction;

		internal int ExtFileAttributes;

		internal int FileType;

		internal int DeviceState;

		internal long CreationTime;

		internal long LastAccessTime;

		internal long LastWriteTime;

		internal long ChangeTime;

		internal long AllocationSize;

		internal long EndOfFile;

		internal bool Directory;

		internal bool IsExtended;

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
			OplockLevel = buffer[bufferIndex++];
			Fid = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			CreateAction = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			CreationTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			LastAccessTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			LastWriteTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			ChangeTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			ExtFileAttributes = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			AllocationSize = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			EndOfFile = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			FileType = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			DeviceState = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			Directory = (buffer[bufferIndex++] & unchecked(0xFF)) > 0;
			return bufferIndex - start;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			return "SmbComNTCreateAndXResponse[" + base.ToString() + ",oplockLevel="
				 + OplockLevel + ",fid=" + Fid + ",createAction=0x" + Hexdump.ToHexString(CreateAction
				, 4) + ",creationTime=" + Extensions.CreateDate(CreationTime) + ",lastAccessTime="
				 + Extensions.CreateDate(LastAccessTime) + ",lastWriteTime=" + Extensions.CreateDate
				(LastWriteTime) + ",changeTime=" + Extensions.CreateDate(ChangeTime) + ",extFileAttributes=0x"
				 + Hexdump.ToHexString(ExtFileAttributes, 4) + ",allocationSize=" + AllocationSize
				 + ",endOfFile=" + EndOfFile + ",fileType=" + FileType + ",deviceState=" + DeviceState
				 + ",directory=" + Directory + "]";
		}
	}
}
