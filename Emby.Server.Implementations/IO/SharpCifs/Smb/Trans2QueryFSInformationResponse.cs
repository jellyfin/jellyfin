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
	internal class Trans2QueryFsInformationResponse : SmbComTransactionResponse
	{
		internal const int SMB_INFO_ALLOCATION = 1;

		internal const int SmbQueryFsSizeInfo = unchecked(0x103);

		internal const int SmbFsFullSizeInformation = 1007;

		internal class SmbInfoAllocation : IAllocInfo
		{
			internal long Alloc;

			internal long Free;

			internal int SectPerAlloc;

			internal int BytesPerSect;

			// information levels
			// Also handles SmbQueryFSSizeInfo
			public virtual long GetCapacity()
			{
				return Alloc * SectPerAlloc * BytesPerSect;
			}

			public virtual long GetFree()
			{
				return Free * SectPerAlloc * BytesPerSect;
			}

			public override string ToString()
			{
				return "SmbInfoAllocation[" + "alloc=" + Alloc + ",free=" + Free + ",sectPerAlloc=" + SectPerAlloc + ",bytesPerSect=" + BytesPerSect
					 + "]";
			}

			internal SmbInfoAllocation(Trans2QueryFsInformationResponse enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly Trans2QueryFsInformationResponse _enclosing;
		}

		private int _informationLevel;

		internal IAllocInfo Info;

		internal Trans2QueryFsInformationResponse(int informationLevel)
		{
			this._informationLevel = informationLevel;
			Command = SmbComTransaction2;
			SubCommand = Smb.SmbComTransaction.Trans2QueryFsInformation;
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
			return 0;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			switch (_informationLevel)
			{
				case SMB_INFO_ALLOCATION:
				{
					return ReadSmbInfoAllocationWireFormat(buffer, bufferIndex);
				}

				case SmbQueryFsSizeInfo:
				{
					return ReadSmbQueryFsSizeInfoWireFormat(buffer, bufferIndex);
				}

				case SmbFsFullSizeInformation:
				{
					return ReadFsFullSizeInformationWireFormat(buffer, bufferIndex);
				}

				default:
				{
					return 0;
				}
			}
		}

		internal virtual int ReadSmbInfoAllocationWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			SmbInfoAllocation info = new SmbInfoAllocation
				(this);
			bufferIndex += 4;
			// skip idFileSystem
			info.SectPerAlloc = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.Alloc = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.Free = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.BytesPerSect = ReadInt2(buffer, bufferIndex);
			bufferIndex += 4;
			this.Info = info;
			return bufferIndex - start;
		}

		internal virtual int ReadSmbQueryFsSizeInfoWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			SmbInfoAllocation info = new SmbInfoAllocation
				(this);
			info.Alloc = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			info.Free = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			info.SectPerAlloc = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.BytesPerSect = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			this.Info = info;
			return bufferIndex - start;
		}

		internal virtual int ReadFsFullSizeInformationWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			SmbInfoAllocation info = new SmbInfoAllocation
				(this);
			// Read total allocation units.
			info.Alloc = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			// read caller available allocation units 
			info.Free = ReadInt8(buffer, bufferIndex);
			bufferIndex += 8;
			// skip actual free units
			bufferIndex += 8;
			info.SectPerAlloc = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			info.BytesPerSect = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			this.Info = info;
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "Trans2QueryFSInformationResponse[" + base.ToString() + "]";
		}
	}
}
