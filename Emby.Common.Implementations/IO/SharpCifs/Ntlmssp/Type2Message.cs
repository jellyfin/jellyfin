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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Ntlmssp
{
	/// <summary>Represents an NTLMSSP Type-2 message.</summary>
	/// <remarks>Represents an NTLMSSP Type-2 message.</remarks>
	public class Type2Message : NtlmMessage
	{
		private static readonly int DefaultFlags;

		private static readonly string DefaultDomain;

		private static readonly byte[] DefaultTargetInformation;

		private byte[] _challenge;

		private string _target;

		private byte[] _context;

		private byte[] _targetInformation;

		static Type2Message()
		{
			DefaultFlags = NtlmsspNegotiateNtlm | (Config.GetBoolean("jcifs.smb.client.useUnicode"
				, true) ? NtlmsspNegotiateUnicode : NtlmsspNegotiateOem);
			DefaultDomain = Config.GetProperty("jcifs.smb.client.domain", null);
			byte[] domain = new byte[0];
			if (DefaultDomain != null)
			{
				try
				{
					domain = Runtime.GetBytesForString(DefaultDomain, UniEncoding);
				}
				catch (IOException)
				{
				}
			}
			int domainLength = domain.Length;
			byte[] server = new byte[0];
			try
			{
				string host = NbtAddress.GetLocalHost().GetHostName();
				if (host != null)
				{
					try
					{
						server = Runtime.GetBytesForString(host, UniEncoding);
					}
					catch (IOException)
					{
					}
				}
			}
			catch (UnknownHostException)
			{
			}
			int serverLength = server.Length;
			byte[] targetInfo = new byte[(domainLength > 0 ? domainLength + 4 : 0) + (serverLength
				 > 0 ? serverLength + 4 : 0) + 4];
			int offset = 0;
			if (domainLength > 0)
			{
				WriteUShort(targetInfo, offset, 2);
				offset += 2;
				WriteUShort(targetInfo, offset, domainLength);
				offset += 2;
				Array.Copy(domain, 0, targetInfo, offset, domainLength);
				offset += domainLength;
			}
			if (serverLength > 0)
			{
				WriteUShort(targetInfo, offset, 1);
				offset += 2;
				WriteUShort(targetInfo, offset, serverLength);
				offset += 2;
				Array.Copy(server, 0, targetInfo, offset, serverLength);
			}
			DefaultTargetInformation = targetInfo;
		}

		/// <summary>
		/// Creates a Type-2 message using default values from the current
		/// environment.
		/// </summary>
		/// <remarks>
		/// Creates a Type-2 message using default values from the current
		/// environment.
		/// </remarks>
		public Type2Message() : this(GetDefaultFlags(), null, null)
		{
		}

		/// <summary>
		/// Creates a Type-2 message in response to the given Type-1 message
		/// using default values from the current environment.
		/// </summary>
		/// <remarks>
		/// Creates a Type-2 message in response to the given Type-1 message
		/// using default values from the current environment.
		/// </remarks>
		/// <param name="type1">The Type-1 message which this represents a response to.</param>
		public Type2Message(Type1Message type1) : this(type1, null, null)
		{
		}

		/// <summary>Creates a Type-2 message in response to the given Type-1 message.</summary>
		/// <remarks>Creates a Type-2 message in response to the given Type-1 message.</remarks>
		/// <param name="type1">The Type-1 message which this represents a response to.</param>
		/// <param name="challenge">The challenge from the domain controller/server.</param>
		/// <param name="target">The authentication target.</param>
		public Type2Message(Type1Message type1, byte[] challenge, string target) : this(GetDefaultFlags
			(type1), challenge, (type1 != null && target == null && type1.GetFlag(NtlmsspRequestTarget
			)) ? GetDefaultDomain() : target)
		{
		}

		/// <summary>Creates a Type-2 message with the specified parameters.</summary>
		/// <remarks>Creates a Type-2 message with the specified parameters.</remarks>
		/// <param name="flags">The flags to apply to this message.</param>
		/// <param name="challenge">The challenge from the domain controller/server.</param>
		/// <param name="target">The authentication target.</param>
		public Type2Message(int flags, byte[] challenge, string target)
		{
			SetFlags(flags);
			SetChallenge(challenge);
			SetTarget(target);
			if (target != null)
			{
				SetTargetInformation(GetDefaultTargetInformation());
			}
		}

		/// <summary>Creates a Type-2 message using the given raw Type-2 material.</summary>
		/// <remarks>Creates a Type-2 message using the given raw Type-2 material.</remarks>
		/// <param name="material">The raw Type-2 material used to construct this message.</param>
		/// <exception cref="System.IO.IOException">If an error occurs while parsing the material.
		/// 	</exception>
		public Type2Message(byte[] material)
		{
			Parse(material);
		}

		/// <summary>Returns the challenge for this message.</summary>
		/// <remarks>Returns the challenge for this message.</remarks>
		/// <returns>A <code>byte[]</code> containing the challenge.</returns>
		public virtual byte[] GetChallenge()
		{
			return _challenge;
		}

		/// <summary>Sets the challenge for this message.</summary>
		/// <remarks>Sets the challenge for this message.</remarks>
		/// <param name="challenge">The challenge from the domain controller/server.</param>
		public virtual void SetChallenge(byte[] challenge)
		{
			this._challenge = challenge;
		}

		/// <summary>Returns the authentication target.</summary>
		/// <remarks>Returns the authentication target.</remarks>
		/// <returns>A <code>String</code> containing the authentication target.</returns>
		public virtual string GetTarget()
		{
			return _target;
		}

		/// <summary>Sets the authentication target.</summary>
		/// <remarks>Sets the authentication target.</remarks>
		/// <param name="target">The authentication target.</param>
		public virtual void SetTarget(string target)
		{
			this._target = target;
		}

		/// <summary>Returns the target information block.</summary>
		/// <remarks>Returns the target information block.</remarks>
		/// <returns>
		/// A <code>byte[]</code> containing the target information block.
		/// The target information block is used by the client to create an
		/// NTLMv2 response.
		/// </returns>
		public virtual byte[] GetTargetInformation()
		{
			return _targetInformation;
		}

		/// <summary>Sets the target information block.</summary>
		/// <remarks>
		/// Sets the target information block.
		/// The target information block is used by the client to create
		/// an NTLMv2 response.
		/// </remarks>
		/// <param name="targetInformation">The target information block.</param>
		public virtual void SetTargetInformation(byte[] targetInformation)
		{
			this._targetInformation = targetInformation;
		}

		/// <summary>Returns the local security context.</summary>
		/// <remarks>Returns the local security context.</remarks>
		/// <returns>
		/// A <code>byte[]</code> containing the local security
		/// context.  This is used by the client to negotiate local
		/// authentication.
		/// </returns>
		public virtual byte[] GetContext()
		{
			return _context;
		}

		/// <summary>Sets the local security context.</summary>
		/// <remarks>
		/// Sets the local security context.  This is used by the client
		/// to negotiate local authentication.
		/// </remarks>
		/// <param name="context">The local security context.</param>
		public virtual void SetContext(byte[] context)
		{
			this._context = context;
		}

		public override byte[] ToByteArray()
		{
			try
			{
				string targetName = GetTarget();
				byte[] challenge = GetChallenge();
				byte[] context = GetContext();
				byte[] targetInformation = GetTargetInformation();
				int flags = GetFlags();
				byte[] target = new byte[0];
				if ((flags & NtlmsspRequestTarget) != 0)
				{
					if (!string.IsNullOrEmpty(targetName))
					{
						target = (flags & NtlmsspNegotiateUnicode) != 0 ? Runtime.GetBytesForString
							(targetName, UniEncoding) : Runtime.GetBytesForString(targetName.ToUpper
							(), GetOemEncoding());
					}
					else
					{
						flags &= (unchecked((int)(0xffffffff)) ^ NtlmsspRequestTarget);
					}
				}
				if (targetInformation != null)
				{
					flags |= NtlmsspNegotiateTargetInfo;
					// empty context is needed for padding when t.i. is supplied.
					if (context == null)
					{
						context = new byte[8];
					}
				}
				int data = 32;
				if (context != null)
				{
					data += 8;
				}
				if (targetInformation != null)
				{
					data += 8;
				}
				byte[] type2 = new byte[data + target.Length + (targetInformation != null ? targetInformation
					.Length : 0)];
				Array.Copy(NtlmsspSignature, 0, type2, 0, 8);
				WriteULong(type2, 8, 2);
				WriteSecurityBuffer(type2, 12, data, target);
				WriteULong(type2, 20, flags);
				Array.Copy(challenge ?? new byte[8], 0, type2, 24, 8);
				if (context != null)
				{
					Array.Copy(context, 0, type2, 32, 8);
				}
				if (targetInformation != null)
				{
					WriteSecurityBuffer(type2, 40, data + target.Length, targetInformation);
				}
				return type2;
			}
			catch (IOException ex)
			{
				throw new InvalidOperationException(ex.Message);
			}
		}

		public override string ToString()
		{
			string target = GetTarget();
			byte[] challenge = GetChallenge();
			byte[] context = GetContext();
			byte[] targetInformation = GetTargetInformation();
			return "Type2Message[target=" + target + ",challenge=" + (challenge == null ? "null"
				 : "<" + challenge.Length + " bytes>") + ",context=" + (context == null ? "null"
				 : "<" + context.Length + " bytes>") + ",targetInformation=" + (targetInformation
				 == null ? "null" : "<" + targetInformation.Length + " bytes>") + ",flags=0x" + 
				Hexdump.ToHexString(GetFlags(), 8) + "]";
		}

		/// <summary>
		/// Returns the default flags for a generic Type-2 message in the
		/// current environment.
		/// </summary>
		/// <remarks>
		/// Returns the default flags for a generic Type-2 message in the
		/// current environment.
		/// </remarks>
		/// <returns>An <code>int</code> containing the default flags.</returns>
		public static int GetDefaultFlags()
		{
			return DefaultFlags;
		}

		/// <summary>
		/// Returns the default flags for a Type-2 message created in response
		/// to the given Type-1 message in the current environment.
		/// </summary>
		/// <remarks>
		/// Returns the default flags for a Type-2 message created in response
		/// to the given Type-1 message in the current environment.
		/// </remarks>
		/// <returns>An <code>int</code> containing the default flags.</returns>
		public static int GetDefaultFlags(Type1Message type1)
		{
			if (type1 == null)
			{
				return DefaultFlags;
			}
			int flags = NtlmsspNegotiateNtlm;
			int type1Flags = type1.GetFlags();
			flags |= ((type1Flags & NtlmsspNegotiateUnicode) != 0) ? NtlmsspNegotiateUnicode
				 : NtlmsspNegotiateOem;
			if ((type1Flags & NtlmsspRequestTarget) != 0)
			{
				string domain = GetDefaultDomain();
				if (domain != null)
				{
					flags |= NtlmsspRequestTarget | NtlmsspTargetTypeDomain;
				}
			}
			return flags;
		}

		/// <summary>Returns the default domain from the current environment.</summary>
		/// <remarks>Returns the default domain from the current environment.</remarks>
		/// <returns>A <code>String</code> containing the domain.</returns>
		public static string GetDefaultDomain()
		{
			return DefaultDomain;
		}

		public static byte[] GetDefaultTargetInformation()
		{
			return DefaultTargetInformation;
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
			if (ReadULong(material, 8) != 2)
			{
				throw new IOException("Not a Type 2 message.");
			}
			int flags = ReadULong(material, 20);
			SetFlags(flags);
			string target = null;
			byte[] bytes = ReadSecurityBuffer(material, 12);
			if (bytes.Length != 0)
			{
				target = Runtime.GetStringForBytes(bytes, ((flags & NtlmsspNegotiateUnicode
					) != 0) ? UniEncoding : GetOemEncoding());
			}
			SetTarget(target);
			for (int i1 = 24; i1 < 32; i1++)
			{
				if (material[i1] != 0)
				{
					byte[] challenge = new byte[8];
					Array.Copy(material, 24, challenge, 0, 8);
					SetChallenge(challenge);
					break;
				}
			}
			int offset = ReadULong(material, 16);
			// offset of targetname start
			if (offset == 32 || material.Length == 32)
			{
				return;
			}
			for (int i2 = 32; i2 < 40; i2++)
			{
				if (material[i2] != 0)
				{
					byte[] context = new byte[8];
					Array.Copy(material, 32, context, 0, 8);
					SetContext(context);
					break;
				}
			}
			if (offset == 40 || material.Length == 40)
			{
				return;
			}
			bytes = ReadSecurityBuffer(material, 40);
			if (bytes.Length != 0)
			{
				SetTargetInformation(bytes);
			}
		}
	}
}
