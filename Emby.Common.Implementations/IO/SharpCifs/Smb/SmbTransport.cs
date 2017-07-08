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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using SharpCifs.Netbios;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;
using SharpCifs.Util.Transport;

namespace SharpCifs.Smb
{
    public class SmbTransport : Transport
    {
        internal static readonly byte[] Buf = new byte[0xFFFF];

        internal static readonly SmbComNegotiate NegotiateRequest = new SmbComNegotiate(
            );

        internal static LogStream LogStatic = LogStream.GetInstance();

        internal static Hashtable DfsRoots = null;


        internal static SmbTransport GetSmbTransport(UniAddress address, int port
            )
        {
            lock (typeof(SmbTransport))
            {
                return GetSmbTransport(address, port, SmbConstants.Laddr, SmbConstants.Lport, null);
            }
        }

        internal static SmbTransport GetSmbTransport(UniAddress address, int port
            , IPAddress localAddr, int localPort, string hostName)
        {
            lock (typeof(SmbTransport))
            {
                SmbTransport conn;

                lock (SmbConstants.Connections)
                {
                    if (SmbConstants.SsnLimit != 1)
                    {
                        conn =
                            SmbConstants.Connections.FirstOrDefault(
                                c =>
                                    c.Matches(address, port, localAddr, localPort, hostName) &&
                                    (SmbConstants.SsnLimit ==
                                     0 || c.Sessions.Count < SmbConstants.SsnLimit));

                        if (conn != null)
                        {
                            return conn;
                        }

                    }

                    conn = new SmbTransport(address, port, localAddr, localPort);
                    SmbConstants.Connections.Insert(0, conn);
                }
                return conn;
            }
        }

        internal class ServerData
        {
            internal byte Flags;

            internal int Flags2;

            internal int MaxMpxCount;

            internal int MaxBufferSize;

            internal int SessionKey;

            internal int Capabilities;

            internal string OemDomainName;

            internal int SecurityMode;

            internal int Security;

            internal bool EncryptedPasswords;

            internal bool SignaturesEnabled;

            internal bool SignaturesRequired;

            internal int MaxNumberVcs;

            internal int MaxRawSize;

            internal long ServerTime;

            internal int ServerTimeZone;

            internal int EncryptionKeyLength;

            internal byte[] EncryptionKey;

            internal byte[] Guid;

            internal ServerData(SmbTransport enclosing)
            {
                this._enclosing = enclosing;
            }

            private readonly SmbTransport _enclosing;
        }

        internal IPAddress LocalAddr;

        internal int LocalPort;

        internal UniAddress Address;

        internal SocketEx Socket;

        internal int Port;

        internal int Mid;

        internal OutputStream Out;

        internal InputStream In;

        internal byte[] Sbuf = new byte[512];

        internal SmbComBlankResponse Key = new SmbComBlankResponse();

        internal long SessionExpiration = Runtime.CurrentTimeMillis() + SmbConstants.SoTimeout;

        internal List<object> Referrals = new List<object>();

        internal SigningDigest Digest;

        internal List<SmbSession> Sessions = new List<SmbSession>();

        internal ServerData Server;

        internal int Flags2 = SmbConstants.Flags2;

        internal int MaxMpxCount = SmbConstants.MaxMpxCount;

        internal int SndBufSize = SmbConstants.SndBufSize;

        internal int RcvBufSize = SmbConstants.RcvBufSize;

        internal int Capabilities = SmbConstants.Capabilities;

        internal int SessionKey = 0x00000000;

        internal bool UseUnicode = SmbConstants.UseUnicode;

        internal string TconHostName;

        internal SmbTransport(UniAddress address, int port, IPAddress localAddr, int localPort
            )
        {
            Server = new ServerData(this);
            this.Address = address;
            this.Port = port;
            this.LocalAddr = localAddr;
            this.LocalPort = localPort;
        }

        internal virtual SmbSession GetSmbSession()
        {
            lock (this)
            {
                return GetSmbSession(new NtlmPasswordAuthentication(null, null, null));
            }
        }

