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
	public class Srvsvc
	{
		public static string GetSyntax()
		{
			return "4b324fc8-1670-01d3-1278-5a47bf6ee188:3.0";
		}

		public class ShareInfo0 : NdrObject
		{
			public string Netname;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(Netname, 1);
				if (Netname != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Netname);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int netnamep = src.Dec_ndr_long();
				if (netnamep != 0)
				{
					src = src.Deferred;
					Netname = src.Dec_ndr_string();
				}
			}
		}

		public class ShareInfoCtr0 : NdrObject
		{
			public int Count;

			public ShareInfo0[] Array;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Array, 1);
				if (Array != null)
				{
					dst = dst.Deferred;
					int arrays = Count;
					dst.Enc_ndr_long(arrays);
					int arrayi = dst.Index;
					dst.Advance(4 * arrays);
					dst = dst.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						Array[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int arrayp = src.Dec_ndr_long();
				if (arrayp != 0)
				{
					src = src.Deferred;
					int arrays = src.Dec_ndr_long();
					int arrayi = src.Index;
					src.Advance(4 * arrays);
					if (Array == null)
					{
						if (arrays < 0 || arrays > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Array = new ShareInfo0[arrays];
					}
					src = src.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						if (Array[i] == null)
						{
							Array[i] = new ShareInfo0();
						}
						Array[i].Decode(src);
					}
				}
			}
		}

		public class ShareInfo1 : NdrObject
		{
			public string Netname;

			public int Type;

			public string Remark;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(Netname, 1);
				dst.Enc_ndr_long(Type);
				dst.Enc_ndr_referent(Remark, 1);
				if (Netname != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Netname);
				}
				if (Remark != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Remark);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int netnamep = src.Dec_ndr_long();
				Type = src.Dec_ndr_long();
				int remarkp = src.Dec_ndr_long();
				if (netnamep != 0)
				{
					src = src.Deferred;
					Netname = src.Dec_ndr_string();
				}
				if (remarkp != 0)
				{
					src = src.Deferred;
					Remark = src.Dec_ndr_string();
				}
			}
		}

		public class ShareInfoCtr1 : NdrObject
		{
			public int Count;

			public ShareInfo1[] Array;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Array, 1);
				if (Array != null)
				{
					dst = dst.Deferred;
					int arrays = Count;
					dst.Enc_ndr_long(arrays);
					int arrayi = dst.Index;
					dst.Advance(12 * arrays);
					dst = dst.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						Array[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int arrayp = src.Dec_ndr_long();
				if (arrayp != 0)
				{
					src = src.Deferred;
					int arrays = src.Dec_ndr_long();
					int arrayi = src.Index;
					src.Advance(12 * arrays);
					if (Array == null)
					{
						if (arrays < 0 || arrays > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Array = new ShareInfo1[arrays];
					}
					src = src.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						if (Array[i] == null)
						{
							Array[i] = new ShareInfo1();
						}
						Array[i].Decode(src);
					}
				}
			}
		}

		public class ShareInfo502 : NdrObject
		{
			public string Netname;

			public int Type;

			public string Remark;

			public int Permissions;

			public int MaxUses;

			public int CurrentUses;

			public string Path;

			public string Password;

			public int SdSize;

			public byte[] SecurityDescriptor;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(Netname, 1);
				dst.Enc_ndr_long(Type);
				dst.Enc_ndr_referent(Remark, 1);
				dst.Enc_ndr_long(Permissions);
				dst.Enc_ndr_long(MaxUses);
				dst.Enc_ndr_long(CurrentUses);
				dst.Enc_ndr_referent(Path, 1);
				dst.Enc_ndr_referent(Password, 1);
				dst.Enc_ndr_long(SdSize);
				dst.Enc_ndr_referent(SecurityDescriptor, 1);
				if (Netname != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Netname);
				}
				if (Remark != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Remark);
				}
				if (Path != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Path);
				}
				if (Password != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Password);
				}
				if (SecurityDescriptor != null)
				{
					dst = dst.Deferred;
					int securityDescriptors = SdSize;
					dst.Enc_ndr_long(securityDescriptors);
					int securityDescriptori = dst.Index;
					dst.Advance(1 * securityDescriptors);
					dst = dst.Derive(securityDescriptori);
					for (int i = 0; i < securityDescriptors; i++)
					{
						dst.Enc_ndr_small(SecurityDescriptor[i]);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int netnamep = src.Dec_ndr_long();
				Type = src.Dec_ndr_long();
				int remarkp = src.Dec_ndr_long();
				Permissions = src.Dec_ndr_long();
				MaxUses = src.Dec_ndr_long();
				CurrentUses = src.Dec_ndr_long();
				int pathp = src.Dec_ndr_long();
				int passwordp = src.Dec_ndr_long();
				SdSize = src.Dec_ndr_long();
				int securityDescriptorp = src.Dec_ndr_long();
				if (netnamep != 0)
				{
					src = src.Deferred;
					Netname = src.Dec_ndr_string();
				}
				if (remarkp != 0)
				{
					src = src.Deferred;
					Remark = src.Dec_ndr_string();
				}
				if (pathp != 0)
				{
					src = src.Deferred;
					Path = src.Dec_ndr_string();
				}
				if (passwordp != 0)
				{
					src = src.Deferred;
					Password = src.Dec_ndr_string();
				}
				if (securityDescriptorp != 0)
				{
					src = src.Deferred;
					int securityDescriptors = src.Dec_ndr_long();
					int securityDescriptori = src.Index;
					src.Advance(1 * securityDescriptors);
					if (SecurityDescriptor == null)
					{
						if (securityDescriptors < 0 || securityDescriptors > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						SecurityDescriptor = new byte[securityDescriptors];
					}
					src = src.Derive(securityDescriptori);
					for (int i = 0; i < securityDescriptors; i++)
					{
						SecurityDescriptor[i] = unchecked((byte)src.Dec_ndr_small());
					}
				}
			}
		}

		public class ShareInfoCtr502 : NdrObject
		{
			public int Count;

			public ShareInfo502[] Array;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(Array, 1);
				if (Array != null)
				{
					dst = dst.Deferred;
					int arrays = Count;
					dst.Enc_ndr_long(arrays);
					int arrayi = dst.Index;
					dst.Advance(40 * arrays);
					dst = dst.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						Array[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int arrayp = src.Dec_ndr_long();
				if (arrayp != 0)
				{
					src = src.Deferred;
					int arrays = src.Dec_ndr_long();
					int arrayi = src.Index;
					src.Advance(40 * arrays);
					if (Array == null)
					{
						if (arrays < 0 || arrays > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Array = new ShareInfo502[arrays];
					}
					src = src.Derive(arrayi);
					for (int i = 0; i < arrays; i++)
					{
						if (Array[i] == null)
						{
							Array[i] = new ShareInfo502();
						}
						Array[i].Decode(src);
					}
				}
			}
		}

		public class ShareEnumAll : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x0f);
			}

			public int Retval;

			public string Servername;

			public int Level;

			public NdrObject Info;

			public int Prefmaxlen;

			public int Totalentries;

			public int ResumeHandle;

			public ShareEnumAll(string servername, int level, NdrObject info, int prefmaxlen, 
				int totalentries, int resumeHandle)
			{
				this.Servername = servername;
				this.Level = level;
				this.Info = info;
				this.Prefmaxlen = prefmaxlen;
				this.Totalentries = totalentries;
				this.ResumeHandle = resumeHandle;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(Servername, 1);
				if (Servername != null)
				{
					dst.Enc_ndr_string(Servername);
				}
				dst.Enc_ndr_long(Level);
				int descr = Level;
				dst.Enc_ndr_long(descr);
				dst.Enc_ndr_referent(Info, 1);
				if (Info != null)
				{
					dst = dst.Deferred;
					Info.Encode(dst);
				}
				dst.Enc_ndr_long(Prefmaxlen);
				dst.Enc_ndr_long(ResumeHandle);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				Level = src.Dec_ndr_long();
				src.Dec_ndr_long();
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					if (Info == null)
					{
						Info = new ShareInfoCtr0();
					}
					src = src.Deferred;
					Info.Decode(src);
				}
				Totalentries = src.Dec_ndr_long();
				ResumeHandle = src.Dec_ndr_long();
				Retval = src.Dec_ndr_long();
			}
		}

		public class ShareGetInfo : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x10);
			}

			public int Retval;

			public string Servername;

			public string Sharename;

			public int Level;

			public NdrObject Info;

			public ShareGetInfo(string servername, string sharename, int level, NdrObject info
				)
			{
				this.Servername = servername;
				this.Sharename = sharename;
				this.Level = level;
				this.Info = info;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(Servername, 1);
				if (Servername != null)
				{
					dst.Enc_ndr_string(Servername);
				}
				dst.Enc_ndr_string(Sharename);
				dst.Enc_ndr_long(Level);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				src.Dec_ndr_long();
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					if (Info == null)
					{
						Info = new ShareInfo0();
					}
					src = src.Deferred;
					Info.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}

		public class ServerInfo100 : NdrObject
		{
			public int PlatformId;

			public string Name;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(PlatformId);
				dst.Enc_ndr_referent(Name, 1);
				if (Name != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Name);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				PlatformId = src.Dec_ndr_long();
				int namep = src.Dec_ndr_long();
				if (namep != 0)
				{
					src = src.Deferred;
					Name = src.Dec_ndr_string();
				}
			}
		}

		public class ServerGetInfo : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x15);
			}

			public int Retval;

			public string Servername;

			public int Level;

			public NdrObject Info;

			public ServerGetInfo(string servername, int level, NdrObject info)
			{
				this.Servername = servername;
				this.Level = level;
				this.Info = info;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(Servername, 1);
				if (Servername != null)
				{
					dst.Enc_ndr_string(Servername);
				}
				dst.Enc_ndr_long(Level);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				src.Dec_ndr_long();
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					if (Info == null)
					{
						Info = new ServerInfo100();
					}
					src = src.Deferred;
					Info.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}

		public class TimeOfDayInfo : NdrObject
		{
			public int Elapsedt;

			public int Msecs;

			public int Hours;

			public int Mins;

			public int Secs;

			public int Hunds;

			public int Timezone;

			public int Tinterval;

			public int Day;

			public int Month;

			public int Year;

			public int Weekday;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Elapsedt);
				dst.Enc_ndr_long(Msecs);
				dst.Enc_ndr_long(Hours);
				dst.Enc_ndr_long(Mins);
				dst.Enc_ndr_long(Secs);
				dst.Enc_ndr_long(Hunds);
				dst.Enc_ndr_long(Timezone);
				dst.Enc_ndr_long(Tinterval);
				dst.Enc_ndr_long(Day);
				dst.Enc_ndr_long(Month);
				dst.Enc_ndr_long(Year);
				dst.Enc_ndr_long(Weekday);
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Elapsedt = src.Dec_ndr_long();
				Msecs = src.Dec_ndr_long();
				Hours = src.Dec_ndr_long();
				Mins = src.Dec_ndr_long();
				Secs = src.Dec_ndr_long();
				Hunds = src.Dec_ndr_long();
				Timezone = src.Dec_ndr_long();
				Tinterval = src.Dec_ndr_long();
				Day = src.Dec_ndr_long();
				Month = src.Dec_ndr_long();
				Year = src.Dec_ndr_long();
				Weekday = src.Dec_ndr_long();
			}
		}

		public class RemoteTod : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x1c);
			}

			public int Retval;

			public string Servername;

			public TimeOfDayInfo Info;

			public RemoteTod(string servername, TimeOfDayInfo info)
			{
				this.Servername = servername;
				this.Info = info;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_referent(Servername, 1);
				if (Servername != null)
				{
					dst.Enc_ndr_string(Servername);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode_out(NdrBuffer src)
			{
				int infop = src.Dec_ndr_long();
				if (infop != 0)
				{
					if (Info == null)
					{
						Info = new TimeOfDayInfo();
					}
					Info.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}
	}
}
