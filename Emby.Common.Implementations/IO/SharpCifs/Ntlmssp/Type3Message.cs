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
using SharpCifs.Netbios;
using SharpCifs.Smb;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Ntlmssp
{
	/// <summary>Represents an NTLMSSP Type-3 message.</summary>
	/// <remarks>Represents an NTLMSSP Type-3 message.</remarks>
	public class Type3Message : NtlmMessage
	{
		internal const long MillisecondsBetween1970And1601 = 11644473600000L;

		private static readonly int DefaultFlags;

		private static readonly string DefaultDomain;

		private static readonly string DefaultUser;

		private static readonly string DefaultPassword;

		private static readonly string DefaultWorkstation;

		private static readonly int LmCompatibility;

		//private static readonly SecureRandom RANDOM = new SecureRandom();

		private byte[] _lmResponse;

		private byte[] _ntResponse;

		private string _domain;

		private string _user;

		private string _workstation;

		private byte[] _masterKey;

		private byte[] _sessionKey;

		static Type3Message()
		{
			DefaultFlags = NtlmsspNegotiateNtlm | (Config.GetBoolean("jcifs.smb.client.useUnicode"
				, true) ? NtlmsspNegotiateUnicode : NtlmsspNegotiateOem);
			DefaultDomain = Config.GetProperty("jcifs.smb.client.domain", null);
			DefaultUser = Config.GetProperty("jcifs.smb.client.username", null);
			DefaultPassword = Config.GetProperty("jcifs.smb.client.password", null);
			string defaultWorkstation = null;
			try
			{
				defaultWorkstation = NbtAddress.GetLocalHost().GetHostName();
			}
			catch (UnknownHostException)
			{
			}
			DefaultWorkstation = defaultWorkstation;
			LmCompatibility = Config.GetInt("jcifs.smb.lmCompatibility", 3);
		}

		/// <summary>
		/// Creates a Type-3 message using default values from the current
		/// environment.
		/// </summary>
		/// <remarks>
		/// Creates a Type-3 message using default values from the current
		/// environment.
		/// </remarks>
		public Type3Message()
		{
			SetFlags(GetDefaultFlags());
			SetDomain(GetDefaultDomain());
			SetUser(GetDefaultUser());
			SetWorkstation(GetDefaultWorkstation());
		}

		/// <summary>
		/// Creates a Type-3 message in response to the given Type-2 message
		/// using default values from the current environment.
		/// </summary>
		/// <remarks>
		/// Creates a Type-3 message in response to the given Type-2 message
		/// using default values from the current environment.
		/// </remarks>
		/// <param name="type2">The Type-2 message which this represents a response to.</param>
		public Type3Message(Type2Message type2)
		{
			SetFlags(GetDefaultFlags(type2));
			SetWorkstation(GetDefaultWorkstation());
			string domain = GetDefaultDomain();
			SetDomain(domain);
			string user = GetDefaultUser();
			SetUser(user);
			string password = GetDefaultPassword();
			switch (LmCompatibility)
			{
				case 0:
				case 1:
				{
					SetLmResponse(GetLMResponse(type2, password));
					SetNtResponse(GetNTResponse(type2, password));
					break;
				}

				case 2:
				{
					byte[] nt = GetNTResponse(type2, password);
					SetLmResponse(nt);
					SetNtResponse(nt);
					break;
				}

				case 3:
				case 4:
				case 5:
				{
					byte[] clientChallenge = new byte[8];
					//RANDOM.NextBytes(clientChallenge);
					SetLmResponse(GetLMv2Response(type2, domain, user, password, clientChallenge));
					break;
				}

				default:
				{
					SetLmResponse(GetLMResponse(type2, password));
					SetNtResponse(GetNTResponse(type2, password));
					break;
				}
			}
		}

		/// <summary>Creates a Type-3 message in response to the given Type-2 message.</summary>
		/// <remarks>Creates a Type-3 message in response to the given Type-2 message.</remarks>
		/// <param name="type2">The Type-2 message which this represents a response to.</param>
		/// <param name="password">The password to use when constructing the response.</param>
		/// <param name="domain">The domain in which the user has an account.</param>
		/// <param name="user">The username for the authenticating user.</param>
		/// <param name="workstation">
		/// The workstation from which authentication is
		/// taking place.
		/// </param>
		public Type3Message(Type2Message type2, string password, string domain, string user
			, string workstation, int flags)
		{
			SetFlags(flags | GetDefaultFlags(type2));
			if (workstation == null)
			{
				workstation = GetDefaultWorkstation();
			}
			SetWorkstation(workstation);
			SetDomain(domain);
			SetUser(user);
			switch (LmCompatibility)
			{
				case 0:
				case 1:
				{
					if ((GetFlags() & NtlmsspNegotiateNtlm2) == 0)
					{
						SetLmResponse(GetLMResponse(type2, password));
						SetNtResponse(GetNTResponse(type2, password));
					}
					else
					{
						// NTLM2 Session Response
						byte[] clientChallenge = new byte[24];
						//RANDOM.NextBytes(clientChallenge);
						Arrays.Fill(clientChallenge, 8, 24, unchecked((byte)unchecked(0x00)));
						// NTLMv1 w/ NTLM2 session sec and key exch all been verified with a debug build of smbclient
						byte[] responseKeyNt = NtlmPasswordAuthentication.NtowFv1(password);
						byte[] ntlm2Response = NtlmPasswordAuthentication.GetNtlm2Response(responseKeyNt, 
							type2.GetChallenge(), clientChallenge);
						SetLmResponse(clientChallenge);
						SetNtResponse(ntlm2Response);
						if ((GetFlags() & NtlmsspNegotiateSign) == NtlmsspNegotiateSign)
						{
							byte[] sessionNonce = new byte[16];
							Array.Copy(type2.GetChallenge(), 0, sessionNonce, 0, 8);
							Array.Copy(clientChallenge, 0, sessionNonce, 8, 8);
							Md4 md4 = new Md4();
							md4.Update(responseKeyNt);
							byte[] userSessionKey = md4.Digest();
							Hmact64 hmac = new Hmact64(userSessionKey);
							hmac.Update(sessionNonce);
							byte[] ntlm2SessionKey = hmac.Digest();
							if ((GetFlags() & NtlmsspNegotiateKeyExch) != 0)
							{
								_masterKey = new byte[16];
								//RANDOM.NextBytes(masterKey);
								byte[] exchangedKey = new byte[16];
								Rc4 rc4 = new Rc4(ntlm2SessionKey);
								rc4.Update(_masterKey, 0, 16, exchangedKey, 0);
								SetSessionKey(exchangedKey);
							}
							else
							{
								_masterKey = ntlm2SessionKey;
								SetSessionKey(_masterKey);
							}
						}
					}
					break;
				}

				case 2:
				{
					byte[] nt = GetNTResponse(type2, password);
					SetLmResponse(nt);
					SetNtResponse(nt);
					break;
				}

				case 3:
				case 4:
				case 5:
				{
					byte[] responseKeyNt1 = NtlmPasswordAuthentication.NtowFv2(domain, user, password
						);
					byte[] clientChallenge1 = new byte[8];
					//RANDOM.NextBytes(clientChallenge_1);
					SetLmResponse(GetLMv2Response(type2, domain, user, password, clientChallenge1));
					byte[] clientChallenge2 = new byte[8];
					//RANDOM.NextBytes(clientChallenge2);
					SetNtResponse(GetNtlMv2Response(type2, responseKeyNt1, clientChallenge2));
					if ((GetFlags() & NtlmsspNegotiateSign) == NtlmsspNegotiateSign)
					{
						Hmact64 hmac = new Hmact64(responseKeyNt1);
						hmac.Update(_ntResponse, 0, 16);
						// only first 16 bytes of ntResponse
						byte[] userSessionKey = hmac.Digest();
						if ((GetFlags() & NtlmsspNegotiateKeyExch) != 0)
						{
							_masterKey = new byte[16];
							//RANDOM.NextBytes(masterKey);
							byte[] exchangedKey = new byte[16];
							Rc4 rc4 = new Rc4(userSessionKey);
							rc4.Update(_masterKey, 0, 16, exchangedKey, 0);
							SetSessionKey(exchangedKey);
						}
						else
						{
							_masterKey = userSessionKey;
							SetSessionKey(_masterKey);
						}
					}
					break;
				}

				default:
				{
					SetLmResponse(GetLMResponse(type2, password));
					SetNtResponse(GetNTResponse(type2, password));
					break;
				}
			}
		}

		/// <summary>Creates a Type-3 message with the specified parameters.</summary>
		/// <remarks>Creates a Type-3 message with the specified parameters.</remarks>
		/// <param name="flags">The flags to apply to this message.</param>
		/// <param name="lmResponse">The LanManager/LMv2 response.</param>
		/// <param name="ntResponse">The NT/NTLMv2 response.</param>
		/// <param name="domain">The domain in which the user has an account.</param>
		/// <param name="user">The username for the authenticating user.</param>
		/// <param name="workstation">
		/// The workstation from which authentication is
		/// taking place.
		/// </param>
		public Type3Message(int flags, byte[] lmResponse, byte[] ntResponse, string domain
			, string user, string workstation)
		{
			SetFlags(flags);
			SetLmResponse(lmResponse);
			SetNtResponse(ntResponse);
			SetDomain(domain);
			SetUser(user);
			SetWorkstation(workstation);
		}

		/// <summary>Creates a Type-3 message using the given raw Type-3 material.</summary>
		/// <remarks>Creates a Type-3 message using the given raw Type-3 material.</remarks>
		/// <param name="material">The raw Type-3 material used to construct this message.</param>
		/// <exception cref="System.IO.IOException">If an error occurs while parsing the material.
		/// 	</exception>
		public Type3Message(byte[] material)
		{
			Parse(material);
		}

		/// <summary>Returns the LanManager/LMv2 response.</summary>
		/// <remarks>Returns the LanManager/LMv2 response.</remarks>
		/// <returns>A <code>byte[]</code> containing the LanManager response.</returns>
		public virtual byte[] GetLMResponse()
		{
			return _lmResponse;
		}

		/// <summary>Sets the LanManager/LMv2 response for this message.</summary>
		/// <remarks>Sets the LanManager/LMv2 response for this message.</remarks>
		/// <param name="lmResponse">The LanManager response.</param>
		public virtual void SetLmResponse(byte[] lmResponse)
		{
			this._lmResponse = lmResponse;
		}

		/// <summary>Returns the NT/NTLMv2 response.</summary>
		/// <remarks>Returns the NT/NTLMv2 response.</remarks>
		/// <returns>A <code>byte[]</code> containing the NT/NTLMv2 response.</returns>
		public virtual byte[] GetNTResponse()
		{
			return _ntResponse;
		}

		/// <summary>Sets the NT/NTLMv2 response for this message.</summary>
		/// <remarks>Sets the NT/NTLMv2 response for this message.</remarks>
		/// <param name="ntResponse">The NT/NTLMv2 response.</param>
		public virtual void SetNtResponse(byte[] ntResponse)
		{
			this._ntResponse = ntResponse;
		}

		/// <summary>Returns the domain in which the user has an account.</summary>
		/// <remarks>Returns the domain in which the user has an account.</remarks>
		/// <returns>A <code>String</code> containing the domain for the user.</returns>
		public virtual string GetDomain()
		{
			return _domain;
		}

		/// <summary>Sets the domain for this message.</summary>
		/// <remarks>Sets the domain for this message.</remarks>
		/// <param name="domain">The domain.</param>
		public virtual void SetDomain(string domain)
		{
			this._domain = domain;
		}

		/// <summary>Returns the username for the authenticating user.</summary>
		/// <remarks>Returns the username for the authenticating user.</remarks>
		/// <returns>A <code>String</code> containing the user for this message.</returns>
		public virtual string GetUser()
		{
			return _user;
		}

		/// <summary>Sets the user for this message.</summary>
		/// <remarks>Sets the user for this message.</remarks>
		/// <param name="user">The user.</param>
		public virtual void SetUser(string user)
		{
			this._user = user;
		}

		/// <summary>Returns the workstation from which authentication is being performed.</summary>
		/// <remarks>Returns the workstation from which authentication is being performed.</remarks>
		/// <returns>A <code>String</code> containing the workstation.</returns>
		public virtual string GetWorkstation()
		{
			return _workstation;
		}

		/// <summary>Sets the workstation for this message.</summary>
		/// <remarks>Sets the workstation for this message.</remarks>
		/// <param name="workstation">The workstation.</param>
		public virtual void SetWorkstation(string workstation)
		{
			this._workstation = workstation;
		}

		/// <summary>
		/// The real session key if the regular session key is actually
		/// the encrypted version used for key exchange.
		/// </summary>
		/// <remarks>
		/// The real session key if the regular session key is actually
		/// the encrypted version used for key exchange.
		/// </remarks>
		/// <returns>A <code>byte[]</code> containing the session key.</returns>
		public virtual byte[] GetMasterKey()
		{
			return _masterKey;
		}

		/// <summary>Returns the session key.</summary>
		/// <remarks>Returns the session key.</remarks>
		/// <returns>A <code>byte[]</code> containing the session key.</returns>
		public virtual byte[] GetSessionKey()
		{
			return _sessionKey;
		}

		/// <summary>Sets the session key.</summary>
		/// <remarks>Sets the session key.</remarks>
		/// <param name="sessionKey">The session key.</param>
		public virtual void SetSessionKey(byte[] sessionKey)
		{
			this._sessionKey = sessionKey;
		}

		public override byte[] ToByteArray()
		{
			try
			{
				int flags = GetFlags();
				bool unicode = (flags & NtlmsspNegotiateUnicode) != 0;
				string oem = unicode ? null : GetOemEncoding();
				string domainName = GetDomain();
				byte[] domain = null;
				if (!string.IsNullOrEmpty(domainName))
				{
					domain = unicode ? Runtime.GetBytesForString(domainName, UniEncoding) : 
						Runtime.GetBytesForString(domainName, oem);
				}
				int domainLength = (domain != null) ? domain.Length : 0;
				string userName = GetUser();
				byte[] user = null;
				if (!string.IsNullOrEmpty(userName))
				{
					user = unicode ? Runtime.GetBytesForString(userName, UniEncoding) : Runtime.GetBytesForString
						(userName.ToUpper(), oem);
				}
				int userLength = (user != null) ? user.Length : 0;
				string workstationName = GetWorkstation();
				byte[] workstation = null;
				if (!string.IsNullOrEmpty(workstationName))
				{
					workstation = unicode ? Runtime.GetBytesForString(workstationName, UniEncoding
						) : Runtime.GetBytesForString(workstationName.ToUpper(), oem);
				}
				int workstationLength = (workstation != null) ? workstation.Length : 0;
				byte[] lmResponse = GetLMResponse();
				int lmLength = (lmResponse != null) ? lmResponse.Length : 0;
				byte[] ntResponse = GetNTResponse();
				int ntLength = (ntResponse != null) ? ntResponse.Length : 0;
				byte[] sessionKey = GetSessionKey();
				int keyLength = (sessionKey != null) ? sessionKey.Length : 0;
				byte[] type3 = new byte[64 + domainLength + userLength + workstationLength + lmLength
					 + ntLength + keyLength];
				Array.Copy(NtlmsspSignature, 0, type3, 0, 8);
				WriteULong(type3, 8, 3);
				int offset = 64;
				WriteSecurityBuffer(type3, 12, offset, lmResponse);
				offset += lmLength;
				WriteSecurityBuffer(type3, 20, offset, ntResponse);
				offset += ntLength;
				WriteSecurityBuffer(type3, 28, offset, domain);
				offset += domainLength;
				WriteSecurityBuffer(type3, 36, offset, user);
				offset += userLength;
				WriteSecurityBuffer(type3, 44, offset, workstation);
				offset += workstationLength;
				WriteSecurityBuffer(type3, 52, offset, sessionKey);
				WriteULong(type3, 60, flags);
				return type3;
			}
			catch (IOException ex)
			{
				throw new InvalidOperationException(ex.Message);
			}
		}

		public override string ToString()
		{
			string user = GetUser();
			string domain = GetDomain();
			string workstation = GetWorkstation();
			byte[] lmResponse = GetLMResponse();
			byte[] ntResponse = GetNTResponse();
			byte[] sessionKey = GetSessionKey();
			return "Type3Message[domain=" + domain + ",user=" + user + ",workstation=" + workstation
				 + ",lmResponse=" + (lmResponse == null ? "null" : "<" + lmResponse.Length + " bytes>"
				) + ",ntResponse=" + (ntResponse == null ? "null" : "<" + ntResponse.Length + " bytes>"
				) + ",sessionKey=" + (sessionKey == null ? "null" : "<" + sessionKey.Length + " bytes>"
				) + ",flags=0x" + Hexdump.ToHexString(GetFlags(), 8) + "]";
		}

		/// <summary>
		/// Returns the default flags for a generic Type-3 message in the
		/// current environment.
		/// </summary>
		/// <remarks>
		/// Returns the default flags for a generic Type-3 message in the
		/// current environment.
		/// </remarks>
		/// <returns>An <code>int</code> containing the default flags.</returns>
		public static int GetDefaultFlags()
		{
			return DefaultFlags;
		}

		/// <summary>
		/// Returns the default flags for a Type-3 message created in response
		/// to the given Type-2 message in the current environment.
		/// </summary>
		/// <remarks>
		/// Returns the default flags for a Type-3 message created in response
		/// to the given Type-2 message in the current environment.
		/// </remarks>
		/// <returns>An <code>int</code> containing the default flags.</returns>
		public static int GetDefaultFlags(Type2Message type2)
		{
			if (type2 == null)
			{
				return DefaultFlags;
			}
			int flags = NtlmsspNegotiateNtlm;
			flags |= ((type2.GetFlags() & NtlmsspNegotiateUnicode) != 0) ? NtlmsspNegotiateUnicode
				 : NtlmsspNegotiateOem;
			return flags;
		}

		/// <summary>
		/// Constructs the LanManager response to the given Type-2 message using
		/// the supplied password.
		/// </summary>
		/// <remarks>
		/// Constructs the LanManager response to the given Type-2 message using
		/// the supplied password.
		/// </remarks>
		/// <param name="type2">The Type-2 message.</param>
		/// <param name="password">The password.</param>
		/// <returns>A <code>byte[]</code> containing the LanManager response.</returns>
		public static byte[] GetLMResponse(Type2Message type2, string password)
		{
			if (type2 == null || password == null)
			{
				return null;
			}
			return NtlmPasswordAuthentication.GetPreNtlmResponse(password, type2.GetChallenge
				());
		}

		public static byte[] GetLMv2Response(Type2Message type2, string domain, string user
			, string password, byte[] clientChallenge)
		{
			if (type2 == null || domain == null || user == null || password == null || clientChallenge
				 == null)
			{
				return null;
			}
			return NtlmPasswordAuthentication.GetLMv2Response(domain, user, password, type2.GetChallenge
				(), clientChallenge);
		}

		public static byte[] GetNtlMv2Response(Type2Message type2, byte[] responseKeyNt, 
			byte[] clientChallenge)
		{
			if (type2 == null || responseKeyNt == null || clientChallenge == null)
			{
				return null;
			}
			long nanos1601 = (Runtime.CurrentTimeMillis() + MillisecondsBetween1970And1601
				) * 10000L;
			return NtlmPasswordAuthentication.GetNtlMv2Response(responseKeyNt, type2.GetChallenge
				(), clientChallenge, nanos1601, type2.GetTargetInformation());
		}

		/// <summary>
		/// Constructs the NT response to the given Type-2 message using
		/// the supplied password.
		/// </summary>
		/// <remarks>
		/// Constructs the NT response to the given Type-2 message using
		/// the supplied password.
		/// </remarks>
		/// <param name="type2">The Type-2 message.</param>
		/// <param name="password">The password.</param>
		/// <returns>A <code>byte[]</code> containing the NT response.</returns>
		public static byte[] GetNTResponse(Type2Message type2, string password)
		{
			if (type2 == null || password == null)
			{
				return null;
			}
			return NtlmPasswordAuthentication.GetNtlmResponse(password, type2.GetChallenge());
		}

		/// <summary>Returns the default domain from the current environment.</summary>
		/// <remarks>Returns the default domain from the current environment.</remarks>
		/// <returns>The default domain.</returns>
		public static string GetDefaultDomain()
		{
			return DefaultDomain;
		}

		/// <summary>Returns the default user from the current environment.</summary>
		/// <remarks>Returns the default user from the current environment.</remarks>
		/// <returns>The default user.</returns>
		public static string GetDefaultUser()
		{
			return DefaultUser;
		}

		/// <summary>Returns the default password from the current environment.</summary>
		/// <remarks>Returns the default password from the current environment.</remarks>
		/// <returns>The default password.</returns>
		public static string GetDefaultPassword()
		{
			return DefaultPassword;
		}

		/// <summary>Returns the default workstation from the current environment.</summary>
		/// <remarks>Returns the default workstation from the current environment.</remarks>
		/// <returns>The default workstation.</returns>
		public static string GetDefaultWorkstation()
		{
			return DefaultWorkstation;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Parse(byte[] material)
		{
			for (int i = 0; i < 8; i++)
			{
				if (material[i] != NtlmsspSignature[i])
				{
					throw new IOException("Not an NTLMSSP message.");
				}
			}
			if (ReadULong(material, 8) != 3)
			{
				throw new IOException("Not a Type 3 message.");
			}
			byte[] lmResponse = ReadSecurityBuffer(material, 12);
			int lmResponseOffset = ReadULong(material, 16);
			byte[] ntResponse = ReadSecurityBuffer(material, 20);
			int ntResponseOffset = ReadULong(material, 24);
			byte[] domain = ReadSecurityBuffer(material, 28);
			int domainOffset = ReadULong(material, 32);
			byte[] user = ReadSecurityBuffer(material, 36);
			int userOffset = ReadULong(material, 40);
			byte[] workstation = ReadSecurityBuffer(material, 44);
			int workstationOffset = ReadULong(material, 48);
			int flags;
			string charset;
			byte[] _sessionKey = null;
			if (lmResponseOffset == 52 || ntResponseOffset == 52 || domainOffset == 52 || userOffset
				 == 52 || workstationOffset == 52)
			{
				flags = NtlmsspNegotiateNtlm | NtlmsspNegotiateOem;
				charset = GetOemEncoding();
			}
			else
			{
				_sessionKey = ReadSecurityBuffer(material, 52);
				flags = ReadULong(material, 60);
				charset = ((flags & NtlmsspNegotiateUnicode) != 0) ? UniEncoding : GetOemEncoding
					();
			}
			SetSessionKey(_sessionKey);
			SetFlags(flags);
			SetLmResponse(lmResponse);
			SetNtResponse(ntResponse);
			SetDomain(Runtime.GetStringForBytes(domain, charset));
			SetUser(Runtime.GetStringForBytes(user, charset));
			SetWorkstation(Runtime.GetStringForBytes(workstation, charset));
		}
	}
}
