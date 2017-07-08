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
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

using Thread = SharpCifs.Util.Sharpen.Thread;

namespace SharpCifs.Netbios
{
    internal class NameServiceClient : IRunnable
    {
        internal const int DefaultSoTimeout = 5000;

        internal const int DefaultRcvBufSize = 576;

        internal const int DefaultSndBufSize = 576;

        internal const int NameServiceUdpPort = 137;

        internal const int DefaultRetryCount = 2;

        internal const int DefaultRetryTimeout = 3000;

        internal const int ResolverLmhosts = 1;

        internal const int ResolverBcast = 2;

        internal const int ResolverWins = 3;

        private static readonly int SndBufSize = Config.GetInt("jcifs.netbios.snd_buf_size"
            , DefaultSndBufSize);

        private static readonly int RcvBufSize = Config.GetInt("jcifs.netbios.rcv_buf_size"
            , DefaultRcvBufSize);

        private static readonly int SoTimeout = Config.GetInt("jcifs.netbios.soTimeout",
            DefaultSoTimeout);

        private static readonly int RetryCount = Config.GetInt("jcifs.netbios.retryCount"
            , DefaultRetryCount);

        private static readonly int RetryTimeout = Config.GetInt("jcifs.netbios.retryTimeout"
            , DefaultRetryTimeout);

        private static readonly int Lport = Config.GetInt("jcifs.netbios.lport", 137);

        private static readonly IPAddress Laddr = Config.GetInetAddress("jcifs.netbios.laddr"
            , null);

        private static readonly string Ro = Config.GetProperty("jcifs.resolveOrder");

        private static LogStream _log = LogStream.GetInstance();

        private readonly object _lock = new object();

        private int _lport;

        private int _closeTimeout;

        private byte[] _sndBuf;

        private byte[] _rcvBuf;

        private SocketEx _socket;

        private Hashtable _responseTable = new Hashtable();

        private Thread _thread;
        
        private int _nextNameTrnId;

        private int[] _resolveOrder;

        private bool _waitResponse = true;

        private AutoResetEvent _autoResetWaitReceive;

        internal IPAddress laddr;

        internal IPAddress Baddr;

        public NameServiceClient()
            : this(Lport, Laddr)
        {
        }

