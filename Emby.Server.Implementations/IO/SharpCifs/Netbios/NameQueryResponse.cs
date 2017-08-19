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
namespace SharpCifs.Netbios
{
	internal class NameQueryResponse : NameServicePacket
	{
		public NameQueryResponse()
		{
			RecordName = new Name();
		}

		internal override int WriteBodyWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadBodyWireFormat(byte[] src, int srcIndex)
		{
			return ReadResourceRecordWireFormat(src, srcIndex);
		}

		internal override int WriteRDataWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadRDataWireFormat(byte[] src, int srcIndex)
		{
			if (ResultCode != 0 || OpCode != Query)
			{
				return 0;
			}
			bool groupName = ((src[srcIndex] & unchecked(0x80)) == unchecked(0x80)) ? true : false;
			int nodeType = (src[srcIndex] & unchecked(0x60)) >> 5;
			srcIndex += 2;
			int address = ReadInt4(src, srcIndex);
			if (address != 0)
			{
				AddrEntry[AddrIndex] = new NbtAddress(RecordName, address, groupName, nodeType);
			}
			else
			{
				AddrEntry[AddrIndex] = null;
			}
			return 6;
		}

		public override string ToString()
		{
			return "NameQueryResponse[" + base.ToString() + ",addrEntry=" + AddrEntry
				 + "]";
		}
	}
}
