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
using SharpCifs.Util.Transport;

namespace SharpCifs.Smb
{
	public abstract class ServerMessageBlock: Response
	{
		internal static LogStream Log = LogStream.GetInstance();

        internal static long Ticks1601 = new DateTime(1601, 1, 1).Ticks;

		internal static readonly byte[] Header = { 0xFF, (byte)('S'), (byte)('M'), 
			(byte)('B'), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

		internal static void WriteInt2(long val, byte[] dst, int dstIndex)
		{
			dst[dstIndex] = unchecked((byte)(val));
			dst[++dstIndex] = unchecked((byte)(val >> 8));
		}

		internal static void WriteInt4(long val, byte[] dst, int dstIndex)
		{
			dst[dstIndex] = unchecked((byte)(val));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >> 8));
		}

		internal static int ReadInt2(byte[] src, int srcIndex)
		{
			return unchecked(src[srcIndex] & 0xFF) + ((src[srcIndex + 1] & 0xFF) << 8);
		}

		internal static int ReadInt4(byte[] src, int srcIndex)
		{
			return unchecked(src[srcIndex] & 0xFF) + ((src[srcIndex + 1] & 0xFF) << 8) + ((src[srcIndex + 2] 
                & 0xFF) << 16) + ((src[srcIndex + 3] & 0xFF) << 24);
		}

		internal static long ReadInt8(byte[] src, int srcIndex)
		{
			return unchecked(ReadInt4(src, srcIndex) & unchecked(0xFFFFFFFFL)) + unchecked((long)(ReadInt4
				(src, srcIndex + 4)) << 32);
		}

