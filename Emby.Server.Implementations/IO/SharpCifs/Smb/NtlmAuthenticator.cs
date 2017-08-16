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
	/// <summary>This class can be extended by applications that wish to trap authentication related exceptions and automatically retry the exceptional operation with different credentials.
	/// 	</summary>
	/// <remarks>This class can be extended by applications that wish to trap authentication related exceptions and automatically retry the exceptional operation with different credentials. Read <a href="../../../authhandler.html">jCIFS Exceptions and NtlmAuthenticator</a> for complete details.
	/// 	</remarks>
	public abstract class NtlmAuthenticator
	{
		private static NtlmAuthenticator _auth;

		private string _url;

		private SmbAuthException _sae;

		private void Reset()
		{
			_url = null;
			_sae = null;
		}

		/// <summary>Set the default <tt>NtlmAuthenticator</tt>.</summary>
		/// <remarks>Set the default <tt>NtlmAuthenticator</tt>. Once the default authenticator is set it cannot be changed. Calling this metho again will have no effect.
		/// 	</remarks>
		public static void SetDefault(NtlmAuthenticator a)
		{
			lock (typeof(NtlmAuthenticator))
			{
				if (_auth != null)
				{
					return;
				}
				_auth = a;
			}
		}

		protected internal string GetRequestingUrl()
		{
			return _url;
		}

		protected internal SmbAuthException GetRequestingException()
		{
			return _sae;
		}

		/// <summary>Used internally by jCIFS when an <tt>SmbAuthException</tt> is trapped to retrieve new user credentials.
		/// 	</summary>
		/// <remarks>Used internally by jCIFS when an <tt>SmbAuthException</tt> is trapped to retrieve new user credentials.
		/// 	</remarks>
		public static NtlmPasswordAuthentication RequestNtlmPasswordAuthentication(string
			 url, SmbAuthException sae)
		{
			if (_auth == null)
			{
				return null;
			}
			lock (_auth)
			{
				_auth._url = url;
				_auth._sae = sae;
				return _auth.GetNtlmPasswordAuthentication();
			}
		}

		/// <summary>An application extending this class must provide an implementation for this method that returns new user credentials try try when accessing SMB resources described by the <tt>getRequestingURL</tt> and <tt>getRequestingException</tt> methods.
		/// 	</summary>
		/// <remarks>
		/// An application extending this class must provide an implementation for this method that returns new user credentials try try when accessing SMB resources described by the <tt>getRequestingURL</tt> and <tt>getRequestingException</tt> methods.
		/// If this method returns <tt>null</tt> the <tt>SmbAuthException</tt> that triggered the authenticator check will simply be rethrown. The default implementation returns <tt>null</tt>.
		/// </remarks>
		protected internal virtual NtlmPasswordAuthentication GetNtlmPasswordAuthentication
			()
		{
			return null;
		}
	}
}
