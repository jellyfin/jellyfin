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
namespace SharpCifs.Dcerpc.Msrpc
{
	public class MsrpcLsarOpenPolicy2 : Lsarpc.LsarOpenPolicy2
	{
		public MsrpcLsarOpenPolicy2(string server, int access, LsaPolicyHandle policyHandle
			) : base(server, new Lsarpc.LsarObjectAttributes(), access, policyHandle)
		{
			ObjectAttributes.Length = 24;
			Lsarpc.LsarQosInfo qos = new Lsarpc.LsarQosInfo();
			qos.Length = 12;
			qos.ImpersonationLevel = 2;
			qos.ContextMode = 1;
			qos.EffectiveOnly = 0;
			ObjectAttributes.SecurityQualityOfService = qos;
			Ptype = 0;
            Flags = DcerpcConstants.DcerpcFirstFrag | DcerpcConstants.DcerpcLastFrag;
		}
	}
}
