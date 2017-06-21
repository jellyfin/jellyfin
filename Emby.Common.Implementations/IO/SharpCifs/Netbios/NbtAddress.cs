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
using System.Linq;
using System.Net;
using SharpCifs.Util;
using SharpCifs.Util.DbsHelper;
using SharpCifs.Util.Sharpen;
using Extensions = SharpCifs.Util.Sharpen.Extensions;

namespace SharpCifs.Netbios
{
    /// <summary>This class represents a NetBIOS over TCP/IP address.</summary>
    /// <remarks>
    /// This class represents a NetBIOS over TCP/IP address. Under normal
    /// conditions, users of jCIFS need not be concerned with this class as
    /// name resolution and session services are handled internally by the smb package.
    /// <p> Applications can use the methods <code>getLocalHost</code>,
    /// <code>getByName</code>, and
    /// <code>getAllByAddress</code> to create a new NbtAddress instance. This
    /// class is symmetric with
    /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
    /// .
    /// <p><b>About NetBIOS:</b> The NetBIOS name
    /// service is a dynamic distributed service that allows hosts to resolve
    /// names by broadcasting a query, directing queries to a server such as
    /// Samba or WINS. NetBIOS is currently the primary networking layer for
    /// providing name service, datagram service, and session service to the
    /// Microsoft Windows platform. A NetBIOS name can be 15 characters long
    /// and hosts usually registers several names on the network. From a
    /// Windows command prompt you can see
    /// what names a host registers with the nbtstat command.
    /// <p><blockquote><pre>
    /// C:\&gt;nbtstat -a 192.168.1.15
    /// NetBIOS Remote Machine Name Table
    /// Name               Type         Status
    /// ---------------------------------------------
    /// JMORRIS2        <00>  UNIQUE      Registered
    /// BILLING-NY      <00>  GROUP       Registered
    /// JMORRIS2        <03>  UNIQUE      Registered
    /// JMORRIS2        <20>  UNIQUE      Registered
    /// BILLING-NY      <1E>  GROUP       Registered
    /// JMORRIS         <03>  UNIQUE      Registered
    /// MAC Address = 00-B0-34-21-FA-3B
    /// </blockquote></pre>
    /// <p> The hostname of this machine is <code>JMORRIS2</code>. It is
    /// a member of the group(a.k.a workgroup and domain) <code>BILLING-NY</code>. To
    /// obtain an
    /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
    /// for a host one might do:
    /// <pre>
    /// InetAddress addr = NbtAddress.getByName( "jmorris2" ).getInetAddress();
    /// </pre>
    /// <p>From a UNIX platform with Samba installed you can perform similar
    /// diagnostics using the <code>nmblookup</code> utility.
    /// </remarks>
    /// <author>Michael B. Allen</author>
    /// <seealso cref="System.Net.IPAddress">System.Net.IPAddress</seealso>
    /// <since>jcifs-0.1</since>
    public sealed class NbtAddress
    {
        internal static readonly string AnyHostsName 
            = "*\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000";

        /// <summary>
        /// This is a special name for querying the master browser that serves the
        /// list of hosts found in "Network Neighborhood".
        /// </summary>
        /// <remarks>
        /// This is a special name for querying the master browser that serves the
        /// list of hosts found in "Network Neighborhood".
        /// </remarks>
        public static readonly string MasterBrowserName = "\u0001\u0002__MSBROWSE__\u0002";

        /// <summary>
        /// A special generic name specified when connecting to a host for which
        /// a name is not known.
        /// </summary>
        /// <remarks>
        /// A special generic name specified when connecting to a host for which
        /// a name is not known. Not all servers respond to this name.
        /// </remarks>
        public static readonly string SmbserverName = "*SMBSERVER     ";

        /// <summary>A B node only broadcasts name queries.</summary>
        /// <remarks>
        /// A B node only broadcasts name queries. This is the default if a
        /// nameserver such as WINS or Samba is not specified.
        /// </remarks>
        public const int BNode = 0;

