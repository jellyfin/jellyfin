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
	public class Rpc
	{
		public class UuidT : NdrObject
		{
			public int TimeLow;

			public short TimeMid;

			public short TimeHiAndVersion;

			public byte ClockSeqHiAndReserved;

			public byte ClockSeqLow;

			public byte[] Node;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(TimeLow);
				dst.Enc_ndr_short(TimeMid);
				dst.Enc_ndr_short(TimeHiAndVersion);
				dst.Enc_ndr_small(ClockSeqHiAndReserved);
				dst.Enc_ndr_small(ClockSeqLow);
				int nodes = 6;
				int nodei = dst.Index;
				dst.Advance(1 * nodes);
				dst = dst.Derive(nodei);
				for (int i = 0; i < nodes; i++)
				{
					dst.Enc_ndr_small(Node[i]);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				TimeLow = src.Dec_ndr_long();
				TimeMid = (short)src.Dec_ndr_short();
				TimeHiAndVersion = (short)src.Dec_ndr_short();
				ClockSeqHiAndReserved = unchecked((byte)src.Dec_ndr_small());
				ClockSeqLow = unchecked((byte)src.Dec_ndr_small());
				int nodes = 6;
				int nodei = src.Index;
				src.Advance(1 * nodes);
				if (Node == null)
				{
					if (nodes < 0 || nodes > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					Node = new byte[nodes];
				}
				src = src.Derive(nodei);
				for (int i = 0; i < nodes; i++)
				{
					Node[i] = unchecked((byte)src.Dec_ndr_small());
				}
			}
		}

		public class PolicyHandle : NdrObject
		{
			public int Type;

			public UuidT Uuid;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Type);
				dst.Enc_ndr_long(Uuid.TimeLow);
				dst.Enc_ndr_short(Uuid.TimeMid);
				dst.Enc_ndr_short(Uuid.TimeHiAndVersion);
				dst.Enc_ndr_small(Uuid.ClockSeqHiAndReserved);
				dst.Enc_ndr_small(Uuid.ClockSeqLow);
				int uuidNodes = 6;
				int uuidNodei = dst.Index;
				dst.Advance(1 * uuidNodes);
				dst = dst.Derive(uuidNodei);
				for (int i = 0; i < uuidNodes; i++)
				{
					dst.Enc_ndr_small(Uuid.Node[i]);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Type = src.Dec_ndr_long();
				src.Align(4);
				if (Uuid == null)
				{
					Uuid = new UuidT();
				}
				Uuid.TimeLow = src.Dec_ndr_long();
				Uuid.TimeMid = (short)src.Dec_ndr_short();
				Uuid.TimeHiAndVersion = (short)src.Dec_ndr_short();
				Uuid.ClockSeqHiAndReserved = unchecked((byte)src.Dec_ndr_small());
				Uuid.ClockSeqLow = unchecked((byte)src.Dec_ndr_small());
				int uuidNodes = 6;
				int uuidNodei = src.Index;
				src.Advance(1 * uuidNodes);
				if (Uuid.Node == null)
				{
					if (uuidNodes < 0 || uuidNodes > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					Uuid.Node = new byte[uuidNodes];
				}
				src = src.Derive(uuidNodei);
				for (int i = 0; i < uuidNodes; i++)
				{
					Uuid.Node[i] = unchecked((byte)src.Dec_ndr_small());
				}
			}
		}

		public class Unicode_string : NdrObject
		{
			public short Length;

			public short MaximumLength;

			public short[] Buffer;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(Length);
				dst.Enc_ndr_short(MaximumLength);
				dst.Enc_ndr_referent(Buffer, 1);
				if (Buffer != null)
				{
					dst = dst.Deferred;
					int bufferl = Length / 2;
					int buffers = MaximumLength / 2;
					dst.Enc_ndr_long(buffers);
					dst.Enc_ndr_long(0);
					dst.Enc_ndr_long(bufferl);
					int bufferi = dst.Index;
					dst.Advance(2 * bufferl);
					dst = dst.Derive(bufferi);
					for (int i = 0; i < bufferl; i++)
					{
						dst.Enc_ndr_short(Buffer[i]);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Length = (short)src.Dec_ndr_short();
				MaximumLength = (short)src.Dec_ndr_short();
				int bufferp = src.Dec_ndr_long();
				if (bufferp != 0)
				{
					src = src.Deferred;
					int buffers = src.Dec_ndr_long();
					src.Dec_ndr_long();
					int bufferl = src.Dec_ndr_long();
					int bufferi = src.Index;
					src.Advance(2 * bufferl);
					if (Buffer == null)
					{
						if (buffers < 0 || buffers > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Buffer = new short[buffers];
					}
					src = src.Derive(bufferi);
					for (int i = 0; i < bufferl; i++)
					{
						Buffer[i] = (short)src.Dec_ndr_short();
					}
				}
			}
		}

		public class SidT : NdrObject
		{
			public byte Revision;

			public byte SubAuthorityCount;

			public byte[] IdentifierAuthority;

			public int[] SubAuthority;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				int subAuthoritys = SubAuthorityCount;
				dst.Enc_ndr_long(subAuthoritys);
				dst.Enc_ndr_small(Revision);
				dst.Enc_ndr_small(SubAuthorityCount);
				int identifierAuthoritys = 6;
				int identifierAuthorityi = dst.Index;
				dst.Advance(1 * identifierAuthoritys);
				int subAuthorityi = dst.Index;
				dst.Advance(4 * subAuthoritys);
				dst = dst.Derive(identifierAuthorityi);
				for (int i = 0; i < identifierAuthoritys; i++)
				{
					dst.Enc_ndr_small(IdentifierAuthority[i]);
				}
				dst = dst.Derive(subAuthorityi);
				for (int i1 = 0; i1 < subAuthoritys; i1++)
				{
					dst.Enc_ndr_long(SubAuthority[i1]);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int subAuthoritys = src.Dec_ndr_long();
				Revision = unchecked((byte)src.Dec_ndr_small());
				SubAuthorityCount = unchecked((byte)src.Dec_ndr_small());
				int identifierAuthoritys = 6;
				int identifierAuthorityi = src.Index;
				src.Advance(1 * identifierAuthoritys);
				int subAuthorityi = src.Index;
				src.Advance(4 * subAuthoritys);
				if (IdentifierAuthority == null)
				{
					if (identifierAuthoritys < 0 || identifierAuthoritys > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					IdentifierAuthority = new byte[identifierAuthoritys];
				}
				src = src.Derive(identifierAuthorityi);
				for (int i = 0; i < identifierAuthoritys; i++)
				{
					IdentifierAuthority[i] = unchecked((byte)src.Dec_ndr_small());
				}
				if (SubAuthority == null)
				{
					if (subAuthoritys < 0 || subAuthoritys > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					SubAuthority = new int[subAuthoritys];
				}
				src = src.Derive(subAuthorityi);
				for (int i1 = 0; i1 < subAuthoritys; i1++)
				{
					SubAuthority[i1] = src.Dec_ndr_long();
				}
			}
		}
	}
}
