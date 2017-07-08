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
	internal abstract class SmbComNtTransactionResponse : SmbComTransactionResponse
	{
	    internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			int start = bufferIndex;
			buffer[bufferIndex++] = unchecked(unchecked(0x00));
			// Reserved
			buffer[bufferIndex++] = unchecked(unchecked(0x00));
			// Reserved
			buffer[bufferIndex++] = unchecked(unchecked(0x00));
			// Reserved
			TotalParameterCount = ReadInt4(buffer, bufferIndex);
			if (BufDataStart == 0)
			{
				BufDataStart = TotalParameterCount;
			}
			bufferIndex += 4;
			TotalDataCount = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			ParameterCount = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			ParameterOffset = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			ParameterDisplacement = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			DataCount = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			DataOffset = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			DataDisplacement = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			SetupCount = buffer[bufferIndex] & unchecked(0xFF);
			bufferIndex += 2;
			if (SetupCount != 0)
			{
				if (Log.Level >= 3)
				{
					Log.WriteLine("setupCount is not zero: " + SetupCount);
				}
			}
			return bufferIndex - start;
		}
	}
}
