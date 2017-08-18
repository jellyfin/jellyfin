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
	internal class NetShareEnum : SmbComTransaction
	{
		private static readonly string Descr = "WrLeh\u0000B13BWz\u0000";

		public NetShareEnum()
		{
			Command = SmbComTransaction;
			SubCommand = NetShareEnum;
			// not really true be used by upper logic
			Name ="\\PIPE\\LANMAN";
			MaxParameterCount = 8;
			//        maxDataCount = 4096; why was this set?
			MaxSetupCount = 0x00;
			SetupCount = 0;
			Timeout = 5000;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			byte[] descr;
			try
			{
				//descr = Runtime.GetBytesForString(Descr, "ASCII");
                descr = Runtime.GetBytesForString(Descr, "UTF-8");
			}
			catch (UnsupportedEncodingException)
			{
				return 0;
			}
			WriteInt2(NetShareEnum, dst, dstIndex);
			dstIndex += 2;
			Array.Copy(descr, 0, dst, dstIndex, descr.Length);
			dstIndex += descr.Length;
			WriteInt2(unchecked(0x0001), dst, dstIndex);
			dstIndex += 2;
			WriteInt2(MaxDataCount, dst, dstIndex);
			dstIndex += 2;
			return dstIndex - start;
		}

		internal override int WriteDataWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int ReadSetupWireFormat(byte[] buffer, int bufferIndex, int len
			)
		{
			return 0;
		}

		internal override int ReadParametersWireFormat(byte[] buffer, int bufferIndex, int
			 len)
		{
			return 0;
		}

		internal override int ReadDataWireFormat(byte[] buffer, int bufferIndex, int len)
		{
			return 0;
		}

		public override string ToString()
		{
			return "NetShareEnum[" + base.ToString() + "]";
		}
	}
}
