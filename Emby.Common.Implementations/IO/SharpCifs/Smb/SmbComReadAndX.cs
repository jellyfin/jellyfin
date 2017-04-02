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
	internal class SmbComReadAndX : AndXServerMessageBlock
	{
		private static readonly int BatchLimit = Config.GetInt("jcifs.smb.client.ReadAndX.Close"
			, 1);

		private long _offset;

		private int _fid;

		private int _openTimeout;

		internal int MaxCount;

		internal int MinCount;

		internal int Remaining;

		public SmbComReadAndX() : base(null)
		{
			Command = SmbComReadAndx;
			_openTimeout = unchecked((int)(0xFFFFFFFF));
		}

		internal SmbComReadAndX(int fid, long offset, int maxCount, ServerMessageBlock andx
			) : base(andx)
		{
			this._fid = fid;
			this._offset = offset;
			this.MaxCount = MinCount = maxCount;
			Command = SmbComReadAndx;
			_openTimeout = unchecked((int)(0xFFFFFFFF));
		}

		internal virtual void SetParam(int fid, long offset, int maxCount)
		{
			this._fid = fid;
			this._offset = offset;
			this.MaxCount = MinCount = maxCount;
		}

		internal override int GetBatchLimit(byte command)
		{
			return command == SmbComClose ? BatchLimit : 0;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(_fid, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_offset, dst, dstIndex);
			dstIndex += 4;
			WriteInt2(MaxCount, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(MinCount, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_openTimeout, dst, dstIndex);
			dstIndex += 4;
			WriteInt2(Remaining, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_offset >> 32, dst, dstIndex);
			dstIndex += 4;
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
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
			return "SmbComReadAndX[" + base.ToString() + ",fid=" + _fid + ",offset="
				 + _offset + ",maxCount=" + MaxCount + ",minCount=" + MinCount + ",openTimeout=" 
				+ _openTimeout + ",remaining=" + Remaining + ",offset=" + _offset + "]";
		}
	}
}
