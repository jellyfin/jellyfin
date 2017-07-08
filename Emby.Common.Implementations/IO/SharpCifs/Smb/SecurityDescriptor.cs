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
using System.IO;

namespace SharpCifs.Smb
{
	public class SecurityDescriptor
	{
		public int Type;

		public Ace[] Aces;

		public SecurityDescriptor()
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public SecurityDescriptor(byte[] buffer, int bufferIndex, int len)
		{
			Decode(buffer, bufferIndex, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int Decode(byte[] buffer, int bufferIndex, int len)
		{
			int start = bufferIndex;
			bufferIndex++;
			// revision
			bufferIndex++;
			Type = ServerMessageBlock.ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			ServerMessageBlock.ReadInt4(buffer, bufferIndex);
			// offset to owner sid
			bufferIndex += 4;
			ServerMessageBlock.ReadInt4(buffer, bufferIndex);
			// offset to group sid
			bufferIndex += 4;
			ServerMessageBlock.ReadInt4(buffer, bufferIndex);
			// offset to sacl
			bufferIndex += 4;
			int daclOffset = ServerMessageBlock.ReadInt4(buffer, bufferIndex);
			bufferIndex = start + daclOffset;
			bufferIndex++;
			// revision
			bufferIndex++;
			int size = ServerMessageBlock.ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			int numAces = ServerMessageBlock.ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			if (numAces > 4096)
			{
				throw new IOException("Invalid SecurityDescriptor");
			}
			if (daclOffset != 0)
			{
				Aces = new Ace[numAces];
				for (int i = 0; i < numAces; i++)
				{
					Aces[i] = new Ace();
					bufferIndex += Aces[i].Decode(buffer, bufferIndex);
				}
			}
			else
			{
				Aces = null;
			}
			return bufferIndex - start;
		}

		public override string ToString()
		{
			string ret = "SecurityDescriptor:\n";
			if (Aces != null)
			{
				for (int ai = 0; ai < Aces.Length; ai++)
				{
					ret += Aces[ai] + "\n";
				}
			}
			else
			{
				ret += "NULL";
			}
			return ret;
		}
	}
}
