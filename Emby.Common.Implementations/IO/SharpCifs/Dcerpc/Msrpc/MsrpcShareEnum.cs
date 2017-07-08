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
using SharpCifs.Smb;

namespace SharpCifs.Dcerpc.Msrpc
{
	public class MsrpcShareEnum : Srvsvc.ShareEnumAll
	{
		internal class MsrpcShareInfo1 : SmbShareInfo
		{
			internal MsrpcShareInfo1(MsrpcShareEnum enclosing, Srvsvc.ShareInfo1 info1)
			{
				this._enclosing = enclosing;
				NetName = info1.Netname;
				Type = info1.Type;
				Remark = info1.Remark;
			}

			private readonly MsrpcShareEnum _enclosing;
		}

		public MsrpcShareEnum(string server) : base("\\\\" + server, 1, new Srvsvc.ShareInfoCtr1
			(), -1, 0, 0)
		{
			Ptype = 0;
            Flags = DcerpcConstants.DcerpcFirstFrag | DcerpcConstants.DcerpcLastFrag;
		}

		public virtual IFileEntry[] GetEntries()
		{
			Srvsvc.ShareInfoCtr1 ctr = (Srvsvc.ShareInfoCtr1)Info;
			MsrpcShareInfo1[] entries = new MsrpcShareInfo1[ctr
				.Count];
			for (int i = 0; i < ctr.Count; i++)
			{
				entries[i] = new MsrpcShareInfo1(this, ctr.Array[i]);
			}
			return entries;
		}
	}
}
