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
using SharpCifs.Ntlmssp;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	/// <summary>For initiating NTLM authentication (including NTLMv2).</summary>
	/// <remarks>For initiating NTLM authentication (including NTLMv2). If you want to add NTLMv2 authentication support to something this is what you want to use. See the code for details. Note that JCIFS does not implement the acceptor side of NTLM authentication.
	/// 	</remarks>
	public class NtlmContext
	{
		internal NtlmPasswordAuthentication Auth;

		internal int NtlmsspFlags;

		internal string Workstation;

		internal bool isEstablished;

		internal byte[] ServerChallenge;

		internal byte[] SigningKey;

		internal string NetbiosName = null;

		internal int State = 1;

		internal LogStream Log;

		public NtlmContext(NtlmPasswordAuthentication auth, bool doSigning)
		{
			this.Auth = auth;
			NtlmsspFlags = NtlmsspFlags | NtlmFlags.NtlmsspRequestTarget | NtlmFlags.NtlmsspNegotiateNtlm2
				 | NtlmFlags.NtlmsspNegotiate128;
			if (doSigning)
			{
				NtlmsspFlags |= NtlmFlags.NtlmsspNegotiateSign | NtlmFlags.NtlmsspNegotiateAlwaysSign
					 | NtlmFlags.NtlmsspNegotiateKeyExch;
			}
			Workstation = Type1Message.GetDefaultWorkstation();
			Log = LogStream.GetInstance();
		}

		public override string ToString()
		{
			string ret = "NtlmContext[auth=" + Auth + ",ntlmsspFlags=0x" + Hexdump.ToHexString
				(NtlmsspFlags, 8) + ",workstation=" + Workstation + ",isEstablished=" + isEstablished
				 + ",state=" + State + ",serverChallenge=";
			if (ServerChallenge == null)
			{
				ret += "null";
			}
			else
			{
				ret += Hexdump.ToHexString(ServerChallenge, 0, ServerChallenge.Length * 2);
			}
			ret += ",signingKey=";
			if (SigningKey == null)
			{
				ret += "null";
			}
			else
			{
				ret += Hexdump.ToHexString(SigningKey, 0, SigningKey.Length * 2);
			}
			ret += "]";
			return ret;
		}

		public virtual bool IsEstablished()
		{
			return isEstablished;
		}

		public virtual byte[] GetServerChallenge()
		{
			return ServerChallenge;
		}

		public virtual byte[] GetSigningKey()
		{
			return SigningKey;
		}

		public virtual string GetNetbiosName()
		{
			return NetbiosName;
		}

		private string GetNtlmsspListItem(byte[] type2Token, int id0)
		{
			int ri = 58;
			for (; ; )
			{
				int id = Encdec.Dec_uint16le(type2Token, ri);
				int len = Encdec.Dec_uint16le(type2Token, ri + 2);
				ri += 4;
				if (id == 0 || (ri + len) > type2Token.Length)
				{
					break;
				}
			    if (id == id0)
			    {
			        try
			        {
			            return Runtime.GetStringForBytes(type2Token, ri, len, SmbConstants.UniEncoding
			                );
			        }
			        catch (UnsupportedEncodingException)
			        {
			            break;
			        }
			    }
			    ri += len;
			}
			return null;
		}

		/// <exception cref="SharpCifs.Smb.SmbException"></exception>
		public virtual byte[] InitSecContext(byte[] token, int offset, int len)
		{
			switch (State)
			{
				case 1:
				{
					Type1Message msg1 = new Type1Message(NtlmsspFlags, Auth.GetDomain(), Workstation);
					token = msg1.ToByteArray();
					if (Log.Level >= 4)
					{
						Log.WriteLine(msg1);
						if (Log.Level >= 6)
						{
							Hexdump.ToHexdump(Log, token, 0, token.Length);
						}
					}
					State++;
					break;
				}

				case 2:
				{
					try
					{
						Type2Message msg2 = new Type2Message(token);
						if (Log.Level >= 4)
						{
							Log.WriteLine(msg2);
							if (Log.Level >= 6)
							{
								Hexdump.ToHexdump(Log, token, 0, token.Length);
							}
						}
						ServerChallenge = msg2.GetChallenge();
						NtlmsspFlags &= msg2.GetFlags();
						//                  netbiosName = getNtlmsspListItem(token, 0x0001);
						Type3Message msg3 = new Type3Message(msg2, Auth.GetPassword(), Auth.GetDomain(), 
							Auth.GetUsername(), Workstation, NtlmsspFlags);
						token = msg3.ToByteArray();
						if (Log.Level >= 4)
						{
							Log.WriteLine(msg3);
							if (Log.Level >= 6)
							{
								Hexdump.ToHexdump(Log, token, 0, token.Length);
							}
						}
						if ((NtlmsspFlags & NtlmFlags.NtlmsspNegotiateSign) != 0)
						{
							SigningKey = msg3.GetMasterKey();
						}
						isEstablished = true;
						State++;
						break;
					}
					catch (Exception e)
					{
						throw new SmbException(e.Message, e);
					}					
				}

				default:
				{
					throw new SmbException("Invalid state");
				}
			}
			return token;
		}
	}
}
