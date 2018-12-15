//
// Marker.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Stephane Delcroix (stephane@delcroix.org)
//
// Copyright (C) 2009 Ruben Vermeersch
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
	///    This enum defines the different markers used in JPEG segments.
	///
	///    See CCITT Rec. T.81 (1992 E), Table B.1 (p.32)
	/// </summary>
	public enum Marker : byte {
		/// <summary>
		///    Start Of Frame marker, non-differential, Huffman coding, Baseline DCT
		/// </summary>
		SOF0 = 0xc0,

		/// <summary>
		///    Start Of Frame marker, non-differential, Huffman coding, Extended Sequential DCT
		/// </summary>
		SOF1,

		/// <summary>
		///    Start Of Frame marker, non-differential, Huffman coding, Progressive DCT
		/// </summary>
		SOF2,

		/// <summary>
		///    Start Of Frame marker, non-differential, Huffman coding, Lossless (sequential)
		/// </summary>
		SOF3,

		/// <summary>
		///    Start Of Frame marker, differential, Huffman coding, Differential Sequential DCT
		/// </summary>
		SOF5 = 0xc5,

		/// <summary>
		///    Start Of Frame marker, differential, Huffman coding, Differential Progressive DCT
		/// </summary>
		SOF6,
		/// <summary>
		///    Start Of Frame marker, differential, Huffman coding, Differential Lossless (sequential)
		/// </summary>
		SOF7,

		/// <summary>
		///    Reserved for JPG extensions
		/// </summary>
		JPG,

		/// <summary>
		///    Start Of Frame marker, non-differential, arithmetic coding, Extended Sequential DCT
		/// </summary>
		SOF9,

		/// <summary>
		///    Start Of Frame marker, non-differential, arithmetic coding, Progressive DCT
		/// </summary>
		SOF10,

		/// <summary>
		///    Start Of Frame marker, non-differential, arithmetic coding, Lossless (sequential)
		/// </summary>
		SOF11,

		/// <summary>
		///    Start Of Frame marker, differential, arithmetic coding, Differential Sequential DCT
		/// </summary>
		SOF13 = 0xcd,

		/// <summary>
		///    Start Of Frame marker, differential, arithmetic coding, Differential Progressive DCT
		/// </summary>
		SOF14,

		/// <summary>
		///    Start Of Frame marker, differential, arithmetic coding, Differential Lossless (sequential)
		/// </summary>
		SOF15,

		/// <summary>
		///    Define Huffman table(s)
		/// </summary>
		DHT = 0xc4,

		/// <summary>
		///    Define arithmetic coding conditioning(s)
		/// </summary>
		DAC = 0xcc,

		//Restart interval termination with modulo 8 count "m"
		/// <summary>
		///    Restart
		/// </summary>
		RST0 = 0xd0,

		/// <summary>
		///    Restart
		/// </summary>
		RST1,

		/// <summary>
		///    Restart
		/// </summary>
		RST2,

		/// <summary>
		///    Restart
		/// </summary>
		RST3,

		/// <summary>
		///    Restart
		/// </summary>
		RST4,

		/// <summary>
		///    Restart
		/// </summary>
		RST5,

		/// <summary>
		///    Restart
		/// </summary>
		RST6,

		/// <summary>
		///    Restart
		/// </summary>
		RST7,

		/// <summary>
		///    Start of Image
		/// </summary>
		SOI = 0xd8,

		/// <summary>
		///    End of Image
		/// </summary>
		EOI,

		/// <summary>
		///    Start of scan
		/// </summary>
		SOS,

		/// <summary>
		///    Define quantization table (s)
		/// </summary>
		DQT,

		/// <summary>
		///    Define number of lines
		/// </summary>
		DNL,

		/// <summary>
		///    Define restart interval
		/// </summary>
		DRI,

		/// <summary>
		///    Define hierarchical progression
		/// </summary>
		DHP,

		/// <summary>
		///    Define reference component
		/// </summary>
		EXP,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP0 = 0xe0,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP1,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP2,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP3,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP4,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP5,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP6,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP7,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP8,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP9,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP10,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP11,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP12,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP13,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP14,

		/// <summary>
		///    Reserved for application segment
		/// </summary>
		APP15,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG0 = 0xf0,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG1,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG2,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG3,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG4,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG5,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG6,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG7,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG8,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG9,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG10,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG11,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG12,

		/// <summary>
		///    Reserved for JPEG extension
		/// </summary>
		JPG13,

		/// <summary>
		///   Comment
		/// </summary>
		COM = 0xfe,
	}
}
