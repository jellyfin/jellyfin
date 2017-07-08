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
	internal class Trans2GetDfsReferralResponse : SmbComTransactionResponse
	{
		internal class Referral
		{
			private int _version;

			private int _size;

			private int _serverType;

			private int _flags;

			private int _proximity;

			private int _pathOffset;

			private int _altPathOffset;

			private int _nodeOffset;

			private string _altPath;

			internal int Ttl;

			internal string Path;

			internal string Node;

			internal virtual int ReadWireFormat(byte[] buffer, int bufferIndex, int len)
			{
				int start = bufferIndex;
				_version = ReadInt2(buffer, bufferIndex);
				if (_version != 3 && _version != 1)
				{
					throw new RuntimeException("Version " + _version + " referral not supported. Please report this to jcifs at samba dot org."
						);
				}
				bufferIndex += 2;
				_size = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
				_serverType = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
				_flags = ReadInt2(buffer, bufferIndex);
				bufferIndex += 2;
				if (_version == 3)
				{
					_proximity = ReadInt2(buffer, bufferIndex);
					bufferIndex += 2;
					Ttl = ReadInt2(buffer, bufferIndex);
					bufferIndex += 2;
					_pathOffset = ReadInt2(buffer, bufferIndex);
					bufferIndex += 2;
					_altPathOffset = ReadInt2(buffer, bufferIndex);
					bufferIndex += 2;
					_nodeOffset = ReadInt2(buffer, bufferIndex);
					bufferIndex += 2;
					Path = _enclosing.ReadString(buffer, start + _pathOffset, len, (_enclosing.Flags2 & SmbConstants.Flags2Unicode) != 0);
					if (_nodeOffset > 0)
					{
						Node = _enclosing.ReadString(buffer, start + _nodeOffset, len, (_enclosing.Flags2 & SmbConstants.Flags2Unicode) != 0);
					}
				}
				else
				{
					if (_version == 1)
					{
						Node = _enclosing.ReadString(buffer, bufferIndex, len, (_enclosing
							.Flags2 & SmbConstants.Flags2Unicode) != 0);
					}
				}
				return _size;
			}

			public override string ToString()
			{
				return "Referral[" + "version=" + _version + ",size=" + _size 
					+ ",serverType=" + _serverType + ",flags=" + _flags + ",proximity=" + _proximity + ",ttl=" + Ttl + ",pathOffset=" + _pathOffset + ",altPathOffset="
					 + _altPathOffset + ",nodeOffset=" + _nodeOffset + ",path=" + Path 
					+ ",altPath=" + _altPath + ",node=" + Node + "]";
			}

			internal Referral(Trans2GetDfsReferralResponse enclosing)
			{
				this._enclosing = enclosing;
			}

			private readonly Trans2GetDfsReferralResponse _enclosing;
		}

		internal int PathConsumed;

		internal int NumReferrals;

		internal int flags;

		internal Referral[] Referrals;

		public Trans2GetDfsReferralResponse()
		{
			SubCommand = Smb.SmbComTransaction.Trans2GetDfsReferral;
		}

		internal override int WriteSetupWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
		}

		internal override int WriteParametersWireFormat(byte[] dst, int dstIndex)
		{
			return 0;
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
			int start = bufferIndex;
			PathConsumed = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
            if ((Flags2 & SmbConstants.Flags2Unicode) != 0)
			{
				PathConsumed /= 2;
			}
			NumReferrals = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			flags = ReadInt2(buffer, bufferIndex);
			bufferIndex += 4;
			Referrals = new Referral[NumReferrals];
			for (int ri = 0; ri < NumReferrals; ri++)
			{
				Referrals[ri] = new Referral(this);
				bufferIndex += Referrals[ri].ReadWireFormat(buffer, bufferIndex, len);
			}
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "Trans2GetDfsReferralResponse[" + base.ToString() + ",pathConsumed="
				 + PathConsumed + ",numReferrals=" + NumReferrals + ",flags=" + flags + "]";
		}
	}
}
