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
using System.IO;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Smb
{
	/// <summary>
	/// There are hundreds of error codes that may be returned by a CIFS
	/// server.
	/// </summary>
	/// <remarks>
	/// There are hundreds of error codes that may be returned by a CIFS
	/// server. Rather than represent each with it's own <code>Exception</code>
	/// class, this class represents all of them. For many of the popular
	/// error codes, constants and text messages like "The device is not ready"
	/// are provided.
	/// <p>
	/// The jCIFS client maps DOS error codes to NTSTATUS codes. This means that
	/// the user may recieve a different error from a legacy server than that of
	/// a newer varient such as Windows NT and above. If you should encounter
	/// such a case, please report it to jcifs at samba dot org and we will
	/// change the mapping.
	/// </remarks>
	
	public class SmbException : IOException
	{
       
        internal static string GetMessageByCode(int errcode)
		{
			if (errcode == 0)
			{
				return "NT_STATUS_SUCCESS";
			}
			if ((errcode & unchecked((int)(0xC0000000))) == unchecked((int)(0xC0000000)))
			{
				int min = 1;
				int max = NtStatus.NtStatusCodes.Length - 1;
				while (max >= min)
				{
					int mid = (min + max) / 2;
                    if (errcode > NtStatus.NtStatusCodes[mid])
					{
						min = mid + 1;
					}
					else
					{
                        if (errcode < NtStatus.NtStatusCodes[mid])
						{
							max = mid - 1;
						}
						else
						{
                            return NtStatus.NtStatusMessages[mid];
						}
					}
				}
			}
			else
			{
				int min = 0;
				int max = DosError.DosErrorCodes.Length - 1;
				while (max >= min)
				{
					int mid = (min + max) / 2;
                    if (errcode > DosError.DosErrorCodes[mid][0])
					{
						min = mid + 1;
					}
					else
					{
                        if (errcode < DosError.DosErrorCodes[mid][0])
						{
							max = mid - 1;
						}
						else
						{
                            return DosError.DosErrorMessages[mid];
						}
					}
				}
			}
			return "0x" + Hexdump.ToHexString(errcode, 8);
		}

		internal static int GetStatusByCode(int errcode)
		{
			if ((errcode & unchecked((int)(0xC0000000))) != 0)
			{
				return errcode;
			}
		    int min = 0;
		    int max = DosError.DosErrorCodes.Length - 1;
		    while (max >= min)
		    {
		        int mid = (min + max) / 2;
		        if (errcode > DosError.DosErrorCodes[mid][0])
		        {
		            min = mid + 1;
		        }
		        else
		        {
		            if (errcode < DosError.DosErrorCodes[mid][0])
		            {
		                max = mid - 1;
		            }
		            else
		            {
		                return DosError.DosErrorCodes[mid][1];
		            }
		        }
		    }
		    return NtStatus.NtStatusUnsuccessful;
		}

		internal static string GetMessageByWinerrCode(int errcode)
		{
			int min = 0;
			int max = WinError.WinerrCodes.Length - 1;
			while (max >= min)
			{
				int mid = (min + max) / 2;
                if (errcode > WinError.WinerrCodes[mid])
				{
					min = mid + 1;
				}
				else
				{
                    if (errcode < WinError.WinerrCodes[mid])
					{
						max = mid - 1;
					}
					else
					{
                        return WinError.WinerrMessages[mid];
					}
				}
			}
			return errcode + string.Empty;
		}

		private int _status;

		private Exception _rootCause;

		public SmbException()
		{
		}

		internal SmbException(int errcode, Exception rootCause) : base(GetMessageByCode(errcode
			))
		{
			_status = GetStatusByCode(errcode);
			this._rootCause = rootCause;
		}

		public SmbException(string msg) : base(msg)
		{
            _status = NtStatus.NtStatusUnsuccessful;
		}

		public SmbException(string msg, Exception rootCause) : base(msg)
		{
			this._rootCause = rootCause;
            _status = NtStatus.NtStatusUnsuccessful;
		}

		public SmbException(int errcode, bool winerr) : base(winerr ? GetMessageByWinerrCode
			(errcode) : GetMessageByCode(errcode))
		{
			_status = winerr ? errcode : GetStatusByCode(errcode);
		}

		public virtual int GetNtStatus()
		{
			return _status;
		}

		public virtual Exception GetRootCause()
		{
			return _rootCause;
		}

		public override string ToString()
		{
		    if (_rootCause != null)
			{
				StringWriter sw = new StringWriter();
				PrintWriter pw = new PrintWriter(sw);
				Runtime.PrintStackTrace(_rootCause, pw);
				return base.ToString() + "\n" + sw;
			}
		    return base.ToString();
		}
	}
}
