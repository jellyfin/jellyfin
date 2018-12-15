//
// NikonShotInfoEntryTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2010 Ruben Vermeersch
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

namespace TagLib.IFD.Tags
{
	/// <summary>
	///    Nikon shot info entry tags.
	///    Based on:
	///    http://exiv2.org/tags-nikon.html
	/// </summary>
	public enum NikonShotInfoEntryTag : ushort
	{

		/// <summary>
		///    Version. (Hex: 0X0000)
		/// </summary>
		Version                                             = 0,

		/// <summary>
		///    Shutter count 1. (Hex: 0X006A)
		/// </summary>
		ShutterCount1                                       = 106,

		/// <summary>
		///    Deleted image count. (Hex: 0X006E)
		/// </summary>
		DeletedImageCount                                   = 110,

		/// <summary>
		///    Vibration reduction. (Hex: 0X0075)
		/// </summary>
		VibrationReduction                                  = 117,

		/// <summary>
		///    . (Hex: 0X0082)
		/// </summary>
		VibrationReduction1                                 = 130,

		/// <summary>
		///    Shutter count 2. (Hex: 0X0157)
		/// </summary>
		ShutterCount2                                       = 343,

		/// <summary>
		///    Vibration reduction 2. (Hex: 0X01AE)
		/// </summary>
		VibrationReduction2                                 = 430,

		/// <summary>
		///    ISO. (Hex: 0X0256)
		/// </summary>
		ISO                                                 = 598,

		/// <summary>
		///    Shutter count. (Hex: 0X0276)
		/// </summary>
		ShutterCount                                        = 630,

	}
}
