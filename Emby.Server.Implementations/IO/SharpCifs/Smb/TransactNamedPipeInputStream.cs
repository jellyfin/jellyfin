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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class TransactNamedPipeInputStream : SmbFileInputStream
	{
		private const int InitPipeSize = 4096;

		private byte[] _pipeBuf = new byte[InitPipeSize];

		private int _begIdx;

		private int _nxtIdx;

		private int _used;

		private bool _dcePipe;

		internal object Lock;

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		internal TransactNamedPipeInputStream(SmbNamedPipe pipe) : base(pipe, (pipe.PipeType
			 & unchecked((int)(0xFFFF00FF))) | SmbFile.OExcl)
		{
			_dcePipe = (pipe.PipeType & SmbNamedPipe.PipeTypeDceTransact) != SmbNamedPipe
				.PipeTypeDceTransact;
			Lock = new object();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			int result = -1;
			lock (Lock)
			{
				try
				{
					while (_used == 0)
					{
						Runtime.Wait(Lock);
					}
				}
				catch (Exception ie)
				{
					throw new IOException(ie.Message);
				}
				result = _pipeBuf[_begIdx] & unchecked(0xFF);
				_begIdx = (_begIdx + 1) % _pipeBuf.Length;
			}
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b)
		{
			return Read(b, 0, b.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b, int off, int len)
		{
			int result = -1;
			int i;
			if (len <= 0)
			{
				return 0;
			}
			lock (Lock)
			{
				try
				{
					while (_used == 0)
					{
						Runtime.Wait(Lock);
					}
				}
				catch (Exception ie)
				{
					throw new IOException(ie.Message);
				}
				i = _pipeBuf.Length - _begIdx;
				result = len > _used ? _used : len;
				if (_used > i && result > i)
				{
					Array.Copy(_pipeBuf, _begIdx, b, off, i);
					off += i;
					Array.Copy(_pipeBuf, 0, b, off, result - i);
				}
				else
				{
					Array.Copy(_pipeBuf, _begIdx, b, off, result);
				}
				_used -= result;
				_begIdx = (_begIdx + result) % _pipeBuf.Length;
			}
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Available()
		{
			if (File.Log.Level >= 3)
			{
				File.Log.WriteLine("Named Pipe available() does not apply to TRANSACT Named Pipes"
					);
			}
			return 0;
		}

		internal virtual int Receive(byte[] b, int off, int len)
		{
			int i;
			if (len > (_pipeBuf.Length - _used))
			{
				byte[] tmp;
				int newSize;
				newSize = _pipeBuf.Length * 2;
				if (len > (newSize - _used))
				{
					newSize = len + _used;
				}
				tmp = _pipeBuf;
				_pipeBuf = new byte[newSize];
				i = tmp.Length - _begIdx;
				if (_used > i)
				{
					Array.Copy(tmp, _begIdx, _pipeBuf, 0, i);
					Array.Copy(tmp, 0, _pipeBuf, i, _used - i);
				}
				else
				{
					Array.Copy(tmp, _begIdx, _pipeBuf, 0, _used);
				}
				_begIdx = 0;
				_nxtIdx = _used;
				tmp = null;
			}
			i = _pipeBuf.Length - _nxtIdx;
			if (len > i)
			{
				Array.Copy(b, off, _pipeBuf, _nxtIdx, i);
				off += i;
				Array.Copy(b, off, _pipeBuf, 0, len - i);
			}
			else
			{
				Array.Copy(b, off, _pipeBuf, _nxtIdx, len);
			}
			_nxtIdx = (_nxtIdx + len) % _pipeBuf.Length;
			_used += len;
			return len;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int Dce_read(byte[] b, int off, int len)
		{
			return base.Read(b, off, len);
		}
	}
}
