//
// TagLib.Jpeg.Table.cs:
//
// Author:
//   Stephane Delcroix (stephane@delcroix.org)
//
// Copyright (c) 2009 Stephane Delcroix
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

namespace TagLib.Jpeg
{
	/// <summary>
	///    Contains static predefined tables and helpers
	/// </summary>
	public static class Table
	{
		/// <summary>
		///    Standard Luminance Quantization table
		///
		///    See CCIT Rec. T.81 (1992 E), K.1 (p143)
		/// </summary>
		public static int [] StandardLuminanceQuantization = new int [] {
			16,  11,  12,  14,  12,  10,  16,  14,
			13,  14,  18,  17,  16,  19,  24,  40,
			26,  24,  22,  22,  24,  49,  35,  37,
			29,  40,  58,  51,  61,  60,  57,  51,
			56,  55,  64,  72,  92,  78,  64,  68,
			87,  69,  55,  56,  80, 109,  81,  87,
			95,  98, 103, 104, 103,  62,  77, 113,
			121, 112, 100, 120,  92, 101, 103, 99
		};

		/// <summary>
		///    Standard Chrominance Quantization table
		///
		///    See CCIT Rec. T.81 (1992 E), K.1 (p143)
		/// </summary>
		public static int [] StandardChrominanceQuantization = new int [] {
			17,  18,  18,  24,  21,  24,  47,  26,
			26,  47,  99,  66,  56,  66,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99,
			99,  99,  99,  99,  99,  99,  99,  99
		};
	}
}
