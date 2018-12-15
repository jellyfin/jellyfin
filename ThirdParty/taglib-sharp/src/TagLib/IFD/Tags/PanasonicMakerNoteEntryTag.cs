//
// PanasonicMakerNoteEntryTag.cs:
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
	///    Panasonic makernote tags.
	///    Based on http://www.exiv2.org/tags-panasonic.html
	/// </summary>
	public enum PanasonicMakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Image Quality. (Hex: 0x0001)
		/// </summary>
		Quality                                             = 1,

		/// <summary>
		///    Firmware version. (Hex: 0X0002)
		/// </summary>
		FirmwareVersion                                     = 2,

		/// <summary>
		///    White balance setting. (Hex: 0X0003)
		/// </summary>
		WhiteBalance                                        = 3,

		/// <summary>
		///    Unknown. (Hex: 0X0004)
		/// </summary>
		Unknown4                                            = 4,

		/// <summary>
		///    Focus mode. (Hex: 0X0007)
		/// </summary>
		FocusMode                                           = 7,

		/// <summary>
		///    AF mode. (Hex: 0X000F)
		/// </summary>
		AFMode                                              = 15,

		/// <summary>
		///    ISO Speed. (Hex: 0X0017)
		/// </summary>
		ISO                                                 = 23,

		/// <summary>
		///    Image stabilization. (Hex: 0X001A)
		/// </summary>
		ImageStabilization                                  = 26,

		/// <summary>
		///    Macro mode. (Hex: 0X001C)
		/// </summary>
		Macro                                               = 28,

		/// <summary>
		///    Shooting mode. (Hex: 0X001F)
		/// </summary>
		ShootingMode                                        = 31,

		/// <summary>
		///    Audio. (Hex: 0X0020)
		/// </summary>
		Audio                                               = 32,

		/// <summary>
		///    Data dump. (Hex: 0X0021)
		/// </summary>
		DataDump                                            = 33,

		/// <summary>
		///    Unknown. (Hex: 0X0022)
		/// </summary>
		Unknown34                                           = 34,

		/// <summary>
		///    White balance adjustment. (Hex: 0X0023)
		/// </summary>
		WhiteBalanceBias                                    = 35,

		/// <summary>
		///    Flash bias. (Hex: 0X0024)
		/// </summary>
		FlashBias                                           = 36,

		/// <summary>
		///    This number is unique, and contains the date of manufacture, but
		///    is not the same as the number printed on the camera body.
		///    (Hex: 0X0025)
		/// </summary>
		InternalSerialNumber                                = 37,

		/// <summary>
		///    Exif version. (Hex: 0X0026)
		/// </summary>
		ExifVersion                                         = 38,

		/// <summary>
		///    Unknown. (Hex: 0X0027)
		/// </summary>
		Unknown39                                           = 39,

		/// <summary>
		///    Color effect. (Hex: 0X0028)
		/// </summary>
		ColorEffect                                         = 40,

		/// <summary>
		///    Time in 1/100s from when the camera was powered on to when the
		///    image is written to memory card. (Hex: 0X0029)
		/// </summary>
		TimeSincePowerOn                                    = 41,

		/// <summary>
		///    Burst mode. (Hex: 0X002A)
		/// </summary>
		BurstMode                                           = 42,

		/// <summary>
		///    Sequence number. (Hex: 0X002B)
		/// </summary>
		SequenceNumber                                      = 43,

		/// <summary>
		///    Contrast setting. (Hex: 0X002C)
		/// </summary>
		Contrast                                            = 44,

		/// <summary>
		///    Noise reduction. (Hex: 0X002D)
		/// </summary>
		NoiseReduction                                      = 45,

		/// <summary>
		///    Self timer. (Hex: 0X002E)
		/// </summary>
		SelfTimer                                           = 46,

		/// <summary>
		///    Unknown. (Hex: 0X002F)
		/// </summary>
		Unknown47                                           = 47,

		/// <summary>
		///    Rotation. (Hex: 0X0030)
		/// </summary>
		Rotation                                            = 48,

		/// <summary>
		///    Unknown. (Hex: 0X0031)
		/// </summary>
		Unknown49                                           = 49,

		/// <summary>
		///    Color mode. (Hex: 0X0032)
		/// </summary>
		ColorMode                                           = 50,

		/// <summary>
		///    Baby (or pet) age. (Hex: 0X0033)
		/// </summary>
		BabyAge                                             = 51,

		/// <summary>
		///    Optical zoom mode. (Hex: 0X0034)
		/// </summary>
		OpticalZoomMode                                     = 52,

		/// <summary>
		///    Conversion lens. (Hex: 0X0035)
		/// </summary>
		ConversionLens                                      = 53,

		/// <summary>
		///    Travel day. (Hex: 0X0036)
		/// </summary>
		TravelDay                                           = 54,

		/// <summary>
		///    Contrast. (Hex: 0X0039)
		/// </summary>
		Contrast2                                           = 57,

		/// <summary>
		///    World time location. (Hex: 0X003A)
		/// </summary>
		WorldTimeLocation                                   = 58,

		/// <summary>
		///    Program ISO. (Hex: 0X003C)
		/// </summary>
		ProgramISO                                          = 60,

		/// <summary>
		///    Saturation. (Hex: 0X0040)
		/// </summary>
		Saturation                                          = 64,

		/// <summary>
		///    Sharpness. (Hex: 0X0041)
		/// </summary>
		Sharpness                                           = 65,

		/// <summary>
		///    Film mode. (Hex: 0X0042)
		/// </summary>
		FilmMode                                            = 66,

		/// <summary>
		///    WB adjust AB. Positive is a shift toward blue. (Hex: 0X0046)
		/// </summary>
		WBAdjustAB                                          = 70,

		/// <summary>
		///    WBAdjustGM. Positive is a shift toward green. (Hex: 0X0047)
		/// </summary>
		WBAdjustGM                                          = 71,

		/// <summary>
		///    Lens type. (Hex: 0X0051)
		/// </summary>
		LensType                                            = 81,

		/// <summary>
		///    Lens serial number. (Hex: 0X0052)
		/// </summary>
		LensSerialNumber                                    = 82,

		/// <summary>
		///    Accessory type. (Hex: 0X0053)
		/// </summary>
		AccessoryType                                       = 83,

		/// <summary>
		///    PrintIM information. (Hex: 0X0E00)
		/// </summary>
		PrintIM                                             = 3584,

		/// <summary>
		///    Unknown. (Hex: 0X4449)
		/// </summary>
		Unknown17481                                        = 17481,

		/// <summary>
		///    MakerNote version. (Hex: 0X8000)
		/// </summary>
		MakerNoteVersion                                    = 32768,

		/// <summary>
		///    Scene mode. (Hex: 0X8001)
		/// </summary>
		SceneMode                                           = 32769,

		/// <summary>
		///    WB red level. (Hex: 0X8004)
		/// </summary>
		WBRedLevel                                          = 32772,

		/// <summary>
		///    WB green level. (Hex: 0X8005)
		/// </summary>
		WBGreenLevel                                        = 32773,

		/// <summary>
		///    WB blue level. (Hex: 0X8006)
		/// </summary>
		WBBlueLevel                                         = 32774,

		/// <summary>
		///    Baby (or pet) age. (Hex: 0X8010)
		/// </summary>
		BabyAge2                                            = 32784,
	}
}
