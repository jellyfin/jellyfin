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
using System.Net;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Netbios
{
	internal abstract class NameServicePacket
	{
		internal const int Query = 0;

		internal const int Wack = 7;

		internal const int FmtErr = 0x1;

		internal const int SrvErr = 0x2;

		internal const int ImpErr = 0x4;

	    internal const int RfsErr = 0x5;

		internal const int ActErr = 0x6;

		internal const int CftErr = 0x7;

		internal const int NbIn = 0x00200001;

		internal const int NbstatIn = 0x00210001;

		internal const int Nb = 0x0020;

		internal const int Nbstat = 0x0021;

		internal const int In = 0x0001;

		internal const int A = 0x0001;

		internal const int Ns = 0x0002;

		internal const int Null = 0x000a;

		internal const int HeaderLength = 12;

		internal const int OpcodeOffset = 2;

		internal const int QuestionOffset = 4;

		internal const int AnswerOffset = 6;

		internal const int AuthorityOffset = 8;

		internal const int AdditionalOffset = 10;

		// opcode
		// rcode
		// type/class
		// header field offsets
		internal static void WriteInt2(int val, byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = unchecked((byte)((val >> 8) & unchecked(0xFF)));
			dst[dstIndex] = unchecked((byte)(val & unchecked(0xFF)));
		}

		internal static void WriteInt4(int val, byte[] dst, int dstIndex)
		{
			dst[dstIndex++] = unchecked((byte)((val >> 24) & unchecked(0xFF)));
			dst[dstIndex++] = unchecked((byte)((val >> 16) & unchecked(0xFF)));
			dst[dstIndex++] = unchecked((byte)((val >> 8) & unchecked(0xFF)));
			dst[dstIndex] = unchecked((byte)(val & unchecked(0xFF)));
		}

		internal static int ReadInt2(byte[] src, int srcIndex)
		{
			return ((src[srcIndex] & unchecked(0xFF)) << 8) + (src[srcIndex + 1] & unchecked(
				0xFF));
		}

		internal static int ReadInt4(byte[] src, int srcIndex)
		{
			return ((src[srcIndex] & unchecked(0xFF)) << 24) + ((src[srcIndex + 1] & unchecked(
				0xFF)) << 16) + ((src[srcIndex + 2] & unchecked(0xFF)) << 8) + (src
				[srcIndex + 3] & unchecked(0xFF));
		}

		internal static int ReadNameTrnId(byte[] src, int srcIndex)
		{
			return ReadInt2(src, srcIndex);
		}

		internal int AddrIndex;

		internal NbtAddress[] AddrEntry;

		internal int NameTrnId;

		internal int OpCode;

		internal int ResultCode;

		internal int QuestionCount;

		internal int AnswerCount;

		internal int AuthorityCount;

		internal int AdditionalCount;

		internal bool Received;

		internal bool IsResponse;

		internal bool IsAuthAnswer;

		internal bool IsTruncated;

		internal bool IsRecurDesired;

		internal bool IsRecurAvailable;

		internal bool IsBroadcast;

		internal Name QuestionName;

		internal Name RecordName;

		internal int QuestionType;

		internal int QuestionClass;

		internal int RecordType;

		internal int RecordClass;

		internal int Ttl;

		internal int RDataLength;

		internal IPAddress Addr;

		public NameServicePacket()
		{
			IsRecurDesired = true;
			IsBroadcast = true;
			QuestionCount = 1;
			QuestionClass = In;
		}

		internal virtual int WriteWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dstIndex += WriteHeaderWireFormat(dst, dstIndex);
			dstIndex += WriteBodyWireFormat(dst, dstIndex);
			return dstIndex - start;
		}

		internal virtual int ReadWireFormat(byte[] src, int srcIndex)
		{
			int start = srcIndex;
			srcIndex += ReadHeaderWireFormat(src, srcIndex);
			srcIndex += ReadBodyWireFormat(src, srcIndex);
			return srcIndex - start;
		}

		internal virtual int WriteHeaderWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			WriteInt2(NameTrnId, dst, dstIndex);
			dst[dstIndex + OpcodeOffset] = unchecked((byte)((IsResponse ? unchecked(0x80) : unchecked(0x00)) + ((OpCode << 3) & unchecked(0x78)) + (IsAuthAnswer
				 ? unchecked(0x04) : unchecked(0x00)) + (IsTruncated ? unchecked(0x02) : unchecked(0x00)) + (IsRecurDesired ? unchecked(0x01)
				 : unchecked(0x00))));
			dst[dstIndex + OpcodeOffset + 1] = unchecked((byte)((IsRecurAvailable ? unchecked(
				0x80) : unchecked(0x00)) + (IsBroadcast ? unchecked(0x10) : 
				unchecked(0x00)) + (ResultCode & unchecked(0x0F))));
			WriteInt2(QuestionCount, dst, start + QuestionOffset);
			WriteInt2(AnswerCount, dst, start + AnswerOffset);
			WriteInt2(AuthorityCount, dst, start + AuthorityOffset);
			WriteInt2(AdditionalCount, dst, start + AdditionalOffset);
			return HeaderLength;
		}

		internal virtual int ReadHeaderWireFormat(byte[] src, int srcIndex)
		{
			NameTrnId = ReadInt2(src, srcIndex);
			IsResponse = ((src[srcIndex + OpcodeOffset] & unchecked(0x80)) == 0) ? false
				 : true;
			OpCode = (src[srcIndex + OpcodeOffset] & unchecked(0x78)) >> 3;
			IsAuthAnswer = ((src[srcIndex + OpcodeOffset] & unchecked(0x04)) == 0) ? 
				false : true;
			IsTruncated = ((src[srcIndex + OpcodeOffset] & unchecked(0x02)) == 0) ? false
				 : true;
			IsRecurDesired = ((src[srcIndex + OpcodeOffset] & unchecked(0x01)) == 0) ? 
				false : true;
			IsRecurAvailable = ((src[srcIndex + OpcodeOffset + 1] & unchecked(0x80)) 
				== 0) ? false : true;
			IsBroadcast = ((src[srcIndex + OpcodeOffset + 1] & unchecked(0x10)) == 0)
				 ? false : true;
			ResultCode = src[srcIndex + OpcodeOffset + 1] & unchecked(0x0F);
			QuestionCount = ReadInt2(src, srcIndex + QuestionOffset);
			AnswerCount = ReadInt2(src, srcIndex + AnswerOffset);
			AuthorityCount = ReadInt2(src, srcIndex + AuthorityOffset);
			AdditionalCount = ReadInt2(src, srcIndex + AdditionalOffset);
			return HeaderLength;
		}

		internal virtual int WriteQuestionSectionWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			dstIndex += QuestionName.WriteWireFormat(dst, dstIndex);
			WriteInt2(QuestionType, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(QuestionClass, dst, dstIndex);
			dstIndex += 2;
			return dstIndex - start;
		}

		internal virtual int ReadQuestionSectionWireFormat(byte[] src, int srcIndex)
		{
			int start = srcIndex;
			srcIndex += QuestionName.ReadWireFormat(src, srcIndex);
			QuestionType = ReadInt2(src, srcIndex);
			srcIndex += 2;
			QuestionClass = ReadInt2(src, srcIndex);
			srcIndex += 2;
			return srcIndex - start;
		}

		internal virtual int WriteResourceRecordWireFormat(byte[] dst, int dstIndex)
		{
			int start = dstIndex;
			if (RecordName == QuestionName)
			{
				dst[dstIndex++] = unchecked(unchecked(0xC0));
				// label string pointer to
				dst[dstIndex++] = unchecked(unchecked(0x0C));
			}
			else
			{
				// questionName (offset 12)
				dstIndex += RecordName.WriteWireFormat(dst, dstIndex);
			}
			WriteInt2(RecordType, dst, dstIndex);
			dstIndex += 2;
			WriteInt2(RecordClass, dst, dstIndex);
			dstIndex += 2;
			WriteInt4(Ttl, dst, dstIndex);
			dstIndex += 4;
			RDataLength = WriteRDataWireFormat(dst, dstIndex + 2);
			WriteInt2(RDataLength, dst, dstIndex);
			dstIndex += 2 + RDataLength;
			return dstIndex - start;
		}

		internal virtual int ReadResourceRecordWireFormat(byte[] src, int srcIndex)
		{
			int start = srcIndex;
			int end;
			if ((src[srcIndex] & unchecked(0xC0)) == unchecked(0xC0))
			{
				RecordName = QuestionName;
				// label string pointer to questionName
				srcIndex += 2;
			}
			else
			{
				srcIndex += RecordName.ReadWireFormat(src, srcIndex);
			}
			RecordType = ReadInt2(src, srcIndex);
			srcIndex += 2;
			RecordClass = ReadInt2(src, srcIndex);
			srcIndex += 2;
			Ttl = ReadInt4(src, srcIndex);
			srcIndex += 4;
			RDataLength = ReadInt2(src, srcIndex);
			srcIndex += 2;
			AddrEntry = new NbtAddress[RDataLength / 6];
			end = srcIndex + RDataLength;
			for (AddrIndex = 0; srcIndex < end; AddrIndex++)
			{
				srcIndex += ReadRDataWireFormat(src, srcIndex);
			}
			return srcIndex - start;
		}

		internal abstract int WriteBodyWireFormat(byte[] dst, int dstIndex);

		internal abstract int ReadBodyWireFormat(byte[] src, int srcIndex);

		internal abstract int WriteRDataWireFormat(byte[] dst, int dstIndex);

		internal abstract int ReadRDataWireFormat(byte[] src, int srcIndex);

		public override string ToString()
		{
			string opCodeString;
			string resultCodeString;
			string questionTypeString;
			string recordTypeString;

			switch (OpCode)
			{
				case Query:
				{
					opCodeString = "QUERY";
					break;
				}

				case Wack:
				{
					opCodeString = "WACK";
					break;
				}

				default:
				{
					opCodeString = Extensions.ToString(OpCode);
					break;
				}
			}
			switch (ResultCode)
			{
				case FmtErr:
				{
					resultCodeString = "FMT_ERR";
					break;
				}

				case SrvErr:
				{
					resultCodeString = "SRV_ERR";
					break;
				}

				case ImpErr:
				{
					resultCodeString = "IMP_ERR";
					break;
				}

				case RfsErr:
				{
					resultCodeString = "RFS_ERR";
					break;
				}

				case ActErr:
				{
					resultCodeString = "ACT_ERR";
					break;
				}

				case CftErr:
				{
					resultCodeString = "CFT_ERR";
					break;
				}

				default:
				{
					resultCodeString = "0x" + Hexdump.ToHexString(ResultCode, 1);
					break;
				}
			}
			switch (QuestionType)
			{
				case Nb:
				{
					questionTypeString = "NB";
					break;
				}

				case Nbstat:
				{
					questionTypeString = "NBSTAT";
					break;
				}

				default:
				{
					questionTypeString = "0x" + Hexdump.ToHexString(QuestionType, 4);
					break;
				}
			}
			switch (RecordType)
			{
				case A:
				{
					recordTypeString = "A";
					break;
				}

				case Ns:
				{
					recordTypeString = "NS";
					break;
				}

				case Null:
				{
					recordTypeString = "NULL";
					break;
				}

				case Nb:
				{
					recordTypeString = "NB";
					break;
				}

				case Nbstat:
				{
					recordTypeString = "NBSTAT";
					break;
				}

				default:
				{
					recordTypeString = "0x" + Hexdump.ToHexString(RecordType, 4);
					break;
				}
			}
			return "nameTrnId=" + NameTrnId + ",isResponse=" + IsResponse + ",opCode="
				 + opCodeString + ",isAuthAnswer=" + IsAuthAnswer + ",isTruncated=" + IsTruncated
				 + ",isRecurAvailable=" + IsRecurAvailable + ",isRecurDesired=" + IsRecurDesired
				 + ",isBroadcast=" + IsBroadcast + ",resultCode=" + ResultCode + ",questionCount="
				 + QuestionCount + ",answerCount=" + AnswerCount + ",authorityCount=" + AuthorityCount
				 + ",additionalCount=" + AdditionalCount + ",questionName=" + QuestionName + ",questionType="
				 + questionTypeString + ",questionClass=" + (QuestionClass == In ? "IN" : "0x" +
				 Hexdump.ToHexString(QuestionClass, 4)) + ",recordName=" + RecordName + ",recordType="
				 + recordTypeString + ",recordClass=" + (RecordClass == In ? "IN" : "0x" + Hexdump
				.ToHexString(RecordClass, 4)) + ",ttl=" + Ttl + ",rDataLength=" + RDataLength;
		}
	}
}
