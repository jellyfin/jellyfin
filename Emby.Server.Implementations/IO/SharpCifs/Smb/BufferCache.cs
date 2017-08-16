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
	public class BufferCache
	{
		private static readonly int MaxBuffers = Config.GetInt("jcifs.smb.maxBuffers", 16
			);

		internal static object[] Cache = new object[MaxBuffers];

		private static int _freeBuffers;

		public static byte[] GetBuffer()
		{
			lock (Cache)
			{
				byte[] buf;
				if (_freeBuffers > 0)
				{
					for (int i = 0; i < MaxBuffers; i++)
					{
						if (Cache[i] != null)
						{
							buf = (byte[])Cache[i];
							Cache[i] = null;
							_freeBuffers--;
							return buf;
						}
					}
				}
				buf = new byte[SmbComTransaction.TransactionBufSize];
				return buf;
			}
		}

		internal static void GetBuffers(SmbComTransaction req, SmbComTransactionResponse 
			rsp)
		{
			lock (Cache)
			{
				req.TxnBuf = GetBuffer();
				rsp.TxnBuf = GetBuffer();
			}
		}

		public static void ReleaseBuffer(byte[] buf)
		{
			lock (Cache)
			{
				if (_freeBuffers < MaxBuffers)
				{
					for (int i = 0; i < MaxBuffers; i++)
					{
						if (Cache[i] == null)
						{
							Cache[i] = buf;
							_freeBuffers++;
							return;
						}
					}
				}
			}
		}
	}
}
