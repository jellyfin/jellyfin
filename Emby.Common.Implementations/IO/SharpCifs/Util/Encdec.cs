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
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Util
{
	public class Encdec
	{
		public const long MillisecondsBetween1970And1601 = 11644473600000L;

		public const long SecBetweeen1904And1970 = 2082844800L;

		public const int Time1970Sec32Be = 1;

		public const int Time1970Sec32Le = 2;

		public const int Time1904Sec32Be = 3;

		public const int Time1904Sec32Le = 4;

		public const int Time1601Nanos64Le = 5;

		public const int Time1601Nanos64Be = 6;

		public const int Time1970Millis64Be = 7;

		public const int Time1970Millis64Le = 8;

		public static int Enc_uint16be(short s, byte[] dst, int di)
		{
			dst[di++] = unchecked((byte)((s >> 8) & unchecked(0xFF)));
			dst[di] = unchecked((byte)(s & unchecked(0xFF)));
			return 2;
		}

		public static int Enc_uint32be(int i, byte[] dst, int di)
		{
			dst[di++] = unchecked((byte)((i >> 24) & unchecked(0xFF)));
			dst[di++] = unchecked((byte)((i >> 16) & unchecked(0xFF)));
			dst[di++] = unchecked((byte)((i >> 8) & unchecked(0xFF)));
			dst[di] = unchecked((byte)(i & unchecked(0xFF)));
			return 4;
		}

		public static int Enc_uint16le(short s, byte[] dst, int di)
		{
			dst[di++] = unchecked((byte)(s & unchecked(0xFF)));
			dst[di] = unchecked((byte)((s >> 8) & unchecked(0xFF)));
			return 2;
		}

		public static int Enc_uint32le(int i, byte[] dst, int di)
		{
			dst[di++] = unchecked((byte)(i & unchecked(0xFF)));
			dst[di++] = unchecked((byte)((i >> 8) & unchecked(0xFF)));
			dst[di++] = unchecked((byte)((i >> 16) & unchecked(0xFF)));
			dst[di] = unchecked((byte)((i >> 24) & unchecked(0xFF)));
			return 4;
		}

		public static short Dec_uint16be(byte[] src, int si)
		{
			return (short)(((src[si] & unchecked(0xFF)) << 8) | (src[si + 1] & unchecked(
				0xFF)));
		}

		public static int Dec_uint32be(byte[] src, int si)
		{
			return ((src[si] & unchecked(0xFF)) << 24) | ((src[si + 1] & unchecked(0xFF)) << 16) | ((src[si + 2] & unchecked(0xFF)) << 8) | (src[si + 3] 
				& unchecked(0xFF));
		}

		public static short Dec_uint16le(byte[] src, int si)
		{
			return (short)((src[si] & unchecked(0xFF)) | ((src[si + 1] & unchecked(0xFF)) << 8));
		}

		public static int Dec_uint32le(byte[] src, int si)
		{
			return (src[si] & unchecked(0xFF)) | ((src[si + 1] & unchecked(0xFF
				)) << 8) | ((src[si + 2] & unchecked(0xFF)) << 16) | ((src[si + 3] & unchecked(
				0xFF)) << 24);
		}

		public static int Enc_uint64be(long l, byte[] dst, int di)
		{
			Enc_uint32be((int)(l & unchecked(0xFFFFFFFFL)), dst, di + 4);
			Enc_uint32be((int)((l >> 32) & unchecked(0xFFFFFFFFL)), dst, di);
			return 8;
		}

		public static int Enc_uint64le(long l, byte[] dst, int di)
		{
			Enc_uint32le((int)(l & unchecked(0xFFFFFFFFL)), dst, di);
			Enc_uint32le((int)((l >> 32) & unchecked(0xFFFFFFFFL)), dst, di + 4);
			return 8;
		}

		public static long Dec_uint64be(byte[] src, int si)
		{
			long l;
			l = Dec_uint32be(src, si) & unchecked(0xFFFFFFFFL);
			l <<= 32;
			l |= Dec_uint32be(src, si + 4) & unchecked(0xFFFFFFFFL);
			return l;
		}

		public static long Dec_uint64le(byte[] src, int si)
		{
			long l;
			l = Dec_uint32le(src, si + 4) & unchecked(0xFFFFFFFFL);
			l <<= 32;
			l |= Dec_uint32le(src, si) & unchecked(0xFFFFFFFFL);
			return l;
		}

		public static int Enc_floatle(float f, byte[] dst, int di)
		{
			return Enc_uint32le((int)BitConverter.DoubleToInt64Bits(f), dst, di);
		}

		public static int Enc_floatbe(float f, byte[] dst, int di)
		{
            return Enc_uint32be((int)BitConverter.DoubleToInt64Bits(f), dst, di);
		}

		public static float Dec_floatle(byte[] src, int si)
		{
			return (float)BitConverter.Int64BitsToDouble(Dec_uint32le(src, si));
		}

		public static float Dec_floatbe(byte[] src, int si)
		{
            return (float)BitConverter.Int64BitsToDouble(Dec_uint32be(src, si));
		}

		public static int Enc_doublele(double d, byte[] dst, int di)
		{
            return Enc_uint64le(BitConverter.DoubleToInt64Bits(d), dst, di);
		}

		public static int Enc_doublebe(double d, byte[] dst, int di)
		{
            return Enc_uint64be(BitConverter.DoubleToInt64Bits(d), dst, di);
		}

		public static double Dec_doublele(byte[] src, int si)
		{
            return BitConverter.Int64BitsToDouble(Dec_uint64le(src, si));
		}

		public static double Dec_doublebe(byte[] src, int si)
		{
            return BitConverter.Int64BitsToDouble(Dec_uint64be(src, si));
		}

		public static int Enc_time(DateTime date, byte[] dst, int di, int enc)
		{
			long t;
			switch (enc)
			{
				case Time1970Sec32Be:
				{
					return Enc_uint32be((int)(date.GetTime() / 1000L), dst, di);
				}

				case Time1970Sec32Le:
				{
					return Enc_uint32le((int)(date.GetTime() / 1000L), dst, di);
				}

				case Time1904Sec32Be:
				{
					return Enc_uint32be((int)((date.GetTime() / 1000L + SecBetweeen1904And1970) &
						 unchecked((int)(0xFFFFFFFF))), dst, di);
				}

				case Time1904Sec32Le:
				{
					return Enc_uint32le((int)((date.GetTime() / 1000L + SecBetweeen1904And1970) &
						 unchecked((int)(0xFFFFFFFF))), dst, di);
				}

				case Time1601Nanos64Be:
				{
					t = (date.GetTime() + MillisecondsBetween1970And1601) * 10000L;
					return Enc_uint64be(t, dst, di);
				}

				case Time1601Nanos64Le:
				{
					t = (date.GetTime() + MillisecondsBetween1970And1601) * 10000L;
					return Enc_uint64le(t, dst, di);
				}

				case Time1970Millis64Be:
				{
					return Enc_uint64be(date.GetTime(), dst, di);
				}

				case Time1970Millis64Le:
				{
					return Enc_uint64le(date.GetTime(), dst, di);
				}

				default:
				{
					throw new ArgumentException("Unsupported time encoding");
				}
			}
		}

		public static DateTime Dec_time(byte[] src, int si, int enc)
		{
			long t;
			switch (enc)
			{
				case Time1970Sec32Be:
				{
					return Sharpen.Extensions.CreateDate(Dec_uint32be(src, si) * 1000L);
				}

				case Time1970Sec32Le:
				{
					return Sharpen.Extensions.CreateDate(Dec_uint32le(src, si) * 1000L);
				}

				case Time1904Sec32Be:
				{
					return Sharpen.Extensions.CreateDate(((Dec_uint32be(src, si) & unchecked(0xFFFFFFFFL)) - SecBetweeen1904And1970) * 1000L);
				}

				case Time1904Sec32Le:
				{
					return Sharpen.Extensions.CreateDate(((Dec_uint32le(src, si) & unchecked(0xFFFFFFFFL)) - SecBetweeen1904And1970) * 1000L);
				}

				case Time1601Nanos64Be:
				{
					t = Dec_uint64be(src, si);
					return Sharpen.Extensions.CreateDate(t / 10000L - MillisecondsBetween1970And1601
						);
				}

				case Time1601Nanos64Le:
				{
					t = Dec_uint64le(src, si);
					return Sharpen.Extensions.CreateDate(t / 10000L - MillisecondsBetween1970And1601
						);
				}

				case Time1970Millis64Be:
				{
					return Sharpen.Extensions.CreateDate(Dec_uint64be(src, si));
				}

				case Time1970Millis64Le:
				{
					return Sharpen.Extensions.CreateDate(Dec_uint64le(src, si));
				}

				default:
				{
					throw new ArgumentException("Unsupported time encoding");
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static int Enc_utf8(string str, byte[] dst, int di, int dlim)
		{
			int start = di;
			int ch;
			int strlen = str.Length;
			for (int i = 0; di < dlim && i < strlen; i++)
			{
				ch = str[i];
				if ((ch >= unchecked(0x0001)) && (ch <= unchecked(0x007F)))
				{
					dst[di++] = unchecked((byte)ch);
				}
				else
				{
					if (ch > unchecked(0x07FF))
					{
						if ((dlim - di) < 3)
						{
							break;
						}
						dst[di++] = unchecked((byte)(unchecked(0xE0) | ((ch >> 12) & unchecked(0x0F))));
						dst[di++] = unchecked((byte)(unchecked(0x80) | ((ch >> 6) & unchecked(0x3F))));
						dst[di++] = unchecked((byte)(unchecked(0x80) | ((ch >> 0) & unchecked(0x3F))));
					}
					else
					{
						if ((dlim - di) < 2)
						{
							break;
						}
						dst[di++] = unchecked((byte)(unchecked(0xC0) | ((ch >> 6) & unchecked(0x1F))));
						dst[di++] = unchecked((byte)(unchecked(0x80) | ((ch >> 0) & unchecked(0x3F))));
					}
				}
			}
			return di - start;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string Dec_utf8(byte[] src, int si, int slim)
		{
			char[] uni = new char[slim - si];
			int ui;
			int ch;
			for (ui = 0; si < slim && (ch = src[si++] & unchecked(0xFF)) != 0; ui++)
			{
				if (ch < unchecked(0x80))
				{
					uni[ui] = (char)ch;
				}
				else
				{
					if ((ch & unchecked(0xE0)) == unchecked(0xC0))
					{
						if ((slim - si) < 2)
						{
							break;
						}
						uni[ui] = (char)((ch & unchecked(0x1F)) << 6);
						ch = src[si++] & unchecked(0xFF);
						uni[ui] |= (char)((char)ch & unchecked(0x3F));
						if ((ch & unchecked(0xC0)) != unchecked(0x80) || uni[ui] < unchecked(
							0x80))
						{
							throw new IOException("Invalid UTF-8 sequence");
						}
					}
					else
					{
						if ((ch & unchecked(0xF0)) == unchecked(0xE0))
						{
							if ((slim - si) < 3)
							{
								break;
							}
							uni[ui] = (char)((ch & unchecked(0x0F)) << 12);
							ch = src[si++] & unchecked(0xFF);
							if ((ch & unchecked(0xC0)) != unchecked(0x80))
							{
								throw new IOException("Invalid UTF-8 sequence");
							}
						    uni[ui] |= (char)((char)(ch & unchecked(0x3F)) << 6);
						    ch = src[si++] & unchecked(0xFF);
						    uni[ui] |= (char)((char)ch & unchecked(0x3F));
						    if ((ch & unchecked(0xC0)) != unchecked(0x80) || uni[ui] < unchecked(
						        0x800))
						    {
						        throw new IOException("Invalid UTF-8 sequence");
						    }
						}
						else
						{
							throw new IOException("Unsupported UTF-8 sequence");
						}
					}
				}
			}
			return new string(uni, 0, ui);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string Dec_ucs2le(byte[] src, int si, int slim, char[] buf)
		{
			int bi;
			for (bi = 0; (si + 1) < slim; bi++, si += 2)
			{
				buf[bi] = (char)Dec_uint16le(src, si);
				if (buf[bi] == '\0')
				{
					break;
				}
			}
			return new string(buf, 0, bi);
		}
	}
}
