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

namespace SharpCifs.Smb
{
	internal class SmbComSessionSetupAndXResponse : AndXServerMessageBlock
	{
		private string _nativeOs = string.Empty;

		private string _nativeLanMan = string.Empty;

		private string _primaryDomain = string.Empty;

		internal bool IsLoggedInAsGuest;

		internal byte[] Blob;

		internal SmbComSessionSetupAndXResponse(ServerMessageBlock andx) : base(andx)
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
			int start = bufferIndex;
			IsLoggedInAsGuest = (buffer[bufferIndex] & 0x01) == 0x01 ? true : false;
			bufferIndex += 2;
			if (ExtendedSecurity)
			{
				int blobLength = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
				Blob = new byte[blobLength];
			}
			return bufferIndex - start;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			int start = bufferIndex;
			if (ExtendedSecurity)
			{
				Array.Copy(buffer, bufferIndex, Blob, 0, Blob.Length);
				bufferIndex += Blob.Length;
			}
			_nativeOs = ReadString(buffer, bufferIndex);
			bufferIndex += StringWireLength(_nativeOs, bufferIndex);
			_nativeLanMan = ReadString(buffer, bufferIndex, start + ByteCount, 255, UseUnicode
				);
			bufferIndex += StringWireLength(_nativeLanMan, bufferIndex);
			if (!ExtendedSecurity)
			{
				_primaryDomain = ReadString(buffer, bufferIndex, start + ByteCount, 255, UseUnicode
					);
				bufferIndex += StringWireLength(_primaryDomain, bufferIndex);
			}
			return bufferIndex - start;
		}

		public override string ToString()
		{
			string result = "SmbComSessionSetupAndXResponse[" + base.ToString() + 
				",isLoggedInAsGuest=" + IsLoggedInAsGuest + ",nativeOs=" + _nativeOs + ",nativeLanMan="
				 + _nativeLanMan + ",primaryDomain=" + _primaryDomain + "]";
			return result;
		}
	}
}
