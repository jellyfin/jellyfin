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
	/// <summary>To filter 0 len updates and for debugging</summary>
	public class SigningDigest 
	{
		internal static LogStream Log = LogStream.GetInstance();

		private MessageDigest _digest;

		private byte[] _macSigningKey;

		private bool _bypass;

		private int _updates;

		private int _signSequence;

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public SigningDigest(byte[] macSigningKey, bool bypass)
		{
			try
			{
				_digest = MessageDigest.GetInstance("MD5");
			}
			catch (NoSuchAlgorithmException ex)
			{
				if (Log.Level > 0)
				{
					Runtime.PrintStackTrace(ex, Log);
				}
				throw new SmbException("MD5", ex);
			}
			this._macSigningKey = macSigningKey;
			this._bypass = bypass;
			_updates = 0;
			_signSequence = 0;
			if (Log.Level >= 5)
			{
				Log.WriteLine("macSigningKey:");
				Hexdump.ToHexdump(Log, macSigningKey, 0, macSigningKey.Length);
			}
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public SigningDigest(SmbTransport transport, NtlmPasswordAuthentication auth)
		{
			try
			{
				_digest = MessageDigest.GetInstance("MD5");
			}
			catch (NoSuchAlgorithmException ex)
			{
				if (Log.Level > 0)
				{
					Runtime.PrintStackTrace(ex, Log);
				}
				throw new SmbException("MD5", ex);
			}
			try
			{
                switch (SmbConstants.LmCompatibility)
				{
					case 0:
					case 1:
					case 2:
					{
						_macSigningKey = new byte[40];
						auth.GetUserSessionKey(transport.Server.EncryptionKey, _macSigningKey, 0);
						Array.Copy(auth.GetUnicodeHash(transport.Server.EncryptionKey), 0, _macSigningKey
							, 16, 24);
						break;
					}

					case 3:
					case 4:
					case 5:
					{
						_macSigningKey = new byte[16];
						auth.GetUserSessionKey(transport.Server.EncryptionKey, _macSigningKey, 0);
						break;
					}

					default:
					{
						_macSigningKey = new byte[40];
						auth.GetUserSessionKey(transport.Server.EncryptionKey, _macSigningKey, 0);
						Array.Copy(auth.GetUnicodeHash(transport.Server.EncryptionKey), 0, _macSigningKey
							, 16, 24);
					    break;
					}
				}
			}
			catch (Exception ex)
			{
				throw new SmbException(string.Empty, ex);
			}
			if (Log.Level >= 5)
			{
                Log.WriteLine("LM_COMPATIBILITY=" + SmbConstants.LmCompatibility);
				Hexdump.ToHexdump(Log, _macSigningKey, 0, _macSigningKey.Length);
			}
		}

		public virtual void Update(byte[] input, int offset, int len)
		{
			if (Log.Level >= 5)
			{
				Log.WriteLine("update: " + _updates + " " + offset + ":" + len);
				Hexdump.ToHexdump(Log, input, offset, Math.Min(len, 256));
				Log.Flush();
			}
			if (len == 0)
			{
				return;
			}
			_digest.Update(input, offset, len);
			_updates++;
		}

		public virtual byte[] Digest()
		{
			byte[] b;
			b = _digest.Digest();
			if (Log.Level >= 5)
			{
				Log.WriteLine("digest: ");
				Hexdump.ToHexdump(Log, b, 0, b.Length);
				Log.Flush();
			}
			_updates = 0;
			return b;
		}

		/// <summary>Performs MAC signing of the SMB.</summary>
		/// <remarks>
		/// Performs MAC signing of the SMB.  This is done as follows.
		/// The signature field of the SMB is overwritted with the sequence number;
		/// The MD5 digest of the MAC signing key + the entire SMB is taken;
		/// The first 8 bytes of this are placed in the signature field.
		/// </remarks>
		/// <param name="data">The data.</param>
		/// <param name="offset">The starting offset at which the SMB header begins.</param>
		/// <param name="length">The length of the SMB data starting at offset.</param>
		internal virtual void Sign(byte[] data, int offset, int length, ServerMessageBlock
			 request, ServerMessageBlock response)
		{
			request.SignSeq = _signSequence;
			if (response != null)
			{
				response.SignSeq = _signSequence + 1;
				response.VerifyFailed = false;
			}
			try
			{
				Update(_macSigningKey, 0, _macSigningKey.Length);
                int index = offset + SmbConstants.SignatureOffset;
				for (int i = 0; i < 8; i++)
				{
					data[index + i] = 0;
				}
				ServerMessageBlock.WriteInt4(_signSequence, data, index);
				Update(data, offset, length);
				Array.Copy(Digest(), 0, data, index, 8);
				if (_bypass)
				{
					_bypass = false;
					Array.Copy(Runtime.GetBytesForString("BSRSPYL "), 0, data, index, 
						8);
				}
			}
			catch (Exception ex)
			{
				if (Log.Level > 0)
				{
					Runtime.PrintStackTrace(ex, Log);
				}
			}
			finally
			{
				_signSequence += 2;
			}
		}

		/// <summary>Performs MAC signature verification.</summary>
		/// <remarks>
		/// Performs MAC signature verification.  This calculates the signature
		/// of the SMB and compares it to the signature field on the SMB itself.
		/// </remarks>
		/// <param name="data">The data.</param>
		/// <param name="offset">The starting offset at which the SMB header begins.</param>
		/// <param name="length">The length of the SMB data starting at offset.</param>
		internal virtual bool Verify(byte[] data, int offset, ServerMessageBlock response
			)
		{
			Update(_macSigningKey, 0, _macSigningKey.Length);
			int index = offset;
            Update(data, index, SmbConstants.SignatureOffset);
            index += SmbConstants.SignatureOffset;
			byte[] sequence = new byte[8];
			ServerMessageBlock.WriteInt4(response.SignSeq, sequence, 0);
			Update(sequence, 0, sequence.Length);
			index += 8;
			if (response.Command == ServerMessageBlock.SmbComReadAndx)
			{
				SmbComReadAndXResponse raxr = (SmbComReadAndXResponse)response;
				int length = response.Length - raxr.DataLength;
                Update(data, index, length - SmbConstants.SignatureOffset - 8);
				Update(raxr.B, raxr.Off, raxr.DataLength);
			}
			else
			{
                Update(data, index, response.Length - SmbConstants.SignatureOffset - 8);
			}
			byte[] signature = Digest();
			for (int i = 0; i < 8; i++)
			{
                if (signature[i] != data[offset + SmbConstants.SignatureOffset + i])
				{
					if (Log.Level >= 2)
					{
						Log.WriteLine("signature verification failure");
						Hexdump.ToHexdump(Log, signature, 0, 8);
                        Hexdump.ToHexdump(Log, data, offset + SmbConstants.SignatureOffset, 8);
					}
					return response.VerifyFailed = true;
				}
			}
			return response.VerifyFailed = false;
		}

		public override string ToString()
		{
            return "LM_COMPATIBILITY=" + SmbConstants.LmCompatibility + " MacSigningKey=" + Hexdump.ToHexString
				(_macSigningKey, 0, _macSigningKey.Length);
		}
	}
}
