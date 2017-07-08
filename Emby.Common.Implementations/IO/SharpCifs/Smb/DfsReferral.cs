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
using System.Collections.Generic;

namespace SharpCifs.Smb
{
	
	public class DfsReferral : SmbException
	{
		public int PathConsumed;

		public long Ttl;

		public string Server;

		public string Share;

		public string Link;

		public string Path;

		public bool ResolveHashes;

		public long Expiration;

		internal DfsReferral Next;

		internal IDictionary<string, DfsReferral> Map;

		internal string Key = null;

		public DfsReferral()
		{
			// Server
			// Share
			// Path relative to tree from which this referral was thrown
			Next = this;
		}

		internal virtual void Append(DfsReferral dr)
		{
			dr.Next = Next;
			Next = dr;
		}

		public override string ToString()
		{
			return "DfsReferral[pathConsumed=" + PathConsumed + ",server=" + Server + ",share="
				 + Share + ",link=" + Link + ",path=" + Path + ",ttl=" + Ttl + ",expiration=" + 
				Expiration + ",resolveHashes=" + ResolveHashes + "]";
		}
	}
}
