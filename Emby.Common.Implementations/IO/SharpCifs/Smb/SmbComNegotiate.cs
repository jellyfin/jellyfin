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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComNegotiate : ServerMessageBlock
	{
	    private const string Dialects = "\u0002NT LM 0.12\u0000";

	    public SmbComNegotiate()
		{
			Command = SmbComNegotiate;
            Flags2 = SmbConstants.DefaultFlags2;
		}

		internal override int WriteParameterWordsWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteBytesWireFormat(byte[] dst, int dstIndex)
		{
			byte[] dialects;
			try
			{
                //dialects = Runtime.GetBytesForString(Dialects, "ASCII");
                dialects = Runtime.GetBytesForString(Dialects, "UTF-8");
			}
			catch (UnsupportedEncodingException)
			{
				return 0;
			}
			Array.Copy(dialects, 0, dst, dstIndex, dialects.Length);
			return dialects.Length;
		}

		internal override int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			)
		{
			return 0;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			return 0;
		}

		public override string ToString()
		{
			return "SmbComNegotiate[" + base.ToString() + ",wordCount=" + WordCount
				 + ",dialects=NT LM 0.12]";
		}
	}
}
