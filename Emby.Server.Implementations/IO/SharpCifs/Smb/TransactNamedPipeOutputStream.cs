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
namespace SharpCifs.Smb
{
	internal class TransactNamedPipeOutputStream : SmbFileOutputStream
	{
		private string _path;

		private SmbNamedPipe _pipe;

		private byte[] _tmp = new byte[1];

		private bool _dcePipe;

		/// <exception cref="System.IO.IOException"></exception>
		internal TransactNamedPipeOutputStream(SmbNamedPipe pipe) : base(pipe, false, (pipe
			.PipeType & unchecked((int)(0xFFFF00FF))) | SmbFile.OExcl)
		{
			this._pipe = pipe;
			_dcePipe = (pipe.PipeType & SmbNamedPipe.PipeTypeDceTransact) == SmbNamedPipe
				.PipeTypeDceTransact;
			_path = pipe.Unc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			_pipe.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int b)
		{
			_tmp[0] = unchecked((byte)b);
			Write(_tmp, 0, 1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b)
		{
			Write(b, 0, b.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(byte[] b, int off, int len)
		{
			if (len < 0)
			{
				len = 0;
			}
			if ((_pipe.PipeType & SmbNamedPipe.PipeTypeCall) == SmbNamedPipe.PipeTypeCall)
			{
				_pipe.Send(new TransWaitNamedPipe(_path), new TransWaitNamedPipeResponse());
				_pipe.Send(new TransCallNamedPipe(_path, b, off, len), new TransCallNamedPipeResponse
					(_pipe));
			}
			else
			{
				if ((_pipe.PipeType & SmbNamedPipe.PipeTypeTransact) == SmbNamedPipe.PipeTypeTransact)
				{
					EnsureOpen();
					TransTransactNamedPipe req = new TransTransactNamedPipe(_pipe.Fid, b, off, len);
					if (_dcePipe)
					{
						req.MaxDataCount = 1024;
					}
					_pipe.Send(req, new TransTransactNamedPipeResponse(_pipe));
				}
			}
		}
	}
}
