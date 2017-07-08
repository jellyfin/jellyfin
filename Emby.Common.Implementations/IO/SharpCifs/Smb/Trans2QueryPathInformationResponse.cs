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
	internal class Trans2QueryPathInformationResponse : SmbComTransactionResponse
	{
		internal const int SMB_QUERY_FILE_BASIC_INFO = unchecked(0x101);

		internal const int SMB_QUERY_FILE_STANDARD_INFO = unchecked(0x102);

		internal class SmbQueryFileBasicInfo : IInfo
		{
			internal long CreateTime;

			internal long LastAccessTime;

			internal long LastWriteTime;

			internal long ChangeTime;

			internal int Attributes;

			// information levels
			public virtual int GetAttributes()
			{
				return Attributes;
			}

			public virtual long GetCreateTime()
			{
				return CreateTime;
			}

			public virtual long GetLastWriteTime()
			{
				return LastWriteTime;
			}

			public virtual long GetSize()
			{
				return 0L;
			}

			public override string ToString()
			{
				return "SmbQueryFileBasicInfo[" + "createTime=" + Extensions.CreateDate
					(CreateTime) + ",lastAccessTime=" + Extensions.CreateDate(LastAccessTime
					) + ",lastWriteTime=" + Extensions.CreateDate(LastWriteTime) + ",changeTime="
					 + Extensions.CreateDate(ChangeTime) + ",attributes=0x" + Hexdump.ToHexString
					(Attributes, 4) + "]";
			}

			internal SmbQueryFileBasicInfo(Trans2QueryPathInformationResponse enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly Trans2QueryPathInformationResponse _enclosing;
		}

		internal class SmbQueryFileStandardInfo : IInfo
		{
			internal long AllocationSize;

			internal long EndOfFile;

			internal int NumberOfLinks;

			internal bool DeletePending;

			internal bool Directory;

			public virtual int GetAttributes()
			{
				return 0;
			}

			public virtual long GetCreateTime()
			{
				return 0L;
			}

			public virtual long GetLastWriteTime()
			{
				return 0L;
			}

			public virtual long GetSize()
			{
				return EndOfFile;
			}

			public override string ToString()
			{
				return "SmbQueryInfoStandard[" + "allocationSize=" + AllocationSize
					 + ",endOfFile=" + EndOfFile + ",numberOfLinks=" + NumberOfLinks + ",deletePending="
					 + DeletePending + ",directory=" + Directory + "]";
			}

			internal SmbQueryFileStandardInfo(Trans2QueryPathInformationResponse enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly Trans2QueryPathInformationResponse _enclosing;
		}

		private int _informationLevel;

		internal IInfo Info;

		internal Trans2QueryPathInformationResponse(int informationLevel)
		{
			this._informationLevel = informationLevel;
			SubCommand = Smb.SmbComTransaction.Trans2QueryPathInformation;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
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

		internal override int ReadSetupWireFormat(byte[] buffer, int bufferIndex, int len
			)
		{
			return 0;
		}

		internal override int ReadParametersWireFormat(byte[] buffer, int bufferIndex, int
			 len)
		{
			// observed two zero bytes here with at least win98
			return 2;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			switch (_informationLevel)
			{
				case SMB_QUERY_FILE_BASIC_INFO:
				{
					return ReadSmbQueryFileBasicInfoWireFormat(buffer, bufferIndex);
				}

				case SMB_QUERY_FILE_STANDARD_INFO:
				{
					return ReadSmbQueryFileStandardInfoWireFormat(buffer, bufferIndex);
				}

				default:
				{
					return 0;
				}
			}
		}

		internal virtual int ReadSmbQueryFileStandardInfoWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			SmbQueryFileStandardInfo info = new SmbQueryFileStandardInfo
				(this);
			info.AllocationSize = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			info.EndOfFile = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			info.NumberOfLinks = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.DeletePending = (buffer[bufferIndex++] & unchecked(0xFF)) > 0;
			info.Directory = (buffer[bufferIndex++] & unchecked(0xFF)) > 0;
			this.Info = info;
			return bufferIndex - start;
		}

		internal virtual int ReadSmbQueryFileBasicInfoWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			SmbQueryFileBasicInfo info = new SmbQueryFileBasicInfo
				(this);
			info.CreateTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			info.LastAccessTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			info.LastWriteTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			info.ChangeTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			info.Attributes = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			this.Info = info;
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "Trans2QueryPathInformationResponse[" + base.ToString() + "]";
		}
	}
}
