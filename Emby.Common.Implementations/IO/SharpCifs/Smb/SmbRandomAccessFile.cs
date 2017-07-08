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
using System.Text;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	public class SmbRandomAccessFile //: DataOutput, DataInput
	{
		private const int WriteOptions = unchecked(0x0842);

		private SmbFile _file;

		private long _fp;

		private int _openFlags;

		private int _access;

		private int _readSize;

		private int _writeSize;

		private int _ch;

		private int _options;

		private byte[] _tmp = new byte[8];

		private SmbComWriteAndXResponse _writeAndxResp;

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbRandomAccessFile(string url, string mode, int shareAccess) : this(new SmbFile
			(url, string.Empty, null, shareAccess), mode)
		{
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		/// <exception cref="System.UriFormatException"></exception>
		/// <exception cref="UnknownHostException"></exception>
		public SmbRandomAccessFile(SmbFile file, string mode)
		{
			this._file = file;
			if (mode.Equals("r"))
			{
				_openFlags = SmbFile.OCreat | SmbFile.ORdonly;
			}
			else
			{
				if (mode.Equals("rw"))
				{
					_openFlags = SmbFile.OCreat | SmbFile.ORdwr | SmbFile.OAppend;
					_writeAndxResp = new SmbComWriteAndXResponse();
					_options = WriteOptions;
					_access = SmbConstants.FileReadData | SmbConstants.FileWriteData;
				}
				else
				{
					throw new ArgumentException("Invalid mode");
				}
			}
			file.Open(_openFlags, _access, SmbFile.AttrNormal, _options);
			_readSize = file.Tree.Session.transport.RcvBufSize - 70;
			_writeSize = file.Tree.Session.transport.SndBufSize - 70;
			_fp = 0L;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual int Read()
		{
			if (Read(_tmp, 0, 1) == -1)
			{
				return -1;
			}
			return _tmp[0] & unchecked(0xFF);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual int Read(byte[] b)
		{
			return Read(b, 0, b.Length);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual int Read(byte[] b, int off, int len)
		{
			if (len <= 0)
			{
				return 0;
			}
			long start = _fp;
			// ensure file is open
			if (_file.IsOpen() == false)
			{
				_file.Open(_openFlags, 0, SmbFile.AttrNormal, _options);
			}
			int r;
			int n;
			SmbComReadAndXResponse response = new SmbComReadAndXResponse(b, off);
			do
			{
				r = len > _readSize ? _readSize : len;
				_file.Send(new SmbComReadAndX(_file.Fid, _fp, r, null), response);
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

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void ReadFully(byte[] b)
		{
			ReadFully(b, 0, b.Length);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void ReadFully(byte[] b, int off, int len)
		{
			int n = 0;
			int count;
			do
			{
				count = Read(b, off + n, len - n);
				if (count < 0)
				{
					throw new SmbException("EOF");
				}
				n += count;
				_fp += count;
			}
			while (n < len);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual int SkipBytes(int n)
		{
			if (n > 0)
			{
				_fp += n;
				return n;
			}
			return 0;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void Write(int b)
		{
			_tmp[0] = unchecked((byte)b);
			Write(_tmp, 0, 1);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void Write(byte[] b)
		{
			Write(b, 0, b.Length);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void Write(byte[] b, int off, int len)
		{
			if (len <= 0)
			{
				return;
			}
			// ensure file is open
			if (_file.IsOpen() == false)
			{
				_file.Open(_openFlags, 0, SmbFile.AttrNormal, _options);
			}
			int w;
			do
			{
				w = len > _writeSize ? _writeSize : len;
				_file.Send(new SmbComWriteAndX(_file.Fid, _fp, len - w, b, off, w, null), _writeAndxResp
					);
				_fp += _writeAndxResp.Count;
				len -= (int)_writeAndxResp.Count;
				off += (int)_writeAndxResp.Count;
			}
			while (len > 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual long GetFilePointer()
		{
			return _fp;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void Seek(long pos)
		{
			_fp = pos;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual long Length()
		{
			return _file.Length();
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void SetLength(long newLength)
		{
			// ensure file is open
			if (_file.IsOpen() == false)
			{
				_file.Open(_openFlags, 0, SmbFile.AttrNormal, _options);
			}
			SmbComWriteResponse rsp = new SmbComWriteResponse();
			_file.Send(new SmbComWrite(_file.Fid, (int)(newLength & unchecked(0xFFFFFFFFL)), 0, _tmp, 0, 0), rsp);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual void Close()
		{
			_file.Close();
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public bool ReadBoolean()
		{
			if ((Read(_tmp, 0, 1)) < 0)
			{
				throw new SmbException("EOF");
			}
			return _tmp[0] != unchecked(unchecked(0x00));
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public byte ReadByte()
		{
			if ((Read(_tmp, 0, 1)) < 0)
			{
				throw new SmbException("EOF");
			}
			return _tmp[0];
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public int ReadUnsignedByte()
		{
			if ((Read(_tmp, 0, 1)) < 0)
			{
				throw new SmbException("EOF");
			}
			return _tmp[0] & unchecked(0xFF);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public short ReadShort()
		{
			if ((Read(_tmp, 0, 2)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_uint16be(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public int ReadUnsignedShort()
		{
			if ((Read(_tmp, 0, 2)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_uint16be(_tmp, 0) & unchecked(0xFFFF);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public char ReadChar()
		{
			if ((Read(_tmp, 0, 2)) < 0)
			{
				throw new SmbException("EOF");
			}
			return (char)Encdec.Dec_uint16be(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public int ReadInt()
		{
			if ((Read(_tmp, 0, 4)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_uint32be(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public long ReadLong()
		{
			if ((Read(_tmp, 0, 8)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_uint64be(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public float ReadFloat()
		{
			if ((Read(_tmp, 0, 4)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_floatbe(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public double ReadDouble()
		{
			if ((Read(_tmp, 0, 8)) < 0)
			{
				throw new SmbException("EOF");
			}
			return Encdec.Dec_doublebe(_tmp, 0);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public string ReadLine()
		{
			StringBuilder input = new StringBuilder();
			int c = -1;
			bool eol = false;
			while (!eol)
			{
				switch (c = Read())
				{
					case -1:
					case '\n':
					{
						eol = true;
						break;
					}

					case '\r':
					{
						eol = true;
						long cur = _fp;
						if (Read() != '\n')
						{
							_fp = cur;
						}
						break;
					}

					default:
					{
						input.Append((char)c);
						break;
					}
				}
			}
			if ((c == -1) && (input.Length == 0))
			{
				return null;
			}
			return input.ToString();
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public string ReadUtf()
		{
			int size = ReadUnsignedShort();
			byte[] b = new byte[size];
			Read(b, 0, size);
			try
			{
				return Encdec.Dec_utf8(b, 0, size);
			}
			catch (IOException ioe)
			{
				throw new SmbException(string.Empty, ioe);
			}
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteBoolean(bool v)
		{
			_tmp[0] = unchecked((byte)(v ? 1 : 0));
			Write(_tmp, 0, 1);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteByte(int v)
		{
			_tmp[0] = unchecked((byte)v);
			Write(_tmp, 0, 1);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteShort(int v)
		{
			Encdec.Enc_uint16be((short)v, _tmp, 0);
			Write(_tmp, 0, 2);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteChar(int v)
		{
			Encdec.Enc_uint16be((short)v, _tmp, 0);
			Write(_tmp, 0, 2);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteInt(int v)
		{
			Encdec.Enc_uint32be(v, _tmp, 0);
			Write(_tmp, 0, 4);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteLong(long v)
		{
			Encdec.Enc_uint64be(v, _tmp, 0);
			Write(_tmp, 0, 8);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteFloat(float v)
		{
			Encdec.Enc_floatbe(v, _tmp, 0);
			Write(_tmp, 0, 4);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteDouble(double v)
		{
			Encdec.Enc_doublebe(v, _tmp, 0);
			Write(_tmp, 0, 8);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteBytes(string s)
		{
			byte[] b = Runtime.GetBytesForString(s);
			Write(b, 0, b.Length);
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
	/*	public void WriteChars(string s)
		{
			int clen = s.Length;
			int blen = 2 * clen;
			byte[] b = new byte[blen];
			char[] c = new char[clen];
			Sharpen.Runtime.GetCharsForString(s, 0, clen, c, 0);
			for (int i = 0, j = 0; i < clen; i++)
			{
				b[j++] = unchecked((byte)((char)(((uchar)c[i]) >> 8)));
				b[j++] = unchecked((byte)((char)(((uchar)c[i]) >> 0)));
			}
			Write(b, 0, blen);
		}*/

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public void WriteUtf(string str)
		{
			int len = str.Length;
			int ch;
			int size = 0;
			byte[] dst;
			for (int i = 0; i < len; i++)
			{
				ch = str[i];
				size += ch > unchecked(0x07F) ? (ch > unchecked(0x7FF) ? 3 : 2) : 1;
			}
			dst = new byte[size];
			WriteShort(size);
			try
			{
				Encdec.Enc_utf8(str, dst, 0, size);
			}
			catch (IOException ioe)
			{
				throw new SmbException(string.Empty, ioe);
			}
			Write(dst, 0, size);
		}
	}
}
