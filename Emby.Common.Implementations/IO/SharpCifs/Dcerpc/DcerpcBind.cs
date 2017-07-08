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
using SharpCifs.Dcerpc.Ndr;
using SharpCifs.Util;

namespace SharpCifs.Dcerpc
{
	public class DcerpcBind : DcerpcMessage
	{
		internal static readonly string[] ResultMessage = { "0", "DCERPC_BIND_ERR_ABSTRACT_SYNTAX_NOT_SUPPORTED"
			, "DCERPC_BIND_ERR_PROPOSED_TRANSFER_SYNTAXES_NOT_SUPPORTED", "DCERPC_BIND_ERR_LOCAL_LIMIT_EXCEEDED"
			 };

		internal static string GetResultMessage(int result)
		{
			return result < 4 ? ResultMessage[result] : "0x" + Hexdump.ToHexString(result, 4
				);
		}

		public override DcerpcException GetResult()
		{
			if (Result != 0)
			{
				return new DcerpcException(GetResultMessage(Result));
			}
			return null;
		}

		internal DcerpcBinding Binding;

		internal int MaxXmit;

		internal int MaxRecv;

		public DcerpcBind()
		{
		}

		internal DcerpcBind(DcerpcBinding binding, DcerpcHandle handle)
		{
			this.Binding = binding;
			MaxXmit = handle.MaxXmit;
			MaxRecv = handle.MaxRecv;
			Ptype = 11;
            Flags = DcerpcConstants.DcerpcFirstFrag | DcerpcConstants.DcerpcLastFrag;
		}

		public override int GetOpnum()
		{
			return 0;
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Encode_in(NdrBuffer dst)
		{
			dst.Enc_ndr_short(MaxXmit);
			dst.Enc_ndr_short(MaxRecv);
			dst.Enc_ndr_long(0);
			dst.Enc_ndr_small(1);
			dst.Enc_ndr_small(0);
			dst.Enc_ndr_short(0);
			dst.Enc_ndr_short(0);
			dst.Enc_ndr_small(1);
			dst.Enc_ndr_small(0);
			Binding.Uuid.Encode(dst);
			dst.Enc_ndr_short(Binding.Major);
			dst.Enc_ndr_short(Binding.Minor);
            DcerpcConstants.DcerpcUuidSyntaxNdr.Encode(dst);
			dst.Enc_ndr_long(2);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Decode_out(NdrBuffer src)
		{
			src.Dec_ndr_short();
			src.Dec_ndr_short();
			src.Dec_ndr_long();
			int n = src.Dec_ndr_short();
			src.Advance(n);
			src.Align(4);
			src.Dec_ndr_small();
			src.Align(4);
			Result = src.Dec_ndr_short();
			src.Dec_ndr_short();
			src.Advance(20);
		}
	}
}
