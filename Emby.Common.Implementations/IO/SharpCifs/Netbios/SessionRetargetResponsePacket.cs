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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	internal class SessionRetargetResponsePacket : SessionServicePacket
	{
		private NbtAddress _retargetAddress;

		private int _retargetPort;

		public SessionRetargetResponsePacket()
		{
			Type = SessionRetargetResponse;
			Length = 6;
		}

		internal override int WriteTrailerWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override int ReadTrailerWireFormat(InputStream @in, byte[] buffer, int bufferIndex
			)
		{
			if (@in.Read(buffer, bufferIndex, Length) != Length)
			{
				throw new IOException("unexpected EOF reading netbios retarget session response");
			}
			int addr = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			_retargetAddress = new NbtAddress(null, addr, false, NbtAddress.BNode);
			_retargetPort = ReadInt2(buffer, bufferIndex);
			return Length;
		}
	}
}
