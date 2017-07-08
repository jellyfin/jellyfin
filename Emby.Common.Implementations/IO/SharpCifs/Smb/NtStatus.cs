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
namespace SharpCifs.Smb
{
	public static class NtStatus
	{
		public const int NtStatusOk = unchecked(0x00000000);

		public const int NtStatusUnsuccessful = unchecked((int)(0xC0000001));

		public const int NtStatusNotImplemented = unchecked((int)(0xC0000002));

		public const int NtStatusInvalidInfoClass = unchecked((int)(0xC0000003));

		public const int NtStatusAccessViolation = unchecked((int)(0xC0000005));

		public const int NtStatusInvalidHandle = unchecked((int)(0xC0000008));

		public const int NtStatusInvalidParameter = unchecked((int)(0xC000000d));

		public const int NtStatusNoSuchDevice = unchecked((int)(0xC000000e));

		public const int NtStatusNoSuchFile = unchecked((int)(0xC000000f));

		public const int NtStatusMoreProcessingRequired = unchecked((int)(0xC0000016)
			);

		public const int NtStatusAccessDenied = unchecked((int)(0xC0000022));

		public const int NtStatusBufferTooSmall = unchecked((int)(0xC0000023));

		public const int NtStatusObjectNameInvalid = unchecked((int)(0xC0000033));

		public const int NtStatusObjectNameNotFound = unchecked((int)(0xC0000034));

		public const int NtStatusObjectNameCollision = unchecked((int)(0xC0000035));

		public const int NtStatusPortDisconnected = unchecked((int)(0xC0000037));

		public const int NtStatusObjectPathInvalid = unchecked((int)(0xC0000039));

		public const int NtStatusObjectPathNotFound = unchecked((int)(0xC000003a));

		public const int NtStatusObjectPathSyntaxBad = unchecked((int)(0xC000003b));

		public const int NtStatusSharingViolation = unchecked((int)(0xC0000043));

		public const int NtStatusDeletePending = unchecked((int)(0xC0000056));

		public const int NtStatusNoLogonServers = unchecked((int)(0xC000005e));

		public const int NtStatusUserExists = unchecked((int)(0xC0000063));

		public const int NtStatusNoSuchUser = unchecked((int)(0xC0000064));

		public const int NtStatusWrongPassword = unchecked((int)(0xC000006a));

		public const int NtStatusLogonFailure = unchecked((int)(0xC000006d));

		public const int NtStatusAccountRestriction = unchecked((int)(0xC000006e));

		public const int NtStatusInvalidLogonHours = unchecked((int)(0xC000006f));

		public const int NtStatusInvalidWorkstation = unchecked((int)(0xC0000070));

		public const int NtStatusPasswordExpired = unchecked((int)(0xC0000071));

		public const int NtStatusAccountDisabled = unchecked((int)(0xC0000072));

		public const int NtStatusNoneMapped = unchecked((int)(0xC0000073));

		public const int NtStatusInvalidSid = unchecked((int)(0xC0000078));

		public const int NtStatusInstanceNotAvailable = unchecked((int)(0xC00000ab));

		public const int NtStatusPipeNotAvailable = unchecked((int)(0xC00000ac));

		public const int NtStatusInvalidPipeState = unchecked((int)(0xC00000ad));

		public const int NtStatusPipeBusy = unchecked((int)(0xC00000ae));

		public const int NtStatusPipeDisconnected = unchecked((int)(0xC00000b0));

		public const int NtStatusPipeClosing = unchecked((int)(0xC00000b1));

		public const int NtStatusPipeListening = unchecked((int)(0xC00000b3));

		public const int NtStatusFileIsADirectory = unchecked((int)(0xC00000ba));

		public const int NtStatusDuplicateName = unchecked((int)(0xC00000bd));

		public const int NtStatusNetworkNameDeleted = unchecked((int)(0xC00000c9));

		public const int NtStatusNetworkAccessDenied = unchecked((int)(0xC00000ca));

		public const int NtStatusBadNetworkName = unchecked((int)(0xC00000cc));

		public const int NtStatusRequestNotAccepted = unchecked((int)(0xC00000d0));

		public const int NtStatusCantAccessDomainInfo = unchecked((int)(0xC00000da));

		public const int NtStatusNoSuchDomain = unchecked((int)(0xC00000df));

		public const int NtStatusNotADirectory = unchecked((int)(0xC0000103));

		public const int NtStatusCannotDelete = unchecked((int)(0xC0000121));

		public const int NtStatusInvalidComputerName = unchecked((int)(0xC0000122));

		public const int NtStatusPipeBroken = unchecked((int)(0xC000014b));

		public const int NtStatusNoSuchAlias = unchecked((int)(0xC0000151));

		public const int NtStatusLogonTypeNotGranted = unchecked((int)(0xC000015b));

		public const int NtStatusNoTrustSamAccount = unchecked((int)(0xC000018b));

		public const int NtStatusTrustedDomainFailure = unchecked((int)(0xC000018c));

		public const int NtStatusNologonWorkstationTrustAccount = unchecked((int)(0xC0000199
			));

		public const int NtStatusPasswordMustChange = unchecked((int)(0xC0000224));

