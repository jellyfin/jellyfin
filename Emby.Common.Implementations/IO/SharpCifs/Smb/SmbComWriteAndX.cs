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
	internal class SmbComWriteAndX : AndXServerMessageBlock
	{
		private static readonly int ReadAndxBatchLimit = Config.GetInt("jcifs.smb.client.WriteAndX.ReadAndX"
			, 1);

		private static readonly int CloseBatchLimit = Config.GetInt("jcifs.smb.client.WriteAndX.Close"
			, 1);

		private int _fid;

		private int _remaining;

		private int _dataLength;

		private int _dataOffset;

		private int _off;

		private byte[] _b;

		private long _offset;

		private int _pad;

		internal int WriteMode;

		public SmbComWriteAndX() : base(null)
		{
			Command = SmbComWriteAndx;
		}

		internal SmbComWriteAndX(int fid, long offset, int remaining, byte[] b, int off, 
			int len, ServerMessageBlock andx) : base(andx)
		{
			this._fid = fid;
			this._offset = offset;
			this._remaining = remaining;
			this._b = b;
			this._off = off;
			_dataLength = len;
			Command = SmbComWriteAndx;
		}

		internal virtual void SetParam(int fid, long offset, int remaining, byte[] b, int
			 off, int len)
		{
			this._fid = fid;
			this._offset = offset;
			this._remaining = remaining;
			this._b = b;
			this._off = off;
			_dataLength = len;
			Digest = null;
		}

		internal override int GetBatchLimit(byte command)
		{
			if (command == SmbComReadAndx)
			{
				return ReadAndxBatchLimit;
			}
			if (command == SmbComClose)
			{
				return CloseBatchLimit;
			}
			return 0;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			_dataOffset = (dstIndex - HeaderStart) + 26;
			// 26 = off from here to pad
			_pad = (_dataOffset - HeaderStart) % 4;
			_pad = _pad == 0 ? 0 : 4 - _pad;
			_dataOffset += _pad;
			WriteInt2(_fid, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_offset, dst, dstIndex);
			dstIndex += 4;
			for (int i = 0; i < 4; i++)
			{
				dst[dstIndex++] = 0xFF;
			}
			WriteInt2(WriteMode, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_remaining, dst, dstIndex);
			dstIndex += 2;
			dst[dstIndex++] = 0x00;
			dst[dstIndex++] =0x00;
			WriteInt2(_dataLength, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_dataOffset, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_offset >> 32, dst, dstIndex);
			dstIndex += 4;
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			while (_pad-- > 0)
			{
				dst[dstIndex++] = 0xEE;
			}
			Array.Copy(_b, _off, dst, dstIndex, _dataLength);
			dstIndex += _dataLength;
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
			return "SmbComWriteAndX[" + base.ToString() + ",fid=" + _fid + ",offset="
				 + _offset + ",writeMode=" + WriteMode + ",remaining=" + _remaining + ",dataLength="
				 + _dataLength + ",dataOffset=" + _dataOffset + "]";
		}
	}
}
