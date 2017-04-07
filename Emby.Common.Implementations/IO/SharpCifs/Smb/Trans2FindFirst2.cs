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

namespace SharpCifs.Smb
{
	internal class Trans2FindFirst2 : SmbComTransaction
	{
		private const int FlagsCloseAfterThisRequest = unchecked(0x01);

		private const int FlagsCloseIfEndReached = unchecked(0x02);

		private const int FlagsReturnResumeKeys = unchecked(0x04);

		private const int FlagsResumeFromPreviousEnd = unchecked(0x08);

		private const int FlagsFindWithBackupIntent = unchecked(0x10);

		private const int DefaultListSize = 65535;

		private const int DefaultListCount = 200;

		private int _searchAttributes;

		private int _flags;

		private int _informationLevel;

		private int _searchStorageType = 0;

		private string _wildcard;

		internal const int SmbInfoStandard = 1;

		internal const int SmbInfoQueryEaSize = 2;

		internal const int SmbInfoQueryEasFromList = 3;

		internal const int SmbFindFileDirectoryInfo = unchecked(0x101);

		internal const int SmbFindFileFullDirectoryInfo = unchecked(0x102);

		internal const int SmbFileNamesInfo = unchecked(0x103);

		internal const int SmbFileBothDirectoryInfo = unchecked(0x104);

		internal static readonly int ListSize = Config.GetInt("jcifs.smb.client.listSize"
			, DefaultListSize);

		internal static readonly int ListCount = Config.GetInt("jcifs.smb.client.listCount"
			, DefaultListCount);

		internal Trans2FindFirst2(string filename, string wildcard, int searchAttributes)
		{
			// flags
			// information levels
			if (filename.Equals("\\"))
			{
				Path = filename;
			}
			else
			{
				Path = filename + "\\";
			}
			this._wildcard = wildcard;
			this._searchAttributes = searchAttributes & unchecked(0x37);
			Command = SmbComTransaction2;
			SubCommand = Trans2FindFirst2;
			_flags = unchecked(0x00);
			_informationLevel = SmbFileBothDirectoryInfo;
			TotalDataCount = 0;
			MaxParameterCount = 10;
			MaxDataCount = ListSize;
			MaxSetupCount = 0;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = SubCommand;
			dst[dstIndex++] = unchecked(unchecked(0x00));
			return 2;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(_searchAttributes, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(ListCount, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_flags, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(_informationLevel, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_searchStorageType, dst, dstIndex);
			dstIndex += 4;
			dstIndex += WriteString(Path + _wildcard, dst, dstIndex);
			return dstIndex - start;
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
			return 0;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			return 0;
		}

		public override string ToString()
		{
			return "Trans2FindFirst2[" + base.ToString() + ",searchAttributes=0x" 
				+ Hexdump.ToHexString(_searchAttributes, 2) + ",searchCount=" + ListCount + ",flags=0x"
				 + Hexdump.ToHexString(_flags, 2) + ",informationLevel=0x" + Hexdump.ToHexString(
				_informationLevel, 3) + ",searchStorageType=" + _searchStorageType + ",filename=" 
				+ Path + "]";
		}
	}
}
