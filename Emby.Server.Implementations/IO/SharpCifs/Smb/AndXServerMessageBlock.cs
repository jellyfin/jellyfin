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
	internal abstract class AndXServerMessageBlock : ServerMessageBlock
	{
		private const int AndxCommandOffset = 1;

		private const int AndxReservedOffset = 2;

		private const int AndxOffsetOffset = 3;

		private byte _andxCommand = unchecked(unchecked(0xFF));

		private int _andxOffset;

		internal ServerMessageBlock Andx;

		public AndXServerMessageBlock()
		{
		}

		internal AndXServerMessageBlock(ServerMessageBlock andx)
		{
			if (andx != null)
			{
				this.Andx = andx;
				_andxCommand = andx.Command;
			}
		}

		internal virtual int GetBatchLimit(byte command)
		{
			return 0;
		}

		internal override int Encode(byte[] dst, int dstIndex)
		{
			int start = HeaderStart = dstIndex;
			dstIndex += WriteHeaderWireFormat(dst, dstIndex);
			dstIndex += WriteAndXWireFormat(dst, dstIndex);
			Length = dstIndex - start;
			if (Digest != null)
			{
				Digest.Sign(dst, HeaderStart, Length, this, Response);
			}
			return Length;
		}

		internal override int Decode(byte[] buffer, int bufferIndex)
		{
			int start = HeaderStart = bufferIndex;
			bufferIndex += ReadHeaderWireFormat(buffer, bufferIndex);
			bufferIndex += ReadAndXWireFormat(buffer, bufferIndex);
			Length = bufferIndex - start;
			return Length;
		}

		internal virtual int WriteAndXWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WordCount = WriteParameterWordsWireFormat(dst, start + AndxOffsetOffset + 2);
			WordCount += 4;
			// for command, reserved, and offset
			dstIndex += WordCount + 1;
			WordCount /= 2;
			dst[start] = unchecked((byte)(WordCount & unchecked(0xFF)));
			ByteCount = WriteBytesWireFormat(dst, dstIndex + 2);
			dst[dstIndex++] = unchecked((byte)(ByteCount & unchecked(0xFF)));
			dst[dstIndex++] = unchecked((byte)((ByteCount >> 8) & unchecked(0xFF)));
			dstIndex += ByteCount;
			if (Andx == null || SmbConstants.UseBatching == false || BatchLevel >= GetBatchLimit(Andx.Command
				))
			{
				_andxCommand = unchecked(unchecked(0xFF));
				Andx = null;
				dst[start + AndxCommandOffset] = unchecked(unchecked(0xFF));
				dst[start + AndxReservedOffset] = unchecked(unchecked(0x00));
				//            dst[start + ANDX_OFFSET_OFFSET] = (byte)0x00;
				//            dst[start + ANDX_OFFSET_OFFSET + 1] = (byte)0x00;
				dst[start + AndxOffsetOffset] = unchecked(unchecked(0xde));
				dst[start + AndxOffsetOffset + 1] = unchecked(unchecked(0xde));
				// andx not used; return
				return dstIndex - start;
			}
			Andx.BatchLevel = BatchLevel + 1;
			dst[start + AndxCommandOffset] = _andxCommand;
			dst[start + AndxReservedOffset] = unchecked(unchecked(0x00));
			_andxOffset = dstIndex - HeaderStart;
			WriteInt2(_andxOffset, dst, start + AndxOffsetOffset);
			Andx.UseUnicode = UseUnicode;
			if (Andx is AndXServerMessageBlock)
			{
				Andx.Uid = Uid;
				dstIndex += ((AndXServerMessageBlock)Andx).WriteAndXWireFormat(dst, dstIndex
					);
			}
			else
			{
				// the andx smb is not of type andx so lets just write it here and
				// were done.
				int andxStart = dstIndex;
				Andx.WordCount = Andx.WriteParameterWordsWireFormat(dst, dstIndex);
				dstIndex += Andx.WordCount + 1;
				Andx.WordCount /= 2;
				dst[andxStart] = unchecked((byte)(Andx.WordCount & unchecked(0xFF)));
				Andx.ByteCount = Andx.WriteBytesWireFormat(dst, dstIndex + 2);
				dst[dstIndex++] = unchecked((byte)(Andx.ByteCount & unchecked(0xFF)));
				dst[dstIndex++] = unchecked((byte)((Andx.ByteCount >> 8) & unchecked(0xFF)
					));
				dstIndex += Andx.ByteCount;
			}
			return dstIndex - start;
		}

		internal virtual int ReadAndXWireFormat(byte[] buffer, int bufferIndex)
		{
			int start = bufferIndex;
			WordCount = buffer[bufferIndex++];
			if (WordCount != 0)
			{
				_andxCommand = buffer[bufferIndex];
				_andxOffset = ReadInt2(buffer, bufferIndex + 2);
				if (_andxOffset == 0)
				{
					_andxCommand = unchecked(unchecked(0xFF));
				}
				if (WordCount > 2)
				{
					ReadParameterWordsWireFormat(buffer, bufferIndex + 4);
					if (Command == SmbComNtCreateAndx && ((SmbComNtCreateAndXResponse)this).IsExtended)
					{
						WordCount += 8;
					}
				}
				bufferIndex = start + 1 + (WordCount * 2);
			}
			ByteCount = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			if (ByteCount != 0)
			{
				int n;
				n = ReadBytesWireFormat(buffer, bufferIndex);
				bufferIndex += ByteCount;
			}
			if (ErrorCode != 0 || _andxCommand == unchecked(unchecked(0xFF)))
			{
				_andxCommand = unchecked(unchecked(0xFF));
				Andx = null;
			}
			else
			{
				if (Andx == null)
				{
					_andxCommand = unchecked(unchecked(0xFF));
					throw new RuntimeException("no andx command supplied with response");
				}
			    bufferIndex = HeaderStart + _andxOffset;
			    Andx.HeaderStart = HeaderStart;
			    Andx.Command = _andxCommand;
			    Andx.ErrorCode = ErrorCode;
			    Andx.Flags = Flags;
			    Andx.Flags2 = Flags2;
			    Andx.Tid = Tid;
			    Andx.Pid = Pid;
			    Andx.Uid = Uid;
			    Andx.Mid = Mid;
			    Andx.UseUnicode = UseUnicode;
			    if (Andx is AndXServerMessageBlock)
			    {
			        bufferIndex += ((AndXServerMessageBlock)Andx).ReadAndXWireFormat(buffer
			            , bufferIndex);
			    }
			    else
			    {
			        buffer[bufferIndex++] = unchecked((byte)(Andx.WordCount & unchecked(0xFF))
			            );
			        if (Andx.WordCount != 0)
			        {
			            if (Andx.WordCount > 2)
			            {
			                bufferIndex += Andx.ReadParameterWordsWireFormat(buffer, bufferIndex);
			            }
			        }
			        Andx.ByteCount = ReadInt2(buffer, bufferIndex);
			        bufferIndex += 2;
			        if (Andx.ByteCount != 0)
			        {
			            Andx.ReadBytesWireFormat(buffer, bufferIndex);
			            bufferIndex += Andx.ByteCount;
			        }
			    }
			    Andx.Received = true;
			}
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return base.ToString() + ",andxCommand=0x" + Hexdump.ToHexString(_andxCommand
				, 2) + ",andxOffset=" + _andxOffset;
		}
	}
}
