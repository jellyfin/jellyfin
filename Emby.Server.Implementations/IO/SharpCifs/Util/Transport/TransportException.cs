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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Util.Transport
{

	public class TransportException : IOException
	{
		private Exception _rootCause;

		public TransportException()
		{
		}

		public TransportException(string msg) : base(msg)
		{
		}

		public TransportException(Exception rootCause)
		{
			this._rootCause = rootCause;
		}

		public TransportException(string msg, Exception rootCause) : base(msg)
		{
			this._rootCause = rootCause;
		}

		public virtual Exception GetRootCause()
		{
			return _rootCause;
		}

		public override string ToString()
		{
		    if (_rootCause != null)
			{
				StringWriter sw = new StringWriter();
				PrintWriter pw = new PrintWriter(sw);
				Runtime.PrintStackTrace(_rootCause, pw);
				return base.ToString() + "\n" + sw;
			}
		    return base.ToString();
		}
	}
}
