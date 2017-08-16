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
	public class Netdfs
	{
		public static string GetSyntax()
		{
			return "4fc742e0-4a10-11cf-8273-00aa004ae673:3.0";
		}

		public const int DfsVolumeFlavorStandalone = unchecked(0x100);

		public const int DfsVolumeFlavorAdBlob = unchecked(0x200);

		public const int DfsStorageStateOffline = unchecked(0x0001);

		public const int DfsStorageStateOnline = unchecked(0x0002);

		public const int DfsStorageStateActive = unchecked(0x0004);

		public class DfsInfo1 : NdrObject
		{
			public string EntryPath;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(EntryPath, 1);
				if (EntryPath != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(EntryPath);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int entryPathp = src.Dec_ndr_long();
				if (entryPathp != 0)
				{
					src = src.Deferred;
					EntryPath = src.Dec_ndr_string();
				}
			}
		}

		public class DfsEnumArray1 : NdrObject
		{
			public int Count;

			public DfsInfo1[] S;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(S, 1);
				if (S != null)
				{
					dst = dst.Deferred;
					int ss = Count;
					dst.Enc_ndr_long(ss);
					int si = dst.Index;
					dst.Advance(4 * ss);
					dst = dst.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						S[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int sp = src.Dec_ndr_long();
				if (sp != 0)
				{
					src = src.Deferred;
					int ss = src.Dec_ndr_long();
					int si = src.Index;
					src.Advance(4 * ss);
					if (S == null)
					{
						if (ss < 0 || ss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						S = new DfsInfo1[ss];
					}
					src = src.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						if (S[i] == null)
						{
							S[i] = new DfsInfo1();
						}
						S[i].Decode(src);
					}
				}
			}
		}

		public class DfsStorageInfo : NdrObject
		{
			public int State;

			public string ServerName;

			public string ShareName;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(State);
				dst.Enc_ndr_referent(ServerName, 1);
				dst.Enc_ndr_referent(ShareName, 1);
				if (ServerName != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(ServerName);
				}
				if (ShareName != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(ShareName);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				State = src.Dec_ndr_long();
				int serverNamep = src.Dec_ndr_long();
				int shareNamep = src.Dec_ndr_long();
				if (serverNamep != 0)
				{
					src = src.Deferred;
					ServerName = src.Dec_ndr_string();
				}
				if (shareNamep != 0)
				{
					src = src.Deferred;
					ShareName = src.Dec_ndr_string();
				}
			}
		}

		public class DfsInfo3 : NdrObject
		{
			public string Path;

			public string Comment;

			public int State;

			public int NumStores;

			public DfsStorageInfo[] Stores;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(Path, 1);
				dst.Enc_ndr_referent(Comment, 1);
				dst.Enc_ndr_long(State);
				dst.Enc_ndr_long(NumStores);
				dst.Enc_ndr_referent(Stores, 1);
				if (Path != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Path);
				}
				if (Comment != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(Comment);
				}
				if (Stores != null)
				{
					dst = dst.Deferred;
					int storess = NumStores;
					dst.Enc_ndr_long(storess);
					int storesi = dst.Index;
					dst.Advance(12 * storess);
					dst = dst.Derive(storesi);
					for (int i = 0; i < storess; i++)
					{
						Stores[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int pathp = src.Dec_ndr_long();
				int commentp = src.Dec_ndr_long();
				State = src.Dec_ndr_long();
				NumStores = src.Dec_ndr_long();
				int storesp = src.Dec_ndr_long();
				if (pathp != 0)
				{
					src = src.Deferred;
					Path = src.Dec_ndr_string();
				}
				if (commentp != 0)
				{
					src = src.Deferred;
					Comment = src.Dec_ndr_string();
				}
				if (storesp != 0)
				{
					src = src.Deferred;
					int storess = src.Dec_ndr_long();
					int storesi = src.Index;
					src.Advance(12 * storess);
					if (Stores == null)
					{
						if (storess < 0 || storess > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						Stores = new DfsStorageInfo[storess];
					}
					src = src.Derive(storesi);
					for (int i = 0; i < storess; i++)
					{
						if (Stores[i] == null)
						{
							Stores[i] = new DfsStorageInfo();
						}
						Stores[i].Decode(src);
					}
				}
			}
		}

		public class DfsEnumArray3 : NdrObject
		{
			public int Count;

			public DfsInfo3[] S;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(S, 1);
				if (S != null)
				{
					dst = dst.Deferred;
					int ss = Count;
					dst.Enc_ndr_long(ss);
					int si = dst.Index;
					dst.Advance(20 * ss);
					dst = dst.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						S[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int sp = src.Dec_ndr_long();
				if (sp != 0)
				{
					src = src.Deferred;
					int ss = src.Dec_ndr_long();
					int si = src.Index;
					src.Advance(20 * ss);
					if (S == null)
					{
						if (ss < 0 || ss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						S = new DfsInfo3[ss];
					}
					src = src.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						if (S[i] == null)
						{
							S[i] = new DfsInfo3();
						}
						S[i].Decode(src);
					}
				}
			}
		}

		public class DfsInfo200 : NdrObject
		{
			public string DfsName;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_referent(DfsName, 1);
				if (DfsName != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(DfsName);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				int dfsNamep = src.Dec_ndr_long();
				if (dfsNamep != 0)
				{
					src = src.Deferred;
					DfsName = src.Dec_ndr_string();
				}
			}
		}

		public class DfsEnumArray200 : NdrObject
		{
			public int Count;

			public DfsInfo200[] S;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(S, 1);
				if (S != null)
				{
					dst = dst.Deferred;
					int ss = Count;
					dst.Enc_ndr_long(ss);
					int si = dst.Index;
					dst.Advance(4 * ss);
					dst = dst.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						S[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int sp = src.Dec_ndr_long();
				if (sp != 0)
				{
					src = src.Deferred;
					int ss = src.Dec_ndr_long();
					int si = src.Index;
					src.Advance(4 * ss);
					if (S == null)
					{
						if (ss < 0 || ss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						S = new DfsInfo200[ss];
					}
					src = src.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						if (S[i] == null)
						{
							S[i] = new DfsInfo200();
						}
						S[i].Decode(src);
					}
				}
			}
		}

		public class DfsInfo300 : NdrObject
		{
			public int Flags;

			public string DfsName;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Flags);
				dst.Enc_ndr_referent(DfsName, 1);
				if (DfsName != null)
				{
					dst = dst.Deferred;
					dst.Enc_ndr_string(DfsName);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Flags = src.Dec_ndr_long();
				int dfsNamep = src.Dec_ndr_long();
				if (dfsNamep != 0)
				{
					src = src.Deferred;
					DfsName = src.Dec_ndr_string();
				}
			}
		}

		public class DfsEnumArray300 : NdrObject
		{
			public int Count;

			public DfsInfo300[] S;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Count);
				dst.Enc_ndr_referent(S, 1);
				if (S != null)
				{
					dst = dst.Deferred;
					int ss = Count;
					dst.Enc_ndr_long(ss);
					int si = dst.Index;
					dst.Advance(8 * ss);
					dst = dst.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						S[i].Encode(dst);
					}
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Count = src.Dec_ndr_long();
				int sp = src.Dec_ndr_long();
				if (sp != 0)
				{
					src = src.Deferred;
					int ss = src.Dec_ndr_long();
					int si = src.Index;
					src.Advance(8 * ss);
					if (S == null)
					{
						if (ss < 0 || ss > unchecked(0xFFFF))
						{
							throw new NdrException(NdrException.InvalidConformance);
						}
						S = new DfsInfo300[ss];
					}
					src = src.Derive(si);
					for (int i = 0; i < ss; i++)
					{
						if (S[i] == null)
						{
							S[i] = new DfsInfo300();
						}
						S[i].Decode(src);
					}
				}
			}
		}

		public class DfsEnumStruct : NdrObject
		{
			public int Level;

			public NdrObject E;

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode(NdrBuffer dst)
			{
				dst.Align(4);
				dst.Enc_ndr_long(Level);
				int descr = Level;
				dst.Enc_ndr_long(descr);
				dst.Enc_ndr_referent(E, 1);
				if (E != null)
				{
					dst = dst.Deferred;
					E.Encode(dst);
				}
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Decode(NdrBuffer src)
			{
				src.Align(4);
				Level = src.Dec_ndr_long();
				src.Dec_ndr_long();
				int ep = src.Dec_ndr_long();
				if (ep != 0)
				{
					if (E == null)
					{
						E = new DfsEnumArray1();
					}
					src = src.Deferred;
					E.Decode(src);
				}
			}
		}

		public class NetrDfsEnumEx : DcerpcMessage
		{
			public override int GetOpnum()
			{
				return unchecked(0x15);
			}

			public int Retval;

			public string DfsName;

			public int Level;

			public int Prefmaxlen;

			public DfsEnumStruct Info;

			public NdrLong Totalentries;

			public NetrDfsEnumEx(string dfsName, int level, int prefmaxlen, DfsEnumStruct
				 info, NdrLong totalentries)
			{
				this.DfsName = dfsName;
				this.Level = level;
				this.Prefmaxlen = prefmaxlen;
				this.Info = info;
				this.Totalentries = totalentries;
			}

			/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
			public override void Encode_in(NdrBuffer dst)
			{
				dst.Enc_ndr_string(DfsName);
				dst.Enc_ndr_long(Level);
				dst.Enc_ndr_long(Prefmaxlen);
				dst.Enc_ndr_referent(Info, 1);
				if (Info != null)
				{
					Info.Encode(dst);
				}
				dst.Enc_ndr_referent(Totalentries, 1);
				if (Totalentries != null)
				{
					Totalentries.Encode(dst);
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
						Info = new DfsEnumStruct();
					}
					Info.Decode(src);
				}
				int totalentriesp = src.Dec_ndr_long();
				if (totalentriesp != 0)
				{
					Totalentries.Decode(src);
				}
				Retval = src.Dec_ndr_long();
			}
		}
	}
}
