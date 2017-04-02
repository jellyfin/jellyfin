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
using System.IO;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	public class Dfs
	{
		internal class CacheEntry
		{
			internal long Expiration;

			internal Hashtable Map;

			internal CacheEntry(long ttl)
			{
				if (ttl == 0)
				{
					ttl = Ttl;
				}
				Expiration = Runtime.CurrentTimeMillis() + ttl * 1000L;
				Map = new Hashtable();
			}
		}

		internal static LogStream Log = LogStream.GetInstance();

		internal static readonly bool StrictView = Config.GetBoolean("jcifs.smb.client.dfs.strictView"
			, false);

		internal static readonly long Ttl = Config.GetLong("jcifs.smb.client.dfs.ttl", 300
			);

		internal static readonly bool Disabled = Config.GetBoolean("jcifs.smb.client.dfs.disabled"
			, false);

		internal static CacheEntry FalseEntry = new CacheEntry(0L);

		internal CacheEntry Domains;

		internal CacheEntry Referrals;

		/// <exception cref="SharpCifs.Smb.SmbAuthException"></exception>
		public virtual Hashtable GetTrustedDomains(NtlmPasswordAuthentication auth)
		{
			if (Disabled || auth.Domain == "?")
			{
				return null;
			}
			if (Domains != null && Runtime.CurrentTimeMillis() > Domains.Expiration)
			{
				Domains = null;
			}
			if (Domains != null)
			{
				return Domains.Map;
			}
			try
			{
				UniAddress addr = UniAddress.GetByName(auth.Domain, true);
				SmbTransport trans = SmbTransport.GetSmbTransport(addr, 0);
				CacheEntry entry = new CacheEntry(Ttl * 10L);
				DfsReferral dr = trans.GetDfsReferrals(auth, string.Empty, 0);
				if (dr != null)
				{
					DfsReferral start = dr;
					do
					{
						string domain = dr.Server.ToLower();
						entry.Map.Put(domain, new Hashtable());
						dr = dr.Next;
					}
					while (dr != start);
					Domains = entry;
					return Domains.Map;
				}
			}
			catch (IOException ioe)
			{
				if (Log.Level >= 3)
				{
					Runtime.PrintStackTrace(ioe, Log);
				}
				if (StrictView && ioe is SmbAuthException)
				{
					throw (SmbAuthException)ioe;
				}
			}
			return null;
		}

		/// <exception cref="SharpCifs.Smb.SmbAuthException"></exception>
		public virtual bool IsTrustedDomain(string domain, NtlmPasswordAuthentication auth
			)
		{
			Hashtable domains = GetTrustedDomains(auth);
			if (domains == null)
			{
				return false;
			}
			domain = domain.ToLower();
			return domains.Get(domain) != null;
		}

		/// <exception cref="SharpCifs.Smb.SmbAuthException"></exception>
		public virtual SmbTransport GetDc(string domain, NtlmPasswordAuthentication auth)
		{
			if (Disabled)
			{
				return null;
			}
			try
			{
				UniAddress addr = UniAddress.GetByName(domain, true);
				SmbTransport trans = SmbTransport.GetSmbTransport(addr, 0);
				DfsReferral dr = trans.GetDfsReferrals(auth, "\\" + domain, 1);
				if (dr != null)
				{
					DfsReferral start = dr;
					IOException e = null;
					do
					{
						try
						{
							addr = UniAddress.GetByName(dr.Server);
							return SmbTransport.GetSmbTransport(addr, 0);
						}
						catch (IOException ioe)
						{
							e = ioe;
						}
						dr = dr.Next;
					}
					while (dr != start);
					throw e;
				}
			}
			catch (IOException ioe)
			{
				if (Log.Level >= 3)
				{
					Runtime.PrintStackTrace(ioe, Log);
				}
				if (StrictView && ioe is SmbAuthException)
				{
					throw (SmbAuthException)ioe;
				}
			}
			return null;
		}

		/// <exception cref="SharpCifs.Smb.SmbAuthException"></exception>
		public virtual DfsReferral GetReferral(SmbTransport trans, string domain, string 
			root, string path, NtlmPasswordAuthentication auth)
		{
			if (Disabled)
			{
				return null;
			}
			try
			{
				string p = "\\" + domain + "\\" + root;
				if (path != null)
				{
					p += path;
				}
				DfsReferral dr = trans.GetDfsReferrals(auth, p, 0);
				if (dr != null)
				{
					return dr;
				}
			}
			catch (IOException ioe)
			{
				if (Log.Level >= 4)
				{
					Runtime.PrintStackTrace(ioe, Log);
				}
				if (StrictView && ioe is SmbAuthException)
				{
					throw (SmbAuthException)ioe;
				}
			}
			return null;
		}

		/// <exception cref="SharpCifs.Smb.SmbAuthException"></exception>
		public virtual DfsReferral Resolve(string domain, string root, string path, NtlmPasswordAuthentication
			 auth)
		{
			lock (this)
			{
				DfsReferral dr = null;
				long now = Runtime.CurrentTimeMillis();
				if (Disabled || root.Equals("IPC$"))
				{
					return null;
				}
				Hashtable domains = GetTrustedDomains(auth);
				if (domains != null)
				{
					domain = domain.ToLower();
					Hashtable roots = (Hashtable)domains.Get(domain);
					if (roots != null)
					{
						SmbTransport trans = null;
						root = root.ToLower();
						CacheEntry links = (CacheEntry)roots.Get(root);
						if (links != null && now > links.Expiration)
						{
							//Sharpen.Collections.Remove(roots, root);
                            roots.Remove(root);
							links = null;
						}
						if (links == null)
						{
							if ((trans = GetDc(domain, auth)) == null)
							{
								return null;
							}
							dr = GetReferral(trans, domain, root, path, auth);
							if (dr != null)
							{
								int len = 1 + domain.Length + 1 + root.Length;
								links = new CacheEntry(0L);
								DfsReferral tmp = dr;
								do
								{
									if (path == null)
									{
										// TODO: fix this
                                        //tmp.map = links.map;
										tmp.Key = "\\";
									}
									tmp.PathConsumed -= len;
									tmp = tmp.Next;
								}
								while (tmp != dr);
								if (dr.Key != null)
								{
									links.Map.Put(dr.Key, dr);
								}
								roots.Put(root, links);
							}
							else
							{
								if (path == null)
								{
									roots.Put(root, FalseEntry);
								}
							}
						}
						else
						{
							if (links == FalseEntry)
							{
								links = null;
							}
						}
						if (links != null)
						{
							string link = "\\";
							dr = (DfsReferral)links.Map.Get(link);
							if (dr != null && now > dr.Expiration)
							{
								//Sharpen.Collections.Remove(links.map, link);
                                links.Map.Remove(link);
								dr = null;
							}
							if (dr == null)
							{
								if (trans == null)
								{
									if ((trans = GetDc(domain, auth)) == null)
									{
										return null;
									}
								}
								dr = GetReferral(trans, domain, root, path, auth);
								if (dr != null)
								{
									dr.PathConsumed -= 1 + domain.Length + 1 + root.Length;
									dr.Link = link;
									links.Map.Put(link, dr);
								}
							}
						}
					}
				}
				if (dr == null && path != null)
				{
					if (Referrals != null && now > Referrals.Expiration)
					{
						Referrals = null;
					}
					if (Referrals == null)
					{
						Referrals = new CacheEntry(0);
					}
					string key = "\\" + domain + "\\" + root;
					if (path.Equals("\\") == false)
					{
						key += path;
					}
					key = key.ToLower();
				    //ListIterator<object> iter = new ListIterator<object>(referrals.map.Keys.GetEnumerator(), 0);
                    foreach (var current in Referrals.Map.Keys)
                    {
                        string _key = (string)current;
						int klen = _key.Length;
						bool match = false;
						if (klen == key.Length)
						{
							match = _key.Equals(key);
						}
						else
						{
							if (klen < key.Length)
							{
								match = _key.RegionMatches(false, 0, key, 0, klen) && key[klen] == '\\';
							}
						}
						if (match)
						{
							dr = (DfsReferral)Referrals.Map.Get(_key);
						}
					}
				}
				return dr;
			}
		}

		internal virtual void Insert(string path, DfsReferral dr)
		{
			lock (this)
			{
				int s1;
				int s2;
				string server;
				string share;
				string key;
				if (Disabled)
				{
					return;
				}
				s1 = path.IndexOf('\\', 1);
				s2 = path.IndexOf('\\', s1 + 1);
				server = Runtime.Substring(path, 1, s1);
				share = Runtime.Substring(path, s1 + 1, s2);
				key = Runtime.Substring(path, 0, dr.PathConsumed).ToLower();
				int ki = key.Length;
				while (ki > 1 && key[ki - 1] == '\\')
				{
					ki--;
				}
				if (ki < key.Length)
				{
					key = Runtime.Substring(key, 0, ki);
				}
				dr.PathConsumed -= 1 + server.Length + 1 + share.Length;
				if (Referrals != null && (Runtime.CurrentTimeMillis() + 10000) > Referrals.Expiration)
				{
					Referrals = null;
				}
				if (Referrals == null)
				{
					Referrals = new CacheEntry(0);
				}
				Referrals.Map.Put(key, dr);
			}
		}
	}
}
