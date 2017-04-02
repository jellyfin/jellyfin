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

namespace SharpCifs.Dcerpc
{
	public abstract class DcerpcMessage : NdrObject
	{
		protected internal int Ptype = -1;

		protected internal int Flags;

		protected internal int Length;

		protected internal int CallId;

		protected internal int AllocHint;

		protected internal int Result;

		public virtual bool IsFlagSet(int flag)
		{
			return (Flags & flag) == flag;
		}

		public virtual void UnsetFlag(int flag)
		{
			Flags &= ~flag;
		}

		public virtual void SetFlag(int flag)
		{
			Flags |= flag;
		}

		public virtual DcerpcException GetResult()
		{
			if (Result != 0)
			{
				return new DcerpcException(Result);
			}
			return null;
		}

		internal virtual void Encode_header(NdrBuffer buf)
		{
			buf.Enc_ndr_small(5);
			buf.Enc_ndr_small(0);
			buf.Enc_ndr_small(Ptype);
			buf.Enc_ndr_small(Flags);
			buf.Enc_ndr_long(unchecked(0x00000010));
			buf.Enc_ndr_short(Length);
			buf.Enc_ndr_short(0);
			buf.Enc_ndr_long(CallId);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		internal virtual void Decode_header(NdrBuffer buf)
		{
			if (buf.Dec_ndr_small() != 5 || buf.Dec_ndr_small() != 0)
			{
				throw new NdrException("DCERPC version not supported");
			}
			Ptype = buf.Dec_ndr_small();
			Flags = buf.Dec_ndr_small();
			if (buf.Dec_ndr_long() != unchecked(0x00000010))
			{
				throw new NdrException("Data representation not supported");
			}
			Length = buf.Dec_ndr_short();
			if (buf.Dec_ndr_short() != 0)
			{
				throw new NdrException("DCERPC authentication not supported");
			}
			CallId = buf.Dec_ndr_long();
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Encode(NdrBuffer buf)
		{
			int start = buf.GetIndex();
			int allocHintIndex = 0;
			buf.Advance(16);
			if (Ptype == 0)
			{
				allocHintIndex = buf.GetIndex();
				buf.Enc_ndr_long(0);
				buf.Enc_ndr_short(0);
				buf.Enc_ndr_short(GetOpnum());
			}
			Encode_in(buf);
			Length = buf.GetIndex() - start;
			if (Ptype == 0)
			{
				buf.SetIndex(allocHintIndex);
				AllocHint = Length - allocHintIndex;
				buf.Enc_ndr_long(AllocHint);
			}
			buf.SetIndex(start);
			Encode_header(buf);
			buf.SetIndex(start + Length);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public override void Decode(NdrBuffer buf)
		{
			Decode_header(buf);
			if (Ptype != 12 && Ptype != 2 && Ptype != 3 && Ptype != 13)
			{
				throw new NdrException("Unexpected ptype: " + Ptype);
			}
			if (Ptype == 2 || Ptype == 3)
			{
				AllocHint = buf.Dec_ndr_long();
				buf.Dec_ndr_short();
				buf.Dec_ndr_short();
			}
			if (Ptype == 3 || Ptype == 13)
			{
				Result = buf.Dec_ndr_long();
			}
			else
			{
				Decode_out(buf);
			}
		}

		public abstract int GetOpnum();

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public abstract void Encode_in(NdrBuffer dst);

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public abstract void Decode_out(NdrBuffer src);
	}
}
