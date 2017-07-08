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
	public class SessionRequestPacket : SessionServicePacket
	{
		private Name _calledName;

		private Name _callingName;

		public SessionRequestPacket()
		{
			_calledName = new Name();
			_callingName = new Name();
		}

		public SessionRequestPacket(Name calledName, Name callingName)
		{
			Type = SessionRequest;
			this._calledName = calledName;
			this._callingName = callingName;
		}

		internal override int WriteTrailerWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dstIndex += _calledName.WriteWireFormat(dst, dstIndex);
			dstIndex += _callingName.WriteWireFormat(dst, dstIndex);
			return dstIndex - start;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override int ReadTrailerWireFormat(InputStream @in, byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			if (@in.Read(buffer, bufferIndex, Length) != Length)
			{
				throw new IOException("invalid session request wire format");
			}
			bufferIndex += _calledName.ReadWireFormat(buffer, bufferIndex);
			bufferIndex += _callingName.ReadWireFormat(buffer, bufferIndex);
			return bufferIndex - start;
		}
	}
}
