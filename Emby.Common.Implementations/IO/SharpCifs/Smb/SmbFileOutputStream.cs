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
using System.IO;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	/// <summary>This <code>OutputStream</code> can write bytes to a file on an SMB file server.
	/// 	</summary>
	/// <remarks>This <code>OutputStream</code> can write bytes to a file on an SMB file server.
	/// 	</remarks>
	public class SmbFileOutputStream : OutputStream
	{
		private SmbFile _file;

		private bool _append;

		private bool _useNtSmbs;

		private int _openFlags;

		private int _access;

		private int _writeSize;

		private long _fp;

		private byte[] _tmp = new byte[1];

		private SmbComWriteAndX _reqx;

		private SmbComWriteAndXResponse _rspx;

		private SmbComWrite _req;

		private SmbComWriteResponse _rsp;

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.OutputStream">System.IO.OutputStream</see>
		/// for writing to a file
		/// on an SMB server addressed by the URL parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of
		/// the smb URL syntax.
		/// </summary>
		/// <param name="url">An smb URL string representing the file to write to</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileOutputStream(string url) : this(url, false)
		{
		}

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.OutputStream">System.IO.OutputStream</see>
		/// for writing bytes to a file on
		/// an SMB server represented by the
		/// <see cref="SmbFile">SmbFile</see>
		/// parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of
		/// the smb URL syntax.
		/// </summary>
		/// <param name="file">An <code>SmbFile</code> specifying the file to write to</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileOutputStream(SmbFile file) : this(file, false)
		{
		}

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.OutputStream">System.IO.OutputStream</see>
		/// for writing bytes to a file on an
		/// SMB server addressed by the URL parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of the smb URL syntax. If the
		/// second argument is <code>true</code>, then bytes will be written to the
		/// end of the file rather than the beginning.
		/// </summary>
		/// <param name="url">An smb URL string representing the file to write to</param>
		/// <param name="append">Append to the end of file</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileOutputStream(string url, bool append) : this(new SmbFile(url), append
			)
		{
		}

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.OutputStream">System.IO.OutputStream</see>
		/// for writing bytes to a file
		/// on an SMB server addressed by the <code>SmbFile</code> parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of
		/// the smb URL syntax. If the second argument is <code>true</code>, then
		/// bytes will be written to the end of the file rather than the beginning.
		/// </summary>
		/// <param name="file">An <code>SmbFile</code> representing the file to write to</param>
		/// <param name="append">Append to the end of file</param>
		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileOutputStream(SmbFile file, bool append) : this(file, append, append
			 ? SmbFile.OCreat | SmbFile.OWronly | SmbFile.OAppend : SmbFile.OCreat | SmbFile
			.OWronly | SmbFile.OTrunc)
		{
		}

		/// <summary>
		/// Creates an
		/// <see cref="System.IO.OutputStream">System.IO.OutputStream</see>
		/// for writing bytes to a file
		/// on an SMB server addressed by the <code>SmbFile</code> parameter. See
		/// <see cref="SmbFile">SmbFile</see>
		/// for a detailed description and examples of
		/// the smb URL syntax.
		/// <p>
		/// The second parameter specifies how the file should be shared. If
		/// <code>SmbFile.FILE_NO_SHARE</code> is specified the client will
		/// have exclusive access to the file. An additional open command
		/// from jCIFS or another application will fail with the "file is being
		/// accessed by another process" error. The <code>FILE_SHARE_READ</code>,
		/// <code>FILE_SHARE_WRITE</code>, and <code>FILE_SHARE_DELETE</code> may be
		/// combined with the bitwise OR '|' to specify that other peocesses may read,
		/// write, and/or delete the file while the jCIFS user has the file open.
		/// </summary>
		/// <param name="url">An smb URL representing the file to write to</param>
		/// <param name="shareAccess">File sharing flag: <code>SmbFile.FILE_NOSHARE</code> or any combination of <code>SmbFile.FILE_READ</code>, <code>SmbFile.FILE_WRITE</code>, and <code>SmbFile.FILE_DELETE</code>
		/// 	</param>
		/// <exception cref="Jcifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbFileOutputStream(string url, int shareAccess) : this(new SmbFile(url, string.Empty
			, null, shareAccess), false)
		{
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		internal SmbFileOutputStream(SmbFile file, bool append, int openFlags)
		{
			this._file = file;
			this._append = append;
			this._openFlags = openFlags;
			_access = ((int)(((uint)openFlags) >> 16)) & 0xFFFF;
			if (append)
			{
				try
				{
					_fp = file.Length();
				}
				catch (SmbAuthException sae)
				{
					throw;
				}
				catch (SmbException)
				{
					_fp = 0L;
				}
			}
			if (file is SmbNamedPipe && file.Unc.StartsWith("\\pipe\\"))
			{
				file.Unc = Runtime.Substring(file.Unc, 5);
				file.Send(new TransWaitNamedPipe("\\pipe" + file.Unc), new TransWaitNamedPipeResponse
					());
			}
			file.Open(openFlags, _access | SmbConstants.FileWriteData, SmbFile.AttrNormal, 
				0);
			this._openFlags &= ~(SmbFile.OCreat | SmbFile.OTrunc);
			_writeSize = file.Tree.Session.transport.SndBufSize - 70;
			_useNtSmbs = file.Tree.Session.transport.HasCapability(SmbConstants.CapNtSmbs
				);
			if (_useNtSmbs)
			{
				_reqx = new SmbComWriteAndX();
				_rspx = new SmbComWriteAndXResponse();
			}
			else
			{
				_req = new SmbComWrite();
				_rsp = new SmbComWriteResponse();
			}
		}

		/// <summary>
		/// Closes this output stream and releases any system resources associated
		/// with it.
		/// </summary>
		/// <remarks>
		/// Closes this output stream and releases any system resources associated
		/// with it.
		/// </remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override void Close()
		{
			_file.Close();
			_tmp = null;
		}

		/// <summary>Writes the specified byte to this file output stream.</summary>
		/// <remarks>Writes the specified byte to this file output stream.</remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override void Write(int b)
		{
			_tmp[0] = unchecked((byte)b);
			Write(_tmp, 0, 1);
		}

		/// <summary>
		/// Writes b.length bytes from the specified byte array to this
		/// file output stream.
		/// </summary>
		/// <remarks>
		/// Writes b.length bytes from the specified byte array to this
		/// file output stream.
		/// </remarks>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override void Write(byte[] b)
		{
			Write(b, 0, b.Length);
		}

		public virtual bool IsOpen()
		{
			return _file.IsOpen();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void EnsureOpen()
		{
			// ensure file is open
			if (_file.IsOpen() == false)
			{
				_file.Open(_openFlags, _access | SmbConstants.FileWriteData, SmbFile.AttrNormal, 
					0);
				if (_append)
				{
					_fp = _file.Length();
				}
			}
		}

		/// <summary>
		/// Writes len bytes from the specified byte array starting at
		/// offset off to this file output stream.
		/// </summary>
		/// <remarks>
		/// Writes len bytes from the specified byte array starting at
		/// offset off to this file output stream.
		/// </remarks>
		/// <param name="b">The array</param>
		/// <exception cref="System.IO.IOException">if a network error occurs</exception>
		public override void Write(byte[] b, int off, int len)
		{
			if (_file.IsOpen() == false && _file is SmbNamedPipe)
			{
				_file.Send(new TransWaitNamedPipe("\\pipe" + _file.Unc), new TransWaitNamedPipeResponse
					());
			}
			WriteDirect(b, off, len, 0);
		}

		/// <summary>Just bypasses TransWaitNamedPipe - used by DCERPC bind.</summary>
		/// <remarks>Just bypasses TransWaitNamedPipe - used by DCERPC bind.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void WriteDirect(byte[] b, int off, int len, int flags)
		{
			if (len <= 0)
			{
				return;
			}
			if (_tmp == null)
			{
				throw new IOException("Bad file descriptor");
			}
			EnsureOpen();
			/*if (file.log.level >= 4)
			{
				file.log.WriteLine("write: fid=" + file.fid + ",off=" + off + ",len=" + len);
			}*/
			int w;
			do
			{
				w = len > _writeSize ? _writeSize : len;
				if (_useNtSmbs)
				{
					_reqx.SetParam(_file.Fid, _fp, len - w, b, off, w);
					if ((flags & 1) != 0)
					{
						_reqx.SetParam(_file.Fid, _fp, len, b, off, w);
						_reqx.WriteMode = 0x8;
					}
					else
					{
						_reqx.WriteMode = 0;
					}
					_file.Send(_reqx, _rspx);
					_fp += _rspx.Count;
					len -= (int)_rspx.Count;
					off += (int)_rspx.Count;
				}
				else
				{
					_req.SetParam(_file.Fid, _fp, len - w, b, off, w);
					_fp += _rsp.Count;
					len -= (int)_rsp.Count;
					off += (int)_rsp.Count;
					_file.Send(_req, _rsp);
				}
			}
			while (len > 0);
		}
	}
}
