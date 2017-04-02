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
	internal class SmbComOpenAndX : AndXServerMessageBlock
	{
		private const int FlagsReturnAdditionalInfo = 0x01;

		private const int FlagsRequestOplock = 0x02;

		private const int FlagsRequestBatchOplock = 0x04;

	    private const int SharingCompatibility = 0x00;

	    private const int SharingDenyReadWriteExecute = 0x10;

		private const int SharingDenyWrite = 0x20;

		private const int SharingDenyReadExecute = 0x30;

		private const int SharingDenyNone = 0x40;

		private const int DoNotCache = 0x1000;

		private const int WriteThrough = 0x4000;

		private const int OpenFnCreate = 0x10;

		private const int OpenFnFailIfExists = 0x00;

		private const int OpenFnOpen = 0x01;

		private const int OpenFnTrunc = 0x02;

		private static readonly int BatchLimit = Config.GetInt("jcifs.smb.client.OpenAndX.ReadAndX"
			, 1);

		internal int flags;

		internal int DesiredAccess;

		internal int SearchAttributes;

		internal int FileAttributes;

		internal int CreationTime;

		internal int OpenFunction;

		internal int AllocationSize;

		internal SmbComOpenAndX(string fileName, int access, int flags, ServerMessageBlock
			 andx) : base(andx)
		{
			// flags (not the same as flags constructor argument)
			// Access Mode Encoding for desiredAccess
			// bit 12
			// bit 14
			// flags is NOT the same as flags member
			Path = fileName;
			Command = SmbComOpenAndx;
			DesiredAccess = access & 0x3;
			if (DesiredAccess == 0x3)
			{
				DesiredAccess = 0x2;
			}
			DesiredAccess |= SharingDenyNone;
			DesiredAccess &= ~0x1;
			// Win98 doesn't like GENERIC_READ ?! -- get Access Denied.
			// searchAttributes
            SearchAttributes = SmbConstants.AttrDirectory | SmbConstants.AttrHidden | SmbConstants.AttrSystem;
			// fileAttributes
			FileAttributes = 0;
			// openFunction
			if ((flags & SmbFile.OTrunc) == SmbFile.OTrunc)
			{
				// truncate the file
				if ((flags & SmbFile.OCreat) == SmbFile.OCreat)
				{
					// create it if necessary
					OpenFunction = OpenFnTrunc | OpenFnCreate;
				}
				else
				{
					OpenFunction = OpenFnTrunc;
				}
			}
			else
			{
				// don't truncate the file
				if ((flags & SmbFile.OCreat) == SmbFile.OCreat)
				{
					// create it if necessary
					if ((flags & SmbFile.OExcl) == SmbFile.OExcl)
					{
						// fail if already exists
						OpenFunction = OpenFnCreate | OpenFnFailIfExists;
					}
					else
					{
						OpenFunction = OpenFnCreate | OpenFnOpen;
					}
				}
				else
				{
					OpenFunction = OpenFnOpen;
				}
			}
		}

		internal override int GetBatchLimit(byte command)
		{
			return command == SmbComReadAndx ? BatchLimit : 0;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(flags, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(DesiredAccess, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(SearchAttributes, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(FileAttributes, dst, dstIndex);
			dstIndex += 2;
			CreationTime = 0;
			WriteInt4(CreationTime, dst, dstIndex);
			dstIndex += 4;
			WriteInt2(OpenFunction, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(AllocationSize, dst, dstIndex);
			dstIndex += 4;
			for (int i = 0; i < 8; i++)
			{
				dst[dstIndex++] = 0x00;
			}
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			if (UseUnicode)
			{
				dst[dstIndex++] = (byte)('\0');
			}
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
			return "SmbComOpenAndX[" + base.ToString() + ",flags=0x" + Hexdump.ToHexString
				(flags, 2) + ",desiredAccess=0x" + Hexdump.ToHexString(DesiredAccess, 4) + ",searchAttributes=0x"
				 + Hexdump.ToHexString(SearchAttributes, 4) + ",fileAttributes=0x" + Hexdump.ToHexString
				(FileAttributes, 4) + ",creationTime=" + Extensions.CreateDate(CreationTime
				) + ",openFunction=0x" + Hexdump.ToHexString(OpenFunction, 2) + ",allocationSize="
				 + AllocationSize + ",fileName=" + Path + "]";
		}
	}
}
