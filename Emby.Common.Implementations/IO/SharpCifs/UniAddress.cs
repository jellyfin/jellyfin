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
using System;
using System.IO;
using System.Linq;
using System.Net;
using SharpCifs.Netbios;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using Extensions = SharpCifs.Util.Sharpen.Extensions;

namespace SharpCifs
{
	/// <summary>
	/// <p>Under normal conditions it is not necessary to use
	/// this class to use jCIFS properly.
	/// </summary>
	/// <remarks>
	/// <p>Under normal conditions it is not necessary to use
	/// this class to use jCIFS properly. Name resolusion is
	/// handled internally to the <code>jcifs.smb</code> package.
	/// <p>
	/// This class is a wrapper for both
	/// <see cref="Jcifs.Netbios.NbtAddress">Jcifs.Netbios.NbtAddress</see>
	/// and
	/// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
	/// . The name resolution mechanisms
	/// used will systematically query all available configured resolution
	/// services including WINS, broadcasts, DNS, and LMHOSTS. See
	/// <a href="../../resolver.html">Setting Name Resolution Properties</a>
	/// and the <code>jcifs.resolveOrder</code> property. Changing
	/// jCIFS name resolution properties can greatly affect the behavior of
	/// the client and may be necessary for proper operation.
	/// <p>
	/// This class should be used in favor of <tt>InetAddress</tt> to resolve
	/// hostnames on LANs and WANs that support a mixture of NetBIOS/WINS and
	/// DNS resolvable hosts.
	/// </remarks>
	public class UniAddress
	{
		private const int ResolverWins = 0;

		private const int ResolverBcast = 1;

		private const int ResolverDns = 2;

		private const int ResolverLmhosts = 3;

		private static int[] _resolveOrder;

		private static IPAddress _baddr;

		private static LogStream _log = LogStream.GetInstance();

		static UniAddress()
		{
			string ro = Config.GetProperty("jcifs.resolveOrder");
			IPAddress nbns = NbtAddress.GetWinsAddress();
			try
			{
				_baddr = Config.GetInetAddress("jcifs.netbios.baddr", Extensions.GetAddressByName
					("255.255.255.255"));
			}
			catch (UnknownHostException)
			{
			}
			if (string.IsNullOrEmpty(ro))
			{
				if (nbns == null)
				{
					_resolveOrder = new int[3];
					_resolveOrder[0] = ResolverLmhosts;
					_resolveOrder[1] = ResolverDns;
					_resolveOrder[2] = ResolverBcast;
				}
				else
				{
					_resolveOrder = new int[4];
					_resolveOrder[0] = ResolverLmhosts;
					_resolveOrder[1] = ResolverWins;
					_resolveOrder[2] = ResolverDns;
					_resolveOrder[3] = ResolverBcast;
				}
			}
			else
			{
				int[] tmp = new int[4];
				StringTokenizer st = new StringTokenizer(ro, ",");
				int i = 0;
				while (st.HasMoreTokens())
				{
					string s = st.NextToken().Trim();
					if (Runtime.EqualsIgnoreCase(s, "LMHOSTS"))
					{
						tmp[i++] = ResolverLmhosts;
					}
					else
					{
						if (Runtime.EqualsIgnoreCase(s, "WINS"))
						{
							if (nbns == null)
							{
								if (_log.Level > 1)
								{
									_log.WriteLine("UniAddress resolveOrder specifies WINS however the " + "jcifs.netbios.wins property has not been set"
										);
								}
								continue;
							}
							tmp[i++] = ResolverWins;
						}
						else
						{
							if (Runtime.EqualsIgnoreCase(s, "BCAST"))
							{
								tmp[i++] = ResolverBcast;
							}
							else
							{
								if (Runtime.EqualsIgnoreCase(s, "DNS"))
								{
									tmp[i++] = ResolverDns;
								}
								else
								{
									if (_log.Level > 1)
									{
										_log.WriteLine("unknown resolver method: " + s);
									}
								}
							}
						}
					}
				}
				_resolveOrder = new int[i];
				Array.Copy(tmp, 0, _resolveOrder, 0, i);
			}
		}

