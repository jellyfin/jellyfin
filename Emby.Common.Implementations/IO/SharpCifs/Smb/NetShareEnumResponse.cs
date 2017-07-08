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
	internal class NetShareEnumResponse : SmbComTransactionResponse
	{
		private int _converter;

		private int _totalAvailableEntries;

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
			Status = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			_converter = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			NumEntries = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			_totalAvailableEntries = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			return bufferIndex - start;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			int start = bufferIndex;
			SmbShareInfo e;
			UseUnicode = false;
			Results = new SmbShareInfo[NumEntries];
			for (int i = 0; i < NumEntries; i++)
			{
				Results[i] = e = new SmbShareInfo();
				e.NetName = ReadString(buffer, bufferIndex, 13, false);
				bufferIndex += 14;
				e.Type = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
				int off = ReadInt4(buffer, bufferIndex);
				bufferIndex += 4;
				off = (off & unchecked(0xFFFF)) - _converter;
				off = start + off;
				e.Remark = ReadString(buffer, off, 128, false);
				if (Log.Level >= 4)
				{
					Log.WriteLine(e);
				}
			}
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "NetShareEnumResponse[" + base.ToString() + ",status=" + Status
				 + ",converter=" + _converter + ",entriesReturned=" + NumEntries + ",totalAvailableEntries="
				 + _totalAvailableEntries + "]";
		}
	}
}
