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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	internal class NodeStatusResponse : NameServicePacket
	{
		private NbtAddress _queryAddress;

		private int _numberOfNames;

		private byte[] _macAddress;

		private byte[] _stats;

		internal NbtAddress[] AddressArray;

		internal NodeStatusResponse(NbtAddress queryAddress)
		{
			this._queryAddress = queryAddress;
			RecordName = new Name();
			_macAddress = new byte[6];
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
			int start = srcIndex;
			_numberOfNames = src[srcIndex] & unchecked(0xFF);
			int namesLength = _numberOfNames * 18;
			int statsLength = RDataLength - namesLength - 1;
			_numberOfNames = src[srcIndex++] & unchecked(0xFF);
			// gotta read the mac first so we can populate addressArray with it
			Array.Copy(src, srcIndex + namesLength, _macAddress, 0, 6);
			srcIndex += ReadNodeNameArray(src, srcIndex);
			_stats = new byte[statsLength];
			Array.Copy(src, srcIndex, _stats, 0, statsLength);
			srcIndex += statsLength;
			return srcIndex - start;
		}

		private int ReadNodeNameArray(byte[] src, int srcIndex)
		{
			int start = srcIndex;
			AddressArray = new NbtAddress[_numberOfNames];
			string n;
			int hexCode;
			string scope = _queryAddress.HostName.Scope;
			bool groupName;
			int ownerNodeType;
			bool isBeingDeleted;
			bool isInConflict;
			bool isActive;
			bool isPermanent;
			int j;
			bool addrFound = false;
			try
			{
				for (int i = 0; i < _numberOfNames; srcIndex += 18, i++)
				{
					for (j = srcIndex + 14; src[j] == unchecked(0x20); j--)
					{
					}
					n = Runtime.GetStringForBytes(src, srcIndex, j - srcIndex + 1, Name.OemEncoding
						);
					hexCode = src[srcIndex + 15] & unchecked(0xFF);
					groupName = ((src[srcIndex + 16] & unchecked(0x80)) == unchecked(0x80)) ? true : false;
					ownerNodeType = (src[srcIndex + 16] & unchecked(0x60)) >> 5;
					isBeingDeleted = ((src[srcIndex + 16] & unchecked(0x10)) == unchecked(0x10)) ? true : false;
					isInConflict = ((src[srcIndex + 16] & unchecked(0x08)) == unchecked(0x08)) ? true : false;
					isActive = ((src[srcIndex + 16] & unchecked(0x04)) == unchecked(0x04)) ? true : false;
					isPermanent = ((src[srcIndex + 16] & unchecked(0x02)) == unchecked(0x02)) ? true : false;
					if (!addrFound && _queryAddress.HostName.HexCode == hexCode && (_queryAddress.HostName
						 == NbtAddress.UnknownName || _queryAddress.HostName.name.Equals(n)))
					{
						if (_queryAddress.HostName == NbtAddress.UnknownName)
						{
							_queryAddress.HostName = new Name(n, hexCode, scope);
						}
						_queryAddress.GroupName = groupName;
						_queryAddress.NodeType = ownerNodeType;
						_queryAddress.isBeingDeleted = isBeingDeleted;
						_queryAddress.isInConflict = isInConflict;
						_queryAddress.isActive = isActive;
						_queryAddress.isPermanent = isPermanent;
						_queryAddress.MacAddress = _macAddress;
						_queryAddress.IsDataFromNodeStatus = true;
						addrFound = true;
						AddressArray[i] = _queryAddress;
					}
					else
					{
						AddressArray[i] = new NbtAddress(new Name(n, hexCode, scope), _queryAddress.Address
							, groupName, ownerNodeType, isBeingDeleted, isInConflict, isActive, isPermanent, 
							_macAddress);
					}
				}
			}
			catch (UnsupportedEncodingException)
			{
			}
			return srcIndex - start;
		}

		public override string ToString()
		{
			return "NodeStatusResponse[" + base.ToString() + "]";
		}
	}
}