		internal class Sem
		{
			internal Sem(int count)
			{
				this.Count = count;
			}

			internal int Count;
		}

		internal class QueryThread : Thread
		{
			internal Sem Sem;

			internal string Host;

			internal string Scope;

			internal int Type;

			internal NbtAddress[] Ans;

			internal IPAddress Svr;

			internal UnknownHostException Uhe;

			internal QueryThread(Sem sem, string host, int type, string scope, IPAddress
				 svr) : base("JCIFS-QueryThread: " + host)
			{
				this.Sem = sem;
				this.Host = host;
				this.Type = type;
				this.Scope = scope;
				this.Svr = svr;
			}

			public override void Run()
			{
				try
				{
				    //Ans = new [] { NbtAddress.GetByName(Host, Type, Scope, Svr) };
				    Ans = NbtAddress.GetAllByName(Host, Type, Scope, Svr);
				}
				catch (UnknownHostException uhe)
				{
					this.Uhe = uhe;
				}
				catch (Exception ex)
				{
					Uhe = new UnknownHostException(ex.Message);
				}
				finally
				{
					lock (Sem)
					{
						Sem.Count--;
						Runtime.Notify(Sem);
					}
				}
			}
		}

		/// <exception cref="UnknownHostException"></exception>
		internal static NbtAddress[] LookupServerOrWorkgroup(string name, IPAddress svr)
		{
			Sem sem = new Sem(2);
			int type = NbtAddress.IsWins(svr) ? unchecked(0x1b) : unchecked(0x1d);
			QueryThread q1X = new QueryThread(sem, name, type, null, svr
				);
			QueryThread q20 = new QueryThread(sem, name, unchecked(0x20), null, svr);
			q1X.SetDaemon(true);
			q20.SetDaemon(true);
			try
			{
				lock (sem)
				{
					q1X.Start();
					q20.Start();
					while (sem.Count > 0 && q1X.Ans == null && q20.Ans == null)
					{
						Runtime.Wait(sem);
					}
				}
			}
			catch (Exception)
			{
				throw new UnknownHostException(name);
			}
			if (q1X.Ans != null)
			{
				return q1X.Ans;
			}
		    if (q20.Ans != null)
		    {
		        return q20.Ans;
		    }
		    throw q1X.Uhe;
		}

		/// <summary>Determines the address of a host given it's host name.</summary>
		/// <remarks>
		/// Determines the address of a host given it's host name. The name can be a
		/// machine name like "jcifs.samba.org",  or an IP address like "192.168.1.15".
		/// </remarks>
		/// <param name="hostname">NetBIOS or DNS hostname to resolve</param>
		/// <exception cref="UnknownHostException">if there is an error resolving the name
		/// 	</exception>
		public static UniAddress GetByName(string hostname)
		{
			return GetByName(hostname, false);
		}

		internal static bool IsDotQuadIp(string hostname)
		{
			if (char.IsDigit(hostname[0]))
			{
				int i;
				int len;
				int dots;
				char[] data;
				i = dots = 0;
				len = hostname.Length;
				data = hostname.ToCharArray();
				while (i < len && char.IsDigit(data[i++]))
				{
					if (i == len && dots == 3)
					{
						// probably an IP address
						return true;
					}
					if (i < len && data[i] == '.')
					{
						dots++;
						i++;
					}
				}
			}
			return false;
		}

