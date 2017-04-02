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
using System.IO;

namespace SharpCifs.Netbios
{

	public class NbtException : IOException
	{
		public const int Success = 0;

		public const int ErrNamSrvc = unchecked(0x01);

		public const int ErrSsnSrvc = unchecked(0x02);

		public const int FmtErr = unchecked(0x1);

		public const int SrvErr = unchecked(0x2);

		public const int ImpErr = unchecked(0x4);

		public const int RfsErr = unchecked(0x5);

		public const int ActErr = unchecked(0x6);

		public const int CftErr = unchecked(0x7);

		public const int ConnectionRefused = -1;

		public const int NotListeningCalled = unchecked(0x80);

		public const int NotListeningCalling = unchecked(0x81);

		public const int CalledNotPresent = unchecked(0x82);

		public const int NoResources = unchecked(0x83);

		public const int Unspecified = unchecked(0x8F);

		public int ErrorClass;

		public int ErrorCode;

		// error classes
		// name service error codes
		// session service error codes
		public static string GetErrorString(int errorClass, int errorCode)
		{
			string result = string.Empty;
			switch (errorClass)
			{
				case Success:
				{
					result += "SUCCESS";
					break;
				}

				case ErrNamSrvc:
				{
					result += "ERR_NAM_SRVC/";
					switch (errorCode)
					{
						case FmtErr:
						{
							result += "FMT_ERR: Format Error";
							goto default;
						}

						default:
						{
							result += "Unknown error code: " + errorCode;
							break;
						}
					}
					break;
				}

				case ErrSsnSrvc:
				{
					result += "ERR_SSN_SRVC/";
					switch (errorCode)
					{
						case ConnectionRefused:
						{
							result += "Connection refused";
							break;
						}

						case NotListeningCalled:
						{
							result += "Not listening on called name";
							break;
						}

						case NotListeningCalling:
						{
							result += "Not listening for calling name";
							break;
						}

						case CalledNotPresent:
						{
							result += "Called name not present";
							break;
						}

						case NoResources:
						{
							result += "Called name present, but insufficient resources";
							break;
						}

						case Unspecified:
						{
							result += "Unspecified error";
							break;
						}

						default:
						{
							result += "Unknown error code: " + errorCode;
							break;
						}
					}
					break;
				}

				default:
				{
					result += "unknown error class: " + errorClass;
					break;
				}
			}
			return result;
		}

		public NbtException(int errorClass, int errorCode) : base(GetErrorString(errorClass
			, errorCode))
		{
			this.ErrorClass = errorClass;
			this.ErrorCode = errorCode;
		}

		public override string ToString()
		{
			return "errorClass=" + ErrorClass + ",errorCode=" + ErrorCode + ",errorString="
				 + GetErrorString(ErrorClass, ErrorCode);
		}
	}
}
