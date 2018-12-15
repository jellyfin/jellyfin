//
// NikonLensData3EntryTag.cs:
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
	///    Nikon lens data entry tags.
	///    Based on:
	///    http://exiv2.org/tags-nikon.html
	/// </summary>
	public enum NikonLensData3EntryTag : ushort
	{

		/// <summary>
		///    Version. (Hex: 0X0000)
		/// </summary>
		Version                                             = 0,

		/// <summary>
		///    Exit pupil position. (Hex: 0X0004)
		/// </summary>
		ExitPupilPosition                                   = 4,

		/// <summary>
		///    AF aperture. (Hex: 0X0005)
		/// </summary>
		AFAperture                                          = 5,

		/// <summary>
		///    Focus position. (Hex: 0X0008)
		/// </summary>
		FocusPosition                                       = 8,

		/// <summary>
		///    Focus distance. (Hex: 0X000A)
		/// </summary>
		FocusDistance                                       = 10,

		/// <summary>
		///    Focal length. (Hex: 0X000B)
		/// </summary>
		FocalLength                                         = 11,

		/// <summary>
		///    Lens ID number. (Hex: 0X000C)
		/// </summary>
		LensIDNumber                                        = 12,

		/// <summary>
		///    Lens F-stops. (Hex: 0X000D)
		/// </summary>
		LensFStops                                          = 13,

		/// <summary>
		///    Min focal length. (Hex: 0X000E)
		/// </summary>
		MinFocalLength                                      = 14,

		/// <summary>
		///    Max focal length. (Hex: 0X000F)
		/// </summary>
		MaxFocalLength                                      = 15,

		/// <summary>
		///    Max aperture at min focal length. (Hex: 0X0010)
		/// </summary>
		MaxApertureAtMinFocal                               = 16,

		/// <summary>
		///    Max aperture at max focal length. (Hex: 0X0011)
		/// </summary>
		MaxApertureAtMaxFocal                               = 17,

		/// <summary>
		///    MCU version. (Hex: 0X0012)
		/// </summary>
		MCUVersion                                          = 18,

		/// <summary>
		///    Effective max aperture. (Hex: 0X0013)
		/// </summary>
		EffectiveMaxAperture                                = 19,

	}
}
