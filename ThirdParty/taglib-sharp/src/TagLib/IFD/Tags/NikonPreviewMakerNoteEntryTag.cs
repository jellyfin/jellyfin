//
// NikonPreviewMakerNoteEntryTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009-2010 Ruben Vermeersch
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

namespace TagLib.IFD.Tags
{
	/// <summary>
	///    Nikon makernote preview image tags
	///    The preview image is contained in a sub-IFD stored by the tag
	///    Nikon3MakerNoteEntryTag.Preview.
	///    Based on:
	///    http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/Nikon.html#PreviewImage
	/// </summary>
	public enum NikonPreviewMakerNoteEntryTag : ushort
	{

		/// <summary>
		///     Compression scheme used on the image data. (Hex: 0x0103)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/compression.html
		/// </summary>
		Compression                                        = 259,

		/// <summary>
		///     The number of pixels per ResolutionUnit in the ImageWidth direction. (Hex: 0x011A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/xresolution.html
		/// </summary>
		XResolution                                        = 282,

		/// <summary>
		///     The number of pixels per ResolutionUnit in the ImageLength direction. (Hex: 0x011B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/yresolution.html
		/// </summary>
		YResolution                                        = 283,

		/// <summary>
		///     The unit of measurement for XResolution and YResolution. (Hex: 0x0128)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/resolutionunit.html
		/// </summary>
		ResolutionUnit                                     = 296,

		/// <summary>
		///     Start of the preview image data. (Hex: 0x0201)
		///     http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/Nikon.html#PreviewImage
		/// </summary>
		PreviewImageStart                                  = 513,

		/// <summary>
		///     Length of the preview image data. (Hex: 0x0202)
		///     http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/Nikon.html#PreviewImage
		/// </summary>
		PreviewImageLength                                 = 514,

		/// <summary>
		///     Specifies the positioning of subsampled chrominance components relative to luminance samples. (Hex: 0x0213)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ycbcrpositioning.html
		/// </summary>
		YCbCrPositioning                                   = 531
	}
}
