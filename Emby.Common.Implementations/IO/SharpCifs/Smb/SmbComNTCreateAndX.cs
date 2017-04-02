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
	internal class SmbComNtCreateAndX : AndXServerMessageBlock
	{
		internal const int FileSupersede = unchecked(0x0);

		internal const int FileOpen = unchecked(0x1);

		internal const int FileCreate = unchecked(0x2);

		internal const int FileOpenIf = unchecked(0x3);

		internal const int FileOverwrite = unchecked(0x4);

		internal const int FileOverwriteIf = unchecked(0x5);

		internal const int FileWriteThrough = unchecked(0x00000002);

		internal const int FileSequentialOnly = unchecked(0x00000004);

		internal const int FileSynchronousIoAlert = unchecked(0x00000010);

		internal const int FileSynchronousIoNonalert = unchecked(0x00000020);

		internal const int SecurityContextTracking = unchecked(0x01);

		internal const int SecurityEffectiveOnly = unchecked(0x02);

		private int _rootDirectoryFid;

		private int _extFileAttributes;

		private int _shareAccess;

		private int _createDisposition;

		private int _createOptions;

		private int _impersonationLevel;

		private long _allocationSize;

		private byte _securityFlags;

		private int _namelenIndex;

		internal int Flags0;

		internal int DesiredAccess;

		internal SmbComNtCreateAndX(string name, int flags, int access, int shareAccess, 
			int extFileAttributes, int createOptions, ServerMessageBlock andx) : base(andx)
		{
			// share access specified in SmbFile
			// create disposition
			// create options
			// security flags
			Path = name;
			Command = SmbComNtCreateAndx;
			DesiredAccess = access;
            DesiredAccess |= SmbConstants.FileReadData | SmbConstants.FileReadEa | SmbConstants.FileReadAttributes;
			// extFileAttributes
			this._extFileAttributes = extFileAttributes;
			// shareAccess
			this._shareAccess = shareAccess;
			// createDisposition
			if ((flags & SmbFile.OTrunc) == SmbFile.OTrunc)
			{
				// truncate the file
				if ((flags & SmbFile.OCreat) == SmbFile.OCreat)
				{
					// create it if necessary
					_createDisposition = FileOverwriteIf;
				}
				else
				{
					_createDisposition = FileOverwrite;
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
						_createDisposition = FileCreate;
					}
					else
					{
						_createDisposition = FileOpenIf;
					}
				}
				else
				{
					_createDisposition = FileOpen;
				}
			}
			if ((createOptions & unchecked(0x0001)) == 0)
			{
				this._createOptions = createOptions | unchecked(0x0040);
			}
			else
			{
				this._createOptions = createOptions;
			}
			_impersonationLevel = unchecked(0x02);
			// As seen on NT :~)
			_securityFlags = unchecked(unchecked(0x03));
		}

		// SECURITY_CONTEXT_TRACKING | SECURITY_EFFECTIVE_ONLY
		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dst[dstIndex++] = unchecked(unchecked(0x00));
			// name length without counting null termination
			_namelenIndex = dstIndex;
			dstIndex += 2;
			WriteInt4(Flags0, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(_rootDirectoryFid, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(DesiredAccess, dst, dstIndex);
			dstIndex += 4;
			WriteInt8(_allocationSize, dst, dstIndex);
			dstIndex += 8;
			WriteInt4(_extFileAttributes, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(_shareAccess, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(_createDisposition, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(_createOptions, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(_impersonationLevel, dst, dstIndex);
			dstIndex += 4;
			dst[dstIndex++] = _securityFlags;
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int n;
			n = WriteString(Path, dst, dstIndex);
			WriteInt2((UseUnicode ? Path.Length * 2 : n), dst, _namelenIndex);
			return n;
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
			return "SmbComNTCreateAndX[" + base.ToString() + ",flags=0x" + Hexdump
				.ToHexString(Flags0, 2) + ",rootDirectoryFid=" + _rootDirectoryFid + ",desiredAccess=0x"
				 + Hexdump.ToHexString(DesiredAccess, 4) + ",allocationSize=" + _allocationSize +
				 ",extFileAttributes=0x" + Hexdump.ToHexString(_extFileAttributes, 4) + ",shareAccess=0x"
				 + Hexdump.ToHexString(_shareAccess, 4) + ",createDisposition=0x" + Hexdump.ToHexString
				(_createDisposition, 4) + ",createOptions=0x" + Hexdump.ToHexString(_createOptions
				, 8) + ",impersonationLevel=0x" + Hexdump.ToHexString(_impersonationLevel, 4) + ",securityFlags=0x"
				 + Hexdump.ToHexString(_securityFlags, 2) + ",name=" + Path + "]";
		}
	}
}
