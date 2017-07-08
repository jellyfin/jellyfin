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
	public static class WinError
	{
		public static int ErrorSuccess = 0;

		public static int ErrorAccessDenied = 5;

		public static int ErrorReqNotAccep = 71;

		public static int ErrorBadPipe = 230;

		public static int ErrorPipeBusy = 231;

		public static int ErrorNoData = 232;

		public static int ErrorPipeNotConnected = 233;

		public static int ErrorMoreData = 234;

		public static int ErrorNoBrowserServersFound = 6118;

		public static int[] WinerrCodes = { ErrorSuccess, ErrorAccessDenied, 
			ErrorReqNotAccep, ErrorBadPipe, ErrorPipeBusy, ErrorNoData, ErrorPipeNotConnected
			, ErrorMoreData, ErrorNoBrowserServersFound };

		public static string[] WinerrMessages = { "The operation completed successfully."
			, "Access is denied.", "No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept."
			, "The pipe state is invalid.", "All pipe instances are busy.", "The pipe is being closed."
			, "No process is on the other end of the pipe.", "More data is available.", "The list of servers for this workgroup is not currently available."
			 };
	}
}