		public const int NtStatusNotFound = unchecked((int)(0xC0000225));

		public const int NtStatusAccountLockedOut = unchecked((int)(0xC0000234));

		public const int NtStatusPathNotCovered = unchecked((int)(0xC0000257));

		public const int NtStatusIoReparseTagNotHandled = unchecked((int)(0xC0000279
			));

		public static int[] NtStatusCodes = { NtStatusOk, NtStatusUnsuccessful
			, NtStatusNotImplemented, NtStatusInvalidInfoClass, NtStatusAccessViolation
			, NtStatusInvalidHandle, NtStatusInvalidParameter, NtStatusNoSuchDevice
			, NtStatusNoSuchFile, NtStatusMoreProcessingRequired, NtStatusAccessDenied
			, NtStatusBufferTooSmall, NtStatusObjectNameInvalid, NtStatusObjectNameNotFound
			, NtStatusObjectNameCollision, NtStatusPortDisconnected, NtStatusObjectPathInvalid
			, NtStatusObjectPathNotFound, NtStatusObjectPathSyntaxBad, NtStatusSharingViolation
			, NtStatusDeletePending, NtStatusNoLogonServers, NtStatusUserExists, NtStatusNoSuchUser
			, NtStatusWrongPassword, NtStatusLogonFailure, NtStatusAccountRestriction
			, NtStatusInvalidLogonHours, NtStatusInvalidWorkstation, NtStatusPasswordExpired
			, NtStatusAccountDisabled, NtStatusNoneMapped, NtStatusInvalidSid, NtStatusInstanceNotAvailable
			, NtStatusPipeNotAvailable, NtStatusInvalidPipeState, NtStatusPipeBusy
			, NtStatusPipeDisconnected, NtStatusPipeClosing, NtStatusPipeListening, 
			NtStatusFileIsADirectory, NtStatusDuplicateName, NtStatusNetworkNameDeleted
			, NtStatusNetworkAccessDenied, NtStatusBadNetworkName, NtStatusRequestNotAccepted
			, NtStatusCantAccessDomainInfo, NtStatusNoSuchDomain, NtStatusNotADirectory
			, NtStatusCannotDelete, NtStatusInvalidComputerName, NtStatusPipeBroken
			, NtStatusNoSuchAlias, NtStatusLogonTypeNotGranted, NtStatusNoTrustSamAccount
			, NtStatusTrustedDomainFailure, NtStatusNologonWorkstationTrustAccount, 
			NtStatusPasswordMustChange, NtStatusNotFound, NtStatusAccountLockedOut
			, NtStatusPathNotCovered, NtStatusIoReparseTagNotHandled };

		public static  string[] NtStatusMessages = { "The operation completed successfully."
			, "A device attached to the system is not functioning.", "Incorrect function.", 
			"The parameter is incorrect.", "Invalid access to memory location.", "The handle is invalid."
			, "The parameter is incorrect.", "The system cannot find the file specified.", "The system cannot find the file specified."
			, "More data is available.", "Access is denied.", "The data area passed to a system call is too small."
			, "The filename, directory name, or volume label syntax is incorrect.", "The system cannot find the file specified."
			, "Cannot create a file when that file already exists.", "The handle is invalid."
			, "The specified path is invalid.", "The system cannot find the path specified."
			, "The specified path is invalid.", "The process cannot access the file because it is being used by another process."
			, "Access is denied.", "There are currently no logon servers available to service the logon request."
			, "The specified user already exists.", "The specified user does not exist.", "The specified network password is not correct."
			, "Logon failure: unknown user name or bad password.", "Logon failure: user account restriction."
			, "Logon failure: account logon time restriction violation.", "Logon failure: user not allowed to log on to this computer."
			, "Logon failure: the specified account password has expired.", "Logon failure: account currently disabled."
			, "No mapping between account names and security IDs was done.", "The security ID structure is invalid."
			, "All pipe instances are busy.", "All pipe instances are busy.", "The pipe state is invalid."
			, "All pipe instances are busy.", "No process is on the other end of the pipe.", 
			"The pipe is being closed.", "Waiting for a process to open the other end of the pipe."
			, "Access is denied.", "A duplicate name exists on the network.", "The specified network name is no longer available."
			, "Network access is denied.", "The network name cannot be found.", "No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept."
			, "Indicates a Windows NT Server could not be contacted or that objects within the domain are protected such that necessary information could not be retrieved."
			, "The specified domain did not exist.", "The directory name is invalid.", "Access is denied."
			, "The format of the specified computer name is invalid.", "The pipe has been ended."
			, "The specified local group does not exist.", "Logon failure: the user has not been granted the requested logon type at this computer."
			, "The SAM database on the Windows NT Server does not have a computer account for this workstation trust relationship."
			, "The trust relationship between the primary domain and the trusted domain failed."
			, "The account used is a Computer Account. Use your global user account or local user account to access this server."
			, "The user must change his password before he logs on the first time.", "NT_STATUS_NOT_FOUND"
			, "The referenced account is currently locked out and may not be logged on to.", 
			"The remote system is not reachable by the transport.", "NT_STATUS_IO_REPARSE_TAG_NOT_HANDLED"
			 };
	}
}
