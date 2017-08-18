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
	public class Lsarpc
	{
		public static string GetSyntax()
		{
			return "12345778-1234-abcd-ef00-0123456789ab:0.0";
		}

		public class LsarQosInfo : NdrObject
		{
			public int Length;

			public short ImpersonationLevel;

			public byte ContextMode;

			public byte EffectiveOnly;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Length);
				dst.Enc_ndr_short(ImpersonationLevel);
				dst.Enc_ndr_small(ContextMode);
				dst.Enc_ndr_small(EffectiveOnly);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Length = src.Dec_ndr_long();
				ImpersonationLevel = (short)src.Dec_ndr_short();
				ContextMode = unchecked((byte)src.Dec_ndr_small());
				EffectiveOnly = unchecked((byte)src.Dec_ndr_small());
			}
		}

		public class LsarObjectAttributes : NdrObject
		{
			public int Length;

			public NdrSmall RootDirectory;

			public Rpc.Unicode_string ObjectName;

			public int Attributes;

			public int SecurityDescriptor;

			public LsarQosInfo SecurityQualityOfService;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Length);
				dst.Enc_ndr_referent(RootDirectory, 1);
				dst.Enc_ndr_referent(ObjectName, 1);
				dst.Enc_ndr_long(Attributes);
				dst.Enc_ndr_long(SecurityDescriptor);
				dst.Enc_ndr_referent(SecurityQualityOfService, 1);
				if (RootDirectory != null)
				{
					dst = dst.Deferred;
					RootDirectory.Encode(dst);
				}
				if (ObjectName != null)
				{
					dst = dst.Deferred;
					ObjectName.Encode(dst);
				}
				if (SecurityQualityOfService != null)
				{
					dst = dst.Deferred;
					SecurityQualityOfService.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Length = src.Dec_ndr_long();
				int rootDirectoryp = src.Dec_ndr_long();
				int objectNamep = src.Dec_ndr_long();
				Attributes = src.Dec_ndr_long();
				SecurityDescriptor = src.Dec_ndr_long();
				int securityQualityOfServicep = src.Dec_ndr_long();
				if (rootDirectoryp != 0)
				{
					src = src.Deferred;
					RootDirectory.Decode(src);
				}
				if (objectNamep != 0)
				{
					if (ObjectName == null)
					{
						ObjectName = new Rpc.Unicode_string();
					}
					src = src.Deferred;
					ObjectName.Decode(src);
				}
				if (securityQualityOfServicep != 0)
				{
					if (SecurityQualityOfService == null)
					{
						SecurityQualityOfService = new LsarQosInfo();
					}
					src = src.Deferred;
					SecurityQualityOfService.Decode(src);
				}
			}
		}

		public class LsarDomainInfo : NdrObject
		{
			public Rpc.Unicode_string Name;

			public Rpc.SidT Sid;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(Name.Length);
				dst.Enc_ndr_short(Name.MaximumLength);
				dst.Enc_ndr_referent(Name.Buffer, 1);
				dst.Enc_ndr_referent(Sid, 1);
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
				if (Sid != null)
				{
					dst = dst.Deferred;
					Sid.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				src.Align(4);
				if (Name == null)
				{
					Name = new Rpc.Unicode_string();
				}
				Name.Length = (short)src.Dec_ndr_short();
				Name.MaximumLength = (short)src.Dec_ndr_short();
				int nameBufferp = src.Dec_ndr_long();
				int sidp = src.Dec_ndr_long();
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
				if (sidp != 0)
				{
					if (Sid == null)
					{
						Sid = new Rpc.SidT();
					}
					src = src.Deferred;
					Sid.Decode(src);
				}
			}
		}

		public class LsarDnsDomainInfo : NdrObject
		{
			public Rpc.Unicode_string Name;

			public Rpc.Unicode_string DnsDomain;

			public Rpc.Unicode_string DnsForest;

			public Rpc.UuidT DomainGuid;

			public Rpc.SidT Sid;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(Name.Length);
				dst.Enc_ndr_short(Name.MaximumLength);
				dst.Enc_ndr_referent(Name.Buffer, 1);
				dst.Enc_ndr_short(DnsDomain.Length);
				dst.Enc_ndr_short(DnsDomain.MaximumLength);
				dst.Enc_ndr_referent(DnsDomain.Buffer, 1);
				dst.Enc_ndr_short(DnsForest.Length);
				dst.Enc_ndr_short(DnsForest.MaximumLength);
				dst.Enc_ndr_referent(DnsForest.Buffer, 1);
				dst.Enc_ndr_long(DomainGuid.TimeLow);
				dst.Enc_ndr_short(DomainGuid.TimeMid);
				dst.Enc_ndr_short(DomainGuid.TimeHiAndVersion);
				dst.Enc_ndr_small(DomainGuid.ClockSeqHiAndReserved);
				dst.Enc_ndr_small(DomainGuid.ClockSeqLow);
				int domainGuidNodes = 6;
				int domainGuidNodei = dst.Index;
				dst.Advance(1 * domainGuidNodes);
				dst.Enc_ndr_referent(Sid, 1);
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
				if (DnsDomain.Buffer != null)
				{
					dst = dst.Deferred;
					int dnsDomainBufferl = DnsDomain.Length / 2;
					int dnsDomainBuffers = DnsDomain.MaximumLength / 2;
					dst.Enc_ndr_long(dnsDomainBuffers);
					dst.Enc_ndr_long(0);
					dst.Enc_ndr_long(dnsDomainBufferl);
					int dnsDomainBufferi = dst.Index;
					dst.Advance(2 * dnsDomainBufferl);
					dst = dst.Derive(dnsDomainBufferi);
					for (int i = 0; i < dnsDomainBufferl; i++)
					{
						dst.Enc_ndr_short(DnsDomain.Buffer[i]);
					}
				}
				if (DnsForest.Buffer != null)
				{
					dst = dst.Deferred;
					int dnsForestBufferl = DnsForest.Length / 2;
					int dnsForestBuffers = DnsForest.MaximumLength / 2;
					dst.Enc_ndr_long(dnsForestBuffers);
					dst.Enc_ndr_long(0);
					dst.Enc_ndr_long(dnsForestBufferl);
					int dnsForestBufferi = dst.Index;
					dst.Advance(2 * dnsForestBufferl);
					dst = dst.Derive(dnsForestBufferi);
					for (int i = 0; i < dnsForestBufferl; i++)
					{
						dst.Enc_ndr_short(DnsForest.Buffer[i]);
					}
				}
				dst = dst.Derive(domainGuidNodei);
				for (int i1 = 0; i1 < domainGuidNodes; i1++)
				{
					dst.Enc_ndr_small(DomainGuid.Node[i1]);
				}
				if (Sid != null)
				{
					dst = dst.Deferred;
					Sid.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				src.Align(4);
				if (Name == null)
				{
					Name = new Rpc.Unicode_string();
				}
				Name.Length = (short)src.Dec_ndr_short();
				Name.MaximumLength = (short)src.Dec_ndr_short();
				int nameBufferp = src.Dec_ndr_long();
				src.Align(4);
				if (DnsDomain == null)
				{
					DnsDomain = new Rpc.Unicode_string();
				}
				DnsDomain.Length = (short)src.Dec_ndr_short();
				DnsDomain.MaximumLength = (short)src.Dec_ndr_short();
				int dnsDomainBufferp = src.Dec_ndr_long();
				src.Align(4);
				if (DnsForest == null)
				{
					DnsForest = new Rpc.Unicode_string();
				}
				DnsForest.Length = (short)src.Dec_ndr_short();
				DnsForest.MaximumLength = (short)src.Dec_ndr_short();
				int dnsForestBufferp = src.Dec_ndr_long();
				src.Align(4);
				if (DomainGuid == null)
				{
					DomainGuid = new Rpc.UuidT();
				}
				DomainGuid.TimeLow = src.Dec_ndr_long();
				DomainGuid.TimeMid = (short)src.Dec_ndr_short();
				DomainGuid.TimeHiAndVersion = (short)src.Dec_ndr_short();
				DomainGuid.ClockSeqHiAndReserved = unchecked((byte)src.Dec_ndr_small());
				DomainGuid.ClockSeqLow = unchecked((byte)src.Dec_ndr_small());
				int domainGuidNodes = 6;
				int domainGuidNodei = src.Index;
				src.Advance(1 * domainGuidNodes);
				int sidp = src.Dec_ndr_long();
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
				if (dnsDomainBufferp != 0)
				{
					src = src.Deferred;
					int dnsDomainBuffers = src.Dec_ndr_long();
					src.Dec_ndr_long();
					int dnsDomainBufferl = src.Dec_ndr_long();
					int dnsDomainBufferi = src.Index;
					src.Advance(2 * dnsDomainBufferl);
					if (DnsDomain.Buffer == null)
					{
						if (dnsDomainBuffers < 0 || dnsDomainBuffers > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						DnsDomain.Buffer = new short[dnsDomainBuffers];
					}
					src = src.Derive(dnsDomainBufferi);
					for (int i = 0; i < dnsDomainBufferl; i++)
					{
						DnsDomain.Buffer[i] = (short)src.Dec_ndr_short();
					}
				}
				if (dnsForestBufferp != 0)
				{
					src = src.Deferred;
					int dnsForestBuffers = src.Dec_ndr_long();
					src.Dec_ndr_long();
					int dnsForestBufferl = src.Dec_ndr_long();
					int dnsForestBufferi = src.Index;
					src.Advance(2 * dnsForestBufferl);
					if (DnsForest.Buffer == null)
					{
						if (dnsForestBuffers < 0 || dnsForestBuffers > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						DnsForest.Buffer = new short[dnsForestBuffers];
					}
					src = src.Derive(dnsForestBufferi);
					for (int i = 0; i < dnsForestBufferl; i++)
					{
						DnsForest.Buffer[i] = (short)src.Dec_ndr_short();
					}
				}
				if (DomainGuid.Node == null)
				{
					if (domainGuidNodes < 0 || domainGuidNodes > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					DomainGuid.Node = new byte[domainGuidNodes];
				}
				src = src.Derive(domainGuidNodei);
				for (int i1 = 0; i1 < domainGuidNodes; i1++)
				{
					DomainGuid.Node[i1] = unchecked((byte)src.Dec_ndr_small());
				}
				if (sidp != 0)
				{
					if (Sid == null)
					{
						Sid = new Rpc.SidT();
					}
					src = src.Deferred;
					Sid.Decode(src);
				}
			}
		}

		public const int PolicyInfoAuditEvents = 2;

		public const int PolicyInfoPrimaryDomain = 3;

		public const int PolicyInfoAccountDomain = 5;

		public const int PolicyInfoServerRole = 6;

		public const int PolicyInfoModification = 9;

		public const int PolicyInfoDnsDomain = 12;

		public class LsarSidPtr : NdrObject
		{
			public Rpc.SidT Sid;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(Sid, 1);
				if (Sid != null)
				{
					dst = dst.Deferred;
					Sid.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int sidp = src.Dec_ndr_long();
				if (sidp != 0)
				{
					if (Sid == null)
					{
						Sid = new Rpc.SidT();
					}
					src = src.Deferred;
					Sid.Decode(src);
				}
			}
		}

		public class LsarSidArray : NdrObject
		{
			public int NumSids;

			public LsarSidPtr[] Sids;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(NumSids);
				dst.Enc_ndr_referent(Sids, 1);
				if (Sids != null)
				{
					dst = dst.Deferred;
					int sidss = NumSids;
					dst.Enc_ndr_long(sidss);
					int sidsi = dst.Index;
					dst.Advance(4 * sidss);
					dst = dst.Derive(sidsi);
					for (int i = 0; i < sidss; i++)
					{
						Sids[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				NumSids = src.Dec_ndr_long();
				int sidsp = src.Dec_ndr_long();
				if (sidsp != 0)
				{
					src = src.Deferred;
					int sidss = src.Dec_ndr_long();
					int sidsi = src.Index;
					src.Advance(4 * sidss);
					if (Sids == null)
					{
						if (sidss < 0 || sidss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Sids = new LsarSidPtr[sidss];
					}
					src = src.Derive(sidsi);
					for (int i = 0; i < sidss; i++)
					{
						if (Sids[i] == null)
						{
							Sids[i] = new LsarSidPtr();
						}
						Sids[i].Decode(src);
					}
				}
			}
		}

		public const int SidNameUseNone = 0;

		public const int SidNameUser = 1;

		public const int SidNameDomGrp = 2;

		public const int SidNameDomain = 3;

		public const int SidNameAlias = 4;

		public const int SidNameWknGrp = 5;

		public const int SidNameDeleted = 6;

		public const int SidNameInvalid = 7;

		public const int SidNameUnknown = 8;

		public class LsarTranslatedSid : NdrObject
		{
			public int SidType;

			public int Rid;

			public int SidIndex;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(SidType);
				dst.Enc_ndr_long(Rid);
				dst.Enc_ndr_long(SidIndex);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				SidType = src.Dec_ndr_short();
				Rid = src.Dec_ndr_long();
				SidIndex = src.Dec_ndr_long();
			}
		}

		public class LsarTransSidArray : NdrObject
		{
			public int Count;

			public LsarTranslatedSid[] Sids;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Sids, 1);
				if (Sids != null)
				{
					dst = dst.Deferred;
					int sidss = Count;
					dst.Enc_ndr_long(sidss);
					int sidsi = dst.Index;
					dst.Advance(12 * sidss);
					dst = dst.Derive(sidsi);
					for (int i = 0; i < sidss; i++)
					{
						Sids[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int sidsp = src.Dec_ndr_long();
				if (sidsp != 0)
				{
					src = src.Deferred;
					int sidss = src.Dec_ndr_long();
					int sidsi = src.Index;
					src.Advance(12 * sidss);
					if (Sids == null)
					{
						if (sidss < 0 || sidss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Sids = new LsarTranslatedSid[sidss];
					}
					src = src.Derive(sidsi);
					for (int i = 0; i < sidss; i++)
					{
						if (Sids[i] == null)
						{
							Sids[i] = new LsarTranslatedSid();
						}
						Sids[i].Decode(src);
					}
				}
			}
		}

		public class LsarTrustInformation : NdrObject
		{
			public Rpc.Unicode_string Name;

			public Rpc.SidT Sid;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(Name.Length);
				dst.Enc_ndr_short(Name.MaximumLength);
				dst.Enc_ndr_referent(Name.Buffer, 1);
				dst.Enc_ndr_referent(Sid, 1);
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
				if (Sid != null)
				{
					dst = dst.Deferred;
					Sid.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				src.Align(4);
				if (Name == null)
				{
					Name = new Rpc.Unicode_string();
				}
				Name.Length = (short)src.Dec_ndr_short();
				Name.MaximumLength = (short)src.Dec_ndr_short();
				int nameBufferp = src.Dec_ndr_long();
				int sidp = src.Dec_ndr_long();
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
				if (sidp != 0)
				{
					if (Sid == null)
					{
						Sid = new Rpc.SidT();
					}
					src = src.Deferred;
					Sid.Decode(src);
				}
			}
		}

		public class LsarRefDomainList : NdrObject
		{
			public int Count;

			public LsarTrustInformation[] Domains;

			public int MaxCount;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Domains, 1);
				dst.Enc_ndr_long(MaxCount);
				if (Domains != null)
				{
					dst = dst.Deferred;
					int domainss = Count;
					dst.Enc_ndr_long(domainss);
					int domainsi = dst.Index;
					dst.Advance(12 * domainss);
					dst = dst.Derive(domainsi);
					for (int i = 0; i < domainss; i++)
					{
						Domains[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int domainsp = src.Dec_ndr_long();
				MaxCount = src.Dec_ndr_long();
				if (domainsp != 0)
				{
					src = src.Deferred;
					int domainss = src.Dec_ndr_long();
					int domainsi = src.Index;
					src.Advance(12 * domainss);
					if (Domains == null)
					{
						if (domainss < 0 || domainss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Domains = new LsarTrustInformation[domainss];
					}
					src = src.Derive(domainsi);
					for (int i = 0; i < domainss; i++)
					{
						if (Domains[i] == null)
						{
							Domains[i] = new LsarTrustInformation();
						}
						Domains[i].Decode(src);
					}
				}
			}
		}

		public class LsarTranslatedName : NdrObject
		{
			public short SidType;

			public Rpc.Unicode_string Name;

			public int SidIndex;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_short(SidType);
				dst.Enc_ndr_short(Name.Length);
				dst.Enc_ndr_short(Name.MaximumLength);
				dst.Enc_ndr_referent(Name.Buffer, 1);
				dst.Enc_ndr_long(SidIndex);
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
				SidType = (short)src.Dec_ndr_short();
				src.Align(4);
				if (Name == null)
				{
					Name = new Rpc.Unicode_string();
				}
				Name.Length = (short)src.Dec_ndr_short();
				Name.MaximumLength = (short)src.Dec_ndr_short();
				int nameBufferp = src.Dec_ndr_long();
				SidIndex = src.Dec_ndr_long();
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

		public class LsarTransNameArray : NdrObject
		{
			public int Count;

			public LsarTranslatedName[] Names;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Names, 1);
				if (Names != null)
				{
					dst = dst.Deferred;
					int namess = Count;
					dst.Enc_ndr_long(namess);
					int namesi = dst.Index;
					dst.Advance(16 * namess);
					dst = dst.Derive(namesi);
					for (int i = 0; i < namess; i++)
					{
						Names[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int namesp = src.Dec_ndr_long();
				if (namesp != 0)
				{
					src = src.Deferred;
					int namess = src.Dec_ndr_long();
					int namesi = src.Index;
					src.Advance(16 * namess);
					if (Names == null)
					{
						if (namess < 0 || namess > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Names = new LsarTranslatedName[namess];
					}
					src = src.Derive(namesi);
					for (int i = 0; i < namess; i++)
					{
						if (Names[i] == null)
						{
							Names[i] = new LsarTranslatedName();
						}
						Names[i].Decode(src);
					}
				}
			}
		}

		public class LsarClose : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x00);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public LsarClose(Rpc.PolicyHandle handle)
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
				Handle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class LsarQueryInformationPolicy : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x07);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public short Level;

			public NdrObject Info;

			public LsarQueryInformationPolicy(Rpc.PolicyHandle handle, short level, NdrObject
				 info)
			{
				this.Handle = handle;
				this.Level = level;
				this.Info = info;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				Handle.Encode(dst);
				dst.Enc_ndr_short(Level);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					src.Dec_ndr_short();
					Info.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}

		public class LsarLookupSids : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x0f);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public LsarSidArray Sids;

			public LsarRefDomainList Domains;

			public LsarTransNameArray Names;

			public short Level;

			public int Count;

			public LsarLookupSids(Rpc.PolicyHandle handle, LsarSidArray sids, LsarRefDomainList
				 domains, LsarTransNameArray names, short level, int count)
			{
				this.Handle = handle;
				this.Sids = sids;
				this.Domains = domains;
				this.Names = names;
				this.Level = level;
				this.Count = count;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				Handle.Encode(dst);
				Sids.Encode(dst);
				Names.Encode(dst);
				dst.Enc_ndr_short(Level);
				dst.Enc_ndr_long(Count);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				int domainsp = src.Dec_ndr_long();
				if (domainsp != 0)
				{
					if (Domains == null)
					{
						Domains = new LsarRefDomainList();
					}
					Domains.Decode(src);
				}
				Names.Decode(src);
				Count = src.Dec_ndr_long();
				Retval = src.Dec_ndr_long();
			}
		}

		public class LsarOpenPolicy2 : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x2c);
			}

			public int Retval;

			public string SystemName;

			public LsarObjectAttributes ObjectAttributes;

			public int DesiredAccess;

			public Rpc.PolicyHandle PolicyHandle;

			public LsarOpenPolicy2(string systemName, LsarObjectAttributes objectAttributes
				, int desiredAccess, Rpc.PolicyHandle policyHandle)
			{
				this.SystemName = systemName;
				this.ObjectAttributes = objectAttributes;
				this.DesiredAccess = desiredAccess;
				this.PolicyHandle = policyHandle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(SystemName, 1);
				if (SystemName != null)
				{
					dst.Enc_ndr_string(SystemName);
				}
				ObjectAttributes.Encode(dst);
				dst.Enc_ndr_long(DesiredAccess);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				PolicyHandle.Decode(src);
				Retval = src.Dec_ndr_long();
			}
		}

		public class LsarQueryInformationPolicy2 : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x2e);
			}

			public int Retval;

			public Rpc.PolicyHandle Handle;

			public short Level;

			public NdrObject Info;

			public LsarQueryInformationPolicy2(Rpc.PolicyHandle handle, short level, NdrObject
				 info)
			{
				this.Handle = handle;
				this.Level = level;
				this.Info = info;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				Handle.Encode(dst);
				dst.Enc_ndr_short(Level);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					src.Dec_ndr_short();
					Info.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}
	}
}