		internal static void WriteInt8(long val, byte[] dst, int dstIndex)
		{
			dst[dstIndex] = unchecked((byte)(val));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >>= 8));
			dst[++dstIndex] = unchecked((byte)(val >> 8));
		}

		internal static long ReadTime(byte[] src, int srcIndex)
		{
			int low = ReadInt4(src, srcIndex);
			int hi = ReadInt4(src, srcIndex + 4);
			long t = ((long)hi << (int)32L) | (low & unchecked((long)(0xFFFFFFFFL)));
			t = (t / 10000L - SmbConstants.MillisecondsBetween1970And1601);
			return t;
		}

		internal static void WriteTime(long t, byte[] dst, int dstIndex)
		{
			if (t != 0L)
			{
                t = (t + SmbConstants.MillisecondsBetween1970And1601) * 10000L;
			}
			WriteInt8(t, dst, dstIndex);
		}

		internal static long ReadUTime(byte[] buffer, int bufferIndex)
		{
			return ReadInt4(buffer, bufferIndex) * 1000L;
		}

		internal static void WriteUTime(long t, byte[] dst, int dstIndex)
		{
			if (t == 0L || t == unchecked((long)(0xFFFFFFFFFFFFFFFFL)))
			{
				WriteInt4(unchecked((int)(0xFFFFFFFF)), dst, dstIndex);
				return;
			}
			// t isn't in DST either
			WriteInt4((int)(t / 1000L), dst, dstIndex);
		}

		internal const byte SmbComCreateDirectory = 0x00;

		internal const byte SmbComDeleteDirectory = 0x01;

		internal const byte SmbComClose = 0x04;

		internal const byte SmbComDelete = 0x06;

		internal const byte SmbComRename = 0x07;

		internal const byte SmbComQueryInformation = 0x08;

		internal const byte SmbComWrite = 0x0B;

		internal const byte SmbComCheckDirectory = 0x10;

		internal const byte SmbComTransaction = 0x25;

		internal const byte SmbComTransactionSecondary = 0x26;

		internal const byte SmbComMove = 0x2A;

		internal const byte SmbComEcho = 0x2B;

		internal const byte SmbComOpenAndx = 0x2D;

		internal const byte SmbComReadAndx = 0x2E;

		internal const byte SmbComWriteAndx = 0x2F;

	    internal const byte SmbComTransaction2 = 0x32;			

		internal const byte SmbComFindClose2 = 0x34;

		internal const byte SmbComTreeDisconnect = 0x71;

		internal const byte SmbComNegotiate = 0x72;

		internal const byte SmbComSessionSetupAndx = 0x73;

		internal const byte SmbComLogoffAndx = 0x74;

		internal const byte SmbComTreeConnectAndx = 0x75;

		internal const byte SmbComNtTransact = 0xA0;

		internal const byte SmbComNtTransactSecondary = 0xA1;

		internal const byte SmbComNtCreateAndx = 0xA2;

		internal byte Command;

		internal byte Flags;

		internal int HeaderStart;

		internal int Length;

		internal int BatchLevel;

		internal int ErrorCode;

		internal int Flags2;

		internal int Tid;

		internal int Pid;

		internal int Uid;

		internal int Mid;

		internal int WordCount;

		internal int ByteCount;

		internal bool UseUnicode;

		internal bool Received;

		internal bool ExtendedSecurity;

		internal long ResponseTimeout = 1;

		internal int SignSeq;

		internal bool VerifyFailed;

		internal NtlmPasswordAuthentication Auth = null;

		internal string Path;

		internal SigningDigest Digest;

		internal ServerMessageBlock Response;

		public ServerMessageBlock()
		{
			Flags = unchecked((byte)(SmbConstants.FlagsPathNamesCaseless | SmbConstants.FlagsPathNamesCanonicalized
				));
			Pid = SmbConstants.Pid;
			BatchLevel = 0;
		}

		internal virtual void Reset()
		{
            Flags = unchecked((byte)(SmbConstants.FlagsPathNamesCaseless | SmbConstants.FlagsPathNamesCanonicalized
				));
			Flags2 = 0;
			ErrorCode = 0;
			Received = false;
			Digest = null;
		}

		internal virtual int WriteString(string str, byte[] dst, int dstIndex)
		{
			return WriteString(str, dst, dstIndex, UseUnicode);
		}

		internal virtual int WriteString(string str, byte[] dst, int dstIndex, bool useUnicode
			)
		{
			int start = dstIndex;
			try
			{
				if (useUnicode)
				{
					// Unicode requires word alignment
					if (((dstIndex - HeaderStart) % 2) != 0)
					{
						dst[dstIndex++] = (byte)('\0');
					}
                    Array.Copy(Runtime.GetBytesForString(str, SmbConstants.UniEncoding), 0, dst, dstIndex
						, str.Length * 2);
					dstIndex += str.Length * 2;
					dst[dstIndex++] = (byte)('\0');
					dst[dstIndex++] = (byte)('\0');
				}
				else
				{
                    byte[] b = Runtime.GetBytesForString(str, SmbConstants.OemEncoding);
					Array.Copy(b, 0, dst, dstIndex, b.Length);
					dstIndex += b.Length;
					dst[dstIndex++] = (byte)('\0');
				}
			}
			catch (UnsupportedEncodingException uee)
			{
				if (Log.Level > 1)
				{
					Runtime.PrintStackTrace(uee, Log);
				}
			}
			return dstIndex - start;
		}

		internal virtual string ReadString(byte[] src, int srcIndex)
		{
			return ReadString(src, srcIndex, 256, UseUnicode);
		}

		internal virtual string ReadString(byte[] src, int srcIndex, int maxLen, bool useUnicode
			)
		{
			int len = 0;
			string str = null;
			try
			{
				if (useUnicode)
				{
					// Unicode requires word alignment
					if (((srcIndex - HeaderStart) % 2) != 0)
					{
						srcIndex++;
					}
					while (src[srcIndex + len] != 0x00 || src[srcIndex
						 + len + 1] != 0x00)
					{
						len += 2;
						if (len > maxLen)
						{
							if (Log.Level > 0)
							{
								Hexdump.ToHexdump(Console.Error, src, srcIndex, maxLen < 128 ? maxLen + 8 : 
									128);
							}
							throw new RuntimeException("zero termination not found");
						}
					}
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.UniEncoding);
				}
				else
				{
					while (src[srcIndex + len] != 0x00)
					{
						len++;
						if (len > maxLen)
						{
							if (Log.Level > 0)
							{
								Hexdump.ToHexdump(Console.Error, src, srcIndex, maxLen < 128 ? maxLen + 8 : 
									128);
							}
							throw new RuntimeException("zero termination not found");
						}
					}
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.OemEncoding);
				}
			}
			catch (UnsupportedEncodingException uee)
			{
				if (Log.Level > 1)
				{
					Runtime.PrintStackTrace(uee, Log);
				}
			}
			return str;
		}

		internal virtual string ReadString(byte[] src, int srcIndex, int srcEnd, int maxLen
			, bool useUnicode)
		{
			int len = 0;
			string str = null;
			try
			{
				if (useUnicode)
				{
					// Unicode requires word alignment
					if (((srcIndex - HeaderStart) % 2) != 0)
					{
						srcIndex++;
					}
					for (len = 0; (srcIndex + len + 1) < srcEnd; len += 2)
					{
						if (src[srcIndex + len] == 0x00 && src[srcIndex
							 + len + 1] == 0x00)
						{
							break;
						}
						if (len > maxLen)
						{
							if (Log.Level > 0)
							{
								Hexdump.ToHexdump(Console.Error, src, srcIndex, maxLen < 128 ? maxLen + 8 : 
									128);
							}
							throw new RuntimeException("zero termination not found");
						}
					}
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.UniEncoding);
				}
				else
				{
					for (len = 0; srcIndex < srcEnd; len++)
					{
						if (src[srcIndex + len] == 0x00)
						{
							break;
						}
						if (len > maxLen)
						{
							if (Log.Level > 0)
							{
								Hexdump.ToHexdump(Console.Error, src, srcIndex, maxLen < 128 ? maxLen + 8 : 
									128);
							}
							throw new RuntimeException("zero termination not found");
						}
					}
                    str = Runtime.GetStringForBytes(src, srcIndex, len, SmbConstants.OemEncoding);
				}
			}
			catch (UnsupportedEncodingException uee)
			{
				if (Log.Level > 1)
				{
					Runtime.PrintStackTrace(uee, Log);
				}
			}
			return str;
		}

		internal virtual int StringWireLength(string str, int offset)
		{
			int len = str.Length + 1;
			if (UseUnicode)
			{
				len = str.Length * 2 + 2;
				len = (offset % 2) != 0 ? len + 1 : len;
			}
			return len;
		}

		internal virtual int ReadStringLength(byte[] src, int srcIndex, int max)
		{
			int len = 0;
			while (src[srcIndex + len] != 0x00)
			{
				if (len++ > max)
				{
					throw new RuntimeException("zero termination not found: " + this);
				}
			}
			return len;
		}

		internal virtual int Encode(byte[] dst, int dstIndex)
		{
			int start = HeaderStart = dstIndex;
			dstIndex += WriteHeaderWireFormat(dst, dstIndex);
			WordCount = WriteParameterWordsWireFormat(dst, dstIndex + 1);
			dst[dstIndex++] = unchecked((byte)((WordCount / 2) & 0xFF));
			dstIndex += WordCount;
			WordCount /= 2;
			ByteCount = WriteBytesWireFormat(dst, dstIndex + 2);
			dst[dstIndex++] = unchecked((byte)(ByteCount & 0xFF));
			dst[dstIndex++] = unchecked((byte)((ByteCount >> 8) & 0xFF));
			dstIndex += ByteCount;
			Length = dstIndex - start;
			if (Digest != null)
			{
				Digest.Sign(dst, HeaderStart, Length, this, Response);
			}
			return Length;
		}

		internal virtual int Decode(byte[] buffer, int bufferIndex)
		{
			int start = HeaderStart = bufferIndex;
			bufferIndex += ReadHeaderWireFormat(buffer, bufferIndex);
			WordCount = buffer[bufferIndex++];
			if (WordCount != 0)
			{
				int n;
				if ((n = ReadParameterWordsWireFormat(buffer, bufferIndex)) != WordCount * 2)
				{
					if (Log.Level >= 5)
					{
						Log.WriteLine("wordCount * 2=" + (WordCount * 2) + " but readParameterWordsWireFormat returned "
							 + n);
					}
				}
				bufferIndex += WordCount * 2;
			}
			ByteCount = ReadInt2(buffer, bufferIndex);
			bufferIndex += 2;
			if (ByteCount != 0)
			{
				int n;
				if ((n = ReadBytesWireFormat(buffer, bufferIndex)) != ByteCount)
				{
					if (Log.Level >= 5)
					{
						Log.WriteLine("byteCount=" + ByteCount + " but readBytesWireFormat returned " + n
							);
					}
				}
				// Don't think we can rely on n being correct here. Must use byteCount.
				// Last paragraph of section 3.13.3 eludes to this.
				bufferIndex += ByteCount;
			}
			Length = bufferIndex - start;
			return Length;
		}

		internal virtual int WriteHeaderWireFormat(byte[] dst, int dstIndex)
		{
			Array.Copy(Header, 0, dst, dstIndex, Header.Length);
            dst[dstIndex + SmbConstants.CmdOffset] = Command;
            dst[dstIndex + SmbConstants.FlagsOffset] = Flags;
            WriteInt2(Flags2, dst, dstIndex + SmbConstants.FlagsOffset + 1);
            dstIndex += SmbConstants.TidOffset;
			WriteInt2(Tid, dst, dstIndex);
			WriteInt2(Pid, dst, dstIndex + 2);
			WriteInt2(Uid, dst, dstIndex + 4);
			WriteInt2(Mid, dst, dstIndex + 6);
            return SmbConstants.HeaderLength;
		}

		internal virtual int ReadHeaderWireFormat(byte[] buffer, int bufferIndex)
		{
            Command = buffer[bufferIndex + SmbConstants.CmdOffset];
            ErrorCode = ReadInt4(buffer, bufferIndex + SmbConstants.ErrorCodeOffset);
            Flags = buffer[bufferIndex + SmbConstants.FlagsOffset];
            Flags2 = ReadInt2(buffer, bufferIndex + SmbConstants.FlagsOffset + 1);
            Tid = ReadInt2(buffer, bufferIndex + SmbConstants.TidOffset);
            Pid = ReadInt2(buffer, bufferIndex + SmbConstants.TidOffset + 2);
            Uid = ReadInt2(buffer, bufferIndex + SmbConstants.TidOffset + 4);
            Mid = ReadInt2(buffer, bufferIndex + SmbConstants.TidOffset + 6);
            return SmbConstants.HeaderLength;
		}

		internal virtual bool IsResponse()
		{
            return (Flags & SmbConstants.FlagsResponse) == SmbConstants.FlagsResponse;
		}

		internal abstract int WriteParameterWordsWireFormat(byte[] dst, int dstIndex);

		internal abstract int WriteBytesWireFormat(byte[] dst, int dstIndex);

		internal abstract int ReadParameterWordsWireFormat(byte[] buffer, int bufferIndex
			);

		internal abstract int ReadBytesWireFormat(byte[] buffer, int bufferIndex);

		public override int GetHashCode()
		{
			return Mid;
		}

		public override bool Equals(object obj)
		{
			return obj is ServerMessageBlock && ((ServerMessageBlock)obj)
				.Mid == Mid;
		}

		public override string ToString()
		{
			string c;
			switch (Command)
			{
				case SmbComNegotiate:
				{
					c = "SMB_COM_NEGOTIATE";
					break;
				}

				case SmbComSessionSetupAndx:
				{
					c = "SMB_COM_SESSION_SETUP_ANDX";
					break;
				}

				case SmbComTreeConnectAndx:
				{
					c = "SMB_COM_TREE_CONNECT_ANDX";
					break;
				}

				case SmbComQueryInformation:
				{
					c = "SMB_COM_QUERY_INFORMATION";
					break;
				}

				case SmbComCheckDirectory:
				{
					c = "SMB_COM_CHECK_DIRECTORY";
					break;
				}

				case SmbComTransaction:
				{
					c = "SMB_COM_TRANSACTION";
					break;
				}

				case SmbComTransaction2:
				{
					c = "SMB_COM_TRANSACTION2";
					break;
				}

				case SmbComTransactionSecondary:
				{
					c = "SMB_COM_TRANSACTION_SECONDARY";
					break;
				}

				case SmbComFindClose2:
				{
					c = "SMB_COM_FIND_CLOSE2";
					break;
				}

				case SmbComTreeDisconnect:
				{
					c = "SMB_COM_TREE_DISCONNECT";
					break;
				}

				case SmbComLogoffAndx:
				{
					c = "SMB_COM_LOGOFF_ANDX";
					break;
				}

				case SmbComEcho:
				{
					c = "SMB_COM_ECHO";
					break;
				}

				case SmbComMove:
				{
					c = "SMB_COM_MOVE";
					break;
				}

				case SmbComRename:
				{
					c = "SMB_COM_RENAME";
					break;
				}

				case SmbComDelete:
				{
					c = "SMB_COM_DELETE";
					break;
				}

				case SmbComDeleteDirectory:
				{
					c = "SMB_COM_DELETE_DIRECTORY";
					break;
				}

				case SmbComNtCreateAndx:
				{
					c = "SMB_COM_NT_CREATE_ANDX";
					break;
				}

				case SmbComOpenAndx:
				{
					c = "SMB_COM_OPEN_ANDX";
					break;
				}

				case SmbComReadAndx:
				{
					c = "SMB_COM_READ_ANDX";
					break;
				}

				case SmbComClose:
				{
					c = "SMB_COM_CLOSE";
					break;
				}

				case SmbComWriteAndx:
				{
					c = "SMB_COM_WRITE_ANDX";
					break;
				}

				case SmbComCreateDirectory:
				{
					c = "SMB_COM_CREATE_DIRECTORY";
					break;
				}

				case SmbComNtTransact:
				{
					c = "SMB_COM_NT_TRANSACT";
					break;
				}

				case SmbComNtTransactSecondary:
				{
					c = "SMB_COM_NT_TRANSACT_SECONDARY";
					break;
				}

				default:
				{
					c = "UNKNOWN";
					break;
				}
			}
			string str = ErrorCode == 0 ? "0" : SmbException.GetMessageByCode(ErrorCode);
			return "command=" + c + ",received=" + Received + ",errorCode=" + str 
				+ ",flags=0x" + Hexdump.ToHexString(Flags & 0xFF, 4) + ",flags2=0x"
				 + Hexdump.ToHexString(Flags2, 4) + ",signSeq=" + SignSeq + ",tid=" + Tid + ",pid="
				 + Pid + ",uid=" + Uid + ",mid=" + Mid + ",wordCount=" + WordCount + ",byteCount="
				 + ByteCount;
		}
	}
}
