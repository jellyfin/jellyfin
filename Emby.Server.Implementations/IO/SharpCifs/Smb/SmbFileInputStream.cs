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
using SharpCifs.Util.Transport;

namespace SharpCifs.Smb
{
	/// <summary>This InputStream can read bytes from a file on an SMB file server.</summary>
	/// <remarks>This InputStream can read bytes from a file on an SMB file server. Offsets are 64 bits.
	/// 	</remarks>
	public class SmbFileInputStream : InputStream
	{
		private long _fp;

		private int _readSize;

		private int _openFlags;

		private int _access;

		private byte[] _tmp = new byte[1];

		internal SmbFile File;

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.InputStream">System.IO.InputStream</see>
		/// for reading bytes from a file on
		/// an SMB server addressed by the <code>url</code> parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of the smb
		/// URL syntax.
		/// </summary>
		/// <param name="url">An smb URL string representing the file to read from</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileInputStream(string url) : this(new SmbFile(url))
		{
		}

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.InputStream">System.IO.InputStream</see>
		/// for reading bytes from a file on
		/// an SMB server represented by the
		/// <see cref="SmbFile">SmbFile</see>
		/// parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of
		/// the smb URL syntax.
		/// </summary>
		/// <param name="file">An <code>SmbFile</code> specifying the file to read from</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileInputStream(SmbFile file) : this(file, SmbFile.ORdonly)
		{
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		internal SmbFileInputStream(SmbFile file, int openFlags)
		{
			this.File = file;
			this._openFlags = openFlags & 0xFFFF;
			_access = ((int)(((uint)openFlags) >> 16)) & 0xFFFF;
			if (file.Type != SmbFile.TypeNamedPipe)
			{
				file.Open(openFlags, _access, SmbFile.AttrNormal, 0);
				this._openFlags &= ~(SmbFile.OCreat | SmbFile.OTrunc);
			}
			else
			{
				file.Connect0();
			}
			_readSize = Math.Min(file.Tree.Session.transport.RcvBufSize - 70, file.Tree.Session
				.transport.Server.MaxBufferSize - 70);
		}

		protected internal virtual IOException SeToIoe(SmbException se)
		{
			IOException ioe = se;
			Exception root = se.GetRootCause();
			if (root is TransportException)
			{
				ioe = (TransportException)root;
				root = ((TransportException)ioe).GetRootCause();
			}
			if (root is Exception)
			{
				ioe = new IOException(root.Message);
				ioe.InitCause(root);
			}
			return ioe;
		}

		/// <summary>Closes this input stream and releases any system resources associated with the stream.
		/// 	</summary>
		/// <remarks>Closes this input stream and releases any system resources associated with the stream.
		/// 	</remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override void Close()
		{
			try
			{
				File.Close();
				_tmp = null;
			}
			catch (SmbException se)
			{
				throw SeToIoe(se);
			}
		}

		/// <summary>Reads a byte of data from this input stream.</summary>
		/// <remarks>Reads a byte of data from this input stream.</remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override int Read()
		{
			// need oplocks to cache otherwise use BufferedInputStream
			if (Read(_tmp, 0, 1) == -1)
			{
				return -1;
			}
			return _tmp[0] & unchecked(0xFF);
		}

		/// <summary>Reads up to b.length bytes of data from this input stream into an array of bytes.
		/// 	</summary>
		/// <remarks>Reads up to b.length bytes of data from this input stream into an array of bytes.
		/// 	</remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override int Read(byte[] b)
		{
			return Read(b, 0, b.Length);
		}

		/// <summary>Reads up to len bytes of data from this input stream into an array of bytes.
		/// 	</summary>
		/// <remarks>Reads up to len bytes of data from this input stream into an array of bytes.
		/// 	</remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override int Read(byte[] b, int off, int len)
		{
			return ReadDirect(b, off, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int ReadDirect(byte[] b, int off, int len)
		{
			if (len <= 0)
			{
				return 0;
			}

			long start = _fp;
			if (_tmp == null)
			{
				throw new IOException("Bad file descriptor");
			}

			// ensure file is open
			File.Open(_openFlags, _access, SmbFile.AttrNormal, 0);
			if (File.Log.Level >= 4)
			{
				File.Log.WriteLine("read: fid=" + File.Fid + ",off=" + off + ",len=" + len);
			}

			SmbComReadAndXResponse response = new SmbComReadAndXResponse(b, off);
			if (File.Type == SmbFile.TypeNamedPipe)
			{
				response.ResponseTimeout = 0;
			}

			int r;
			int n;
			do
			{
				r = len > _readSize ? _readSize : len;
				if (File.Log.Level >= 4)
				{
					File.Log.WriteLine("read: len=" + len + ",r=" + r + ",fp=" + _fp);
				}

				try
				{
					SmbComReadAndX request = new SmbComReadAndX(File.Fid, _fp, r, null);
					if (File.Type == SmbFile.TypeNamedPipe)
					{
						request.MinCount = request.MaxCount = request.Remaining = 1024;
					}
                    //Ç±Ç±Ç≈ì«Ç›çûÇÒÇ≈Ç¢ÇÈÇÁÇµÇ¢ÅB
					File.Send(request, response);
				}
				catch (SmbException se)
				{
					if (File.Type == SmbFile.TypeNamedPipe && se.GetNtStatus() == NtStatus.NtStatusPipeBroken)
					{
						return -1;
					}
					throw SeToIoe(se);
				}

				if ((n = response.DataLength) <= 0)
				{
					return (int)((_fp - start) > 0L ? _fp - start : -1);
				}

				_fp += n;
				len -= n;
				response.Off += n;
			}
			while (len > 0 && n == r);


			return (int)(_fp - start);
		}

		/// <summary>This stream class is unbuffered.</summary>
		/// <remarks>
		/// This stream class is unbuffered. Therefore this method will always
		/// return 0 for streams connected to regular files. However, a
		/// stream created from a Named Pipe this method will query the server using a
		/// "peek named pipe" operation and return the number of available bytes
		/// on the server.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override int Available()
		{
			SmbNamedPipe pipe;
			TransPeekNamedPipe req;
			TransPeekNamedPipeResponse resp;
			if (File.Type != SmbFile.TypeNamedPipe)
			{
				return 0;
			}
			try
			{
				pipe = (SmbNamedPipe)File;
				File.Open(SmbFile.OExcl, pipe.PipeType & 0xFF0000, SmbFile.AttrNormal
					, 0);
				req = new TransPeekNamedPipe(File.Unc, File.Fid);
				resp = new TransPeekNamedPipeResponse(pipe);
				pipe.Send(req, resp);
				if (resp.status == TransPeekNamedPipeResponse.StatusDisconnected || resp.status 
					== TransPeekNamedPipeResponse.StatusServerEndClosed)
				{
					File.Opened = false;
					return 0;
				}
				return resp.Available;
			}
			catch (SmbException se)
			{
				throw SeToIoe(se);
			}
		}

		/// <summary>Skip n bytes of data on this stream.</summary>
		/// <remarks>
		/// Skip n bytes of data on this stream. This operation will not result
		/// in any IO with the server. Unlink <tt>InputStream</tt> value less than
		/// the one provided will not be returned if it exceeds the end of the file
		/// (if this is a problem let us know).
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long n)
		{
			if (n > 0)
			{
				_fp += n;
				return n;
			}
			return 0;
		}


        /// <summary>
        /// Position in Stream
        /// </summary>
        /// <remarks>
        /// Add by dobes
        /// mod interface to WrappedSystemStream readable, for random access.
        /// </remarks>
	    internal override long Position {
	        get { return this._fp; }
	        set
	        {
	            var tmpPos = value;
	            var length = File.Length();
	            if (tmpPos < 0)
	                tmpPos = 0;
                else if (length < tmpPos)
	                tmpPos = length;
	            this._fp = tmpPos;
	        }
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Add by dobes
        /// mod interface to WrappedSystemStream readable, for random access.
        /// </remarks>
	    internal override bool CanSeek()
	    {
	        return (File.Length() >= 0);
	    }

        /// <summary>
        /// Get file length
        /// </summary>
        public override long Length
	    {
	        get { return File.Length(); }
	    }
        
	}
}
