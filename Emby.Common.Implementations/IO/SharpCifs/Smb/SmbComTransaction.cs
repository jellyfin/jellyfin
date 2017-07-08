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
using SharpCifs.Util;

namespace SharpCifs.Smb
{
    internal abstract class SmbComTransaction : ServerMessageBlock
    {
        private static readonly int DefaultMaxDataCount = Config.GetInt("jcifs.smb.client.transaction_buf_size"
            , TransactionBufSize) - 512;

        private const int PrimarySetupOffset = 61;

        private const int SecondaryParameterOffset = 51;

        private const int DisconnectTid = unchecked(0x01);

        private const int OneWayTransaction = unchecked(0x02);

        private const int PaddingSize = 2;

        private int _flags = unchecked(0x00);

        private int _fid;

        private int _pad;

        private int _pad1;

        private bool _hasMore = true;

        private bool _isPrimary = true;

        private int _bufParameterOffset;

        private int _bufDataOffset;

        internal const int TransactionBufSize = unchecked(0xFFFF);

        internal const byte Trans2FindFirst2 = unchecked(unchecked(0x01));

        internal const byte Trans2FindNext2 = unchecked(unchecked(0x02));

        internal const byte Trans2QueryFsInformation = unchecked(unchecked(0x03));

        internal const byte Trans2QueryPathInformation = unchecked(unchecked(0x05));

        internal const byte Trans2GetDfsReferral = unchecked(unchecked(0x10));

        internal const byte Trans2SetFileInformation = unchecked(unchecked(0x08));

        internal const int NetShareEnum = unchecked(0x0000);

        internal const int NetServerEnum2 = unchecked(0x0068);

        internal const int NetServerEnum3 = unchecked(0x00D7);

        internal const byte TransPeekNamedPipe = unchecked(unchecked(0x23
            ));

        internal const byte TransWaitNamedPipe = unchecked(unchecked(0x53
            ));

        internal const byte TransCallNamedPipe = unchecked(unchecked(0x54
            ));

        internal const byte TransTransactNamedPipe = unchecked(unchecked(0x26));

        protected internal int primarySetupOffset;

        protected internal int secondaryParameterOffset;

        protected internal int ParameterCount;

        protected internal int ParameterOffset;

        protected internal int ParameterDisplacement;

        protected internal int DataCount;

        protected internal int DataOffset;

        protected internal int DataDisplacement;

        internal int TotalParameterCount;

        internal int TotalDataCount;

        internal int MaxParameterCount;

        internal int MaxDataCount = DefaultMaxDataCount;

        internal byte MaxSetupCount;

        internal int Timeout = 0;

        internal int SetupCount = 1;

        internal byte SubCommand;

        internal string Name = string.Empty;

        internal int MaxBufferSize;

        internal byte[] TxnBuf;

        public SmbComTransaction()
        {
            // relative to headerStart
            // set in SmbTransport.sendTransaction() before nextElement called
            MaxParameterCount = 1024;
            primarySetupOffset = PrimarySetupOffset;
            secondaryParameterOffset = SecondaryParameterOffset;
        }

        internal override void Reset()
        {
            base.Reset();
            _isPrimary = _hasMore = true;
        }

        internal virtual void Reset(int key, string lastName)
        {
            Reset();
        }

        public virtual bool MoveNext()
        {
            return _hasMore;
        }

        public virtual object Current()
        {
            if (_isPrimary)
            {
                _isPrimary = false;
                ParameterOffset = primarySetupOffset + (SetupCount * 2) + 2;
                if (Command != SmbComNtTransact)
                {
                    if (Command == SmbComTransaction && IsResponse() == false)
                    {
                        ParameterOffset += StringWireLength(Name, ParameterOffset);
                    }
                }
                else
                {
                    if (Command == SmbComNtTransact)
                    {
                        ParameterOffset += 2;
                    }
                }
                _pad = ParameterOffset % PaddingSize;
                _pad = _pad == 0 ? 0 : PaddingSize - _pad;
                ParameterOffset += _pad;
                TotalParameterCount = WriteParametersWireFormat(TxnBuf, _bufParameterOffset);
                _bufDataOffset = TotalParameterCount;
                // data comes right after data
                int available = MaxBufferSize - ParameterOffset;
                ParameterCount = Math.Min(TotalParameterCount, available);
                available -= ParameterCount;
                DataOffset = ParameterOffset + ParameterCount;
                _pad1 = DataOffset % PaddingSize;
                _pad1 = _pad1 == 0 ? 0 : PaddingSize - _pad1;
                DataOffset += _pad1;
                TotalDataCount = WriteDataWireFormat(TxnBuf, _bufDataOffset);
                DataCount = Math.Min(TotalDataCount, available);
            }
            else
            {
                if (Command != SmbComNtTransact)
                {
                    Command = SmbComTransactionSecondary;
                }
                else
                {
                    Command = SmbComNtTransactSecondary;
                }
                // totalParameterCount and totalDataCount are set ok from primary
                ParameterOffset = SecondaryParameterOffset;
                if ((TotalParameterCount - ParameterDisplacement) > 0)
                {
                    _pad = ParameterOffset % PaddingSize;
                    _pad = _pad == 0 ? 0 : PaddingSize - _pad;
                    ParameterOffset += _pad;
                }
                // caclulate parameterDisplacement before calculating new parameterCount
                ParameterDisplacement += ParameterCount;
                int available = MaxBufferSize - ParameterOffset - _pad;
                ParameterCount = Math.Min(TotalParameterCount - ParameterDisplacement, available);
                available -= ParameterCount;
                DataOffset = ParameterOffset + ParameterCount;
                _pad1 = DataOffset % PaddingSize;
                _pad1 = _pad1 == 0 ? 0 : PaddingSize - _pad1;
                DataOffset += _pad1;
                DataDisplacement += DataCount;
                available -= _pad1;
                DataCount = Math.Min(TotalDataCount - DataDisplacement, available);
            }
            if ((ParameterDisplacement + ParameterCount) >= TotalParameterCount && (DataDisplacement
                 + DataCount) >= TotalDataCount)
            {
                _hasMore = false;
            }
            return this;

        }

        internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
        {
            int start = dstIndex;
            WriteInt2(TotalParameterCount, dst, dstIndex);
            dstIndex += 2;
            WriteInt2(TotalDataCount, dst, dstIndex);
            dstIndex += 2;
            if (Command != SmbComTransactionSecondary)
            {
                WriteInt2(MaxParameterCount, dst, dstIndex);
                dstIndex += 2;
                WriteInt2(MaxDataCount, dst, dstIndex);
                dstIndex += 2;
                dst[dstIndex++] = MaxSetupCount;
                dst[dstIndex++] = unchecked(unchecked(0x00));
                // Reserved1
                WriteInt2(_flags, dst, dstIndex);
                dstIndex += 2;
                WriteInt4(Timeout, dst, dstIndex);
                dstIndex += 4;
                dst[dstIndex++] = unchecked(unchecked(0x00));
                // Reserved2
                dst[dstIndex++] = unchecked(unchecked(0x00));
            }
            WriteInt2(ParameterCount, dst, dstIndex);
            dstIndex += 2;
            //        writeInt2(( parameterCount == 0 ? 0 : parameterOffset ), dst, dstIndex );
            WriteInt2(ParameterOffset, dst, dstIndex);
            dstIndex += 2;
            if (Command == SmbComTransactionSecondary)
            {
                WriteInt2(ParameterDisplacement, dst, dstIndex);
                dstIndex += 2;
            }
            WriteInt2(DataCount, dst, dstIndex);
            dstIndex += 2;
            WriteInt2((DataCount == 0 ? 0 : DataOffset), dst, dstIndex);
            dstIndex += 2;
            if (Command == SmbComTransactionSecondary)
            {
                WriteInt2(DataDisplacement, dst, dstIndex);
                dstIndex += 2;
            }
            else
            {
                dst[dstIndex++] = unchecked((byte)SetupCount);
                dst[dstIndex++] = unchecked(unchecked(0x00));
                // Reserved3
                dstIndex += WriteSetupWireFormat(dst, dstIndex);
            }
            return dstIndex - start;
        }

        internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
        {
            int start = dstIndex;
            int p = _pad;
            if (Command == SmbComTransaction && IsResponse() == false)
            {
                dstIndex += WriteString(Name, dst, dstIndex);
            }
            if (ParameterCount > 0)
            {
                while (p-- > 0)
                {
                    dst[dstIndex++] = unchecked(unchecked(0x00));
                }
                // Pad
                Array.Copy(TxnBuf, _bufParameterOffset, dst, dstIndex, ParameterCount);
                dstIndex += ParameterCount;
            }
            if (DataCount > 0)
            {
                p = _pad1;
                while (p-- > 0)
                {
                    dst[dstIndex++] = unchecked(unchecked(0x00));
                }
                // Pad1
                Array.Copy(TxnBuf, _bufDataOffset, dst, dstIndex, DataCount);
                _bufDataOffset += DataCount;
                dstIndex += DataCount;
            }
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

        internal abstract int WriteSetupWireFormat(byte[] dst, int dstIndex);

        internal abstract int WriteParametersWireFormat(byte[] dst, int dstIndex);

        internal abstract int WriteDataWireFormat(byte[] dst, int dstIndex);

        internal abstract int ReadSetupWireFormat(byte[] buffer, int bufferIndex, int len
            );

        internal abstract int ReadParametersWireFormat(byte[] buffer, int bufferIndex, int
             len);

        internal abstract int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len);

        public override string ToString()
        {
            return base.ToString() + ",totalParameterCount=" + TotalParameterCount
                 + ",totalDataCount=" + TotalDataCount + ",maxParameterCount=" + MaxParameterCount
                 + ",maxDataCount=" + MaxDataCount + ",maxSetupCount=" + (int)MaxSetupCount + ",flags=0x"
                 + Hexdump.ToHexString(_flags, 2) + ",timeout=" + Timeout + ",parameterCount=" +
                ParameterCount + ",parameterOffset=" + ParameterOffset + ",parameterDisplacement="
                 + ParameterDisplacement + ",dataCount=" + DataCount + ",dataOffset=" + DataOffset
                 + ",dataDisplacement=" + DataDisplacement + ",setupCount=" + SetupCount + ",pad="
                 + _pad + ",pad1=" + _pad1;
        }
    }
}