        internal NameServiceClient(int lport, IPAddress laddr)
        {
            this._lport = lport;

            this.laddr = laddr 
                            ?? Config.GetLocalHost() 
                            ?? Extensions.GetAddressesByName(Dns.GetHostName()).FirstOrDefault();

            try
            {
                Baddr = Config.GetInetAddress("jcifs.netbios.baddr", Extensions.GetAddressByName("255.255.255.255"));
            }
            catch (Exception ex)
            {
            }

            _sndBuf = new byte[SndBufSize];
            _rcvBuf = new byte[RcvBufSize];


            if (string.IsNullOrEmpty(Ro))
            {
                if (NbtAddress.GetWinsAddress() == null)
                {
                    _resolveOrder = new int[2];
                    _resolveOrder[0] = ResolverLmhosts;
                    _resolveOrder[1] = ResolverBcast;
                }
                else
                {
                    _resolveOrder = new int[3];
                    _resolveOrder[0] = ResolverLmhosts;
                    _resolveOrder[1] = ResolverWins;
                    _resolveOrder[2] = ResolverBcast;
                }
            }
            else
            {
                int[] tmp = new int[3];
                StringTokenizer st = new StringTokenizer(Ro, ",");
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
                            if (NbtAddress.GetWinsAddress() == null)
                            {
                                if (_log.Level > 1)
                                {
                                    _log.WriteLine("NetBIOS resolveOrder specifies WINS however the " + "jcifs.netbios.wins property has not been set"
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
                                }
                                else
                                {
                                    // skip
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

        internal virtual int GetNextNameTrnId()
        {
            if ((++_nextNameTrnId & unchecked(0xFFFF)) == 0)
            {
                _nextNameTrnId = 1;
            }
            return _nextNameTrnId;
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void EnsureOpen(int timeout)
        {
            _closeTimeout = 0;
            if (SoTimeout != 0)
            {
                _closeTimeout = Math.Max(SoTimeout, timeout);
            }
            // If socket is still good, the new closeTimeout will
            // be ignored; see tryClose comment.
            if (_socket == null)
            {
                _socket = new SocketEx(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                
                //IPAddress.`Address` property deleted
                //_socket.Bind(new IPEndPoint(laddr.Address, _lport));
                _socket.Bind(new IPEndPoint(laddr, _lport));

                if (_waitResponse)
                {
                    _thread = new Thread(this); //new Sharpen.Thread(this, "JCIFS-NameServiceClient");
                    _thread.SetDaemon(true);
                    _thread.Start();                    
                }
            }
        }

        internal virtual void TryClose()
        {
            lock (_lock)
            {
                if (_socket != null)
                {
                    //Socket.`Close` method deleted
                    //_socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
                _thread = null;

                if (_waitResponse)
                {
                    _responseTable.Clear();
                } else
                {
                    _autoResetWaitReceive.Set();
                }
            }
        }

        public virtual void Run()
        {
            int nameTrnId;
            NameServicePacket response;

            try
            {

                while (_thread == Thread.CurrentThread())
                {
                    _socket.SoTimeOut = _closeTimeout;

                    int len = _socket.Receive(_rcvBuf, 0, RcvBufSize);

                    if (_log.Level > 3)
                    {
                        _log.WriteLine("NetBIOS: new data read from socket");
                    }
                    nameTrnId = NameServicePacket.ReadNameTrnId(_rcvBuf, 0);
                    response = (NameServicePacket)_responseTable.Get(nameTrnId);


                    if (response == null || response.Received)
                    {
                        continue;
                    }

                    lock (response)
                    {
                        response.ReadWireFormat(_rcvBuf, 0);

                        if (_log.Level > 3)
                        {
                            _log.WriteLine(response);
                            Hexdump.ToHexdump(_log, _rcvBuf, 0, len);
                        }

                        if (response.IsResponse)
                        {
                            response.Received = true;

                            Runtime.Notify(response);
                        }
                    }
                }

            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                if (_log.Level > 2)
                {
                    Runtime.PrintStackTrace(ex, _log);
                }
            }
            finally
            {
                TryClose();
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void Send(NameServicePacket request, NameServicePacket response,
            int timeout)
        {
            int nid = 0;
            int max = NbtAddress.Nbns.Length;
            if (max == 0)
            {
                max = 1;
            }

            lock (response)
            {

                while (max-- > 0)
                {
                    try
                    {
                        lock (_lock)
                        {
                            request.NameTrnId = GetNextNameTrnId();
                            nid = request.NameTrnId;
                            response.Received = false;
                            _responseTable.Put(nid, response);
                            EnsureOpen(timeout + 1000);
                            int requestLenght = request.WriteWireFormat(_sndBuf, 0);
                            _socket.Send(_sndBuf, 0, requestLenght, new IPEndPoint(request.Addr, _lport));
                            if (_log.Level > 3)
                            {
                                _log.WriteLine(request);
                                Hexdump.ToHexdump(_log, _sndBuf, 0, requestLenght);
                            }

                        }
                        if (_waitResponse)
                        {
                            long start = Runtime.CurrentTimeMillis();
                            while (timeout > 0)
                            {
                                Runtime.Wait(response, timeout);
                                if (response.Received && request.QuestionType == response.RecordType)
                                {
                                    return;
                                }
                                response.Received = false;
                                timeout -= (int)(Runtime.CurrentTimeMillis() - start);
                            }
                        }
                    }
                    catch (Exception ie)
                    {
                        throw new IOException(ie.Message);
                    }
                    finally
                    {
                        //Sharpen.Collections.Remove(responseTable, nid);
                        if (_waitResponse)
                        {
                            _responseTable.Remove(nid);
                        }
                    }
                    if (_waitResponse)
                    {
                        lock (_lock)
                        {
                            if (NbtAddress.IsWins(request.Addr) == false)
                            {
                                break;
                            }
                            if (request.Addr == NbtAddress.GetWinsAddress())
                            {
                                NbtAddress.SwitchWins();
                            }
                            request.Addr = NbtAddress.GetWinsAddress();
                        }
                    }
                }
            }
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual NbtAddress[] GetAllByName(Name name, IPAddress addr)
        {
            int n;
            NameQueryRequest request = new NameQueryRequest(name);
            NameQueryResponse response = new NameQueryResponse();
            request.Addr = addr ?? NbtAddress.GetWinsAddress();
            request.IsBroadcast = request.Addr == null;
            if (request.IsBroadcast)
            {
                request.Addr = Baddr;
                n = RetryCount;
            }
            else
            {
                request.IsBroadcast = false;
                n = 1;
            }
            do
            {
                try
                {
                    Send(request, response, RetryTimeout);
                }
                catch (IOException ioe)
                {
                    if (_log.Level > 1)
                    {
                        Runtime.PrintStackTrace(ioe, _log);
                    }
                    throw new UnknownHostException(ioe);
                }
                if (response.Received && response.ResultCode == 0)
                {
                    return response.AddrEntry;
                }
            }
            while (--n > 0 && request.IsBroadcast);
            throw new UnknownHostException();
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual NbtAddress GetByName(Name name, IPAddress addr)
        {
            int n;

            NameQueryRequest request = new NameQueryRequest(name);
            NameQueryResponse response = new NameQueryResponse();
            if (addr != null)
            {
                request.Addr = addr;
                request.IsBroadcast = (addr.GetAddressBytes()[3] == unchecked(unchecked(0xFF)));
                n = RetryCount;
                do
                {
                    try
                    {
                        Send(request, response, RetryTimeout);
                    }
                    catch (IOException ioe)
                    {
                        if (_log.Level > 1)
                        {
                            Runtime.PrintStackTrace(ioe, _log);
                        }
                        throw new UnknownHostException(ioe);
                    }
                    if (response.Received && response.ResultCode == 0
                        && response.IsResponse)
                    {
                        int last = response.AddrEntry.Length - 1;
                        response.AddrEntry[last].HostName.SrcHashCode = addr.GetHashCode();
                        return response.AddrEntry[last];
                    }
                }
                while (--n > 0 && request.IsBroadcast);
                throw new UnknownHostException();
            }
            for (int i = 0; i < _resolveOrder.Length; i++)
            {
                try
                {
                    switch (_resolveOrder[i])
                    {
                        case ResolverLmhosts:
                            {
                                NbtAddress ans = Lmhosts.GetByName(name);
                                if (ans != null)
                                {
                                    ans.HostName.SrcHashCode = 0;
                                    // just has to be different
                                    // from other methods
                                    return ans;
                                }
                                break;
                            }

                        case ResolverWins:
                        case ResolverBcast:
                            {
                                if (_resolveOrder[i] == ResolverWins && name.name != NbtAddress.MasterBrowserName
                                     && name.HexCode != unchecked(0x1d))
                                {
                                    request.Addr = NbtAddress.GetWinsAddress();
                                    request.IsBroadcast = false;
                                }
                                else
                                {
                                    request.Addr = Baddr;
                                    request.IsBroadcast = true;
                                }
                                n = RetryCount;
                                while (n-- > 0)
                                {
                                    try
                                    {
                                        Send(request, response, RetryTimeout);
                                    }
                                    catch (IOException ioe)
                                    {
                                        if (_log.Level > 1)
                                        {
                                            Runtime.PrintStackTrace(ioe, _log);
                                        }
                                        throw new UnknownHostException(ioe);
                                    }
                                    if (response.Received && response.ResultCode == 0
                                        && response.IsResponse)
                                    {

                                        response.AddrEntry[0].HostName.SrcHashCode = request.Addr.GetHashCode();
                                        return response.AddrEntry[0];
                                    }
                                    if (_resolveOrder[i] == ResolverWins)
                                    {
                                        break;
                                    }
                                }
                                break;
                            }
                    }
                }
                catch (IOException)
                {
                }
            }
            throw new UnknownHostException();
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual NbtAddress[] GetNodeStatus(NbtAddress addr)
        {
            int n;
            int srcHashCode;
            NodeStatusRequest request;
            NodeStatusResponse response;
            response = new NodeStatusResponse(addr);
            request = new NodeStatusRequest(new Name(NbtAddress.AnyHostsName, unchecked(0x00), null));
            request.Addr = addr.GetInetAddress();
            n = RetryCount;
            while (n-- > 0)
            {
                try
                {
                    Send(request, response, RetryTimeout);
                }
                catch (IOException ioe)
                {
                    if (_log.Level > 1)
                    {
                        Runtime.PrintStackTrace(ioe, _log);
                    }
                    throw new UnknownHostException(ioe);
                }
                if (response.Received && response.ResultCode == 0)
                {
                    srcHashCode = request.Addr.GetHashCode();
                    for (int i = 0; i < response.AddressArray.Length; i++)
                    {
                        response.AddressArray[i].HostName.SrcHashCode = srcHashCode;
                    }
                    return response.AddressArray;
                }
            }
            throw new UnknownHostException();
        }

        internal virtual NbtAddress[] GetHosts()
        {
            try
            {
                _waitResponse = false;

                byte[] bAddrBytes = laddr.GetAddressBytes();

                for (int i = 1; i <= 254; i++)
                {
                    NodeStatusRequest request;
                    NodeStatusResponse response;

                    byte[] addrBytes = {
                        bAddrBytes[0],
                        bAddrBytes[1],
                        bAddrBytes[2],
                        (byte)i
                    };

                    IPAddress addr = new IPAddress(addrBytes);

                    //response = new NodeStatusResponse(new NbtAddress(NbtAddress.UnknownName,
                    //    (int)addr.Address, false, 0x20));
                    response = new NodeStatusResponse(new NbtAddress(NbtAddress.UnknownName,
                        BitConverter.ToInt32(addr.GetAddressBytes(), 0) , false, 0x20));

                    request = new NodeStatusRequest(new Name(NbtAddress.AnyHostsName, unchecked(0x20), null));
                    request.Addr = addr;
                    Send(request, response, 0);
                }

            }
            catch (IOException ioe)
            {
                if (_log.Level > 1)
                {
                    Runtime.PrintStackTrace(ioe, _log);
                }
                throw new UnknownHostException(ioe);
            }
            
            _autoResetWaitReceive = new AutoResetEvent(false);
            _thread = new Thread(this); 
            _thread.SetDaemon(true);
            _thread.Start();

            _autoResetWaitReceive.WaitOne();         

            List<NbtAddress> result = new List<NbtAddress>();

            foreach (var key in _responseTable.Keys)
            {
                NodeStatusResponse resp = (NodeStatusResponse)_responseTable[key];

                if (resp.Received && resp.ResultCode == 0)
                {
                    foreach (var entry in resp.AddressArray)
                    {
                        if (entry.HostName.HexCode == 0x20)
                        {
                            result.Add(entry);
                        }
                    }
                }
            }

            _responseTable.Clear();

            _waitResponse = true;

            return result.Count > 0 ? result.ToArray() : null;
        }
    }
}
