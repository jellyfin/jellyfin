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
	internal class NetServerEnum2 : SmbComTransaction
	{
		internal const int SvTypeAll = unchecked((int)(0xFFFFFFFF));

		internal const int SvTypeDomainEnum = unchecked((int)(0x80000000));

		internal static readonly string[] Descr = { "WrLehDO\u0000B16BBDz\u0000"
			, "WrLehDz\u0000B16BBDz\u0000" };

		internal string Domain;

		internal string LastName;

		internal int ServerTypes;

		internal NetServerEnum2(string domain, int serverTypes)
		{
			this.Domain = domain;
			this.ServerTypes = serverTypes;
			Command = SmbComTransaction;
			SubCommand = NetServerEnum2;
			// not really true be used by upper logic
			Name = "\\PIPE\\LANMAN";
			MaxParameterCount = 8;
			MaxDataCount = 16384;
			MaxSetupCount = unchecked(unchecked(0x00));
			SetupCount = 0;
			Timeout = 5000;
		}

		internal override void Reset(int key, string lastName)
		{
			base.Reset();
			this.LastName = lastName;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			byte[] descr;
			int which = SubCommand == NetServerEnum2 ? 0 : 1;
			try
			{
			    descr = Runtime.GetBytesForString(Descr[which], "UTF-8"); //"ASCII");
			}
			catch (UnsupportedEncodingException)
			{
				return 0;
			}
			WriteInt2(SubCommand & unchecked(0xFF), dst, dstIndex);
			dstIndex += 2;
			Array.Copy(descr, 0, dst, dstIndex, descr.Length);
			dstIndex += descr.Length;
			WriteInt2(unchecked(0x0001), dst, dstIndex);
			dstIndex += 2;
			WriteInt2(MaxDataCount, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(ServerTypes, dst, dstIndex);
			dstIndex += 4;
			dstIndex += WriteString(Domain.ToUpper(), dst, dstIndex, false);
			if (which == 1)
			{
				dstIndex += WriteString(LastName.ToUpper(), dst, dstIndex, false);
			}
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
			return "NetServerEnum2[" + base.ToString() + ",name=" + Name + ",serverTypes="
				 + (ServerTypes == SvTypeAll ? "SV_TYPE_ALL" : "SV_TYPE_DOMAIN_ENUM") + "]";
		}
	}
}
