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
	/// <summary>Represents an NTLMSSP Type-1 message.</summary>
	/// <remarks>Represents an NTLMSSP Type-1 message.</remarks>
	public class Type1Message : NtlmMessage
	{
		private static readonly int DefaultFlags;

		private static readonly string DefaultDomain;

		private static readonly string DefaultWorkstation;

		private string _suppliedDomain;

		private string _suppliedWorkstation;

		static Type1Message()
		{
			DefaultFlags = NtlmsspNegotiateNtlm | (Config.GetBoolean("jcifs.smb.client.useUnicode"
				, true) ? NtlmsspNegotiateUnicode : NtlmsspNegotiateOem);
			DefaultDomain = Config.GetProperty("jcifs.smb.client.domain", null);
			string defaultWorkstation = null;
			try
			{
				defaultWorkstation = NbtAddress.GetLocalHost().GetHostName();
			}
			catch (UnknownHostException)
			{
			}
			DefaultWorkstation = defaultWorkstation;
		}

		/// <summary>
		/// Creates a Type-1 message using default values from the current
		/// environment.
		/// </summary>
		/// <remarks>
		/// Creates a Type-1 message using default values from the current
		/// environment.
		/// </remarks>
		public Type1Message() : this(GetDefaultFlags(), GetDefaultDomain(), GetDefaultWorkstation
			())
		{
		}

		/// <summary>Creates a Type-1 message with the specified parameters.</summary>
		/// <remarks>Creates a Type-1 message with the specified parameters.</remarks>
		/// <param name="flags">The flags to apply to this message.</param>
		/// <param name="suppliedDomain">The supplied authentication domain.</param>
		/// <param name="suppliedWorkstation">The supplied workstation name.</param>
		public Type1Message(int flags, string suppliedDomain, string suppliedWorkstation)
		{
			SetFlags(GetDefaultFlags() | flags);
			SetSuppliedDomain(suppliedDomain);
			if (suppliedWorkstation == null)
			{
				suppliedWorkstation = GetDefaultWorkstation();
			}
			SetSuppliedWorkstation(suppliedWorkstation);
		}

		/// <summary>Creates a Type-1 message using the given raw Type-1 material.</summary>
		/// <remarks>Creates a Type-1 message using the given raw Type-1 material.</remarks>
		/// <param name="material">The raw Type-1 material used to construct this message.</param>
		/// <exception cref="System.IO.IOException">If an error occurs while parsing the material.
		/// 	</exception>
		public Type1Message(byte[] material)
		{
			Parse(material);
		}

		/// <summary>Returns the supplied authentication domain.</summary>
		/// <remarks>Returns the supplied authentication domain.</remarks>
		/// <returns>A <code>String</code> containing the supplied domain.</returns>
		public virtual string GetSuppliedDomain()
		{
			return _suppliedDomain;
		}

		/// <summary>Sets the supplied authentication domain for this message.</summary>
		/// <remarks>Sets the supplied authentication domain for this message.</remarks>
		/// <param name="suppliedDomain">The supplied domain for this message.</param>
		public virtual void SetSuppliedDomain(string suppliedDomain)
		{
			this._suppliedDomain = suppliedDomain;
		}

		/// <summary>Returns the supplied workstation name.</summary>
		/// <remarks>Returns the supplied workstation name.</remarks>
		/// <returns>A <code>String</code> containing the supplied workstation name.</returns>
		public virtual string GetSuppliedWorkstation()
		{
			return _suppliedWorkstation;
		}

		/// <summary>Sets the supplied workstation name for this message.</summary>
		/// <remarks>Sets the supplied workstation name for this message.</remarks>
		/// <param name="suppliedWorkstation">The supplied workstation for this message.</param>
		public virtual void SetSuppliedWorkstation(string suppliedWorkstation)
		{
			this._suppliedWorkstation = suppliedWorkstation;
		}

		public override byte[] ToByteArray()
		{
			try
			{
				string suppliedDomain = GetSuppliedDomain();
				string suppliedWorkstation = GetSuppliedWorkstation();
				int flags = GetFlags();
				bool hostInfo = false;
				byte[] domain = new byte[0];
				if (!string.IsNullOrEmpty(suppliedDomain))
				{
					hostInfo = true;
					flags |= NtlmsspNegotiateOemDomainSupplied;
					domain = Runtime.GetBytesForString(suppliedDomain.ToUpper(), GetOemEncoding
						());
				}
				else
				{
					flags &= (NtlmsspNegotiateOemDomainSupplied ^ unchecked((int)(0xffffffff)));
				}
				byte[] workstation = new byte[0];
				if (!string.IsNullOrEmpty(suppliedWorkstation))
				{
					hostInfo = true;
					flags |= NtlmsspNegotiateOemWorkstationSupplied;
					workstation = Runtime.GetBytesForString(suppliedWorkstation.ToUpper(), GetOemEncoding
						());
				}
				else
				{
					flags &= (NtlmsspNegotiateOemWorkstationSupplied ^ unchecked((int)(0xffffffff
						)));
				}
				byte[] type1 = new byte[hostInfo ? (32 + domain.Length + workstation.Length) : 16
					];
				Array.Copy(NtlmsspSignature, 0, type1, 0, 8);
				WriteULong(type1, 8, 1);
				WriteULong(type1, 12, flags);
				if (hostInfo)
				{
					WriteSecurityBuffer(type1, 16, 32, domain);
					WriteSecurityBuffer(type1, 24, 32 + domain.Length, workstation);
				}
				return type1;
			}
			catch (IOException ex)
			{
				throw new InvalidOperationException(ex.Message);
			}
		}

		public override string ToString()
		{
			string suppliedDomain = GetSuppliedDomain();
			string suppliedWorkstation = GetSuppliedWorkstation();
			return "Type1Message[suppliedDomain=" + (suppliedDomain ?? "null"
				) + ",suppliedWorkstation=" + (suppliedWorkstation ?? "null"
				) + ",flags=0x" + Hexdump.ToHexString(GetFlags(), 8) + "]";
		}

		/// <summary>
		/// Returns the default flags for a generic Type-1 message in the
		/// current environment.
		/// </summary>
		/// <remarks>
		/// Returns the default flags for a generic Type-1 message in the
		/// current environment.
		/// </remarks>
		/// <returns>An <code>int</code> containing the default flags.</returns>
		public static int GetDefaultFlags()
		{
			return DefaultFlags;
		}

		/// <summary>Returns the default domain from the current environment.</summary>
		/// <remarks>Returns the default domain from the current environment.</remarks>
		/// <returns>A <code>String</code> containing the default domain.</returns>
		public static string GetDefaultDomain()
		{
			return DefaultDomain;
		}

		/// <summary>Returns the default workstation from the current environment.</summary>
		/// <remarks>Returns the default workstation from the current environment.</remarks>
		/// <returns>A <code>String</code> containing the default workstation.</returns>
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
			if (ReadULong(material, 8) != 1)
			{
				throw new IOException("Not a Type 1 message.");
			}
			int flags = ReadULong(material, 12);
			string suppliedDomain = null;
			if ((flags & NtlmsspNegotiateOemDomainSupplied) != 0)
			{
				byte[] domain = ReadSecurityBuffer(material, 16);
				suppliedDomain = Runtime.GetStringForBytes(domain, GetOemEncoding());
			}
			string suppliedWorkstation = null;
			if ((flags & NtlmsspNegotiateOemWorkstationSupplied) != 0)
			{
				byte[] workstation = ReadSecurityBuffer(material, 24);
				suppliedWorkstation = Runtime.GetStringForBytes(workstation, GetOemEncoding
					());
			}
			SetFlags(flags);
			SetSuppliedDomain(suppliedDomain);
			SetSuppliedWorkstation(suppliedWorkstation);
		}
	}
}
