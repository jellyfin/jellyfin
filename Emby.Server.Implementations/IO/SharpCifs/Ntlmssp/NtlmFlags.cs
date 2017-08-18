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
namespace SharpCifs.Ntlmssp
{
	/// <summary>Flags used during negotiation of NTLMSSP authentication.</summary>
	/// <remarks>Flags used during negotiation of NTLMSSP authentication.</remarks>
	public abstract class NtlmFlags
	{
		/// <summary>Indicates whether Unicode strings are supported or used.</summary>
		/// <remarks>Indicates whether Unicode strings are supported or used.</remarks>
		public const int NtlmsspNegotiateUnicode = unchecked(0x00000001);

		/// <summary>Indicates whether OEM strings are supported or used.</summary>
		/// <remarks>Indicates whether OEM strings are supported or used.</remarks>
		public const int NtlmsspNegotiateOem = unchecked(0x00000002);

		/// <summary>
		/// Indicates whether the authentication target is requested from
		/// the server.
		/// </summary>
		/// <remarks>
		/// Indicates whether the authentication target is requested from
		/// the server.
		/// </remarks>
		public const int NtlmsspRequestTarget = unchecked(0x00000004);

		/// <summary>
		/// Specifies that communication across the authenticated channel
		/// should carry a digital signature (message integrity).
		/// </summary>
		/// <remarks>
		/// Specifies that communication across the authenticated channel
		/// should carry a digital signature (message integrity).
		/// </remarks>
		public const int NtlmsspNegotiateSign = unchecked(0x00000010);

		/// <summary>
		/// Specifies that communication across the authenticated channel
		/// should be encrypted (message confidentiality).
		/// </summary>
		/// <remarks>
		/// Specifies that communication across the authenticated channel
		/// should be encrypted (message confidentiality).
		/// </remarks>
		public const int NtlmsspNegotiateSeal = unchecked(0x00000020);

		/// <summary>Indicates datagram authentication.</summary>
		/// <remarks>Indicates datagram authentication.</remarks>
		public const int NtlmsspNegotiateDatagramStyle = unchecked(0x00000040);

		/// <summary>
		/// Indicates that the LAN Manager session key should be used for
		/// signing and sealing authenticated communication.
		/// </summary>
		/// <remarks>
		/// Indicates that the LAN Manager session key should be used for
		/// signing and sealing authenticated communication.
		/// </remarks>
		public const int NtlmsspNegotiateLmKey = unchecked(0x00000080);

		public const int NtlmsspNegotiateNetware = unchecked(0x00000100);

		/// <summary>Indicates support for NTLM authentication.</summary>
		/// <remarks>Indicates support for NTLM authentication.</remarks>
		public const int NtlmsspNegotiateNtlm = unchecked(0x00000200);

		/// <summary>
		/// Indicates whether the OEM-formatted domain name in which the
		/// client workstation has membership is supplied in the Type-1 message.
		/// </summary>
		/// <remarks>
		/// Indicates whether the OEM-formatted domain name in which the
		/// client workstation has membership is supplied in the Type-1 message.
		/// This is used in the negotation of local authentication.
		/// </remarks>
		public const int NtlmsspNegotiateOemDomainSupplied = unchecked(0x00001000);

		/// <summary>
		/// Indicates whether the OEM-formatted workstation name is supplied
		/// in the Type-1 message.
		/// </summary>
		/// <remarks>
		/// Indicates whether the OEM-formatted workstation name is supplied
		/// in the Type-1 message.  This is used in the negotiation of local
		/// authentication.
		/// </remarks>
		public const int NtlmsspNegotiateOemWorkstationSupplied = unchecked(0x00002000);

		/// <summary>
		/// Sent by the server to indicate that the server and client are
		/// on the same machine.
		/// </summary>
		/// <remarks>
		/// Sent by the server to indicate that the server and client are
		/// on the same machine.  This implies that the server will include
		/// a local security context handle in the Type 2 message, for
		/// use in local authentication.
		/// </remarks>
		public const int NtlmsspNegotiateLocalCall = unchecked(0x00004000);

		/// <summary>
		/// Indicates that authenticated communication between the client
		/// and server should carry a "dummy" digital signature.
		/// </summary>
		/// <remarks>
		/// Indicates that authenticated communication between the client
		/// and server should carry a "dummy" digital signature.
		/// </remarks>
		public const int NtlmsspNegotiateAlwaysSign = unchecked(0x00008000);

		/// <summary>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a domain.
		/// </summary>
		/// <remarks>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a domain.
		/// </remarks>
		public const int NtlmsspTargetTypeDomain = unchecked(0x00010000);

		/// <summary>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a server.
		/// </summary>
		/// <remarks>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a server.
		/// </remarks>
		public const int NtlmsspTargetTypeServer = unchecked(0x00020000);

		/// <summary>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a share (presumably for share-level
		/// authentication).
		/// </summary>
		/// <remarks>
		/// Sent by the server in the Type 2 message to indicate that the
		/// target authentication realm is a share (presumably for share-level
		/// authentication).
		/// </remarks>
		public const int NtlmsspTargetTypeShare = unchecked(0x00040000);

		/// <summary>
		/// Indicates that the NTLM2 signing and sealing scheme should be used
		/// for protecting authenticated communications.
		/// </summary>
		/// <remarks>
		/// Indicates that the NTLM2 signing and sealing scheme should be used
		/// for protecting authenticated communications.  This refers to a
		/// particular session security scheme, and is not related to the use
		/// of NTLMv2 authentication.
		/// </remarks>
		public const int NtlmsspNegotiateNtlm2 = unchecked(0x00080000);

		public const int NtlmsspRequestInitResponse = unchecked(0x00100000);

		public const int NtlmsspRequestAcceptResponse = unchecked(0x00200000);

		public const int NtlmsspRequestNonNtSessionKey = unchecked(0x00400000
			);

		/// <summary>
		/// Sent by the server in the Type 2 message to indicate that it is
		/// including a Target Information block in the message.
		/// </summary>
		/// <remarks>
		/// Sent by the server in the Type 2 message to indicate that it is
		/// including a Target Information block in the message.  The Target
		/// Information block is used in the calculation of the NTLMv2 response.
		/// </remarks>
		public const int NtlmsspNegotiateTargetInfo = unchecked(0x00800000);

		/// <summary>Indicates that 128-bit encryption is supported.</summary>
		/// <remarks>Indicates that 128-bit encryption is supported.</remarks>
		public const int NtlmsspNegotiate128 = unchecked(0x20000000);

		public const int NtlmsspNegotiateKeyExch = unchecked(0x40000000);

		/// <summary>Indicates that 56-bit encryption is supported.</summary>
		/// <remarks>Indicates that 56-bit encryption is supported.</remarks>
		public const int NtlmsspNegotiate56 = unchecked((int)(0x80000000));
	}
}
