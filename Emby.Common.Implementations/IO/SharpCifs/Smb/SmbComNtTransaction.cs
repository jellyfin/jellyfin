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
	internal abstract class SmbComNtTransaction : SmbComTransaction
	{
		private const int NttPrimarySetupOffset = 69;

		private const int NttSecondaryParameterOffset = 51;

		internal const int NtTransactQuerySecurityDesc = 6;

		internal int Function;

		public SmbComNtTransaction()
		{
			// relative to headerStart
			primarySetupOffset = NttPrimarySetupOffset;
			secondaryParameterOffset = NttSecondaryParameterOffset;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			if (Command != SmbComNtTransactSecondary)
			{
				dst[dstIndex++] = MaxSetupCount;
			}
			else
			{
				dst[dstIndex++] = unchecked(unchecked(0x00));
			}
			// Reserved
			dst[dstIndex++] = unchecked(unchecked(0x00));
			// Reserved
			dst[dstIndex++] = unchecked(unchecked(0x00));
			// Reserved
			WriteInt4(TotalParameterCount, dst, dstIndex);
			dstIndex += 4;
			WriteInt4(TotalDataCount, dst, dstIndex);
			dstIndex += 4;
			if (Command != SmbComNtTransactSecondary)
			{
				WriteInt4(MaxParameterCount, dst, dstIndex);
				dstIndex += 4;
				WriteInt4(MaxDataCount, dst, dstIndex);
				dstIndex += 4;
			}
			WriteInt4(ParameterCount, dst, dstIndex);
			dstIndex += 4;
			WriteInt4((ParameterCount == 0 ? 0 : ParameterOffset), dst, dstIndex);
			dstIndex += 4;
			if (Command == SmbComNtTransactSecondary)
			{
				WriteInt4(ParameterDisplacement, dst, dstIndex);
				dstIndex += 4;
			}
			WriteInt4(DataCount, dst, dstIndex);
			dstIndex += 4;
			WriteInt4((DataCount == 0 ? 0 : DataOffset), dst, dstIndex);
			dstIndex += 4;
			if (Command == SmbComNtTransactSecondary)
			{
				WriteInt4(DataDisplacement, dst, dstIndex);
				dstIndex += 4;
				dst[dstIndex++] = unchecked(unchecked(0x00));
			}
			else
			{
				// Reserved1
				dst[dstIndex++] = unchecked((byte)SetupCount);
				WriteInt2(Function, dst, dstIndex);
				dstIndex += 2;
				dstIndex += WriteSetupWireFormat(dst, dstIndex);
			}
			return dstIndex - start;
		}
	}
}
