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
	/// <summary>
	/// The <code>SmbAuthException</code> encapsulates the variety of
	/// authentication related error codes returned by an SMB server.
	/// </summary>
	/// <remarks>
	/// The <code>SmbAuthException</code> encapsulates the variety of
	/// authentication related error codes returned by an SMB server.
	/// <p>
	/// See <a href="../../../authhandler.html">jCIFS Exceptions and NtlmAuthenticator</a> for more information about <code>SmbAuthException</code>.
	/// </remarks>
	
	public class SmbAuthException : SmbException
	{
		internal SmbAuthException(int errcode) : base(errcode, null)
		{
		}
	}
}
