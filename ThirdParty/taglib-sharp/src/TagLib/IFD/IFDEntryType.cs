//
// IFDEntryType.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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

namespace TagLib.IFD
{
	/// <summary>
	///    A type indicator, which identifies how the corresponding value
	///    field should be interpreted.
	/// </summary>
	public enum IFDEntryType : ushort
	{
		/// <summary>
		///    Unknown (shouldn't occur)
		/// </summary>
		Unknown = 0,

		/// <summary>
		///    8-bit unsigned integer.
		/// </summary>
		Byte = 1,

		/// <summary>
		///    8-bit byte that contains a 7-bit ASCII code; the last byte
		///    must be NUL (binary zero).
		/// </summary>
		Ascii = 2,

		/// <summary>
		///    16-bit (2-byte) unsigned integer.
		/// </summary>
		Short = 3,

		/// <summary>
		///    32-bit (4-byte) unsigned integer.
		/// </summary>
		Long = 4,

		/// <summary>
		///    Two LONGs: the first represents the numerator of a
		///    fraction; the second, the denominator.
		/// </summary>
		Rational = 5,

		/// <summary>
		///    An 8-bit signed (twos-complement) integer.
		/// </summary>
		SByte = 6,

		/// <summary>
		///    An 8-bit byte that may contain anything, depending on
		///    the definition of the field.
		/// </summary>
		Undefined = 7,

		/// <summary>
		///    A 16-bit (2-byte) signed (twos-complement) integer.
		/// </summary>
		SShort = 8,

		/// <summary>
		///    A 32-bit (4-byte) signed (twos-complement) integer.
		/// </summary>
		SLong = 9,

		/// <summary>
		///    Two SLONG’s: the first represents the numerator of a
		///    fraction, the second the denominator.
		/// </summary>
		SRational = 10,

		/// <summary>
		///    Single precision (4-byte) IEEE format.
		/// </summary>
		Float = 11,

		/// <summary>
		///    Double precision (8-byte) IEEE format.
		/// </summary>
		Double = 12,

		/// <summary>
		///    IFD
		/// </summary>
		IFD = 13
	}
}
