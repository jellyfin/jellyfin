// This code is derived from jcifs smb client library <jcifs at samba dot org>
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
// 
// Ported to C# by J. Arturo <webmaster at komodosoft.net>
using System;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComSessionSetupAndX : AndXServerMessageBlock
	{
		private static readonly int BatchLimit = Config.GetInt("jcifs.smb.client.SessionSetupAndX.TreeConnectAndX"
			, 1);

		private static readonly bool DisablePlainTextPasswords = Config.GetBoolean("jcifs.smb.client.disablePlainTextPasswords"
			, true);

		private byte[] _lmHash;

		private byte[] _ntHash;

		private byte[] _blob;

		private int _sessionKey;

		private int _capabilities;

		private string _accountName;

		private string _primaryDomain;

		internal SmbSession Session;

		internal object Cred;

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		internal SmbComSessionSetupAndX(SmbSession session, ServerMessageBlock andx, object
			 cred) : base(andx)
		{
			Command = SmbComSessionSetupAndx;
			this.Session = session;
			this.Cred = cred;
			_sessionKey = session.transport.SessionKey;
			_capabilities = session.transport.Capabilities;
            if (session.transport.Server.Security == SmbConstants.SecurityUser)
			{
				if (cred is NtlmPasswordAuthentication)
				{
					NtlmPasswordAuthentication auth = (NtlmPasswordAuthentication)cred;
					if (auth == NtlmPasswordAuthentication.Anonymous)
					{
						_lmHash = new byte[0];
						_ntHash = new byte[0];
						_capabilities &= ~SmbConstants.CapExtendedSecurity;
					}
					else
					{
						if (session.transport.Server.EncryptedPasswords)
						{
							_lmHash = auth.GetAnsiHash(session.transport.Server.EncryptionKey);
							_ntHash = auth.GetUnicodeHash(session.transport.Server.EncryptionKey);
							// prohibit HTTP auth attempts for the null session
							if (_lmHash.Length == 0 && _ntHash.Length == 0)
							{
								throw new RuntimeException("Null setup prohibited.");
							}
						}
						else
						{
						    if (DisablePlainTextPasswords)
							{
								throw new RuntimeException("Plain text passwords are disabled");
							}
						    if (UseUnicode)
						    {
						        // plain text
						        string password = auth.GetPassword();
						        _lmHash = new byte[0];
						        _ntHash = new byte[(password.Length + 1) * 2];
						        WriteString(password, _ntHash, 0);
						    }
						    else
						    {
						        // plain text
						        string password = auth.GetPassword();
						        _lmHash = new byte[(password.Length + 1) * 2];
						        _ntHash = new byte[0];
						        WriteString(password, _lmHash, 0);
						    }
						}
					}
					_accountName = auth.Username;
					if (UseUnicode)
					{
						_accountName = _accountName.ToUpper();
					}
					_primaryDomain = auth.Domain.ToUpper();
				}
				else
				{
					if (cred is byte[])
					{
						_blob = (byte[])cred;
					}
					else
					{
						throw new SmbException("Unsupported credential type");
					}
				}
			}
			else
			{
                if (session.transport.Server.Security == SmbConstants.SecurityShare)
				{
					if (cred is NtlmPasswordAuthentication)
					{
						NtlmPasswordAuthentication auth = (NtlmPasswordAuthentication)cred;
						_lmHash = new byte[0];
						_ntHash = new byte[0];
						_accountName = auth.Username;
						if (UseUnicode)
						{
							_accountName = _accountName.ToUpper();
						}
						_primaryDomain = auth.Domain.ToUpper();
					}
					else
					{
						throw new SmbException("Unsupported credential type");
					}
				}
				else
				{
					throw new SmbException("Unsupported");
				}
			}
		}

		internal override int GetBatchLimit(byte command)
		{
			return command == SmbComTreeConnectAndx ? BatchLimit : 0;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(Session.transport.SndBufSize, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(Session.transport.MaxMpxCount, dst, dstIndex);
			dstIndex += 2;
            WriteInt2(SmbConstants.VcNumber, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(_sessionKey, dst, dstIndex);
			dstIndex += 4;
			if (_blob != null)
			{
				WriteInt2(_blob.Length, dst, dstIndex);
				dstIndex += 2;
			}
			else
			{
				WriteInt2(_lmHash.Length, dst, dstIndex);
				dstIndex += 2;
				WriteInt2(_ntHash.Length, dst, dstIndex);
				dstIndex += 2;
			}
			dst[dstIndex++] = unchecked(unchecked(0x00));
			dst[dstIndex++] = unchecked(unchecked(0x00));
			dst[dstIndex++] = unchecked(unchecked(0x00));
			dst[dstIndex++] = unchecked(unchecked(0x00));
			WriteInt4(_capabilities, dst, dstIndex);
			dstIndex += 4;
			return dstIndex - start;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			if (_blob != null)
			{
				Array.Copy(_blob, 0, dst, dstIndex, _blob.Length);
				dstIndex += _blob.Length;
			}
			else
			{
				Array.Copy(_lmHash, 0, dst, dstIndex, _lmHash.Length);
				dstIndex += _lmHash.Length;
				Array.Copy(_ntHash, 0, dst, dstIndex, _ntHash.Length);
				dstIndex += _ntHash.Length;
				dstIndex += WriteString(_accountName, dst, dstIndex);
				dstIndex += WriteString(_primaryDomain, dst, dstIndex);
			}
            dstIndex += WriteString(SmbConstants.NativeOs, dst, dstIndex);
            dstIndex += WriteString(SmbConstants.NativeLanman, dst, dstIndex);
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
			string result = "SmbComSessionSetupAndX[" + base.ToString() + ",snd_buf_size="
				 + Session.transport.SndBufSize + ",maxMpxCount=" + Session.transport.MaxMpxCount
                 + ",VC_NUMBER=" + SmbConstants.VcNumber + ",sessionKey=" + _sessionKey + ",lmHash.length="
				 + (_lmHash == null ? 0 : _lmHash.Length) + ",ntHash.length=" + (_ntHash == null ? 
				0 : _ntHash.Length) + ",capabilities=" + _capabilities + ",accountName=" + _accountName
                 + ",primaryDomain=" + _primaryDomain + ",NATIVE_OS=" + SmbConstants.NativeOs
                 + ",NATIVE_LANMAN=" + SmbConstants.NativeLanman + "]";
			return result;
		}
	}
}