        /// <summary>
        /// A Point-to-Point node, or P node, unicasts queries to a nameserver
        /// only.
        /// </summary>
        /// <remarks>
        /// A Point-to-Point node, or P node, unicasts queries to a nameserver
        /// only. Natrually the <code>jcifs.netbios.nameserver</code> property must
        /// be set.
        /// </remarks>
        public const int PNode = 1;

        /// <summary>
        /// Try Broadcast queries first, then try to resolve the name using the
        /// nameserver.
        /// </summary>
        /// <remarks>
        /// Try Broadcast queries first, then try to resolve the name using the
        /// nameserver.
        /// </remarks>
        public const int MNode = 2;

        /// <summary>A Hybrid node tries to resolve a name using the nameserver first.</summary>
        /// <remarks>
        /// A Hybrid node tries to resolve a name using the nameserver first. If
        /// that fails use the broadcast address. This is the default if a nameserver
        /// is provided. This is the behavior of Microsoft Windows machines.
        /// </remarks>
        public const int HNode = 3;

        internal static readonly IPAddress[] Nbns 
            = Config.GetInetAddressArray("jcifs.netbios.wins", ",", new IPAddress[0]);

        private static readonly NameServiceClient Client = new NameServiceClient();

        private const int DefaultCachePolicy = 30;

        private static readonly int CachePolicy 
            = Config.GetInt("jcifs.netbios.cachePolicy", DefaultCachePolicy);

        private const int Forever = -1;

        private static int _nbnsIndex;

        private static readonly Hashtable AddressCache = new Hashtable();

        private static readonly Hashtable LookupTable = new Hashtable();

        internal static readonly Name UnknownName = new Name("0.0.0.0", unchecked(0x00), null);

        internal static readonly NbtAddress UnknownAddress 
            = new NbtAddress(UnknownName, 0, false, BNode);

