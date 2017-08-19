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
    internal abstract class SmbComTransactionResponse : ServerMessageBlock
    {
        private const int SetupOffset = 61;

        private const int DisconnectTid = unchecked(0x01);

        private const int OneWayTransaction = unchecked(0x02);

        private int _pad;

        private int _pad1;

        private bool _parametersDone;

        private bool _dataDone;

        protected internal int TotalParameterCount;

        protected internal int TotalDataCount;

        protected internal int ParameterCount;

        protected internal int ParameterOffset;

        protected internal int ParameterDisplacement;

        protected internal int DataOffset;

        protected internal int DataDisplacement;

        protected internal int SetupCount;

        protected internal int BufParameterStart;

        protected internal int BufDataStart;

        internal int DataCount;

        internal byte SubCommand;

        internal bool HasMore = true;

        internal bool IsPrimary = true;

        internal byte[] TxnBuf;

        internal int Status;

        internal int NumEntries;

        internal IFileEntry[] Results;

        public SmbComTransactionResponse()
        {
            // relative to headerStart
            TxnBuf = null;
        }

        internal override void Reset()
        {
            base.Reset();
            BufDataStart = 0;
            IsPrimary = HasMore = true;
            _parametersDone = _dataDone = false;
        }

        public virtual bool MoveNext()
        {
            return ErrorCode == 0 && HasMore;
        }

        public virtual object Current()
        {
            if (IsPrimary)
            {
                IsPrimary = false;
            }
            return this;
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
            int start = bufferIndex;
            TotalParameterCount = ReadInt2(buffer, bufferIndex);
            if (BufDataStart == 0)
            {
                BufDataStart = TotalParameterCount;
            }
            bufferIndex += 2;
            TotalDataCount = ReadInt2(buffer, bufferIndex);
            bufferIndex += 4;
            // Reserved
            ParameterCount = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            ParameterOffset = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            ParameterDisplacement = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            DataCount = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            DataOffset = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            DataDisplacement = ReadInt2(buffer, bufferIndex);
            bufferIndex += 2;
            SetupCount = buffer[bufferIndex] & unchecked(0xFF);
            bufferIndex += 2;
            if (SetupCount != 0)
            {
                if (Log.Level > 2)
                {
                    Log.WriteLine("setupCount is not zero: " + SetupCount);
                }
            }
            return bufferIndex - start;
        }

        internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
        {
            _pad = _pad1 = 0;
            int n;
            if (ParameterCount > 0)
            {
                bufferIndex += _pad = ParameterOffset - (bufferIndex - HeaderStart);
                Array.Copy(buffer, bufferIndex, TxnBuf, BufParameterStart + ParameterDisplacement
                    , ParameterCount);
                bufferIndex += ParameterCount;
            }
            if (DataCount > 0)
            {
                bufferIndex += _pad1 = DataOffset - (bufferIndex - HeaderStart);
                Array.Copy(buffer, bufferIndex, TxnBuf, BufDataStart + DataDisplacement,
                    DataCount);
                bufferIndex += DataCount;
            }
            if (!_parametersDone && (ParameterDisplacement + ParameterCount) == TotalParameterCount)
            {
                _parametersDone = true;
            }
            if (!_dataDone && (DataDisplacement + DataCount) == TotalDataCount)
            {
                _dataDone = true;
            }
            if (_parametersDone && _dataDone)
            {
                HasMore = false;
                ReadParametersWireFormat(TxnBuf, BufParameterStart, TotalParameterCount);
                ReadDataWireFormat(TxnBuf, BufDataStart, TotalDataCount);
            }
            return _pad + ParameterCount + _pad1 + DataCount;
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
                 + ",totalDataCount=" + TotalDataCount + ",parameterCount=" + ParameterCount + ",parameterOffset="
                 + ParameterOffset + ",parameterDisplacement=" + ParameterDisplacement + ",dataCount="
                 + DataCount + ",dataOffset=" + DataOffset + ",dataDisplacement=" + DataDisplacement
                 + ",setupCount=" + SetupCount + ",pad=" + _pad + ",pad1=" + _pad1;
        }
    }
}
