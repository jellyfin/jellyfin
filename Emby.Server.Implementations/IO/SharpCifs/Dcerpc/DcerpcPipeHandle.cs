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
using SharpCifs.Smb;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Dcerpc
{
	public class DcerpcPipeHandle : DcerpcHandle
	{
		internal SmbNamedPipe Pipe;

		internal SmbFileInputStream In;

		internal SmbFileOutputStream Out;

		internal bool IsStart = true;

		/// <exception cref="UnknownHostException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
		public DcerpcPipeHandle(string url, NtlmPasswordAuthentication auth)
		{
			Binding = ParseBinding(url);
			url = "smb://" + Binding.Server + "/IPC$/" + Runtime.Substring(Binding.Endpoint
				, 6);
			string @params = string.Empty;
			string server;
			string address;
			server = (string)Binding.GetOption("server");
			if (server != null)
			{
				@params += "&server=" + server;
			}
			address = (string)Binding.GetOption("address");
			if (server != null)
			{
				@params += "&address=" + address;
			}
			if (@params.Length > 0)
			{
				url += "?" + Runtime.Substring(@params, 1);
			}
			Pipe = new SmbNamedPipe(url, (unchecked(0x2019F) << 16) | SmbNamedPipe.PipeTypeRdwr
				 | SmbNamedPipe.PipeTypeDceTransact, auth);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void DoSendFragment(byte[] buf, int off, int length, 
			bool isDirect)
		{
			if (Out != null && Out.IsOpen() == false)
			{
				throw new IOException("DCERPC pipe is no longer open");
			}
			if (In == null)
			{
				In = (SmbFileInputStream)Pipe.GetNamedPipeInputStream();
			}
			if (Out == null)
			{
				Out = (SmbFileOutputStream)Pipe.GetNamedPipeOutputStream();
			}
			if (isDirect)
			{
				Out.WriteDirect(buf, off, length, 1);
				return;
			}
			Out.Write(buf, off, length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void DoReceiveFragment(byte[] buf, bool isDirect)
		{
			int off;
			int flags;
			int length;
			if (buf.Length < MaxRecv)
			{
				throw new ArgumentException("buffer too small");
			}
			if (IsStart && !isDirect)
			{
				// start of new frag, do trans
				off = In.Read(buf, 0, 1024);
			}
			else
			{
				off = In.ReadDirect(buf, 0, buf.Length);
			}
			if (buf[0] != 5 && buf[1] != 0)
			{
				throw new IOException("Unexpected DCERPC PDU header");
			}
			flags = buf[3] & unchecked(0xFF);
			// next read is start of new frag
            IsStart = (flags & DcerpcConstants.DcerpcLastFrag) == DcerpcConstants.DcerpcLastFrag;
			length = Encdec.Dec_uint16le(buf, 8);
			if (length > MaxRecv)
			{
				throw new IOException("Unexpected fragment length: " + length);
			}
			while (off < length)
			{
				off += In.ReadDirect(buf, off, length - off);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			State = 0;
			if (Out != null)
			{
				Out.Close();
			}
		}
	}
}
