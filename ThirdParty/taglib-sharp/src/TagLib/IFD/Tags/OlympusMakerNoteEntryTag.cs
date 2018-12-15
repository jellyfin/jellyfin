//
// OlympusMakerNoteEntryTag.cs:
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
	///    Olympus makernote tags.
	///    Based on http://www.exiv2.org/tags-olympus.html
	/// </summary>
	public enum OlympusMakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Thumbnail image. (Hex: 0X0100)
		/// </summary>
		ThumbnailImage                                      = 256,

		/// <summary>
		///    Picture taking mode. (Hex: 0X0200)
		/// </summary>
		SpecialMode                                         = 512,

		/// <summary>
		///    Image quality setting. (Hex: 0X0201)
		/// </summary>
		Quality                                             = 513,

		/// <summary>
		///    Macro mode. (Hex: 0X0202)
		/// </summary>
		Macro                                               = 514,

		/// <summary>
		///    Black and white mode. (Hex: 0X0203)
		/// </summary>
		BWMode                                              = 515,

		/// <summary>
		///    Digital zoom ratio. (Hex: 0X0204)
		/// </summary>
		DigitalZoom                                         = 516,

		/// <summary>
		///    Focal plane diagonal. (Hex: 0X0205)
		/// </summary>
		FocalPlaneDiagonal                                  = 517,

		/// <summary>
		///    Lens distortion parameters. (Hex: 0X0206)
		/// </summary>
		LensDistortionParams                                = 518,

		/// <summary>
		///    Software firmware version. (Hex: 0X0207)
		/// </summary>
		FirmwareVersion                                     = 519,

		/// <summary>
		///    ASCII format data such as [PictureInfo]. (Hex: 0X0208)
		/// </summary>
		PictureInfo                                         = 520,

		/// <summary>
		///    Camera ID data. (Hex: 0X0209)
		/// </summary>
		CameraID                                            = 521,

		/// <summary>
		///    Pre-capture frames. (Hex: 0X0300)
		/// </summary>
		PreCaptureFrames                                    = 768,

		/// <summary>
		///    One touch white balance. (Hex: 0X0302)
		/// </summary>
		OneTouchWB                                          = 770,

		/// <summary>
		///    Serial number. (Hex: 0X0404)
		/// </summary>
		SerialNumber                                        = 1028,

		/// <summary>
		///    PrintIM information. (Hex: 0X0E00)
		/// </summary>
		PrintIM                                             = 3584,

		/// <summary>
		///    Various camera settings 1. (Hex: 0X0F00)
		/// </summary>
		DataDump1                                           = 3840,

		/// <summary>
		///    Various camera settings 2. (Hex: 0X0F01)
		/// </summary>
		DataDump2                                           = 3841,

		/// <summary>
		///    Shutter speed value. (Hex: 0X1000)
		/// </summary>
		ShutterSpeed                                        = 4096,

		/// <summary>
		///    ISO speed value. (Hex: 0X1001)
		/// </summary>
		ISOSpeed                                            = 4097,

		/// <summary>
		///    Aperture value. (Hex: 0X1002)
		/// </summary>
		ApertureValue                                       = 4098,

		/// <summary>
		///    Brightness value. (Hex: 0X1003)
		/// </summary>
		Brightness                                          = 4099,

		/// <summary>
		///    Flash mode. (Hex: 0X1004)
		/// </summary>
		FlashMode                                           = 4100,

		/// <summary>
		///    Flash device. (Hex: 0X1005)
		/// </summary>
		FlashDevice                                         = 4101,

		/// <summary>
		///    Exposure compensation value. (Hex: 0X1006)
		/// </summary>
		Bracket                                             = 4102,

		/// <summary>
		///    Sensor temperature. (Hex: 0X1007)
		/// </summary>
		SensorTemperature                                   = 4103,

		/// <summary>
		///    Lens temperature. (Hex: 0X1008)
		/// </summary>
		LensTemperature                                     = 4104,

		/// <summary>
		///    Focus mode. (Hex: 0X100B)
		/// </summary>
		FocusMode                                           = 4107,

		/// <summary>
		///    Manual focus distance. (Hex: 0X100C)
		/// </summary>
		FocusDistance                                       = 4108,

		/// <summary>
		///    Zoom step count. (Hex: 0X100D)
		/// </summary>
		Zoom                                                = 4109,

		/// <summary>
		///    Macro focus step count. (Hex: 0X100E)
		/// </summary>
		MacroFocus                                          = 4110,

		/// <summary>
		///    Sharpness factor. (Hex: 0X100F)
		/// </summary>
		SharpnessFactor                                     = 4111,

		/// <summary>
		///    Flash charge level. (Hex: 0X1010)
		/// </summary>
		FlashChargeLevel                                    = 4112,

		/// <summary>
		///    Color matrix. (Hex: 0X1011)
		/// </summary>
		ColorMatrix                                         = 4113,

		/// <summary>
		///    Black level. (Hex: 0X1012)
		/// </summary>
		BlackLevel                                          = 4114,

		/// <summary>
		///    White balance mode. (Hex: 0X1015)
		/// </summary>
		WhiteBalance                                        = 4117,

		/// <summary>
		///    Red balance. (Hex: 0X1017)
		/// </summary>
		RedBalance                                          = 4119,

		/// <summary>
		///    Blue balance. (Hex: 0X1018)
		/// </summary>
		BlueBalance                                         = 4120,

		/// <summary>
		///    Serial number 2. (Hex: 0X101A)
		/// </summary>
		SerialNumber2                                       = 4122,

		/// <summary>
		///    Flash exposure compensation. (Hex: 0X1023)
		/// </summary>
		FlashBias                                           = 4131,

		/// <summary>
		///    External flash bounce. (Hex: 0X1026)
		/// </summary>
		ExternalFlashBounce                                 = 4134,

		/// <summary>
		///    External flash zoom. (Hex: 0X1027)
		/// </summary>
		ExternalFlashZoom                                   = 4135,

		/// <summary>
		///    External flash mode. (Hex: 0X1028)
		/// </summary>
		ExternalFlashMode                                   = 4136,

		/// <summary>
		///    Contrast setting. (Hex: 0X1029)
		/// </summary>
		Contrast                                            = 4137,

		/// <summary>
		///    Sharpness factor. (Hex: 0X102A)
		/// </summary>
		SharpnessFactor2                                    = 4138,

		/// <summary>
		///    Color control. (Hex: 0X102B)
		/// </summary>
		ColorControl                                        = 4139,

		/// <summary>
		///    Valid bits. (Hex: 0X102C)
		/// </summary>
		ValidBits                                           = 4140,

		/// <summary>
		///    Coring filter. (Hex: 0X102D)
		/// </summary>
		CoringFilter                                        = 4141,

		/// <summary>
		///    Image width. (Hex: 0X102E)
		/// </summary>
		ImageWidth                                          = 4142,

		/// <summary>
		///    Image height. (Hex: 0X102F)
		/// </summary>
		ImageHeight                                         = 4143,

		/// <summary>
		///    Compression ratio. (Hex: 0X1034)
		/// </summary>
		CompressionRatio                                    = 4148,

		/// <summary>
		///    Preview image embedded. (Hex: 0X1035)
		/// </summary>
		Thumbnail                                           = 4149,

		/// <summary>
		///    Offset of the preview image. (Hex: 0X1036)
		/// </summary>
		ThumbnailOffset                                     = 4150,

		/// <summary>
		///    Size of the preview image. (Hex: 0X1037)
		/// </summary>
		ThumbnailLength                                     = 4151,

		/// <summary>
		///    CCD scan mode. (Hex: 0X1039)
		/// </summary>
		CCDScanMode                                         = 4153,

		/// <summary>
		///    Noise reduction. (Hex: 0X103A)
		/// </summary>
		NoiseReduction                                      = 4154,

		/// <summary>
		///    Infinity lens step. (Hex: 0X103B)
		/// </summary>
		InfinityLensStep                                    = 4155,

		/// <summary>
		///    Near lens step. (Hex: 0X103C)
		/// </summary>
		NearLensStep                                        = 4156,

		/// <summary>
		///    Camera equipment sub-IFD. (Hex: 0X2010)
		/// </summary>
		Equipment                                           = 8208,

		/// <summary>
		///    Camera Settings sub-IFD. (Hex: 0X2020)
		/// </summary>
		CameraSettings                                      = 8224,

		/// <summary>
		///    Raw development sub-IFD. (Hex: 0X2030)
		/// </summary>
		RawDevelopment                                      = 8240,

		/// <summary>
		///    Raw development 2 sub-IFD. (Hex: 0X2031)
		/// </summary>
		RawDevelopment2                                     = 8241,

		/// <summary>
		///    Image processing sub-IFD. (Hex: 0X2040)
		/// </summary>
		ImageProcessing                                     = 8256,

		/// <summary>
		///    Focus sub-IFD. (Hex: 0X2050)
		/// </summary>
		FocusInfo                                           = 8272,

		/// <summary>
		///    Raw sub-IFD. (Hex: 0X3000)
		/// </summary>
		RawInfo                                             = 12288,
	}
}
