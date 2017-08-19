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
	internal class NetServerEnum2Response : SmbComTransactionResponse
	{
		internal class ServerInfo1 : IFileEntry
		{
			internal string Name;

			internal int VersionMajor;

			internal int VersionMinor;

			internal int Type;

			internal string CommentOrMasterBrowser;

			public virtual string GetName()
			{
				return Name;
			}

			public new virtual int GetType()
			{
				return (Type & unchecked((int)(0x80000000))) != 0 ? SmbFile.TypeWorkgroup : 
					SmbFile.TypeServer;
			}

			public virtual int GetAttributes()
			{
				return SmbFile.AttrReadonly | SmbFile.AttrDirectory;
			}

			public virtual long CreateTime()
			{
				return 0L;
			}

			public virtual long LastModified()
			{
				return 0L;
			}

			public virtual long Length()
			{
				return 0L;
			}

			public override string ToString()
			{
				return "ServerInfo1[" + "name=" + Name + ",versionMajor=" + VersionMajor + ",versionMinor=" + VersionMinor + ",type=0x" + Hexdump.ToHexString
					(Type, 8) + ",commentOrMasterBrowser=" + CommentOrMasterBrowser + "]";
			}

			internal ServerInfo1(NetServerEnum2Response enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly NetServerEnum2Response _enclosing;
		}

		private int _converter;

		private int _totalAvailableEntries;

		internal string LastName;

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
			ServerInfo1 e = null;
			Results = new ServerInfo1[NumEntries];
			for (int i = 0; i < NumEntries; i++)
			{
				Results[i] = e = new ServerInfo1(this);
				e.Name = ReadString(buffer, bufferIndex, 16, false);
				bufferIndex += 16;
				e.VersionMajor = buffer[bufferIndex++] & unchecked(0xFF);
				e.VersionMinor = buffer[bufferIndex++] & unchecked(0xFF);
				e.Type = ReadInt4(buffer, bufferIndex);
				bufferIndex += 4;
				int off = ReadInt4(buffer, bufferIndex);
				bufferIndex += 4;
				off = (off & unchecked(0xFFFF)) - _converter;
				off = start + off;
				e.CommentOrMasterBrowser = ReadString(buffer, off, 48, false);
				if (Log.Level >= 4)
				{
					Log.WriteLine(e);
				}
			}
			LastName = NumEntries == 0 ? null : e.Name;
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "NetServerEnum2Response[" + base.ToString() + ",status=" + Status
				 + ",converter=" + _converter + ",entriesReturned=" + NumEntries + ",totalAvailableEntries="
				 + _totalAvailableEntries + ",lastName=" + LastName + "]";
		}
	}
}
