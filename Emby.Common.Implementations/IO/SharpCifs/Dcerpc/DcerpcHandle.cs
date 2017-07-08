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
using SharpCifs.Dcerpc.Ndr;
using SharpCifs.Smb;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Dcerpc
{
	public abstract class DcerpcHandle
	{
		/// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
		protected internal static DcerpcBinding ParseBinding(string str)
		{
			int state;
			int mark;
			int si;
			char[] arr = str.ToCharArray();
			string proto = null;
			string key = null;
			DcerpcBinding binding = null;
			state = mark = si = 0;
			do
			{
				char ch = arr[si];
				switch (state)
				{
					case 0:
					{
						if (ch == ':')
						{
							proto = Runtime.Substring(str, mark, si);
							mark = si + 1;
							state = 1;
						}
						break;
					}

					case 1:
					{
						if (ch == '\\')
						{
							mark = si + 1;
							break;
						}
						state = 2;
						goto case 2;
					}

					case 2:
					{
						if (ch == '[')
						{
							string server = Runtime.Substring(str, mark, si).Trim();
							if (server.Length == 0)
							{
								server = "127.0.0.1";
							}
							binding = new DcerpcBinding(proto, Runtime.Substring(str, mark, si));
							mark = si + 1;
							state = 5;
						}
						break;
					}

					case 5:
					{
						if (ch == '=')
						{
							key = Runtime.Substring(str, mark, si).Trim();
							mark = si + 1;
						}
						else
						{
							if (ch == ',' || ch == ']')
							{
								string val = Runtime.Substring(str, mark, si).Trim();
								if (key == null)
								{
									key = "endpoint";
								}
								binding.SetOption(key, val);
								key = null;
							}
						}
						break;
					}

					default:
					{
						si = arr.Length;
						break;
					}
				}
				si++;
			}
			while (si < arr.Length);
			if (binding == null || binding.Endpoint == null)
			{
				throw new DcerpcException("Invalid binding URL: " + str);
			}
			return binding;
		}

		protected internal DcerpcBinding Binding;

		protected internal int MaxXmit = 4280;

		protected internal int MaxRecv;

		protected internal int State;

		protected internal IDcerpcSecurityProvider SecurityProvider;

		private static int _callId = 1;

		/// <exception cref="UnknownHostException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
		public static DcerpcHandle GetHandle(string url, NtlmPasswordAuthentication auth)
		{
			if (url.StartsWith("ncacn_np:"))
			{
				return new DcerpcPipeHandle(url, auth);
			}
			throw new DcerpcException("DCERPC transport not supported: " + url);
		}

		/// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Bind()
		{
			lock (this)
			{
				try
				{
					State = 1;
					DcerpcMessage bind = new DcerpcBind(Binding, this);
					Sendrecv(bind);
				}
				catch (IOException ioe)
				{
					State = 0;
					throw;
				}
			}
		}

		/// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Sendrecv(DcerpcMessage msg)
		{
			byte[] stub;
			byte[] frag;
			NdrBuffer buf;
			NdrBuffer fbuf;
			bool isLast;
			bool isDirect;
			DcerpcException de;
			if (State == 0)
			{
				Bind();
			}
			isDirect = true;
			stub = BufferCache.GetBuffer();
			try
			{
				int off;
				int tot;
				int n;
				buf = new NdrBuffer(stub, 0);
                msg.Flags = DcerpcConstants.DcerpcFirstFrag | DcerpcConstants.DcerpcLastFrag;
				msg.CallId = _callId++;
				msg.Encode(buf);
				if (SecurityProvider != null)
				{
					buf.SetIndex(0);
					SecurityProvider.Wrap(buf);
				}
				tot = buf.GetLength() - 24;
				off = 0;
				while (off < tot)
				{
					n = tot - off;
					if ((24 + n) > MaxXmit)
					{
                        msg.Flags &= ~DcerpcConstants.DcerpcLastFrag;
						n = MaxXmit - 24;
					}
					else
					{
                        msg.Flags |= DcerpcConstants.DcerpcLastFrag;
						isDirect = false;
						msg.AllocHint = n;
					}
					msg.Length = 24 + n;
					if (off > 0)
					{
                        msg.Flags &= ~DcerpcConstants.DcerpcFirstFrag;
					}
                    if ((msg.Flags & (DcerpcConstants.DcerpcFirstFrag | DcerpcConstants.DcerpcLastFrag)) != (DcerpcConstants.DcerpcFirstFrag |
                        DcerpcConstants.DcerpcLastFrag))
					{
						buf.Start = off;
						buf.Reset();
						msg.Encode_header(buf);
						buf.Enc_ndr_long(msg.AllocHint);
						buf.Enc_ndr_short(0);
						buf.Enc_ndr_short(msg.GetOpnum());
					}
					DoSendFragment(stub, off, msg.Length, isDirect);
					off += n;
				}
				DoReceiveFragment(stub, isDirect);
				buf.Reset();
				buf.SetIndex(8);
				buf.SetLength(buf.Dec_ndr_short());
				if (SecurityProvider != null)
				{
					SecurityProvider.Unwrap(buf);
				}
				buf.SetIndex(0);
				msg.Decode_header(buf);
				off = 24;
                if (msg.Ptype == 2 && msg.IsFlagSet(DcerpcConstants.DcerpcLastFrag) == false)
				{
					off = msg.Length;
				}
				frag = null;
				fbuf = null;
                while (msg.IsFlagSet(DcerpcConstants.DcerpcLastFrag) == false)
				{
					int stubFragLen;
					if (frag == null)
					{
						frag = new byte[MaxRecv];
						fbuf = new NdrBuffer(frag, 0);
					}
					DoReceiveFragment(frag, isDirect);
					fbuf.Reset();
					fbuf.SetIndex(8);
					fbuf.SetLength(fbuf.Dec_ndr_short());
					if (SecurityProvider != null)
					{
						SecurityProvider.Unwrap(fbuf);
					}
					fbuf.Reset();
					msg.Decode_header(fbuf);
					stubFragLen = msg.Length - 24;
					if ((off + stubFragLen) > stub.Length)
					{
						// shouldn't happen if alloc_hint is correct or greater
						byte[] tmp = new byte[off + stubFragLen];
						Array.Copy(stub, 0, tmp, 0, off);
						stub = tmp;
					}
					Array.Copy(frag, 24, stub, off, stubFragLen);
					off += stubFragLen;
				}
				buf = new NdrBuffer(stub, 0);
				msg.Decode(buf);
			}
			finally
			{
				BufferCache.ReleaseBuffer(stub);
			}
			if ((de = msg.GetResult()) != null)
			{
				throw de;
			}
		}

		public virtual void SetDcerpcSecurityProvider(IDcerpcSecurityProvider securityProvider
			)
		{
			this.SecurityProvider = securityProvider;
		}

		public virtual string GetServer()
		{
			if (this is DcerpcPipeHandle)
			{
				return ((DcerpcPipeHandle)this).Pipe.GetServer();
			}
			return null;
		}

		public virtual Principal GetPrincipal()
		{
			if (this is DcerpcPipeHandle)
			{
				return ((DcerpcPipeHandle)this).Pipe.GetPrincipal();
			}
			return null;
		}

		public override string ToString()
		{
			return Binding.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract void DoSendFragment(byte[] buf, int off, int length, 
			bool isDirect);

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract void DoReceiveFragment(byte[] buf, bool isDirect);

		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Close();

		public DcerpcHandle()
		{
			MaxRecv = MaxXmit;
		}
	}
}
