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
	public static class DcerpcError
	{
		public static int DcerpcFaultOther = unchecked(0x00000001);

		public static int DcerpcFaultAccessDenied = unchecked(0x00000005);

		public static int DcerpcFaultCantPerform = unchecked(0x000006D8);

		public static int DcerpcFaultNdr = unchecked(0x000006F7);

		public static int DcerpcFaultInvalidTag = unchecked(0x1C000006);

		public static int DcerpcFaultContextMismatch = unchecked(0x1C00001A);

		public static int DcerpcFaultOpRngError = unchecked(0x1C010002);

		public static int DcerpcFaultUnkIf = unchecked(0x1C010003);

		public static int DcerpcFaultProtoError = unchecked(0x1c01000b);

		public static int[] DcerpcFaultCodes = { DcerpcFaultOther, DcerpcFaultAccessDenied
			, DcerpcFaultCantPerform, DcerpcFaultNdr, DcerpcFaultInvalidTag, DcerpcFaultContextMismatch
			, DcerpcFaultOpRngError, DcerpcFaultUnkIf, DcerpcFaultProtoError };

		public static string[] DcerpcFaultMessages = { "DCERPC_FAULT_OTHER"
			, "DCERPC_FAULT_ACCESS_DENIED", "DCERPC_FAULT_CANT_PERFORM", "DCERPC_FAULT_NDR", 
			"DCERPC_FAULT_INVALID_TAG", "DCERPC_FAULT_CONTEXT_MISMATCH", "DCERPC_FAULT_OP_RNG_ERROR"
			, "DCERPC_FAULT_UNK_IF", "DCERPC_FAULT_PROTO_ERROR" };
	}
}
