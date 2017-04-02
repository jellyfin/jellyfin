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

namespace SharpCifs.Dcerpc
{
	public class Uuid : Rpc.UuidT
	{
		public static int Hex_to_bin(char[] arr, int offset, int length)
		{
			int value = 0;
			int ai;
			int count;
			count = 0;
			for (ai = offset; ai < arr.Length && count < length; ai++)
			{
				value <<= 4;
				switch (arr[ai])
				{
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					{
						value += arr[ai] - '0';
						break;
					}

					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
					{
						value += 10 + (arr[ai] - 'A');
						break;
					}

					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
					{
						value += 10 + (arr[ai] - 'a');
						break;
					}

					default:
					{
						throw new ArgumentException(new string(arr, offset, length));
					}
				}
				count++;
			}
			return value;
		}

		internal static readonly char[] Hexchars = { '0', '1', '2', '3', '4', 
			'5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		public static string Bin_to_hex(int value, int length)
		{
			char[] arr = new char[length];
			int ai = arr.Length;
			while (ai-- > 0)
			{
				arr[ai] = Hexchars[value & unchecked(0xF)];
				value = (int)(((uint)value) >> 4);
			}
			return new string(arr);
		}

		private static byte B(int i)
		{
			return unchecked((byte)(i & unchecked(0xFF)));
		}

		private static short S(int i)
		{
			return (short)(i & unchecked(0xFFFF));
		}

		public Uuid(Rpc.UuidT uuid)
		{
			TimeLow = uuid.TimeLow;
			TimeMid = uuid.TimeMid;
			TimeHiAndVersion = uuid.TimeHiAndVersion;
			ClockSeqHiAndReserved = uuid.ClockSeqHiAndReserved;
			ClockSeqLow = uuid.ClockSeqLow;
			Node = new byte[6];
			Node[0] = uuid.Node[0];
			Node[1] = uuid.Node[1];
			Node[2] = uuid.Node[2];
			Node[3] = uuid.Node[3];
			Node[4] = uuid.Node[4];
			Node[5] = uuid.Node[5];
		}

		public Uuid(string str)
		{
			char[] arr = str.ToCharArray();
			TimeLow = Hex_to_bin(arr, 0, 8);
			TimeMid = S(Hex_to_bin(arr, 9, 4));
			TimeHiAndVersion = S(Hex_to_bin(arr, 14, 4));
			ClockSeqHiAndReserved = B(Hex_to_bin(arr, 19, 2));
			ClockSeqLow = B(Hex_to_bin(arr, 21, 2));
			Node = new byte[6];
			Node[0] = B(Hex_to_bin(arr, 24, 2));
			Node[1] = B(Hex_to_bin(arr, 26, 2));
			Node[2] = B(Hex_to_bin(arr, 28, 2));
			Node[3] = B(Hex_to_bin(arr, 30, 2));
			Node[4] = B(Hex_to_bin(arr, 32, 2));
			Node[5] = B(Hex_to_bin(arr, 34, 2));
		}

		public override string ToString()
		{
			return Bin_to_hex(TimeLow, 8) + '-' + Bin_to_hex(TimeMid, 4) + '-' + Bin_to_hex
				(TimeHiAndVersion, 4) + '-' + Bin_to_hex(ClockSeqHiAndReserved, 2) + Bin_to_hex
				(ClockSeqLow, 2) + '-' + Bin_to_hex(Node[0], 2) + Bin_to_hex(Node[1], 2) + Bin_to_hex
				(Node[2], 2) + Bin_to_hex(Node[3], 2) + Bin_to_hex(Node[4], 2) + Bin_to_hex(Node
				[5], 2);
		}
	}
}
