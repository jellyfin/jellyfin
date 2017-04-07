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

using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class Trans2FindFirst2Response : SmbComTransactionResponse
	{
		internal const int SmbInfoStandard = 1;

		internal const int SmbInfoQueryEaSize = 2;

		internal const int SmbInfoQueryEasFromList = 3;

		internal const int SmbFindFileDirectoryInfo = unchecked(0x101);

		internal const int SmbFindFileFullDirectoryInfo = unchecked(0x102);

		internal const int SmbFileNamesInfo = unchecked(0x103);

		internal const int SmbFileBothDirectoryInfo = unchecked(0x104);

		internal class SmbFindFileBothDirectoryInfo : IFileEntry
		{
			internal int NextEntryOffset;

			internal int FileIndex;

			internal long CreationTime;

			internal long LastAccessTime;

			internal long LastWriteTime;

			internal long ChangeTime;

			internal long EndOfFile;

			internal long AllocationSize;

			internal int ExtFileAttributes;

			internal int FileNameLength;

			internal int EaSize;

			internal int ShortNameLength;

			internal string ShortName;

			internal string Filename;

			// information levels
			public virtual string GetName()
			{
				return Filename;
			}

			public virtual int GetType()
			{
				return SmbFile.TypeFilesystem;
			}

			public virtual int GetAttributes()
			{
				return ExtFileAttributes;
			}

			public virtual long CreateTime()
			{
				return CreationTime;
			}

			public virtual long LastModified()
			{
				return LastWriteTime;
			}

			public virtual long Length()
			{
				return EndOfFile;
			}

			public override string ToString()
			{
				return "SmbFindFileBothDirectoryInfo[" + "nextEntryOffset=" + NextEntryOffset
					 + ",fileIndex=" + FileIndex + ",creationTime=" + Extensions.CreateDate
					(CreationTime) + ",lastAccessTime=" + Extensions.CreateDate(LastAccessTime
					) + ",lastWriteTime=" + Extensions.CreateDate(LastWriteTime) + ",changeTime="
					 + Extensions.CreateDate(ChangeTime) + ",endOfFile=" + EndOfFile
					 + ",allocationSize=" + AllocationSize + ",extFileAttributes=" + ExtFileAttributes
					 + ",fileNameLength=" + FileNameLength + ",eaSize=" + EaSize + ",shortNameLength="
					 + ShortNameLength + ",shortName=" + ShortName + ",filename=" + Filename
					 + "]";
			}

			internal SmbFindFileBothDirectoryInfo(Trans2FindFirst2Response enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly Trans2FindFirst2Response _enclosing;
		}

		internal int Sid;

		internal bool IsEndOfSearch;

		internal int EaErrorOffset;

		internal int LastNameOffset;

		internal int LastNameBufferIndex;

		internal string LastName;

		internal int ResumeKey;

		public Trans2FindFirst2Response()
		{
			Command = SmbComTransaction2;
			SubCommand = Smb.SmbComTransaction.Trans2FindFirst2;
		}

		internal virtual string ReadString(byte[] src, int srcIndex, int len)
		{
			string str = null;
			try
			{
				if (UseUnicode)
				{
					// should Unicode alignment be corrected for here?
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.UniEncoding);
				}
				else
				{
					if (len > 0 && src[srcIndex + len - 1] == '\0')
					{
						len--;
					}
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.OemEncoding
						);
				}
			}
			catch (UnsupportedEncodingException uee)
			{
				if (Log.Level > 1)
				{
					Runtime.PrintStackTrace(uee, Log);
				}
			}
			return str;
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
			int start = bufferIndex;
			if (SubCommand == Smb.SmbComTransaction.Trans2FindFirst2)
			{
				Sid = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
			}
			NumEntries = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			IsEndOfSearch = (buffer[bufferIndex] & unchecked(0x01)) == unchecked(0x01) ? true : false;
			bufferIndex += 2;
			EaErrorOffset = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			LastNameOffset = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			return bufferIndex - start;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			int start = bufferIndex;
			SmbFindFileBothDirectoryInfo e;
			LastNameBufferIndex = bufferIndex + LastNameOffset;
			Results = new SmbFindFileBothDirectoryInfo[NumEntries];
			for (int i = 0; i < NumEntries; i++)
			{
				Results[i] = e = new SmbFindFileBothDirectoryInfo(this);
				e.NextEntryOffset = ReadInt4(buffer, bufferIndex);
				e.FileIndex = ReadInt4(buffer, bufferIndex + 4);
				e.CreationTime = ReadTime(buffer, bufferIndex + 8);
				//      e.lastAccessTime = readTime( buffer, bufferIndex + 16 );
				e.LastWriteTime = ReadTime(buffer, bufferIndex + 24);
				//      e.changeTime = readTime( buffer, bufferIndex + 32 );
				e.EndOfFile = ReadInt8(buffer, bufferIndex + 40);
				//      e.allocationSize = readInt8( buffer, bufferIndex + 48 );
				e.ExtFileAttributes = ReadInt4(buffer, bufferIndex + 56);
				e.FileNameLength = ReadInt4(buffer, bufferIndex + 60);
				//      e.eaSize = readInt4( buffer, bufferIndex + 64 );
				//      e.shortNameLength = buffer[bufferIndex + 68] & 0xFF;
				//      e.shortName = readString( buffer, bufferIndex + 70, e.shortNameLength );
				e.Filename = ReadString(buffer, bufferIndex + 94, e.FileNameLength);
				if (LastNameBufferIndex >= bufferIndex && (e.NextEntryOffset == 0 || LastNameBufferIndex
					 < (bufferIndex + e.NextEntryOffset)))
				{
					LastName = e.Filename;
					ResumeKey = e.FileIndex;
				}
				bufferIndex += e.NextEntryOffset;
			}
			//return bufferIndex - start;
			return DataCount;
		}

		public override string ToString()
		{
			string c;
			if (SubCommand == Smb.SmbComTransaction.Trans2FindFirst2)
			{
				c = "Trans2FindFirst2Response[";
			}
			else
			{
				c = "Trans2FindNext2Response[";
			}
			return c + base.ToString() + ",sid=" + Sid + ",searchCount=" + NumEntries
				 + ",isEndOfSearch=" + IsEndOfSearch + ",eaErrorOffset=" + EaErrorOffset + ",lastNameOffset="
				 + LastNameOffset + ",lastName=" + LastName + "]";
		}
	}
}
