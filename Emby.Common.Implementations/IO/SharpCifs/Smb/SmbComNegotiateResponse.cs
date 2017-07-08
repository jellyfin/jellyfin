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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	internal class SmbComNegotiateResponse : ServerMessageBlock
	{
		internal int DialectIndex;

		internal SmbTransport.ServerData Server;

		internal SmbComNegotiateResponse(SmbTransport.ServerData server)
		{
			this.Server = server;
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
			DialectIndex = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			if (DialectIndex > 10)
			{
				return bufferIndex - start;
			}
			Server.SecurityMode = buffer[bufferIndex++] & unchecked(0xFF);
			Server.Security = Server.SecurityMode & unchecked(0x01);
			Server.EncryptedPasswords = (Server.SecurityMode & unchecked(0x02)) == unchecked(
				0x02);
			Server.SignaturesEnabled = (Server.SecurityMode & unchecked(0x04)) == unchecked(
				0x04);
			Server.SignaturesRequired = (Server.SecurityMode & unchecked(0x08)) == unchecked(
				0x08);
			Server.MaxMpxCount = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			Server.MaxNumberVcs = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			Server.MaxBufferSize = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			Server.MaxRawSize = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			Server.SessionKey = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			Server.Capabilities = ReadInt4(buffer, bufferIndex);
			bufferIndex += 4;
			Server.ServerTime = ReadTime(buffer, bufferIndex);
			bufferIndex += 8;
			Server.ServerTimeZone = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			Server.EncryptionKeyLength = buffer[bufferIndex++] & unchecked(0xFF);
			return bufferIndex - start;
		}

		internal override int ReadBytesWireFormat(byte[] buffer, int bufferIndex)
		{
			int start = bufferIndex;
			if ((Server.Capabilities & SmbConstants.CapExtendedSecurity) == 0)
			{
				Server.EncryptionKey = new byte[Server.EncryptionKeyLength];
				Array.Copy(buffer, bufferIndex, Server.EncryptionKey, 0, Server.EncryptionKeyLength
					);
				bufferIndex += Server.EncryptionKeyLength;
				if (ByteCount > Server.EncryptionKeyLength)
				{
					int len = 0;
					// TODO: we can use new string routine here
					try
					{
                        if ((Flags2 & SmbConstants.Flags2Unicode) == SmbConstants.Flags2Unicode)
						{
							while (buffer[bufferIndex + len] != unchecked(unchecked(0x00)) || buffer
								[bufferIndex + len + 1] != unchecked(unchecked(0x00)))
							{
								len += 2;
								if (len > 256)
								{
									throw new RuntimeException("zero termination not found");
								}
							}
							Server.OemDomainName = Runtime.GetStringForBytes(buffer, bufferIndex, len
                                , SmbConstants.UniEncoding);
						}
						else
						{
							while (buffer[bufferIndex + len] != unchecked(unchecked(0x00)))
							{
								len++;
								if (len > 256)
								{
									throw new RuntimeException("zero termination not found");
								}
							}
							Server.OemDomainName = Runtime.GetStringForBytes(buffer, bufferIndex, len
                                , SmbConstants.OemEncoding);
						}
					}
					catch (UnsupportedEncodingException uee)
					{
						if (Log.Level > 1)
						{
							Runtime.PrintStackTrace(uee, Log);
						}
					}
					bufferIndex += len;
				}
				else
				{
				    Server.OemDomainName = "";
				}
			}
			else
			{
				Server.Guid = new byte[16];
				Array.Copy(buffer, bufferIndex, Server.Guid, 0, 16);
				Server.OemDomainName = "";
			}
			// ignore SPNEGO token for now ...
			return bufferIndex - start;
		}

		public override string ToString()
		{
			return "SmbComNegotiateResponse[" + base.ToString() + ",wordCount=" + 
				WordCount + ",dialectIndex=" + DialectIndex + ",securityMode=0x" + Hexdump.ToHexString
                (Server.SecurityMode, 1) + ",security=" + (Server.Security == SmbConstants.SecurityShare ? "share"
				 : "user") + ",encryptedPasswords=" + Server.EncryptedPasswords + ",maxMpxCount="
				 + Server.MaxMpxCount + ",maxNumberVcs=" + Server.MaxNumberVcs + ",maxBufferSize="
				 + Server.MaxBufferSize + ",maxRawSize=" + Server.MaxRawSize + ",sessionKey=0x" 
				+ Hexdump.ToHexString(Server.SessionKey, 8) + ",capabilities=0x" + Hexdump.ToHexString
				(Server.Capabilities, 8) + ",serverTime=" + Extensions.CreateDate(Server
				.ServerTime) + ",serverTimeZone=" + Server.ServerTimeZone + ",encryptionKeyLength="
				 + Server.EncryptionKeyLength + ",byteCount=" + ByteCount + ",oemDomainName=" + 
				Server.OemDomainName + "]";
		}
	}
}