        internal static readonly byte[] UnknownMacAddress =
        {
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)),
            unchecked(unchecked(0x00)) 
        };

        private sealed class CacheEntry
        {
            internal Name HostName;

            internal NbtAddress Address;

            internal long Expiration;

            internal CacheEntry(Name hostName, NbtAddress address, long expiration)
            {
                this.HostName = hostName;
                this.Address = address;
                this.Expiration = expiration;
            }
        }

        private static NbtAddress Localhost;

        static NbtAddress()
        {
            IPAddress localInetAddress;
            string localHostname;
            Name localName;
            AddressCache.Put(UnknownName, new CacheEntry(UnknownName, 
                                                         UnknownAddress, 
                                                         Forever));
            localInetAddress = Client.laddr;
            if (localInetAddress == null)
            {
                try
                {
                    localInetAddress = Extensions.GetAddressByName("127.0.0.1");
                }
                catch (UnknownHostException)
                {
                }
            }
            localHostname = Config.GetProperty("jcifs.netbios.hostname", null);
            if (string.IsNullOrEmpty(localHostname))
            {
                /*
                byte[] addr = localInetAddress.GetAddressBytes();

                localHostname = "JCIFS" 
                                + (addr[2] & unchecked((int)(0xFF))) 
                                + "_" + (addr[3] & unchecked((int)(0xFF))) 
                                + "_" + Hexdump.ToHexString(
                                            (int)(new Random().NextDouble() 
                                                    * (double)unchecked((int)(0xFF))),
                                            2
                                        );
                */
                try
                {
                    localHostname = Dns.GetHostName();
                }
                catch (Exception)
                {
                    localHostname = "JCIFS_127_0_0_1";
                }
            }
            localName = new Name(localHostname, 
                                 unchecked(0x00), 
                                 Config.GetProperty("jcifs.netbios.scope", null));
            Localhost = new NbtAddress(localName, 
                                       localInetAddress.GetHashCode(), 
                                       false, 
                                       BNode, 
                                       false, 
                                       false, 
                                       true, 
                                       false, 
                                       UnknownMacAddress);
            CacheAddress(localName, Localhost, Forever);
        }

        private static void CacheAddress(Name hostName, NbtAddress addr)
        {
            if (CachePolicy == 0)
            {
                return;
            }
            long expiration = -1;
            if (CachePolicy != Forever)
            {
                expiration = Runtime.CurrentTimeMillis() + CachePolicy * 1000;
            }
            CacheAddress(hostName, addr, expiration);
        }

        private static void CacheAddress(Name hostName, NbtAddress addr, long expiration)
        {
            if (CachePolicy == 0)
            {
                return;
            }
            lock (AddressCache)
            {
                CacheEntry entry = (CacheEntry)AddressCache.Get(hostName);
                if (entry == null)
                {
                    entry = new CacheEntry(hostName, addr, expiration);
                    AddressCache.Put(hostName, entry);
                }
                else
                {
                    entry.Address = addr;
                    entry.Expiration = expiration;
                }
            }
        }

        private static void CacheAddressArray(NbtAddress[] addrs)
        {
            if (CachePolicy == 0)
            {
                return;
            }
            long expiration = -1;
            if (CachePolicy != Forever)
            {
                expiration = Runtime.CurrentTimeMillis() + CachePolicy * 1000;
            }
            lock (AddressCache)
            {
                for (int i = 0; i < addrs.Length; i++)
                {
                    CacheEntry entry = (CacheEntry)AddressCache.Get(addrs[i].HostName);
                    if (entry == null)
                    {
                        entry = new CacheEntry(addrs[i].HostName, addrs[i], expiration);
                        AddressCache.Put(addrs[i].HostName, entry);
                    }
                    else
                    {
                        entry.Address = addrs[i];
                        entry.Expiration = expiration;
                    }
                }
            }
        }

        private static NbtAddress GetCachedAddress(Name hostName)
        {
            if (CachePolicy == 0)
            {
                return null;
            }
            lock (AddressCache)
            {
                CacheEntry entry = (CacheEntry)AddressCache.Get(hostName);
                if (entry != null 
                    && entry.Expiration < Runtime.CurrentTimeMillis() 
                    && entry.Expiration>= 0)
                {
                    entry = null;
                }
                return entry != null 
                        ? entry.Address 
                        : null;
            }
        }

        /// <exception cref="UnknownHostException"></exception>
        private static NbtAddress DoNameQuery(Name name, IPAddress svr)
        {
            NbtAddress addr;
            if (name.HexCode == unchecked(0x1d) && svr == null)
            {
                svr = Client.Baddr;
            }
            // bit of a hack but saves a lookup
            name.SrcHashCode = svr != null 
                                ? svr.GetHashCode() 
                                : 0;
            addr = GetCachedAddress(name);
            if (addr == null)
            {
                if ((addr = (NbtAddress)CheckLookupTable(name)) == null)
                {
                    try
                    {
                        addr = Client.GetByName(name, svr);
                    }
                    catch (UnknownHostException)
                    {
                        addr = UnknownAddress;
                    }
                    finally
                    {
                        CacheAddress(name, addr);
                        UpdateLookupTable(name);
                    }
                }
            }
            if (addr == UnknownAddress)
            {
                throw new UnknownHostException(name.ToString());
            }
            return addr;
        }

        private static object CheckLookupTable(Name name)
        {
            object obj;
            lock (LookupTable)
            {
                if (LookupTable.ContainsKey(name) == false)
                {
                    LookupTable.Put(name, name);
                    return null;
                }
                while (LookupTable.ContainsKey(name))
                {
                    try
                    {
                        Runtime.Wait(LookupTable);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            obj = GetCachedAddress(name);
            if (obj == null)
            {
                lock (LookupTable)
                {
                    LookupTable.Put(name, name);
                }
            }
            return obj;
        }

        private static void UpdateLookupTable(Name name)
        {
            lock (LookupTable)
            {
                //Sharpen.Collections.Remove(LOOKUP_TABLE, name);
                LookupTable.Remove(name);
                Runtime.NotifyAll(LookupTable);
            }
        }



        /// <summary>Retrieves the local host address.</summary>
        /// <remarks>Retrieves the local host address.</remarks>
        /// <exception cref="UnknownHostException">
        /// This is not likely as the IP returned
        /// by <code>InetAddress</code> should be available
        /// </exception>
        public static NbtAddress GetLocalHost()
        {
            return Localhost;
        }

        public static NbtAddress[] GetHosts()
        {
            //Log.Out("NbtAddress.GetHosts");
            return new NameServiceClient().GetHosts();
        }

        public static Name GetLocalName()
        {
            return Localhost.HostName;
        }

        /// <summary>Determines the address of a host given it's host name.</summary>
        /// <remarks>
        /// Determines the address of a host given it's host name. The name can be a NetBIOS name like
        /// "freto" or an IP address like "192.168.1.15". It cannot be a DNS name;
        /// the analygous
        /// <see cref="SharpCifs.UniAddress">Jcifs.UniAddress</see>
        /// or
        /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
        /// <code>getByName</code> methods can be used for that.
        /// </remarks>
        /// <param name="host">hostname to resolve</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress GetByName(string host)
        {
            return GetByName(host, unchecked(0x00), null);
        }

        /// <summary>Determines the address of a host given it's host name.</summary>
        /// <remarks>
        /// Determines the address of a host given it's host name. NetBIOS
        /// names also have a <code>type</code>. Types(aka Hex Codes)
        /// are used to distiquish the various services on a host. &lt;a
        /// href="../../../nbtcodes.html"&gt;Here</a> is
        /// a fairly complete list of NetBIOS hex codes. Scope is not used but is
        /// still functional in other NetBIOS products and so for completeness it has been
        /// implemented. A <code>scope</code> of <code>null</code> or <code>""</code>
        /// signifies no scope.
        /// </remarks>
        /// <param name="host">the name to resolve</param>
        /// <param name="type">the hex code of the name</param>
        /// <param name="scope">the scope of the name</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress GetByName(string host, int type, string scope)
        {
            return GetByName(host, type, scope, null);
        }

        /// <exception cref="UnknownHostException"></exception>
        public static NbtAddress GetByName(string host, int type, string scope, IPAddress svr)
        {
            if (string.IsNullOrEmpty(host))
            {
                return GetLocalHost();
            }
            if (!char.IsDigit(host[0]))
            {
                return DoNameQuery(new Name(host, type, scope), svr);
            }
            int ip = unchecked(0x00);
            int hitDots = 0;
            char[] data = host.ToCharArray();
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                if (c < 48 || c > 57)
                {
                    return DoNameQuery(new Name(host, type, scope), svr);
                }
                int b = unchecked(0x00);
                while (c != '.')
                {
                    if (c < 48 || c > 57)
                    {
                        return DoNameQuery(new Name(host, type, scope), svr);
                    }
                    b = b * 10 + c - '0';
                    if (++i >= data.Length)
                    {
                        break;
                    }
                    c = data[i];
                }
                if (b > unchecked(0xFF))
                {
                    return DoNameQuery(new Name(host, type, scope), svr);
                }
                ip = (ip << 8) + b;
                hitDots++;
            }
            if (hitDots != 4 || host.EndsWith("."))
            {
                return DoNameQuery(new Name(host, type, scope), svr);
            }
            return new NbtAddress(UnknownName, ip, false, BNode);
        }

        /// <exception cref="UnknownHostException"></exception>
        public static NbtAddress[] GetAllByName(string host, 
                                                int type, 
                                                string scope, 
                                                IPAddress svr)
        {
            return Client.GetAllByName(new Name(host, type, scope), svr);
        }

        /// <summary>Retrieve all addresses of a host by it's address.</summary>
        /// <remarks>
        /// Retrieve all addresses of a host by it's address. NetBIOS hosts can
        /// have many names for a given IP address. The name and IP address make the
        /// NetBIOS address. This provides a way to retrieve the other names for a
        /// host with the same IP address.
        /// </remarks>
        /// <param name="host">hostname to lookup all addresses for</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress[] GetAllByAddress(string host)
        {
            return GetAllByAddress(GetByName(host, unchecked(0x00), null));
        }

        /// <summary>Retrieve all addresses of a host by it's address.</summary>
        /// <remarks>
        /// Retrieve all addresses of a host by it's address. NetBIOS hosts can
        /// have many names for a given IP address. The name and IP address make
        /// the NetBIOS address. This provides a way to retrieve the other names
        /// for a host with the same IP address.  See
        /// <see cref="GetByName(string)">GetByName(string)</see>
        /// for a description of <code>type</code>
        /// and <code>scope</code>.
        /// </remarks>
        /// <param name="host">hostname to lookup all addresses for</param>
        /// <param name="type">the hexcode of the name</param>
        /// <param name="scope">the scope of the name</param>
        /// <exception cref="UnknownHostException">if there is an error resolving the name
        /// </exception>
        public static NbtAddress[] GetAllByAddress(string host, int type, string scope)
        {
            return GetAllByAddress(GetByName(host, type, scope));
        }

        /// <summary>Retrieve all addresses of a host by it's address.</summary>
        /// <remarks>
        /// Retrieve all addresses of a host by it's address. NetBIOS hosts can
        /// have many names for a given IP address. The name and IP address make the
        /// NetBIOS address. This provides a way to retrieve the other names for a
        /// host with the same IP address.
        /// </remarks>
        /// <param name="addr">the address to query</param>
        /// <exception cref="UnknownHostException">if address cannot be resolved</exception>
        public static NbtAddress[] GetAllByAddress(NbtAddress addr)
        {
            try
            {
                NbtAddress[] addrs = Client.GetNodeStatus(addr);
                CacheAddressArray(addrs);
                return addrs;
            }
            catch (UnknownHostException)
            {
                throw new UnknownHostException(
                    "no name with type 0x" + Hexdump.ToHexString(addr.HostName.HexCode, 2) 
                    + (((addr.HostName.Scope == null) || (addr.HostName.Scope.Length == 0)) 
                        ? " with no scope" 
                        : " with scope " + addr.HostName.Scope) 
                    + " for host " + addr.GetHostAddress()
                );
            }
        }

        public static IPAddress GetWinsAddress()
        {
            return Nbns.Length == 0 ? null : Nbns[_nbnsIndex];
        }

        public static bool IsWins(IPAddress svr)
        {
            for (int i = 0; svr != null && i < Nbns.Length; i++)
            {
                if (svr.GetHashCode() == Nbns[i].GetHashCode())
                {
                    return true;
                }
            }
            return false;
        }

        internal static IPAddress SwitchWins()
        {
            _nbnsIndex = (_nbnsIndex + 1) < Nbns.Length ? _nbnsIndex + 1 : 0;
            return Nbns.Length == 0 ? null : Nbns[_nbnsIndex];
        }

        internal Name HostName;

        internal int Address;

        internal int NodeType;

        internal bool GroupName;

        internal bool isBeingDeleted;

        internal bool isInConflict;

        internal bool isActive;

        internal bool isPermanent;

        internal bool IsDataFromNodeStatus;

        internal byte[] MacAddress;

        internal string CalledName;

        internal NbtAddress(Name hostName, int address, bool groupName, int nodeType)
        {
            this.HostName = hostName;
            this.Address = address;
            this.GroupName = groupName;
            this.NodeType = nodeType;
        }

        internal NbtAddress(Name hostName, 
                            int address, 
                            bool groupName, 
                            int nodeType, 
                            bool isBeingDeleted, 
                            bool isInConflict, 
                            bool isActive, 
                            bool isPermanent, 
                            byte[] macAddress)
        {
            this.HostName = hostName;
            this.Address = address;
            this.GroupName = groupName;
            this.NodeType = nodeType;
            this.isBeingDeleted = isBeingDeleted;
            this.isInConflict = isInConflict;
            this.isActive = isActive;
            this.isPermanent = isPermanent;
            this.MacAddress = macAddress;
            IsDataFromNodeStatus = true;
        }

        public string FirstCalledName()
        {
            CalledName = HostName.name;
            if (char.IsDigit(CalledName[0]))
            {
                int i;
                int len;
                int dots;
                char[] data;
                i = dots = 0;
                len = CalledName.Length;
                data = CalledName.ToCharArray();
                while (i < len && char.IsDigit(data[i++]))
                {
                    if (i == len && dots == 3)
                    {
                        // probably an IP address
                        CalledName = SmbserverName;
                        break;
                    }
                    if (i < len && data[i] == '.')
                    {
                        dots++;
                        i++;
                    }
                }
            }
            else
            {
                switch (HostName.HexCode)
                {
                    case unchecked(0x1B):
                    case unchecked(0x1C):
                    case unchecked(0x1D):
                        {
                            CalledName = SmbserverName;
                            break;
                        }
                }
            }
            return CalledName;
        }

        public string NextCalledName()
        {
            if (CalledName == HostName.name)
            {
                CalledName = SmbserverName;
            }
            else
            {
                if (CalledName == SmbserverName)
                {
                    NbtAddress[] addrs;
                    try
                    {
                        addrs = Client.GetNodeStatus(this);
                        if (HostName.HexCode == unchecked(0x1D))
                        {
                            for (int i = 0; i < addrs.Length; i++)
                            {
                                if (addrs[i].HostName.HexCode == unchecked(0x20))
                                {
                                    return addrs[i].HostName.name;
                                }
                            }
                            return null;
                        }
                        if (IsDataFromNodeStatus)
                        {
                            CalledName = null;
                            return HostName.name;
                        }
                    }
                    catch (UnknownHostException)
                    {
                        CalledName = null;
                    }
                }
                else
                {
                    CalledName = null;
                }
            }
            return CalledName;
        }

        /// <exception cref="UnknownHostException"></exception>
        internal void CheckData()
        {
            if (HostName == UnknownName)
            {
                GetAllByAddress(this);
            }
        }

        /// <exception cref="UnknownHostException"></exception>
        internal void CheckNodeStatusData()
        {
            if (IsDataFromNodeStatus == false)
            {
                GetAllByAddress(this);
            }
        }

        /// <summary>Determines if the address is a group address.</summary>
        /// <remarks>
        /// Determines if the address is a group address. This is also
        /// known as a workgroup name or group name.
        /// </remarks>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public bool IsGroupAddress()
        {
            CheckData();
            return GroupName;
        }

        /// <summary>Checks the node type of this address.</summary>
        /// <remarks>Checks the node type of this address.</remarks>
        /// <returns>
        /// 
        /// <see cref="BNode">B_NODE</see>
        /// ,
        /// <see cref="PNode">P_NODE</see>
        /// ,
        /// <see cref="MNode">M_NODE</see>
        /// ,
        /// <see cref="HNode">H_NODE</see>
        /// </returns>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public int GetNodeType()
        {
            CheckData();
            return NodeType;
        }

        /// <summary>Determines if this address in the process of being deleted.</summary>
        /// <remarks>Determines if this address in the process of being deleted.</remarks>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public bool IsBeingDeleted()
        {
            CheckNodeStatusData();
            return isBeingDeleted;
        }

        /// <summary>Determines if this address in conflict with another address.</summary>
        /// <remarks>Determines if this address in conflict with another address.</remarks>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public bool IsInConflict()
        {
            CheckNodeStatusData();
            return isInConflict;
        }

        /// <summary>Determines if this address is active.</summary>
        /// <remarks>Determines if this address is active.</remarks>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public bool IsActive()
        {
            CheckNodeStatusData();
            return isActive;
        }

        /// <summary>Determines if this address is set to be permanent.</summary>
        /// <remarks>Determines if this address is set to be permanent.</remarks>
        /// <exception cref="UnknownHostException">if the host cannot be resolved to find out.
        /// </exception>
        public bool IsPermanent()
        {
            CheckNodeStatusData();
            return isPermanent;
        }

        /// <summary>Retrieves the MAC address of the remote network interface.</summary>
        /// <remarks>Retrieves the MAC address of the remote network interface. Samba returns all zeros.
        /// </remarks>
        /// <returns>the MAC address as an array of six bytes</returns>
        /// <exception cref="UnknownHostException">
        /// if the host cannot be resolved to
        /// determine the MAC address.
        /// </exception>
        public byte[] GetMacAddress()
        {
            CheckNodeStatusData();
            return MacAddress;
        }

        /// <summary>The hostname of this address.</summary>
        /// <remarks>
        /// The hostname of this address. If the hostname is null the local machines
        /// IP address is returned.
        /// </remarks>
        /// <returns>the text representation of the hostname associated with this address</returns>
        public string GetHostName()
        {
            if (HostName == UnknownName)
            {
                return GetHostAddress();
            }
            return HostName.name;
        }

        /// <summary>Returns the raw IP address of this NbtAddress.</summary>
        /// <remarks>
        /// Returns the raw IP address of this NbtAddress. The result is in network
        /// byte order: the highest order byte of the address is in getAddress()[0].
        /// </remarks>
        /// <returns>a four byte array</returns>
        public byte[] GetAddress()
        {
            byte[] addr = new byte[4];
            addr[0] = unchecked((byte)(((int)(((uint)Address) >> 24)) & unchecked(0xFF)));
            addr[1] = unchecked((byte)(((int)(((uint)Address) >> 16)) & unchecked(0xFF)));
            addr[2] = unchecked((byte)(((int)(((uint)Address) >> 8)) & unchecked(0xFF)));
            addr[3] = unchecked((byte)(Address & unchecked(0xFF)));
            return addr;
        }

        /// <summary>To convert this address to an <code>InetAddress</code>.</summary>
        /// <remarks>To convert this address to an <code>InetAddress</code>.</remarks>
        /// <returns>
        /// the
        /// <see cref="System.Net.IPAddress">System.Net.IPAddress</see>
        /// representation of this address.
        /// </returns>
        /// <exception cref="UnknownHostException"></exception>
        public IPAddress GetInetAddress()
        {
            return Extensions.GetAddressByName(GetHostAddress());
        }

        /// <summary>
        /// Returns this IP adress as a
        /// <see cref="string">string</see>
        /// in the form "%d.%d.%d.%d".
        /// </summary>
        public string GetHostAddress()
        {
            return (((int)(((uint)Address) >> 24)) & unchecked(0xFF)) 
                    + "." + (((int)(((uint)Address) >> 16)) & unchecked(0xFF)) 
                    + "." + (((int)(((uint)Address) >> 8)) & unchecked(0xFF)) 
                    + "." + (((int)(((uint)Address) >> 0)) & unchecked(0xFF));
        }

        /// <summary>Returned the hex code associated with this name(e.g.</summary>
        /// <remarks>Returned the hex code associated with this name(e.g. 0x20 is for the file service)
        /// </remarks>
        public int GetNameType()
        {
            return HostName.HexCode;
        }

        /// <summary>Returns a hashcode for this IP address.</summary>
        /// <remarks>
        /// Returns a hashcode for this IP address. The hashcode comes from the IP address
        /// and is not generated from the string representation. So because NetBIOS nodes
        /// can have many names, all names associated with an IP will have the same
        /// hashcode.
        /// </remarks>
        public override int GetHashCode()
        {
            return Address;
        }

        /// <summary>Determines if this address is equal two another.</summary>
        /// <remarks>
        /// Determines if this address is equal two another. Only the IP Addresses
        /// are compared. Similar to the
        /// <see cref="GetHashCode()">GetHashCode()</see>
        /// method, the comparison
        /// is based on the integer IP address and not the string representation.
        /// </remarks>
        public override bool Equals(object obj)
        {
            return (obj != null) 
                    && (obj is NbtAddress) 
                    && (((NbtAddress)obj).Address == Address);
        }

        /// <summary>
        /// Returns the
        /// <see cref="string">string</see>
        /// representaion of this address.
        /// </summary>
        public override string ToString()
        {
            return HostName + "/" + GetHostAddress();
        }
    }
}
