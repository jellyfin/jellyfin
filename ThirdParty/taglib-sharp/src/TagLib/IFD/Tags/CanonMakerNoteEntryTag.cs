//
// CanonMakerNoteEntryTag.cs:
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
	///    Label tags for Canon Makernote.
	///    Based on http://www.burren.cx/david/canon.html and http://www.exiv2.org/tags-canon.html
	/// </summary>
	public enum CanonMakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Unknown field at tag 0x0000. (Hex: 0x0000)
		/// </summary>
		Unknown0                                           = 0,

		/// <summary>
		///    Camera Settings. (Hex: 0x0001)
		/// </summary>
		CameraSettings                                      = 1,

		/// <summary>
		///    Focal Length. (Hex: 0x0002)
		/// </summary>
		FocalLength                                         = 2,

		/// <summary>
		///    Unknown field at tag 0x0000. (Hex: 0x0003)
		/// </summary>
		Unknown3                                            = 3,

		/// <summary>
		///    Shot Information. (Hex: 0x0004)
		/// </summary>
		ShotInfo                                            = 4,

		/// <summary>
		///    Panorama. (Hex: 0x0005)
		/// </summary>
		Panorama                                            = 5,

		/// <summary>
		///    Image Type. (Hex: 0x0006)
		/// </summary>
		ImageType                                           = 6,

		/// <summary>
		///    Firmware Version. (Hex: 0x0007)
		/// </summary>
		FirmwareVersion                                     = 7,

		/// <summary>
		///    Image Number. (Hex: 0x0008)
		/// </summary>
		ImageNumber                                         = 8,

		/// <summary>
		///    Owner Name. (Hex: 0x0009)
		/// </summary>
		OwnerName                                           = 9,

		/// <summary>
		///    Serial Number. (Hex: 0x000C)
		/// </summary>
		SerialNumber                                        = 12,

		/// <summary>
		///    Unknown field at tag 0x0000. (Hex: 0x000D)
		/// </summary>
		Unknown13                                           = 13,

		/// <summary>
		///    Custom Functions. (Hex: 0x000F)
		/// </summary>
		CustomFunctions                                     = 15,

		/// <summary>
		///    Model ID. (Hex: 0x0010)
		/// </summary>
		ModelID                                             = 16,

		/// <summary>
		///    Picture Info. (Hex: 0x0012)
		/// </summary>
		PictureInfo                                         = 18,

		/// <summary>
		///    Serial Number Format. (Hex: 0x0015)
		/// </summary>
		SerialNumberFormat                                  = 21,

		/// <summary>
		///    Canon File Info. (Hex: 0x0093)
		/// </summary>
		CanonFileInfo                                       = 147,

		/// <summary>
		///    Lens Model. (Hex: 0x0095)
		/// </summary>
		LensModel                                           = 149,

		/// <summary>
		///    Serial Info. (Hex: 0x0096)
		/// </summary>
		SerialInfo                                          = 150,

		/// <summary>
		///    Processing Info. (Hex: 0x00A0)
		/// </summary>
		ProcessingInfo                                      = 160,

		/// <summary>
		///    White Balance Table. (Hex: 0x00A9)
		/// </summary>
		WhiteBalanceTable                                   = 169,

		/// <summary>
		///    Measured Color. (Hex: 0x00AA)
		/// </summary>
		MeasuredColor                                       = 170,

		/// <summary>
		///    Color Space. (Hex: 0x00B4)
		/// </summary>
		ColorSpace                                          = 180,

		/// <summary>
		///    Sensor Info. (Hex: 0x00E0)
		/// </summary>
		SensorInfo                                          = 224,

		/// <summary>
		///    Black Level. (Hex: 0x4008)
		/// </summary>
		BlackLevel                                          = 16392,
	}
}
