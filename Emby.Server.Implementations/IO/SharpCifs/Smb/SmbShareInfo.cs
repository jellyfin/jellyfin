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
	public class SmbShareInfo : IFileEntry
	{
		protected internal string NetName;

		protected internal int Type;

		protected internal string Remark;

		public SmbShareInfo()
		{
		}

		public SmbShareInfo(string netName, int type, string remark)
		{
			this.NetName = netName;
			this.Type = type;
			this.Remark = remark;
		}

		public virtual string GetName()
		{
			return NetName;
		}

		public new virtual int GetType() 
		{
			switch (Type & unchecked(0xFFFF))
			{
				case 1:
				{
					return SmbFile.TypePrinter;
				}

				case 3:
				{
					return SmbFile.TypeNamedPipe;
				}
			}
			return SmbFile.TypeShare;
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

		public override bool Equals(object obj)
		{
			if (obj is SmbShareInfo)
			{
				SmbShareInfo si = (SmbShareInfo)obj;
				return NetName.Equals(si.NetName);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int hashCode = NetName.GetHashCode();
			return hashCode;
		}

		public override string ToString()
		{
			return "SmbShareInfo[" + "netName=" + NetName + ",type=0x" + Hexdump.ToHexString
				(Type, 8) + ",remark=" + Remark + "]";
		}
	}
}
