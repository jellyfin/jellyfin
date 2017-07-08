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
	internal class SmbComQueryInformationResponse : ServerMessageBlock, IInfo
	{
		private int _fileAttributes = 0x0000;

		private long _lastWriteTime;

		private long _serverTimeZoneOffset;

		private int _fileSize;

		internal SmbComQueryInformationResponse(long serverTimeZoneOffset)
		{
			this._serverTimeZoneOffset = serverTimeZoneOffset;
			Command = SmbComQueryInformation;
		}

		public virtual int GetAttributes()
		{
			return _fileAttributes;
		}

		public virtual long GetCreateTime()
		{
			return _lastWriteTime + _serverTimeZoneOffset;
		}

		public virtual long GetLastWriteTime()
		{
			return _lastWriteTime + _serverTimeZoneOffset;
		}

		public virtual long GetSize()
		{
			return _fileSize;
		}

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
			if (WordCount == 0)
			{
				return 0;
			}
			_fileAttributes = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			_lastWriteTime = ReadUTime(buffer, bufferIndex);
			bufferIndex += 4;
			_fileSize = ReadInt4(buffer, bufferIndex);
			return 20;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			return "SmbComQueryInformationResponse[" + base.ToString() + ",fileAttributes=0x"
				 + Hexdump.ToHexString(_fileAttributes, 4) + ",lastWriteTime=" + Extensions.CreateDate
				(_lastWriteTime) + ",fileSize=" + _fileSize + "]";
		}
	}
}
