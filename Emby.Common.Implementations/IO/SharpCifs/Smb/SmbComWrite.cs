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
using System;

namespace SharpCifs.Smb
{
	internal class SmbComWrite : ServerMessageBlock
	{
		private int _fid;

		private int _count;

		private int _offset;

		private int _remaining;

		private int _off;

		private byte[] _b;

		public SmbComWrite()
		{
			Command = SmbComWrite;
		}

		internal SmbComWrite(int fid, int offset, int remaining, byte[] b, int off, int len
			)
		{
			this._fid = fid;
			_count = len;
			this._offset = offset;
			this._remaining = remaining;
			this._b = b;
			this._off = off;
			Command = SmbComWrite;
		}

		internal virtual void SetParam(int fid, long offset, int remaining, byte[] b, int
			 off, int len)
		{
			this._fid = fid;
			this._offset = (int)(offset & unchecked(0xFFFFFFFFL));
			this._remaining = remaining;
			this._b = b;
			this._off = off;
			_count = len;
			Digest = null;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(_fid, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_count, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_offset, dst, dstIndex);
			dstIndex += 4;
			WriteInt2(_remaining, dst, dstIndex);
			dstIndex += 2;
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dst[dstIndex++] = 0x01;
			WriteInt2(_count, dst, dstIndex);
			dstIndex += 2;
			Array.Copy(_b, _off, dst, dstIndex, _count);
			dstIndex += _count;
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
			return "SmbComWrite[" + base.ToString() + ",fid=" + _fid + ",count=" + 
				_count + ",offset=" + _offset + ",remaining=" + _remaining + "]";
		}
	}
}
