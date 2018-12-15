//
// NikonPictureControlEntryTag.cs:
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
	///    Nikon picture control entry tags.
	///    Based on:
	///    http://exiv2.org/tags-nikon.html
	/// </summary>
	public enum NikonPictureControlEntryTag : ushort
	{

		/// <summary>
		///    Version. (Hex: 0X0000)
		/// </summary>
		Version                                             = 0,

		/// <summary>
		///    Name. (Hex: 0X0004)
		/// </summary>
		Name                                                = 4,

		/// <summary>
		///    Base. (Hex: 0X0018)
		/// </summary>
		Base                                                = 24,

		/// <summary>
		///    Adjust. (Hex: 0X0030)
		/// </summary>
		Adjust                                              = 48,

		/// <summary>
		///    Quick adjust. (Hex: 0X0031)
		/// </summary>
		QuickAdjust                                         = 49,

		/// <summary>
		///    Sharpness. (Hex: 0X0032)
		/// </summary>
		Sharpness                                           = 50,

		/// <summary>
		///    Contrast. (Hex: 0X0033)
		/// </summary>
		Contrast                                            = 51,

		/// <summary>
		///    Brightness. (Hex: 0X0034)
		/// </summary>
		Brightness                                          = 52,

		/// <summary>
		///    Saturation. (Hex: 0X0035)
		/// </summary>
		Saturation                                          = 53,

		/// <summary>
		///    Hue adjustment. (Hex: 0X0036)
		/// </summary>
		HueAdjustment                                       = 54,

		/// <summary>
		///    Filter effect. (Hex: 0X0037)
		/// </summary>
		FilterEffect                                        = 55,

		/// <summary>
		///    Toning effect. (Hex: 0X0038)
		/// </summary>
		ToningEffect                                        = 56,

		/// <summary>
		///    Toning saturation. (Hex: 0X0039)
		/// </summary>
		ToningSaturation                                    = 57,

	}
}
