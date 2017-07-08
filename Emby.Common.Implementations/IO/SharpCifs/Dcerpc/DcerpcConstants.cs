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
namespace SharpCifs.Dcerpc
{
	public static class DcerpcConstants
	{
		public static Uuid DcerpcUuidSyntaxNdr = new Uuid("8a885d04-1ceb-11c9-9fe8-08002b104860"
			);

		public static int DcerpcFirstFrag = unchecked(0x01);

		public static int DcerpcLastFrag = unchecked(0x02);

		public static int DcerpcPendingCancel = unchecked(0x04);

		public static int DcerpcReserved1 = unchecked(0x08);

		public static int DcerpcConcMpx = unchecked(0x10);

		public static int DcerpcDidNotExecute = unchecked(0x20);

		public static int DcerpcMaybe = unchecked(0x40);

		public static int DcerpcObjectUuid = unchecked(0x80);
	}
}