		internal static bool IsAllDigits(string hostname)
		{
			for (int i = 0; i < hostname.Length; i++)
			{
				if (char.IsDigit(hostname[i]) == false)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Lookup <tt>hostname</tt> and return it's <tt>UniAddress</tt>.</summary>
		/// <remarks>
		/// Lookup <tt>hostname</tt> and return it's <tt>UniAddress</tt>. If the
		/// <tt>possibleNTDomainOrWorkgroup</tt> parameter is <tt>true</tt> an
		/// addtional name query will be performed to locate a master browser.
		/// </remarks>
		/// <exception cref="UnknownHostException"></exception>
		public static UniAddress GetByName(string hostname, bool possibleNtDomainOrWorkgroup
			)
		{
			UniAddress[] addrs = GetAllByName(hostname, possibleNtDomainOrWorkgroup
				);
			return addrs[0];
		}

		/// <exception cref="UnknownHostException"></exception>
		public static UniAddress[] GetAllByName(string hostname, bool possibleNtDomainOrWorkgroup
			)
		{
			object addr;
			int i;
			if (string.IsNullOrEmpty(hostname))
			{
				throw new UnknownHostException();
			}
			if (IsDotQuadIp(hostname))
			{
				UniAddress[] addrs = new UniAddress[1];
				addrs[0] = new UniAddress(NbtAddress.GetByName(hostname));
				return addrs;
			}
			for (i = 0; i < _resolveOrder.Length; i++)
			{
				try
				{
					switch (_resolveOrder[i])
					{
						case ResolverLmhosts:
						{
							if ((addr = Lmhosts.GetByName(hostname)) == null)
							{
								continue;
							}
							break;
						}

						case ResolverWins:
						{
							if (hostname == NbtAddress.MasterBrowserName || hostname.Length > 15)
							{
								// invalid netbios name
								continue;
							}
							if (possibleNtDomainOrWorkgroup)
							{
								addr = LookupServerOrWorkgroup(hostname, NbtAddress.GetWinsAddress());
							}
							else
							{
								addr = NbtAddress.GetByName(hostname, unchecked(0x20), null, NbtAddress.GetWinsAddress
									());
							}
							break;
						}

						case ResolverBcast:
						{
							if (hostname.Length > 15)
							{
								// invalid netbios name
								continue;
							}

						    try
						    {
                                if (possibleNtDomainOrWorkgroup)
                                {
                                    NbtAddress[] iaddrs = LookupServerOrWorkgroup(hostname, _baddr);

                                    UniAddress[] addrs = new UniAddress[iaddrs.Length];
                                    for (int ii = 0; ii < iaddrs.Length; ii++)
                                    {
                                        addrs[ii] = new UniAddress(iaddrs[ii]);
                                    }
                                    return addrs;

                                }
                                else
                                {
                                    addr = NbtAddress.GetByName(hostname, unchecked(0x20), null, _baddr);
                                }

						    }
						    catch (Exception ex)
						    {
						        if (i == _resolveOrder.Length - 1)
						        {
						            throw ex;
						        }
						        else
						        {
						            continue;
						        }
						    }
							break;
						}

						case ResolverDns:
						{
							if (IsAllDigits(hostname))
							{
								throw new UnknownHostException(hostname);
							}

                            IPAddress[] iaddrs = Extensions.GetAddressesByName(hostname);

                            if (iaddrs == null || iaddrs.Length == 0)
                            {
                                continue;
                            }

                            return iaddrs.Select(iaddr => new UniAddress(iaddr)).ToArray();                            
						}

						default:
						{
							// Success
							throw new UnknownHostException(hostname);
						}
					}
					UniAddress[] addrs1 = new UniAddress[1];
					addrs1[0] = new UniAddress(addr);
					return addrs1;
				}
				catch (IOException)
				{
				}
			}
			// Success
			// Failure
			throw new UnknownHostException(hostname);
		}

		internal object Addr;

		internal string CalledName;

		/// <summary>
		/// Create a <tt>UniAddress</tt> by wrapping an <tt>InetAddress</tt> or
		/// <tt>NbtAddress</tt>.
		/// </summary>
		/// <remarks>
		/// Create a <tt>UniAddress</tt> by wrapping an <tt>InetAddress</tt> or
		/// <tt>NbtAddress</tt>.
		/// </remarks>
		public UniAddress(object addr)
		{
			if (addr == null)
			{
				throw new ArgumentException();
			}
			this.Addr = addr;
		}

		/// <summary>Return the IP address of this address as a 32 bit integer.</summary>
		/// <remarks>Return the IP address of this address as a 32 bit integer.</remarks>
		public override int GetHashCode()
		{
			return Addr.GetHashCode();
		}

		/// <summary>Compare two addresses for equality.</summary>
		/// <remarks>
		/// Compare two addresses for equality. Two <tt>UniAddress</tt>s are equal
		/// if they are both <tt>UniAddress</tt>' and refer to the same IP address.
		/// </remarks>
		public override bool Equals(object obj)
		{
			return obj is UniAddress && Addr.Equals(((UniAddress)obj).Addr);
		}

		/// <summary>Guess first called name to try for session establishment.</summary>
		/// <remarks>
		/// Guess first called name to try for session establishment. This
		/// method is used exclusively by the <tt>jcifs.smb</tt> package.
		/// </remarks>
		public virtual string FirstCalledName()
		{
			if (Addr is NbtAddress)
			{
				return ((NbtAddress)Addr).FirstCalledName();
			}
		    CalledName = ((IPAddress) Addr).GetHostAddress();
		    if (IsDotQuadIp(CalledName))
		    {
		        CalledName = NbtAddress.SmbserverName;
		    }
		    else
		    {
		        int i = CalledName.IndexOf('.');
		        if (i > 1 && i < 15)
		        {
		            CalledName = Runtime.Substring(CalledName, 0, i).ToUpper();
		        }
		        else
		        {
		            if (CalledName.Length > 15)
		            {
		                CalledName = NbtAddress.SmbserverName;
		            }
		            else
		            {
		                CalledName = CalledName.ToUpper();
		            }
		        }
		    }
		    return CalledName;
		}

		/// <summary>Guess next called name to try for session establishment.</summary>
		/// <remarks>
		/// Guess next called name to try for session establishment. This
		/// method is used exclusively by the <tt>jcifs.smb</tt> package.
		/// </remarks>
		public virtual string NextCalledName()
		{
			if (Addr is NbtAddress)
			{
				return ((NbtAddress)Addr).NextCalledName();
			}
		    if (CalledName != NbtAddress.SmbserverName)
		    {
		        CalledName = NbtAddress.SmbserverName;
		        return CalledName;
		    }
		    return null;
		}

		/// <summary>Return the underlying <tt>NbtAddress</tt> or <tt>InetAddress</tt>.</summary>
		/// <remarks>Return the underlying <tt>NbtAddress</tt> or <tt>InetAddress</tt>.</remarks>
		public virtual object GetAddress()
		{
			return Addr;
		}

		/// <summary>Return the hostname of this address such as "MYCOMPUTER".</summary>
		/// <remarks>Return the hostname of this address such as "MYCOMPUTER".</remarks>
		public virtual string GetHostName()
		{
			if (Addr is NbtAddress)
			{
				return ((NbtAddress)Addr).GetHostName();
			}
		    return ((IPAddress) Addr).GetHostAddress();
		}

		/// <summary>Return the IP address as text such as "192.168.1.15".</summary>
		/// <remarks>Return the IP address as text such as "192.168.1.15".</remarks>
		public virtual string GetHostAddress()
		{
			if (Addr is NbtAddress)
			{
				return ((NbtAddress)Addr).GetHostAddress();
			}
			return ((IPAddress)Addr).GetHostAddress();
		}

	    public virtual IPAddress GetHostIpAddress()
	    {
	        return (IPAddress) Addr;
	    }

		/// <summary>
		/// Return the a text representation of this address such as
		/// <tt>MYCOMPUTER/192.168.1.15</tt>.
		/// </summary>
		/// <remarks>
		/// Return the a text representation of this address such as
		/// <tt>MYCOMPUTER/192.168.1.15</tt>.
		/// </remarks>
		public override string ToString()
		{
			return Addr.ToString();
		}
	}
}