        internal virtual SmbSession GetSmbSession(NtlmPasswordAuthentication auth)
        {
            lock (this)
            {
                SmbSession ssn;
                long now;

                ssn = Sessions.FirstOrDefault(s => s.Matches(auth));
                if (ssn != null)
                {
                    ssn.Auth = auth;
                    return ssn;
                }

                if (SmbConstants.SoTimeout > 0 && SessionExpiration < (now = Runtime.CurrentTimeMillis()))
                {
                    SessionExpiration = now + SmbConstants.SoTimeout;

                    foreach (var session in Sessions.Where(s => s.Expiration < now))
                    {
                        session.Logoff(false);
                    }
                }
                ssn = new SmbSession(Address, Port, LocalAddr, LocalPort, auth);
                ssn.transport = this;
                Sessions.Add(ssn);
                return ssn;
            }
        }

        internal virtual bool Matches(UniAddress address, int port, IPAddress localAddr,
            int localPort, string hostName)
        {
            if (hostName == null)
            {
                hostName = address.GetHostName();
            }
            return (TconHostName == null || Runtime.EqualsIgnoreCase(hostName, TconHostName)) && address.Equals(this.Address) && (port == -1 || port == this.Port
                 || (port == 445 && this.Port == 139)) && (localAddr == this.LocalAddr || (localAddr
                 != null && localAddr.Equals(this.LocalAddr))) && localPort == this.LocalPort;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual bool HasCapability(int cap)
        {
            try
            {
                Connect(SmbConstants.ResponseTimeout);
            }
            catch (IOException ioe)
            {
                throw new SmbException(ioe.Message, ioe);
            }
            return (Capabilities & cap) == cap;
        }

        internal virtual bool IsSignatureSetupRequired(NtlmPasswordAuthentication auth)
        {
            return (Flags2 & SmbConstants.Flags2SecuritySignatures) != 0 && Digest ==
                 null && auth != NtlmPasswordAuthentication.Null && NtlmPasswordAuthentication.Null
                .Equals(auth) == false;
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void Ssn139()
        {
            Name calledName = new Name(Address.FirstCalledName(), 0x20, null
                );
            do
            {
                Socket = new SocketEx(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (LocalAddr != null)
                {
                    Socket.Bind2(new IPEndPoint(LocalAddr, LocalPort));
                }

                Socket.Connect(new IPEndPoint(IPAddress.Parse(Address.GetHostAddress()), 139), SmbConstants.ConnTimeout);
                Socket.SoTimeOut = SmbConstants.SoTimeout;

                Out = Socket.GetOutputStream();
                In = Socket.GetInputStream();
                SessionServicePacket ssp = new SessionRequestPacket(calledName, NbtAddress.GetLocalName
                    ());
                Out.Write(Sbuf, 0, ssp.WriteWireFormat(Sbuf, 0));
                if (Readn(In, Sbuf, 0, 4) < 4)
                {
                    try
                    {
                        //Socket.`Close` method deleted
                        //Socket.Close();
                        Socket.Dispose();
                    }
                    catch (IOException)
                    {
                    }
                    throw new SmbException("EOF during NetBIOS session request");
                }
                switch (Sbuf[0] & 0xFF)
                {
                    case SessionServicePacket.PositiveSessionResponse:
                        {
                            if (Log.Level >= 4)
                            {
                                Log.WriteLine("session established ok with " + Address);
                            }
                            return;
                        }

                    case SessionServicePacket.NegativeSessionResponse:
                        {
                            int errorCode = In.Read() & 0xFF;
                            switch (errorCode)
                            {
                                case NbtException.CalledNotPresent:
                                case NbtException.NotListeningCalled:
                                    {
                                        //Socket.`Close` method deleted
                                        //Socket.Close();
                                        Socket.Dispose();
                                        break;
                                    }

                                default:
                                    {
                                        Disconnect(true);
                                        throw new NbtException(NbtException.ErrSsnSrvc, errorCode);
                                    }
                            }
                            break;
                        }

                    case -1:
                        {
                            Disconnect(true);
                            throw new NbtException(NbtException.ErrSsnSrvc, NbtException.ConnectionRefused
                                );
                        }

                    default:
                        {
                            Disconnect(true);
                            throw new NbtException(NbtException.ErrSsnSrvc, 0);
                        }
                }
            }
            while ((calledName.name = Address.NextCalledName()) != null);
            throw new IOException("Failed to establish session with " + Address);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void Negotiate(int port, ServerMessageBlock resp)
        {
            lock (Sbuf)
            {
                if (port == 139)
                {
                    Ssn139();
                }
                else
                {
                    if (port == -1)
                    {
                        port = SmbConstants.DefaultPort;
                    }
                    // 445
                    Socket = new SocketEx(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    if (LocalAddr != null)
                    {
                        Socket.Bind2(new IPEndPoint(LocalAddr, LocalPort));
                    }

                    Socket.Connect(new IPEndPoint(IPAddress.Parse(Address.GetHostAddress()), port), SmbConstants.ConnTimeout);
                    Socket.SoTimeOut = SmbConstants.SoTimeout;
                    Out = Socket.GetOutputStream();
                    In = Socket.GetInputStream();
                }
                if (++Mid == 32000)
                {
                    Mid = 1;
                }
                NegotiateRequest.Mid = Mid;
                int n = NegotiateRequest.Encode(Sbuf, 4);
                Encdec.Enc_uint32be(n & 0xFFFF, Sbuf, 0);
                if (Log.Level >= 4)
                {
                    Log.WriteLine(NegotiateRequest);
                    if (Log.Level >= 6)
                    {
                        Hexdump.ToHexdump(Log, Sbuf, 4, n);
                    }
                }
                Out.Write(Sbuf, 0, 4 + n);
                Out.Flush();
                if (PeekKey() == null)
                {
                    throw new IOException("transport closed in negotiate");
                }
                int size = Encdec.Dec_uint16be(Sbuf, 2) & 0xFFFF;
                if (size < 33 || (4 + size) > Sbuf.Length)
                {
                    throw new IOException("Invalid payload size: " + size);
                }
                Readn(In, Sbuf, 4 + 32, size - 32);
                resp.Decode(Sbuf, 4);
                if (Log.Level >= 4)
                {
                    Log.WriteLine(resp);
                    if (Log.Level >= 6)
                    {
                        Hexdump.ToHexdump(Log, Sbuf, 4, n);
                    }
                }
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        public virtual void Connect()
        {
            try
            {
                base.Connect(SmbConstants.ResponseTimeout);
            }
            catch (TransportException te)
            {
                throw new SmbException("Failed to connect: " + Address, te);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void DoConnect()
        {
            SmbComNegotiateResponse resp = new SmbComNegotiateResponse(Server);
            try
            {
                Negotiate(Port, resp);
            }
            catch (ConnectException)
            {
                Port = (Port == -1 || Port == SmbConstants.DefaultPort) ? 139 : SmbConstants.DefaultPort;
                Negotiate(Port, resp);
            }
            if (resp.DialectIndex > 10)
            {
                throw new SmbException("This client does not support the negotiated dialect.");
            }
            if ((Server.Capabilities & SmbConstants.CapExtendedSecurity) != SmbConstants.CapExtendedSecurity && Server
                .EncryptionKeyLength != 8 && SmbConstants.LmCompatibility == 0)
            {
                throw new SmbException("Unexpected encryption key length: " + Server.EncryptionKeyLength
                    );
            }
            TconHostName = Address.GetHostName();
            if (Server.SignaturesRequired || (Server.SignaturesEnabled && SmbConstants.Signpref))
            {
                Flags2 |= SmbConstants.Flags2SecuritySignatures;
            }
            else
            {
                Flags2 &= 0xFFFF ^ SmbConstants.Flags2SecuritySignatures;
            }
            MaxMpxCount = Math.Min(MaxMpxCount, Server.MaxMpxCount);
            if (MaxMpxCount < 1)
            {
                MaxMpxCount = 1;
            }
            SndBufSize = Math.Min(SndBufSize, Server.MaxBufferSize);
            Capabilities &= Server.Capabilities;
            if ((Server.Capabilities & SmbConstants.CapExtendedSecurity) == SmbConstants.CapExtendedSecurity)
            {
                Capabilities |= SmbConstants.CapExtendedSecurity;
            }
            // & doesn't copy high bit
            if ((Capabilities & SmbConstants.CapUnicode) == 0)
            {
                // server doesn't want unicode
                if (SmbConstants.ForceUnicode)
                {
                    Capabilities |= SmbConstants.CapUnicode;
                }
                else
                {
                    UseUnicode = false;
                    Flags2 &= 0xFFFF ^ SmbConstants.Flags2Unicode;
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void DoDisconnect(bool hard)
        {
            try
            {
                foreach (var ssn in Sessions)
                {
                    ssn.Logoff(hard);
                }

                Out.Close();
                In.Close();

                //Socket.`Close` method deleted
                //Socket.Close();
                Socket.Dispose();
            }
            finally
            {
                Digest = null;
                Socket = null;
                TconHostName = null;
            }

        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void MakeKey(ServerMessageBlock request)
        {
            if (++Mid == 32000)
            {
                Mid = 1;
            }
            request.Mid = Mid;
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override ServerMessageBlock PeekKey()
        {
            int n;
            do
            {
                if ((n = Readn(In, Sbuf, 0, 4)) < 4)
                {
                    return null;
                }
            }
            while (Sbuf[0] == 0x85);
            if ((n = Readn(In, Sbuf, 4, 32)) < 32)
            {
                return null;
            }
            if (Log.Level >= 4)
            {
                Log.WriteLine("New data read: " + this);
                Hexdump.ToHexdump(Log, Sbuf, 4, 32);
            }
            for (; ; )
            {
                if (Sbuf[0] == 0x00 && Sbuf[1] == 0x00 &&
                    Sbuf[4] == 0xFF &&
                    Sbuf[5] == 'S' &&
                    Sbuf[6] == 'M' &&
                    Sbuf[7] == 'B')
                {
                    break;
                }
                for (int i = 0; i < 35; i++)
                {
                    Sbuf[i] = Sbuf[i + 1];
                }
                int b;
                if ((b = In.Read()) == -1)
                {
                    return null;
                }
                Sbuf[35] = unchecked((byte)b);
            }
            Key.Mid = Encdec.Dec_uint16le(Sbuf, 34) & 0xFFFF;
            return Key;
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void DoSend(ServerMessageBlock request)
        {
            lock (Buf)
            {
                ServerMessageBlock smb = request;
                int n = smb.Encode(Buf, 4);
                Encdec.Enc_uint32be(n & 0xFFFF, Buf, 0);
                if (Log.Level >= 4)
                {
                    do
                    {
                        Log.WriteLine(smb);
                    }
                    while (smb is AndXServerMessageBlock && (smb = ((AndXServerMessageBlock)smb).Andx
                        ) != null);
                    if (Log.Level >= 6)
                    {
                        Hexdump.ToHexdump(Log, Buf, 4, n);
                    }
                }
                Out.Write(Buf, 0, 4 + n);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void DoSend0(ServerMessageBlock request)
        {
            try
            {
                DoSend(request);
            }
            catch (IOException ioe)
            {
                if (Log.Level > 2)
                {
                    Runtime.PrintStackTrace(ioe, Log);
                }
                try
                {
                    Disconnect(true);
                }
                catch (IOException ioe2)
                {
                    Runtime.PrintStackTrace(ioe2, Log);
                }
                throw;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void DoRecv(Response response)
        {
            ServerMessageBlock resp = (ServerMessageBlock)response;
            resp.UseUnicode = UseUnicode;
            resp.ExtendedSecurity = (Capabilities & SmbConstants.CapExtendedSecurity) == SmbConstants.CapExtendedSecurity;
            lock (Buf)
            {
                Array.Copy(Sbuf, 0, Buf, 0, 4 + SmbConstants.HeaderLength);
                int size = Encdec.Dec_uint16be(Buf, 2) & 0xFFFF;
                if (size < (SmbConstants.HeaderLength + 1) || (4 + size) > RcvBufSize)
                {
                    throw new IOException("Invalid payload size: " + size);
                }
                int errorCode = Encdec.Dec_uint32le(Buf, 9) & unchecked((int)(0xFFFFFFFF));
                if (resp.Command == ServerMessageBlock.SmbComReadAndx && (errorCode == 0 || errorCode
                     == unchecked((int)(0x80000005))))
                {
                    // overflow indicator normal for pipe
                    SmbComReadAndXResponse r = (SmbComReadAndXResponse)resp;
                    int off = SmbConstants.HeaderLength;
                    Readn(In, Buf, 4 + off, 27);
                    off += 27;
                    resp.Decode(Buf, 4);
                    int pad = r.DataOffset - off;
                    if (r.ByteCount > 0 && pad > 0 && pad < 4)
                    {
                        Readn(In, Buf, 4 + off, pad);
                    }
                    if (r.DataLength > 0)
                    {
                        Readn(In, r.B, r.Off, r.DataLength);
                    }
                }
                else
                {
                    Readn(In, Buf, 4 + 32, size - 32);
                    resp.Decode(Buf, 4);
                    if (resp is SmbComTransactionResponse)
                    {
                        ((SmbComTransactionResponse)resp).Current();
                    }
                }
                if (Digest != null && resp.ErrorCode == 0)
                {
                    Digest.Verify(Buf, 4, resp);
                }
                if (Log.Level >= 4)
                {
                    Log.WriteLine(response);
                    if (Log.Level >= 6)
                    {
                        Hexdump.ToHexdump(Log, Buf, 4, size);
                    }
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        protected internal override void DoSkip()
        {
            int size = Encdec.Dec_uint16be(Sbuf, 2) & 0xFFFF;
            if (size < 33 || (4 + size) > RcvBufSize)
            {
                In.Skip(In.Available());
            }
            else
            {
                In.Skip(size - 32);
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void CheckStatus(ServerMessageBlock req, ServerMessageBlock resp
            )
        {
            resp.ErrorCode = SmbException.GetStatusByCode(resp.ErrorCode);
            switch (resp.ErrorCode)
            {
                case NtStatus.NtStatusOk:
                    {
                        break;
                    }

                case NtStatus.NtStatusAccessDenied:
                case NtStatus.NtStatusWrongPassword:
                case NtStatus.NtStatusLogonFailure:
                case NtStatus.NtStatusAccountRestriction:
                case NtStatus.NtStatusInvalidLogonHours:
                case NtStatus.NtStatusInvalidWorkstation:
                case NtStatus.NtStatusPasswordExpired:
                case NtStatus.NtStatusAccountDisabled:
                case NtStatus.NtStatusAccountLockedOut:
                case NtStatus.NtStatusTrustedDomainFailure:
                    {
                        throw new SmbAuthException(resp.ErrorCode);
                    }

                case NtStatus.NtStatusPathNotCovered:
                    {
                        if (req.Auth == null)
                        {
                            throw new SmbException(resp.ErrorCode, null);
                        }
                        DfsReferral dr = GetDfsReferrals(req.Auth, req.Path, 1);
                        if (dr == null)
                        {
                            throw new SmbException(resp.ErrorCode, null);
                        }
                        SmbFile.Dfs.Insert(req.Path, dr);
                        throw dr;
                    }

                case unchecked((int)(0x80000005)):
                    {
                        break;
                    }

                case NtStatus.NtStatusMoreProcessingRequired:
                    {
                        break;
                    }

                default:
                    {
                        throw new SmbException(resp.ErrorCode, null);
                    }
            }
            if (resp.VerifyFailed)
            {
                throw new SmbException("Signature verification failed.");
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual void Send(ServerMessageBlock request, ServerMessageBlock response
            )
        {
            Connect();
            request.Flags2 |= Flags2;
            request.UseUnicode = UseUnicode;
            request.Response = response;
            if (request.Digest == null)
            {
                request.Digest = Digest;
            }
            try
            {
                if (response == null)
                {
                    DoSend0(request);
                    return;
                }
                if (request is SmbComTransaction)
                {
                    response.Command = request.Command;
                    SmbComTransaction req = (SmbComTransaction)request;
                    SmbComTransactionResponse resp = (SmbComTransactionResponse)response;
                    req.MaxBufferSize = SndBufSize;
                    resp.Reset();
                    try
                    {
                        BufferCache.GetBuffers(req, resp);
                        req.Current();
                        if (req.MoveNext())
                        {
                            SmbComBlankResponse interim = new SmbComBlankResponse();
                            Sendrecv(req, interim, SmbConstants.ResponseTimeout);
                            if (interim.ErrorCode != 0)
                            {
                                CheckStatus(req, interim);
                            }
                            req.Current();
                        }
                        else
                        {
                            MakeKey(req);
                        }
                        lock (this)
                        {
                            response.Received = false;
                            resp.IsReceived = false;
                            try
                            {
                                ResponseMap.Put(req, resp);
                                do
                                {
                                    DoSend0(req);
                                }
                                while (req.MoveNext() && req.Current() != null);
                                long timeout = SmbConstants.ResponseTimeout;
                                resp.Expiration = Runtime.CurrentTimeMillis() + timeout;
                                while (resp.MoveNext())
                                {
                                    Runtime.Wait(this, timeout);
                                    timeout = resp.Expiration - Runtime.CurrentTimeMillis();
                                    if (timeout <= 0)
                                    {
                                        throw new TransportException(this + " timedout waiting for response to " + req);
                                    }
                                }
                                if (response.ErrorCode != 0)
                                {
                                    CheckStatus(req, resp);
                                }
                            }
                            catch (Exception ie)
                            {
                                if (ie is SmbException)
                                {
                                    throw;
                                }
                                else
                                {
                                    throw new TransportException(ie);                                    
                                }
                            }
                            finally
                            {
                                //Sharpen.Collections.Remove<Hashtable, SmbComTransaction>(response_map, req);
                                ResponseMap.Remove(req);
                            }
                        }
                    }
                    finally
                    {
                        BufferCache.ReleaseBuffer(req.TxnBuf);
                        BufferCache.ReleaseBuffer(resp.TxnBuf);
                    }
                }
                else
                {
                    response.Command = request.Command;
                    Sendrecv(request, response, SmbConstants.ResponseTimeout);
                }
            }
            catch (SmbException se)
            {
                throw;
            }
            catch (IOException ioe)
            {
                throw new SmbException(ioe.Message, ioe);
            }
            CheckStatus(request, response);
        }

        public override string ToString()
        {
            return base.ToString() + "[" + Address + ":" + Port + "]";
        }

        internal virtual void DfsPathSplit(string path, string[] result)
        {
            int ri = 0;
            int rlast = result.Length - 1;
            int i = 0;
            int b = 0;
            int len = path.Length;
            do
            {
                if (ri == rlast)
                {
                    result[rlast] = Runtime.Substring(path, b);
                    return;
                }
                if (i == len || path[i] == '\\')
                {
                    result[ri++] = Runtime.Substring(path, b, i);
                    b = i + 1;
                }
            }
            while (i++ < len);
            while (ri < result.Length)
            {
                result[ri++] = string.Empty;
            }
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual DfsReferral GetDfsReferrals(NtlmPasswordAuthentication auth, string
             path, int rn)
        {
            SmbTree ipc = GetSmbSession(auth).GetSmbTree("IPC$", null);
            Trans2GetDfsReferralResponse resp = new Trans2GetDfsReferralResponse();
            ipc.Send(new Trans2GetDfsReferral(path), resp);
            if (resp.NumReferrals == 0)
            {
                return null;
            }
            if (rn == 0 || resp.NumReferrals < rn)
            {
                rn = resp.NumReferrals;
            }
            DfsReferral dr = new DfsReferral();
            string[] arr = new string[4];
            long expiration = Runtime.CurrentTimeMillis() + Dfs.Ttl * 1000;
            int di = 0;
            for (; ; )
            {
                dr.ResolveHashes = auth.HashesExternal;
                dr.Ttl = resp.Referrals[di].Ttl;
                dr.Expiration = expiration;
                if (path.Equals(string.Empty))
                {
                    dr.Server = Runtime.Substring(resp.Referrals[di].Path, 1).ToLower();
                }
                else
                {
                    DfsPathSplit(resp.Referrals[di].Node, arr);
                    dr.Server = arr[1];
                    dr.Share = arr[2];
                    dr.Path = arr[3];
                }
                dr.PathConsumed = resp.PathConsumed;
                di++;
                if (di == rn)
                {
                    break;
                }
                dr.Append(new DfsReferral());
                dr = dr.Next;
            }
            return dr.Next;
        }

        /// <exception cref="SharpCifs.Smb.SmbException"></exception>
        internal virtual DfsReferral[] __getDfsReferrals(NtlmPasswordAuthentication auth,
            string path, int rn)
        {
            SmbTree ipc = GetSmbSession(auth).GetSmbTree("IPC$", null);
            Trans2GetDfsReferralResponse resp = new Trans2GetDfsReferralResponse();
            ipc.Send(new Trans2GetDfsReferral(path), resp);
            if (rn == 0 || resp.NumReferrals < rn)
            {
                rn = resp.NumReferrals;
            }
            DfsReferral[] drs = new DfsReferral[rn];
            string[] arr = new string[4];
            long expiration = Runtime.CurrentTimeMillis() + Dfs.Ttl * 1000;
            for (int di = 0; di < drs.Length; di++)
            {
                DfsReferral dr = new DfsReferral();
                dr.ResolveHashes = auth.HashesExternal;
                dr.Ttl = resp.Referrals[di].Ttl;
                dr.Expiration = expiration;
                if (path.Equals(string.Empty))
                {
                    dr.Server = Runtime.Substring(resp.Referrals[di].Path, 1).ToLower();
                }
                else
                {
                    DfsPathSplit(resp.Referrals[di].Node, arr);
                    dr.Server = arr[1];
                    dr.Share = arr[2];
                    dr.Path = arr[3];
                }
                dr.PathConsumed = resp.PathConsumed;
                drs[di] = dr;
            }
            return drs;
        }
    }
}
