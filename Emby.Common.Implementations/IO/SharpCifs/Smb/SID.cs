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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SharpCifs.Dcerpc;
using SharpCifs.Dcerpc.Msrpc;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using Hashtable = SharpCifs.Util.Sharpen.Hashtable; //not System.Collections.Hashtable

namespace SharpCifs.Smb
{
    /// <summary>
    /// A Windows SID is a numeric identifier used to represent Windows
    /// accounts.
    /// </summary>
    /// <remarks>
    /// A Windows SID is a numeric identifier used to represent Windows
    /// accounts. SIDs are commonly represented using a textual format such as
    /// <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt> but they may
    /// also be resolved to yield the name of the associated Windows account
    /// such as <tt>Administrators</tt> or <tt>MYDOM\alice</tt>.
    /// <p>
    /// Consider the following output of <tt>examples/SidLookup.java</tt>:
    /// <pre>
    /// toString: S-1-5-21-4133388617-793952518-2001621813-512
    /// toDisplayString: WNET\Domain Admins
    /// getType: 2
    /// getTypeText: Domain group
    /// getDomainName: WNET
    /// getAccountName: Domain Admins
    /// </pre>
    /// </remarks>
    public class Sid : Rpc.SidT
    {
        public const int SidTypeUseNone = Lsarpc.SidNameUseNone;

        public const int SidTypeUser = Lsarpc.SidNameUser;

        public const int SidTypeDomGrp = Lsarpc.SidNameDomGrp;

        public const int SidTypeDomain = Lsarpc.SidNameDomain;

        public const int SidTypeAlias = Lsarpc.SidNameAlias;

        public const int SidTypeWknGrp = Lsarpc.SidNameWknGrp;

        public const int SidTypeDeleted = Lsarpc.SidNameDeleted;

        public const int SidTypeInvalid = Lsarpc.SidNameInvalid;

        public const int SidTypeUnknown = Lsarpc.SidNameUnknown;

        internal static readonly string[] SidTypeNames =
        {
            "0", "User", "Domain group", "Domain", "Local group",
            "Builtin group", "Deleted", "Invalid", "Unknown"
        };

        public const int SidFlagResolveSids = unchecked(0x0001);

        public static Sid Everyone;

        public static Sid CreatorOwner;

        public static Sid SYSTEM;

        static Sid()
        {
            try
            {
                Everyone = new Sid("S-1-1-0");
                CreatorOwner = new Sid("S-1-3-0");
                SYSTEM = new Sid("S-1-5-18");
            }
            catch (SmbException)
            {
            }
        }

        internal static Hashtable SidCache = new Hashtable();

