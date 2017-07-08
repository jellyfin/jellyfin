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

namespace SharpCifs.Dcerpc.Msrpc
{
	public class Samr
	{
		public static string GetSyntax()
		{
			return "12345778-1234-abcd-ef00-0123456789ac:1.0";
		}

		public const int AcbDisabled = 1;

		public const int AcbHomdirreq = 2;

		public const int AcbPwnotreq = 4;

		public const int AcbTempdup = 8;

		public const int AcbNormal = 16;

		public const int AcbMns = 32;

		public const int AcbDomtrust = 64;

		public const int AcbWstrust = 128;

		public const int AcbSvrtrust = 256;

		public const int AcbPwnoexp = 512;

		public const int AcbAutolock = 1024;

		public const int AcbEncTxtPwdAllowed = 2048;

		public const int AcbSmartcardRequired = 4096;

		public const int AcbTrustedForDelegation = 8192;

		public const int AcbNotDelegated = 16384;

		public const int AcbUseDesKeyOnly = 32768;

		public const int AcbDontRequirePreauth = 65536;

		public class SamrCloseHandle : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x01);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public SamrCloseHandle(Rpc.PolicyHandle handle)
			{
				this.Handle = handle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				Handle.Encode(dst);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrConnect2 : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x39);
			}

			public int Retval;

			public string SystemName;

			public int AccessMask;

			public Rpc.PolicyHandle Handle;

			public SamrConnect2(string systemName, int accessMask, Rpc.PolicyHandle handle
				)
			{
				this.SystemName = systemName;
				this.AccessMask = accessMask;
				this.Handle = handle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(SystemName, 1);
				if (SystemName != null)
				{
					dst.Enc_ndr_string(SystemName);
				}
				dst.Enc_ndr_long(AccessMask);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				Handle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrConnect4 : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x3e);
			}

			public int Retval;

			public string SystemName;

			public int Unknown;

			public int AccessMask;

			public Rpc.PolicyHandle Handle;

			public SamrConnect4(string systemName, int unknown, int accessMask, Rpc.PolicyHandle
				 handle)
			{
				this.SystemName = systemName;
				this.Unknown = unknown;
				this.AccessMask = accessMask;
				this.Handle = handle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(SystemName, 1);
				if (SystemName != null)
				{
					dst.Enc_ndr_string(SystemName);
				}
				dst.Enc_ndr_long(Unknown);
				dst.Enc_ndr_long(AccessMask);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				Handle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrOpenDomain : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x07);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public int AccessMask;

			public Rpc.SidT Sid;

			public Rpc.PolicyHandle DomainHandle;

			public SamrOpenDomain(Rpc.PolicyHandle handle, int accessMask, Rpc.SidT sid, Rpc.PolicyHandle
				 domainHandle)
			{
				this.Handle = handle;
				this.AccessMask = accessMask;
				this.Sid = sid;
				this.DomainHandle = domainHandle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				Handle.Encode(dst);
				dst.Enc_ndr_long(AccessMask);
				Sid.Encode(dst);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				DomainHandle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrSamEntry : NdrObject
		{
			public int Idx;

			public Rpc.Unicode_string Name;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Idx);
				dst.Enc_ndr_short(Name.Length);
				dst.Enc_ndr_short(Name.MaximumLength);
				dst.Enc_ndr_referent(Name.Buffer, 1);
				if (Name.Buffer != null)
				{
					dst = dst.Deferred;
					int nameBufferl = Name.Length / 2;
					int nameBuffers = Name.MaximumLength / 2;
					dst.Enc_ndr_long(nameBuffers);
					dst.Enc_ndr_long(0);
					dst.Enc_ndr_long(nameBufferl);
					int nameBufferi = dst.Index;
					dst.Advance(2 * nameBufferl);
					dst = dst.Derive(nameBufferi);
					for (int i = 0; i < nameBufferl; i++)
					{
						dst.Enc_ndr_short(Name.Buffer[i]);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Idx = src.Dec_ndr_long();
				src.Align(4);
				if (Name == null)
				{
					Name = new Rpc.Unicode_string();
				}
				Name.Length = (short)src.Dec_ndr_short();
				Name.MaximumLength = (short)src.Dec_ndr_short();
				int nameBufferp = src.Dec_ndr_long();
				if (nameBufferp != 0)
				{
					src = src.Deferred;
					int nameBuffers = src.Dec_ndr_long();
					src.Dec_ndr_long();
					int nameBufferl = src.Dec_ndr_long();
					int nameBufferi = src.Index;
					src.Advance(2 * nameBufferl);
					if (Name.Buffer == null)
					{
						if (nameBuffers < 0 || nameBuffers > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Name.Buffer = new short[nameBuffers];
					}
					src = src.Derive(nameBufferi);
					for (int i = 0; i < nameBufferl; i++)
					{
						Name.Buffer[i] = (short)src.Dec_ndr_short();
					}
				}
			}
		}

		public class SamrSamArray : NdrObject
		{
			public int Count;

			public SamrSamEntry[] Entries;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Entries, 1);
				if (Entries != null)
				{
					dst = dst.Deferred;
					int entriess = Count;
					dst.Enc_ndr_long(entriess);
					int entriesi = dst.Index;
					dst.Advance(12 * entriess);
					dst = dst.Derive(entriesi);
					for (int i = 0; i < entriess; i++)
					{
						Entries[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int entriesp = src.Dec_ndr_long();
				if (entriesp != 0)
				{
					src = src.Deferred;
					int entriess = src.Dec_ndr_long();
					int entriesi = src.Index;
					src.Advance(12 * entriess);
					if (Entries == null)
					{
						if (entriess < 0 || entriess > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Entries = new SamrSamEntry[entriess];
					}
					src = src.Derive(entriesi);
					for (int i = 0; i < entriess; i++)
					{
						if (Entries[i] == null)
						{
							Entries[i] = new SamrSamEntry();
						}
						Entries[i].Decode(src);
					}
				}
			}
		}

		public class SamrEnumerateAliasesInDomain : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x0f);
			}

			public int Retval;

			public Rpc.PolicyHandle DomainHandle;

			public int ResumeHandle;

			public int AcctFlags;

			public SamrSamArray Sam;

			public int NumEntries;

			public SamrEnumerateAliasesInDomain(Rpc.PolicyHandle domainHandle, int resumeHandle
				, int acctFlags, SamrSamArray sam, int numEntries)
			{
				this.DomainHandle = domainHandle;
				this.ResumeHandle = resumeHandle;
				this.AcctFlags = acctFlags;
				this.Sam = sam;
				this.NumEntries = numEntries;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				DomainHandle.Encode(dst);
				dst.Enc_ndr_long(ResumeHandle);
				dst.Enc_ndr_long(AcctFlags);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				ResumeHandle = src.Dec_ndr_long();
				int samp = src.Dec_ndr_long();
				if (samp != 0)
				{
					if (Sam == null)
					{
						Sam = new SamrSamArray();
					}
					Sam.Decode(src);
				}
				NumEntries = src.Dec_ndr_long();
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrOpenAlias : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x1b);
			}

			public int Retval;

			public Rpc.PolicyHandle DomainHandle;

			public int AccessMask;

			public int Rid;

			public Rpc.PolicyHandle AliasHandle;

			public SamrOpenAlias(Rpc.PolicyHandle domainHandle, int accessMask, int rid, Rpc.PolicyHandle
				 aliasHandle)
			{
				this.DomainHandle = domainHandle;
				this.AccessMask = accessMask;
				this.Rid = rid;
				this.AliasHandle = aliasHandle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				DomainHandle.Encode(dst);
				dst.Enc_ndr_long(AccessMask);
				dst.Enc_ndr_long(Rid);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				AliasHandle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class SamrGetMembersInAlias : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x21);
			}

			public int Retval;

			public Rpc.PolicyHandle AliasHandle;

			public Lsarpc.LsarSidArray Sids;

			public SamrGetMembersInAlias(Rpc.PolicyHandle aliasHandle, Lsarpc.LsarSidArray 
				sids)
			{
				this.AliasHandle = aliasHandle;
				this.Sids = sids;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				AliasHandle.Encode(dst);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				Sids.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public const int SeGroupMandatory = 1;

		public const int SeGroupEnabledByDefault = 2;

		public const int SeGroupEnabled = 4;

		public const int SeGroupOwner = 8;

		public const int SeGroupUseForDenyOnly = 16;

		public const int SeGroupResource = 536870912;

		public const int SeGroupLogonId = -1073741824;

		public class SamrRidWithAttribute : NdrObject
		{
			public int Rid;

			public int Attributes;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Rid);
				dst.Enc_ndr_long(Attributes);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Rid = src.Dec_ndr_long();
				Attributes = src.Dec_ndr_long();
			}
		}

		public class SamrRidWithAttributeArray : NdrObject
		{
			public int Count;

			public SamrRidWithAttribute[] Rids;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Rids, 1);
				if (Rids != null)
				{
					dst = dst.Deferred;
					int ridss = Count;
					dst.Enc_ndr_long(ridss);
					int ridsi = dst.Index;
					dst.Advance(8 * ridss);
					dst = dst.Derive(ridsi);
					for (int i = 0; i < ridss; i++)
					{
						Rids[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int ridsp = src.Dec_ndr_long();
				if (ridsp != 0)
				{
					src = src.Deferred;
					int ridss = src.Dec_ndr_long();
					int ridsi = src.Index;
					src.Advance(8 * ridss);
					if (Rids == null)
					{
						if (ridss < 0 || ridss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Rids = new SamrRidWithAttribute[ridss];
					}
					src = src.Derive(ridsi);
					for (int i = 0; i < ridss; i++)
					{
						if (Rids[i] == null)
						{
							Rids[i] = new SamrRidWithAttribute();
						}
						Rids[i].Decode(src);
					}
				}
			}
		}
	}
}
