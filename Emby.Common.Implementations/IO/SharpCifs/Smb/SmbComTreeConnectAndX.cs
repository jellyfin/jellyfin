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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComTreeConnectAndX : AndXServerMessageBlock
	{
		private static readonly bool DisablePlainTextPasswords = Config.GetBoolean("jcifs.smb.client.disablePlainTextPasswords"
			, true);

		private SmbSession _session;

		private bool _disconnectTid = false;

		private string _service;

		private byte[] _password;

		private int _passwordLength;

		internal string path;

		private static byte[] _batchLimits = { 1, 1, 1, 1, 1, 1, 1, 1, 0 };

		static SmbComTreeConnectAndX()
		{
			string s;
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.CheckDirectory")) !=
				 null)
			{
				_batchLimits[0] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.CreateDirectory")) 
				!= null)
			{
				_batchLimits[2] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.Delete")) != null)
			{
				_batchLimits[3] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.DeleteDirectory")) 
				!= null)
			{
				_batchLimits[4] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.OpenAndX")) != null)
			{
				_batchLimits[5] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.Rename")) != null)
			{
				_batchLimits[6] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.Transaction")) != null)
			{
				_batchLimits[7] = byte.Parse(s);
			}
			if ((s = Config.GetProperty("jcifs.smb.client.TreeConnectAndX.QueryInformation"))
				 != null)
			{
				_batchLimits[8] = byte.Parse(s);
			}
		}

		internal SmbComTreeConnectAndX(SmbSession session, string path, string service, ServerMessageBlock
			 andx) : base(andx)
		{
			this._session = session;
			this.path = path;
			this._service = service;
			Command = SmbComTreeConnectAndx;
		}

		internal override int GetBatchLimit(byte command)
		{
			int c = command & unchecked(0xFF);
			switch (c)
			{
				case SmbComCheckDirectory:
				{
					// why isn't this just return batchLimits[c]?
					return _batchLimits[0];
				}

				case SmbComCreateDirectory:
				{
					return _batchLimits[2];
				}

				case SmbComDelete:
				{
					return _batchLimits[3];
				}

				case SmbComDeleteDirectory:
				{
					return _batchLimits[4];
				}

				case SmbComOpenAndx:
				{
					return _batchLimits[5];
				}

				case SmbComRename:
				{
					return _batchLimits[6];
				}

				case SmbComTransaction:
				{
					return _batchLimits[7];
				}

				case SmbComQueryInformation:
				{
					return _batchLimits[8];
				}
			}
			return 0;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			if (_session.transport.Server.Security == SmbConstants.SecurityShare && (_session.Auth.HashesExternal
				 || _session.Auth.Password.Length > 0))
			{
				if (_session.transport.Server.EncryptedPasswords)
				{
					// encrypted
					_password = _session.Auth.GetAnsiHash(_session.transport.Server.EncryptionKey);
					_passwordLength = _password.Length;
				}
				else
				{
					if (DisablePlainTextPasswords)
					{
						throw new RuntimeException("Plain text passwords are disabled");
					}
				    // plain text
				    _password = new byte[(_session.Auth.Password.Length + 1) * 2];
				    _passwordLength = WriteString(_session.Auth.Password, _password, 0);
				}
			}
			else
			{
				// no password in tree connect
				_passwordLength = 1;
			}
			dst[dstIndex++] = _disconnectTid ? unchecked((byte)unchecked(0x01)) : unchecked(
				(byte)unchecked(0x00));
			dst[dstIndex++] = unchecked(unchecked(0x00));
			WriteInt2(_passwordLength, dst, dstIndex);
			return 4;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			if (_session.transport.Server.Security == SmbConstants.SecurityShare && (_session.Auth.HashesExternal
				 || _session.Auth.Password.Length > 0))
			{
				Array.Copy(_password, 0, dst, dstIndex, _passwordLength);
				dstIndex += _passwordLength;
			}
			else
			{
				// no password in tree connect
				dst[dstIndex++] = unchecked(unchecked(0x00));
			}
			dstIndex += WriteString(path, dst, dstIndex);
			try
			{
//				Array.Copy(Runtime.GetBytesForString(_service, "ASCII"), 0, dst, dstIndex
					//, _service.Length);
                Array.Copy(Runtime.GetBytesForString(_service, "UTF-8"), 0, dst, dstIndex
                    , _service.Length);
			}
			catch (UnsupportedEncodingException)
			{
				return 0;
			}
			dstIndex += _service.Length;
			dst[dstIndex++] = unchecked((byte)('\0'));
			return dstIndex - start;
		}

		internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			return 0;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			string result = "SmbComTreeConnectAndX[" + base.ToString() + ",disconnectTid="
				 + _disconnectTid + ",passwordLength=" + _passwordLength + ",password=" + Hexdump.
				ToHexString(_password, _passwordLength, 0) + ",path=" + path + ",service=" + _service
				 + "]";
			return result;
		}
	}
}
