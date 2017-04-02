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

using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComTreeConnectAndXResponse : AndXServerMessageBlock
	{
		private const int SmbSupportSearchBits = unchecked(0x0001);

		private const int SmbShareIsInDfs = unchecked(0x0002);

		internal bool SupportSearchBits;

		internal bool ShareIsInDfs;

		internal string Service;

		internal string NativeFileSystem = string.Empty;

		internal SmbComTreeConnectAndXResponse(ServerMessageBlock andx) : base(andx)
		{
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			SupportSearchBits = (buffer[bufferIndex] & SmbSupportSearchBits) == SmbSupportSearchBits;
			ShareIsInDfs = (buffer[bufferIndex] & SmbShareIsInDfs) == SmbShareIsInDfs;
			return 2;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			int start = bufferIndex;
			int len = ReadStringLength(buffer, bufferIndex, 32);
			try
			{
				//Service = Runtime.GetStringForBytes(buffer, bufferIndex, len, "ASCII");
                Service = Runtime.GetStringForBytes(buffer, bufferIndex, len, "UTF-8");
			}
			catch (UnsupportedEncodingException)
			{
				return 0;
			}
			bufferIndex += len + 1;
			// win98 observed not returning nativeFileSystem
			return bufferIndex - start;
		}

		public override string ToString()
		{
			string result = "SmbComTreeConnectAndXResponse[" + base.ToString() + ",supportSearchBits="
				 + SupportSearchBits + ",shareIsInDfs=" + ShareIsInDfs + ",service=" + Service +
				 ",nativeFileSystem=" + NativeFileSystem + "]";
			return result;
		}
	}
}