        /// <exception cref="System.IO.IOException"></exception>
        internal static void ResolveSids(DcerpcHandle handle,
                                         LsaPolicyHandle policyHandle,
                                         Sid[] sids)
        {
            MsrpcLookupSids rpc = new MsrpcLookupSids(policyHandle, sids);
            handle.Sendrecv(rpc);
            switch (rpc.Retval)
            {
                case 0:
                case NtStatus.NtStatusNoneMapped:
                case unchecked(0x00000107):
                    {
                        // NT_STATUS_SOME_NOT_MAPPED
                        break;
                    }

                default:
                    {
                        throw new SmbException(rpc.Retval, false);
                    }
            }
            for (int si = 0; si < sids.Length; si++)
            {
                sids[si].Type = rpc.Names.Names[si].SidType;
                sids[si].DomainName = null;
                switch (sids[si].Type)
                {
                    case SidTypeUser:
                    case SidTypeDomGrp:
                    case SidTypeDomain:
                    case SidTypeAlias:
                    case SidTypeWknGrp:
                        {
                            int sidIndex = rpc.Names.Names[si].SidIndex;
                            Rpc.Unicode_string ustr = rpc.Domains.Domains[sidIndex].Name;
                            sids[si].DomainName = (new UnicodeString(ustr, false)).ToString();
                            break;
                        }
                }
                sids[si].AcctName = (new UnicodeString(rpc.Names.Names[si].Name, false)).ToString();
                sids[si].OriginServer = null;
                sids[si].OriginAuth = null;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static void ResolveSids0(string authorityServerName,
                                          NtlmPasswordAuthentication auth,
                                          Sid[] sids)
        {
            DcerpcHandle handle = null;
            LsaPolicyHandle policyHandle = null;
            lock (SidCache)
            {
                try
                {
                    handle = DcerpcHandle.GetHandle("ncacn_np:" + authorityServerName
                                                    + "[\\PIPE\\lsarpc]", auth);
                    string server = authorityServerName;
                    int dot = server.IndexOf('.');
                    if (dot > 0 && char.IsDigit(server[0]) == false)
                    {
                        server = Runtime.Substring(server, 0, dot);
                    }
                    policyHandle = new LsaPolicyHandle(handle, "\\\\" + server, unchecked(0x00000800));
                    ResolveSids(handle, policyHandle, sids);
                }
                finally
                {
                    if (handle != null)
                    {
                        if (policyHandle != null)
                        {
                            policyHandle.Close();
                        }
                        handle.Close();
                    }
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static void ResolveSids(string authorityServerName,
                                       NtlmPasswordAuthentication auth,
                                       Sid[] sids,
                                       int offset,
                                       int length)
        {
            List<object> list = new List<object>(); //new List<object>(sids.Length);
            int si;
            lock (SidCache)
            {
                for (si = 0; si < length; si++)
                {
                    Sid sid = (Sid)SidCache.Get(sids[offset + si]);
                    if (sid != null)
                    {
                        sids[offset + si].Type = sid.Type;
                        sids[offset + si].DomainName = sid.DomainName;
                        sids[offset + si].AcctName = sid.AcctName;
                    }
                    else
                    {
                        list.Add(sids[offset + si]);
                    }
                }
                if (list.Count > 0)
                {
                    //sids = (Jcifs.Smb.SID[])Sharpen.Collections.ToArray(list, new Jcifs.Smb.SID[0]);
                    sids = (Sid[])list.ToArray();
                    ResolveSids0(authorityServerName, auth, sids);
                    for (si = 0; si < sids.Length; si++)
                    {
                        SidCache.Put(sids[si], sids[si]);
                    }
                }
            }
        }

        /// <summary>Resolve an array of SIDs using a cache and at most one MSRPC request.</summary>
        /// <remarks>
        /// Resolve an array of SIDs using a cache and at most one MSRPC request.
        /// 
        /// This method will attempt
        /// to resolve SIDs using a cache and cache the results of any SIDs that
        /// required resolving with the authority. SID cache entries are currently not
        /// expired because under normal circumstances SID information never changes.
        /// </remarks>
        /// <param name="authorityServerName">
        /// The hostname of the server that should be queried. For maximum efficiency this should be the hostname of a domain controller however a member server will work as well and a domain controller may not return names for SIDs corresponding to local accounts for which the domain controller is not an authority.
        /// </param>
        /// <param name="auth">
        /// The credentials that should be used to communicate with the named server. As usual, <tt>null</tt> indicates that default credentials should be used.
        /// </param>
        /// <param name="sids">
        /// The SIDs that should be resolved. After this function is called, the names associated with the SIDs may be queried with the <tt>toDisplayString</tt>, <tt>getDomainName</tt>, and <tt>getAccountName</tt> methods.
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        public static void ResolveSids(string authorityServerName,
                                       NtlmPasswordAuthentication auth,
                                       Sid[] sids)
        {
            List<object> list = new List<object>(); //new List<object>(sids.Length);
            int si;
            lock (SidCache)
            {
                for (si = 0; si < sids.Length; si++)
                {
                    Sid sid = (Sid)SidCache.Get(sids[si]);
                    if (sid != null)
                    {
                        sids[si].Type = sid.Type;
                        sids[si].DomainName = sid.DomainName;
                        sids[si].AcctName = sid.AcctName;
                    }
                    else
                    {
                        list.Add(sids[si]);
                    }
                }
                if (list.Count > 0)
                {
                    //sids = (Jcifs.Smb.SID[])Sharpen.Collections.ToArray(list, new Jcifs.Smb.SID[0]);
                    sids = (Sid[])list.ToArray();
                    ResolveSids0(authorityServerName, auth, sids);
                    for (si = 0; si < sids.Length; si++)
                    {
                        SidCache.Put(sids[si], sids[si]);
                    }
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static Sid GetServerSid(string server,
                                       NtlmPasswordAuthentication auth)
        {
            DcerpcHandle handle = null;
            LsaPolicyHandle policyHandle = null;
            Lsarpc.LsarDomainInfo info = new Lsarpc.LsarDomainInfo();
            MsrpcQueryInformationPolicy rpc;
            lock (SidCache)
            {
                try
                {
                    handle = DcerpcHandle.GetHandle("ncacn_np:" + server + "[\\PIPE\\lsarpc]", auth);
                    // NetApp doesn't like the 'generic' access mask values
                    policyHandle = new LsaPolicyHandle(handle, null, unchecked(0x00000001));
                    rpc = new MsrpcQueryInformationPolicy(policyHandle,
                                                          Lsarpc.PolicyInfoAccountDomain,
                                                          info);
                    handle.Sendrecv(rpc);
                    if (rpc.Retval != 0)
                    {
                        throw new SmbException(rpc.Retval, false);
                    }
                    return new Sid(info.Sid,
                                   SidTypeDomain,
                                   (new UnicodeString(info.Name, false)).ToString(),
                                   null,
                                   false);
                }
                finally
                {
                    if (handle != null)
                    {
                        if (policyHandle != null)
                        {
                            policyHandle.Close();
                        }
                        handle.Close();
                    }
                }
            }
        }

        public static byte[] ToByteArray(Rpc.SidT sid)
        {
            byte[] dst = new byte[1 + 1 + 6 + sid.SubAuthorityCount * 4];
            int di = 0;
            dst[di++] = sid.Revision;
            dst[di++] = sid.SubAuthorityCount;
            Array.Copy(sid.IdentifierAuthority, 0, dst, di, 6);
            di += 6;
            for (int ii = 0; ii < sid.SubAuthorityCount; ii++)
            {
                Encdec.Enc_uint32le(sid.SubAuthority[ii], dst, di);
                di += 4;
            }
            return dst;
        }

        internal int Type;

        internal string DomainName;

        internal string AcctName;

        internal string OriginServer;

        internal NtlmPasswordAuthentication OriginAuth;

        public Sid(byte[] src, int si)
        {
            Revision = src[si++];
            SubAuthorityCount = src[si++];
            IdentifierAuthority = new byte[6];
            Array.Copy(src, si, IdentifierAuthority, 0, 6);
            si += 6;
            if (SubAuthorityCount > 100)
            {
                throw new RuntimeException("Invalid SID sub_authority_count");
            }
            SubAuthority = new int[SubAuthorityCount];
            for (int i = 0; i < SubAuthorityCount; i++)
            {
                SubAuthority[i] = ServerMessageBlock.ReadInt4(src, si);
                si += 4;
            }
        }

        /// <summary>
        /// Construct a SID from it's textual representation such as
        /// <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt>.
        /// </summary>
        /// <remarks>
        /// Construct a SID from it's textual representation such as
        /// <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt>.
        /// </remarks>
        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public Sid(string textual)
        {
            StringTokenizer st = new StringTokenizer(textual, "-");
            if (st.CountTokens() < 3 || !st.NextToken().Equals("S"))
            {
                // need S-N-M
                throw new SmbException("Bad textual SID format: " + textual);
            }
            Revision = byte.Parse(st.NextToken());
            string tmp = st.NextToken();
            long id = 0;
            if (tmp.StartsWith("0x"))
            {
                //id = long.Parse(Sharpen.Runtime.Substring(tmp, 2), 16);
                id = long.Parse(Runtime.Substring(tmp, 2));
            }
            else
            {
                id = long.Parse(tmp);
            }
            IdentifierAuthority = new byte[6];
            for (int i = 5; id > 0; i--)
            {
                IdentifierAuthority[i] = unchecked((byte)(id % 256));
                id >>= 8;
            }
            SubAuthorityCount = unchecked((byte)st.CountTokens());
            if (SubAuthorityCount > 0)
            {
                SubAuthority = new int[SubAuthorityCount];
                for (int i1 = 0; i1 < SubAuthorityCount; i1++)
                {
                    SubAuthority[i1] = (int)(long.Parse(st.NextToken()) & unchecked(0xFFFFFFFFL));
                }
            }
        }

        /// <summary>
        /// Construct a SID from a domain SID and an RID
        /// (relative identifier).
        /// </summary>
        /// <remarks>
        /// Construct a SID from a domain SID and an RID
        /// (relative identifier). For example, a domain SID
        /// <tt>S-1-5-21-1496946806-2192648263-3843101252</tt> and RID <tt>1029</tt> would
        /// yield the SID <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt>.
        /// </remarks>
        public Sid(Sid domsid, int rid)
        {
            Revision = domsid.Revision;
            IdentifierAuthority = domsid.IdentifierAuthority;
            SubAuthorityCount = unchecked((byte)(domsid.SubAuthorityCount + 1));
            SubAuthority = new int[SubAuthorityCount];
            int i;
            for (i = 0; i < domsid.SubAuthorityCount; i++)
            {
                SubAuthority[i] = domsid.SubAuthority[i];
            }
            SubAuthority[i] = rid;
        }

        public Sid(Rpc.SidT sid,
                   int type,
                   string domainName,
                   string acctName,
                   bool decrementAuthority)
        {
            Revision = sid.Revision;
            SubAuthorityCount = sid.SubAuthorityCount;
            IdentifierAuthority = sid.IdentifierAuthority;
            SubAuthority = sid.SubAuthority;
            this.Type = type;
            this.DomainName = domainName;
            this.AcctName = acctName;
            if (decrementAuthority)
            {
                SubAuthorityCount--;
                SubAuthority = new int[SubAuthorityCount];
                for (int i = 0; i < SubAuthorityCount; i++)
                {
                    SubAuthority[i] = sid.SubAuthority[i];
                }
            }
        }

        public virtual Sid GetDomainSid()
        {
            return new Sid(this,
                           SidTypeDomain,
                           DomainName,
                           null,
                           GetType() != SidTypeDomain);
        }

        public virtual int GetRid()
        {
            if (GetType() == SidTypeDomain)
            {
                throw new ArgumentException("This SID is a domain sid");
            }
            return SubAuthority[SubAuthorityCount - 1];
        }

        /// <summary>Returns the type of this SID indicating the state or type of account.</summary>
        /// <remarks>
        /// Returns the type of this SID indicating the state or type of account.
        /// <p>
        /// SID types are described in the following table.
        /// <tt>
        /// <table>
        /// <tr><th>Type</th><th>Name</th></tr>
        /// <tr><td>SID_TYPE_USE_NONE</td><td>0</td></tr>
        /// <tr><td>SID_TYPE_USER</td><td>User</td></tr>
        /// <tr><td>SID_TYPE_DOM_GRP</td><td>Domain group</td></tr>
        /// <tr><td>SID_TYPE_DOMAIN</td><td>Domain</td></tr>
        /// <tr><td>SID_TYPE_ALIAS</td><td>Local group</td></tr>
        /// <tr><td>SID_TYPE_WKN_GRP</td><td>Builtin group</td></tr>
        /// <tr><td>SID_TYPE_DELETED</td><td>Deleted</td></tr>
        /// <tr><td>SID_TYPE_INVALID</td><td>Invalid</td></tr>
        /// <tr><td>SID_TYPE_UNKNOWN</td><td>Unknown</td></tr>
        /// </table>
        /// </tt>
        /// </remarks>
        public virtual int GetType()
        {
            if (OriginServer != null)
            {
                ResolveWeak();
            }
            return Type;
        }

        /// <summary>
        /// Return text represeting the SID type suitable for display to
        /// users.
        /// </summary>
        /// <remarks>
        /// Return text represeting the SID type suitable for display to
        /// users. Text includes 'User', 'Domain group', 'Local group', etc.
        /// </remarks>
        public virtual string GetTypeText()
        {
            if (OriginServer != null)
            {
                ResolveWeak();
            }
            return SidTypeNames[Type];
        }

        /// <summary>
        /// Return the domain name of this SID unless it could not be
        /// resolved in which case the numeric representation is returned.
        /// </summary>
        /// <remarks>
        /// Return the domain name of this SID unless it could not be
        /// resolved in which case the numeric representation is returned.
        /// </remarks>
        public virtual string GetDomainName()
        {
            if (OriginServer != null)
            {
                ResolveWeak();
            }
            if (Type == SidTypeUnknown)
            {
                string full = ToString();
                return Runtime.Substring(full, 0, full.Length - GetAccountName().Length - 1);
            }
            return DomainName;
        }

        /// <summary>
        /// Return the sAMAccountName of this SID unless it could not
        /// be resolved in which case the numeric RID is returned.
        /// </summary>
        /// <remarks>
        /// Return the sAMAccountName of this SID unless it could not
        /// be resolved in which case the numeric RID is returned. If this
        /// SID is a domain SID, this method will return an empty String.
        /// </remarks>
        public virtual string GetAccountName()
        {
            if (OriginServer != null)
            {
                ResolveWeak();
            }
            if (Type == SidTypeUnknown)
            {
                return string.Empty + SubAuthority[SubAuthorityCount - 1];
            }
            if (Type == SidTypeDomain)
            {
                return string.Empty;
            }
            return AcctName;
        }

        public override int GetHashCode()
        {
            int hcode = IdentifierAuthority[5];
            for (int i = 0; i < SubAuthorityCount; i++)
            {
                hcode += 65599 * SubAuthority[i];
            }
            return hcode;
        }

        public override bool Equals(object obj)
        {
            if (obj is Sid)
            {
                Sid sid = (Sid)obj;
                if (sid == this)
                {
                    return true;
                }
                if (sid.SubAuthorityCount == SubAuthorityCount)
                {
                    int i = SubAuthorityCount;
                    while (i-- > 0)
                    {
                        if (sid.SubAuthority[i] != SubAuthority[i])
                        {
                            return false;
                        }
                    }
                    for (i = 0; i < 6; i++)
                    {
                        if (sid.IdentifierAuthority[i] != IdentifierAuthority[i])
                        {
                            return false;
                        }
                    }
                    return sid.Revision == Revision;
                }
            }
            return false;
        }

        /// <summary>
        /// Return the numeric representation of this sid such as
        /// <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt>.
        /// </summary>
        /// <remarks>
        /// Return the numeric representation of this sid such as
        /// <tt>S-1-5-21-1496946806-2192648263-3843101252-1029</tt>.
        /// </remarks>
        public override string ToString()
        {
            string ret = "S-" + (Revision & unchecked(0xFF)) + "-";
            if (IdentifierAuthority[0] != unchecked(0)
                || IdentifierAuthority[1] != unchecked(0))
            {
                ret += "0x";
                ret += Hexdump.ToHexString(IdentifierAuthority, 0, 6);
            }
            else
            {
                int shift = 0;
                long id = 0;
                for (int i = 5; i > 1; i--)
                {
                    id += (IdentifierAuthority[i] & unchecked(0xFFL)) << shift;
                    shift += 8;
                }
                ret += id;
            }
            for (int i1 = 0; i1 < SubAuthorityCount; i1++)
            {
                ret += "-" + (SubAuthority[i1] & unchecked(0xFFFFFFFFL));
            }
            return ret;
        }

        /// <summary>
        /// Return a String representing this SID ideal for display to
        /// users.
        /// </summary>
        /// <remarks>
        /// Return a String representing this SID ideal for display to
        /// users. This method should return the same text that the ACL
        /// editor in Windows would display.
        /// <p>
        /// Specifically, if the SID has
        /// been resolved and it is not a domain SID or builtin account,
        /// the full DOMAIN\name form of the account will be
        /// returned (e.g. MYDOM\alice or MYDOM\Domain Users).
        /// If the SID has been resolved but it is is a domain SID,
        /// only the domain name will be returned (e.g. MYDOM).
        /// If the SID has been resolved but it is a builtin account,
        /// only the name component will be returned (e.g. SYSTEM).
        /// If the sid cannot be resolved the numeric representation from
        /// toString() is returned.
        /// </remarks>
        public virtual string ToDisplayString()
        {
            if (OriginServer != null)
            {
                ResolveWeak();
            }
            if (DomainName != null)
            {
                string str;
                if (Type == SidTypeDomain)
                {
                    str = DomainName;
                }
                else
                {
                    if (Type == SidTypeWknGrp || DomainName.Equals("BUILTIN"))
                    {
                        if (Type == SidTypeUnknown)
                        {
                            str = ToString();
                        }
                        else
                        {
                            str = AcctName;
                        }
                    }
                    else
                    {
                        str = DomainName + "\\" + AcctName;
                    }
                }
                return str;
            }
            return ToString();
        }

        /// <summary>Manually resolve this SID.</summary>
        /// <remarks>
        /// Manually resolve this SID. Normally SIDs are automatically
        /// resolved. However, if a SID is constructed explicitly using a SID
        /// constructor, JCIFS will have no knowledge of the server that created the
        /// SID and therefore cannot possibly resolve it automatically. In this case,
        /// this method will be necessary.
        /// </remarks>
        /// <param name="authorityServerName">The FQDN of the server that is an authority for the SID.
        /// </param>
        /// <param name="auth">Credentials suitable for accessing the SID's information.</param>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Resolve(string authorityServerName,
                                    NtlmPasswordAuthentication auth)
        {
            Sid[] sids = new Sid[1];
            sids[0] = this;
            ResolveSids(authorityServerName, auth, sids);
        }

        internal virtual void ResolveWeak()
        {
            if (OriginServer != null)
            {
                try
                {
                    Resolve(OriginServer, OriginAuth);
                }
                catch (IOException)
                {
                }
                finally
                {
                    OriginServer = null;
                    OriginAuth = null;
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static Sid[] GetGroupMemberSids0(DcerpcHandle handle,
                                                  SamrDomainHandle domainHandle,
                                                  Sid domsid,
                                                  int rid,
                                                  int flags)
        {
            SamrAliasHandle aliasHandle = null;
            Lsarpc.LsarSidArray sidarray = new Lsarpc.LsarSidArray();
            MsrpcGetMembersInAlias rpc = null;
            try
            {
                aliasHandle = new SamrAliasHandle(handle, domainHandle, unchecked(0x0002000c), rid);
                rpc = new MsrpcGetMembersInAlias(aliasHandle, sidarray);
                handle.Sendrecv(rpc);
                if (rpc.Retval != 0)
                {
                    throw new SmbException(rpc.Retval, false);
                }
                Sid[] sids = new Sid[rpc.Sids.NumSids];
                string originServer = handle.GetServer();
                NtlmPasswordAuthentication originAuth
                    = (NtlmPasswordAuthentication)handle.GetPrincipal();
                for (int i = 0; i < sids.Length; i++)
                {
                    sids[i] = new Sid(rpc.Sids.Sids[i].Sid, 0, null, null, false);
                    sids[i].OriginServer = originServer;
                    sids[i].OriginAuth = originAuth;
                }
                if (sids.Length > 0 && (flags & SidFlagResolveSids) != 0)
                {
                    ResolveSids(originServer, originAuth, sids);
                }
                return sids;
            }
            finally
            {
                if (aliasHandle != null)
                {
                    aliasHandle.Close();
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual Sid[] GetGroupMemberSids(string authorityServerName,
                                                NtlmPasswordAuthentication auth,
                                                int flags)
        {
            if (Type != SidTypeDomGrp && Type != SidTypeAlias)
            {
                return new Sid[0];
            }
            DcerpcHandle handle = null;
            SamrPolicyHandle policyHandle = null;
            SamrDomainHandle domainHandle = null;
            Sid domsid = GetDomainSid();
            lock (SidCache)
            {
                try
                {
                    handle = DcerpcHandle.GetHandle("ncacn_np:" + authorityServerName
                                                    + "[\\PIPE\\samr]", auth);
                    policyHandle = new SamrPolicyHandle(handle,
                                                        authorityServerName,
                                                        unchecked(0x00000030));
                    domainHandle = new SamrDomainHandle(handle,
                                                        policyHandle,
                                                        unchecked(0x00000200),
                                                        domsid);
                    return GetGroupMemberSids0(handle,
                                               domainHandle,
                                               domsid,
                                               GetRid(),
                                               flags);
                }
                finally
                {
                    if (handle != null)
                    {
                        if (policyHandle != null)
                        {
                            if (domainHandle != null)
                            {
                                domainHandle.Close();
                            }
                            policyHandle.Close();
                        }
                        handle.Close();
                    }
                }
            }
        }

        /// <summary>
        /// This specialized method returns a Map of users and local groups for the
        /// target server where keys are SIDs representing an account and each value
        /// is an List<object> of SIDs represents the local groups that the account is
        /// a member of.
        /// </summary>
        /// <remarks>
        /// This specialized method returns a Map of users and local groups for the
        /// target server where keys are SIDs representing an account and each value
        /// is an List<object> of SIDs represents the local groups that the account is
        /// a member of.
        /// <p/>
        /// This method is designed to assist with computing access control for a
        /// given user when the target object's ACL has local groups. Local groups
        /// are not listed in a user's group membership (e.g. as represented by the
        /// tokenGroups constructed attribute retrived via LDAP).
        /// <p/>
        /// Domain groups nested inside a local group are currently not expanded. In
        /// this case the key (SID) type will be SID_TYPE_DOM_GRP rather than
        /// SID_TYPE_USER.
        /// </remarks>
        /// <param name="authorityServerName">The server from which the local groups will be queried.
        /// </param>
        /// <param name="auth">The credentials required to query groups and group members.</param>
        /// <param name="flags">
        /// Flags that control the behavior of the operation. When all
        /// name associated with SIDs will be required, the SID_FLAG_RESOLVE_SIDS
        /// flag should be used which causes all group member SIDs to be resolved
        /// together in a single more efficient operation.
        /// </param>
        /// <exception cref="System.IO.IOException"></exception>
        internal static Hashtable GetLocalGroupsMap(string authorityServerName, NtlmPasswordAuthentication
             auth, int flags)
        {
            Sid domsid = GetServerSid(authorityServerName, auth);
            DcerpcHandle handle = null;
            SamrPolicyHandle policyHandle = null;
            SamrDomainHandle domainHandle = null;
            Samr.SamrSamArray sam = new Samr.SamrSamArray();
            MsrpcEnumerateAliasesInDomain rpc;
            lock (SidCache)
            {
                try
                {
                    handle = DcerpcHandle.GetHandle("ncacn_np:" + authorityServerName
                                                    + "[\\PIPE\\samr]", auth);
                    policyHandle = new SamrPolicyHandle(handle,
                                                        authorityServerName,
                                                        unchecked(0x02000000));
                    domainHandle = new SamrDomainHandle(handle,
                                                        policyHandle,
                                                        unchecked(0x02000000),
                                                        domsid);
                    rpc = new MsrpcEnumerateAliasesInDomain(domainHandle,
                                                            unchecked(0xFFFF),
                                                            sam);
                    handle.Sendrecv(rpc);
                    if (rpc.Retval != 0)
                    {
                        throw new SmbException(rpc.Retval, false);
                    }
                    Hashtable map = new Hashtable();
                    for (int ei = 0; ei < rpc.Sam.Count; ei++)
                    {
                        Samr.SamrSamEntry entry = rpc.Sam.Entries[ei];
                        Sid[] mems = GetGroupMemberSids0(handle,
                                                         domainHandle,
                                                         domsid,
                                                         entry.Idx,
                                                         flags);
                        Sid groupSid = new Sid(domsid, entry.Idx);
                        groupSid.Type = SidTypeAlias;
                        groupSid.DomainName = domsid.GetDomainName();
                        groupSid.AcctName = (new UnicodeString(entry.Name, false)).ToString();
                        for (int mi = 0; mi < mems.Length; mi++)
                        {
                            List<object> groups = (List<object>)map.Get(mems[mi]);
                            if (groups == null)
                            {
                                groups = new List<object>();
                                map.Put(mems[mi], groups);
                            }
                            if (!groups.Contains(groupSid))
                            {
                                groups.Add(groupSid);
                            }
                        }
                    }
                    return map;
                }
                finally
                {
                    if (handle != null)
                    {
                        if (policyHandle != null)
                        {
                            if (domainHandle != null)
                            {
                                domainHandle.Close();
                            }
                            policyHandle.Close();
                        }
                        handle.Close();
                    }
                }
            }
        }
    }
}
