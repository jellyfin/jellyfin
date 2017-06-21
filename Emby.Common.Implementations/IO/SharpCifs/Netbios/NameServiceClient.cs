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
using SharpCifs.Smb;
using SharpCifs.Util;
using SharpCifs.Util.DbsHelper;
using SharpCifs.Util.Sharpen;

using Thread = SharpCifs.Util.Sharpen.Thread;
using System.Threading.Tasks;

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

        private static readonly int SndBufSize
            = Config.GetInt("jcifs.netbios.snd_buf_size", DefaultSndBufSize);

        private static readonly int RcvBufSize
            = Config.GetInt("jcifs.netbios.rcv_buf_size", DefaultRcvBufSize);

        private static readonly int SoTimeout
            = Config.GetInt("jcifs.netbios.soTimeout", DefaultSoTimeout);

        private static readonly int RetryCount
            = Config.GetInt("jcifs.netbios.retryCount", DefaultRetryCount);

        private static readonly int RetryTimeout
            = Config.GetInt("jcifs.netbios.retryTimeout", DefaultRetryTimeout);

        private static readonly int Lport
            = Config.GetInt("jcifs.netbios.lport", 137);

        private static readonly IPAddress Laddr
            = Config.GetInetAddress("jcifs.netbios.laddr", null);

        private static readonly string Ro
            = Config.GetProperty("jcifs.resolveOrder");

        private static LogStream _log = LogStream.GetInstance();

        private readonly object _lock = new object();

        private int _lport;

        private int _closeTimeout;

        private byte[] _sndBuf;

        private byte[] _rcvBuf;

        private SocketEx _socketSender;

        private Hashtable _responseTable = new Hashtable();

        private Thread _thread;

        private int _nextNameTrnId;

        private int[] _resolveOrder;

        private bool _waitResponse = true;

        private bool _isActive = false;

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
                            ?? Extensions.GetLocalAddresses()?.FirstOrDefault();

            if (this.laddr == null)
                throw new ArgumentNullException("IPAddress NOT found. if exec on localhost, set vallue to [jcifs.smb.client.laddr]");

            try
            {
                Baddr = Config.GetInetAddress("jcifs.netbios.baddr",
                                              Extensions.GetAddressByName("255.255.255.255"));
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
                                    _log.WriteLine("NetBIOS resolveOrder specifies WINS however the "
                                                   + "jcifs.netbios.wins property has not been set");
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
            //Log.Out($"NameServiceClient.EnsureOpen");

            _closeTimeout = 0;
            if (SoTimeout != 0)
            {
                _closeTimeout = Math.Max(SoTimeout, timeout);
            }

            var localPort = (SmbConstants.Lport == 0) ? _lport : SmbConstants.Lport;

            // If socket is still good, the new closeTimeout will
            // be ignored; see tryClose comment.
            if (
                _socketSender == null
                || _socketSender.LocalEndPoint == null
                || _socketSender.GetLocalPort() != localPort
                || !IPAddress.Any.Equals(_socketSender.GetLocalInetAddress())
            )
            {
                if (_socketSender != null)
                {
                    _socketSender.Dispose();
                    _socketSender = null;
                }

                _socketSender = new SocketEx(AddressFamily.InterNetwork, 
                                             SocketType.Dgram, 
                                             ProtocolType.Udp);

                _socketSender.Bind(new IPEndPoint(IPAddress.Any, localPort));


                if (_waitResponse)
                {
                    if (_thread != null)
                    {
                        _thread.Cancel(true);
                        _thread.Dispose();
                    }

                    _thread = new Thread(this);
                    _thread.SetDaemon(true);
                    _thread.Start(true);
                }
            }
        }

        internal virtual void TryClose()
        {
            //Log.Out("NameSerciceClient.TryClose");

            if (this._isActive)
            {
                //Log.Out("NameSerciceClient.TryClose - Now in Processing... Exit.");
                return;
            }

            lock (_lock)
            {
                if (_socketSender != null)
                {
                    _socketSender.Dispose();
                    _socketSender = null;
                    //Log.Out("NameSerciceClient.TryClose - _socketSender.Disposed");
                }

                if (_thread != null)
                {
                    _thread.Cancel(true);
                    _thread.Dispose();
                    _thread = null;
                    //Log.Out("NameSerciceClient.TryClose - _thread.Aborted");
                }

                if (_waitResponse)
                {
                    _responseTable.Clear();
                }
                else
                {
                    _autoResetWaitReceive.Set();
                }
            }
        }


        private int _recievedLength = -1;
        public virtual void Run()
        {
            int nameTrnId;
            NameServicePacket response;

            try
            {
                while (Thread.CurrentThread().Equals(_thread))
                {
                    if (_thread.IsCanceled)
                        break;

                    var localPort = (SmbConstants.Lport == 0) ? _lport : SmbConstants.Lport;

                    var sockEvArg = new SocketAsyncEventArgs();
                    sockEvArg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                    sockEvArg.SetBuffer(_rcvBuf, 0, RcvBufSize);
                    sockEvArg.Completed += this.OnReceiveCompleted;

                    _socketSender.SoTimeOut = _closeTimeout;

                    this._recievedLength = -1;

                    //Log.Out($"NameServiceClient.Run - Wait Recieve: {IPAddress.Any}: {localPort}");
                    _socketSender.ReceiveFromAsync(sockEvArg);

                    while (this._recievedLength == -1)
                    {
                        if (_thread.IsCanceled)
                            break;

                        Task.Delay(300).GetAwaiter().GetResult();
                    }

                    sockEvArg?.Dispose();


                    if (_thread.IsCanceled)
                        break;

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
                        if (_thread.IsCanceled)
                            break;

                        response.ReadWireFormat(_rcvBuf, 0);

                        if (_log.Level > 3)
                        {
                            _log.WriteLine(response);
                            Hexdump.ToHexdump(_log, _rcvBuf, 0, this._recievedLength);
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


        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            //Log.Out("NameServiceClient.OnReceiveCompleted");
            this._recievedLength = e.BytesTransferred;
        }


        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void Send(NameServicePacket request,
                                   NameServicePacket response,
                                   int timeout)
        {
            //Log.Out("NameSerciceClient.Send - Start");

            int nid = 0;
            int max = NbtAddress.Nbns.Length;
            if (max == 0)
            {
                max = 1;
            }

            lock (response)
            {
                this._isActive = true;

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

                            //Log.Out($"NameSerciceClient.Send - timeout = {timeout}");
                            EnsureOpen(timeout + 1000);
                            
                            int requestLenght = request.WriteWireFormat(_sndBuf, 0);
                            byte[] msg = new byte[requestLenght];
                            Array.Copy(_sndBuf, msg, requestLenght);

                            _socketSender.SetSocketOption(SocketOptionLevel.Socket,
                                                          SocketOptionName.Broadcast,
                                                          request.IsBroadcast
                                                            ? 1
                                                            : 0);

                            _socketSender.SendTo(msg, new IPEndPoint(request.Addr, _lport));
                            //Log.Out("NameSerciceClient.Send - Sended");

                            if (_log.Level > 3)
                            {
                                _log.WriteLine(request);
                                Hexdump.ToHexdump(_log, _sndBuf, 0, requestLenght);
                            }
                        }

                        if (_waitResponse)
                        {
                            long start = Runtime.CurrentTimeMillis();
                            var isRecieved = false;
                            var startTime = DateTime.Now;
                            while (timeout > 0)
                            {
                                Runtime.Wait(response, timeout);
                                if (response.Received && request.QuestionType == response.RecordType)
                                {
                                    //return;
                                    isRecieved = true;
                                    break;
                                }
                                response.Received = false;
                                timeout -= (int)(Runtime.CurrentTimeMillis() - start);

                                //if (timeout <= 0)
                                //{
                                //    Log.Out($"NameSerciceClient.Send Timeout! - {(DateTime.Now - startTime).TotalMilliseconds} msec");
                                //}
                            }
                            
                            if (isRecieved)
                                break;
                        }
                    }
                    catch (Exception ie)
                    {
                        if (_waitResponse)
                            _responseTable.Remove(nid);

                        //Log.Out("NameSerciceClient.Send - IOException");

                        throw new IOException(ie.Message);
                    }
                    finally
                    {
                        if (_waitResponse)
                            _responseTable.Remove(nid);
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

                this._isActive = false;
                //Log.Out("NameSerciceClient.Send - Normaly Ended.");
            }
        }

        /// <exception cref="UnknownHostException"></exception>
        internal virtual NbtAddress[] GetAllByName(Name name, IPAddress addr)
        {
            //Log.Out("NameSerciceClient.GetAllByName");

            int n;
            NameQueryRequest request = new NameQueryRequest(name);
            NameQueryResponse response = new NameQueryResponse();
            request.Addr = addr ?? NbtAddress.GetWinsAddress();
            request.IsBroadcast = (request.Addr == null
                                    || request.Addr.ToString() == Baddr.ToString());

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
            //Log.Out("NameSerciceClient.GetByName");

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

                    if (response.Received 
                        && response.ResultCode == 0
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
                                if (_resolveOrder[i] == ResolverWins
                                    && name.name != NbtAddress.MasterBrowserName
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
                                    if (response.Received 
                                        && response.ResultCode == 0
                                        && response.IsResponse)
                                    {
                                        response.AddrEntry[0].HostName.SrcHashCode
                                            = request.Addr.GetHashCode();
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
            //Log.Out("NameSerciceClient.GetNodeStatus");

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
            //Log.Out("NbtServiceClient.GetHosts");

            try
            {
                _waitResponse = false;

                byte[] bAddrBytes = laddr.GetAddressBytes();

                for (int i = 1; i <= 254; i++)
                {
                    //Log.Out($"NbtServiceClient.GetHosts - {i}");

                    NodeStatusRequest request;
                    NodeStatusResponse response;

                    byte[] addrBytes = {
                        bAddrBytes[0],
                        bAddrBytes[1],
                        bAddrBytes[2],
                        (byte)i
                    };

                    IPAddress addr = new IPAddress(addrBytes);

                    response = new NodeStatusResponse(
                        new NbtAddress(NbtAddress.UnknownName,
                                       BitConverter.ToInt32(addr.GetAddressBytes(), 0),
                                       false,
                                       0x20)
                    );

                    request = new NodeStatusRequest(new Name(NbtAddress.AnyHostsName,
                                                    unchecked(0x20),
                                                    null))
                    {
                        Addr = addr
                    };

                    Send(request, response, 0);
                }
            }
            catch (IOException ioe)
            {
                //Log.Out(ioe);

                if (_log.Level > 1)
                {
                    Runtime.PrintStackTrace(ioe, _log);
                }
                throw new UnknownHostException(ioe);
            }

            _autoResetWaitReceive = new AutoResetEvent(false);

            if (_thread != null)
            {
                _thread.Cancel(true);
                _thread.Dispose();
            }

            _thread = new Thread(this);
            _thread.SetDaemon(true);
            _thread.Start(true);

            _autoResetWaitReceive.WaitOne();

            var result = new List<NbtAddress>();

            foreach (var key in _responseTable.Keys)
            {
                var resp = (NodeStatusResponse)_responseTable[key];

                if (!resp.Received || resp.ResultCode != 0)
                    continue;

                result.AddRange(resp.AddressArray
                                    .Where(entry => entry.HostName.HexCode == 0x20));
            }

            _responseTable.Clear();

            _waitResponse = true;

            return result.Count > 0 ? result.ToArray() : null;
        }
    }
}
