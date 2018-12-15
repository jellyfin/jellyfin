//
// PentaxMakerNoteEntryTag.cs:
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
	///    Pentax makernote tags.
	///    Based on http://www.exiv2.org/tags-pentax.html
	/// </summary>
	public enum PentaxMakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Pentax Makernote version. (Hex: 0X0000)
		/// </summary>
		Version                                             = 0,

		/// <summary>
		///    Camera shooting mode. (Hex: 0X0001)
		/// </summary>
		Mode                                                = 1,

		/// <summary>
		///    Resolution of a preview image. (Hex: 0X0002)
		/// </summary>
		PreviewResolution                                   = 2,

		/// <summary>
		///    Size of an IFD containing a preview image. (Hex: 0X0003)
		/// </summary>
		PreviewLength                                       = 3,

		/// <summary>
		///    Offset to an IFD containing a preview image. (Hex: 0X0004)
		/// </summary>
		PreviewOffset                                       = 4,

		/// <summary>
		///    Pentax model idenfication. (Hex: 0X0005)
		/// </summary>
		ModelID                                             = 5,

		/// <summary>
		///    Date. (Hex: 0X0006)
		/// </summary>
		Date                                                = 6,

		/// <summary>
		///    Time. (Hex: 0X0007)
		/// </summary>
		Time                                                = 7,

		/// <summary>
		///    Image quality settings. (Hex: 0X0008)
		/// </summary>
		Quality                                             = 8,

		/// <summary>
		///    Image size settings. (Hex: 0X0009)
		/// </summary>
		Size                                                = 9,

		/// <summary>
		///    Flash mode settings. (Hex: 0X000C)
		/// </summary>
		Flash                                               = 12,

		/// <summary>
		///    Focus mode settings. (Hex: 0X000D)
		/// </summary>
		Focus                                               = 13,

		/// <summary>
		///    Selected AF point. (Hex: 0X000E)
		/// </summary>
		AFPoint                                             = 14,

		/// <summary>
		///    AF point in focus. (Hex: 0X000F)
		/// </summary>
		AFPointInFocus                                      = 15,

		/// <summary>
		///    Exposure time. (Hex: 0X0012)
		/// </summary>
		ExposureTime                                        = 18,

		/// <summary>
		///    F-Number. (Hex: 0X0013)
		/// </summary>
		FNumber                                             = 19,

		/// <summary>
		///    ISO sensitivity settings. (Hex: 0X0014)
		/// </summary>
		ISO                                                 = 20,

		/// <summary>
		///    Exposure compensation. (Hex: 0X0016)
		/// </summary>
		ExposureCompensation                                = 22,

		/// <summary>
		///    MeteringMode. (Hex: 0X0017)
		/// </summary>
		MeteringMode                                        = 23,

		/// <summary>
		///    AutoBracketing. (Hex: 0X0018)
		/// </summary>
		AutoBracketing                                      = 24,

		/// <summary>
		///    White ballance. (Hex: 0X0019)
		/// </summary>
		WhiteBallance                                       = 25,

		/// <summary>
		///    White ballance mode. (Hex: 0X001A)
		/// </summary>
		WhiteBallanceMode                                   = 26,

		/// <summary>
		///    Blue color balance. (Hex: 0X001B)
		/// </summary>
		BlueBalance                                         = 27,

		/// <summary>
		///    Red color balance. (Hex: 0X001C)
		/// </summary>
		RedBalance                                          = 28,

		/// <summary>
		///    FocalLength. (Hex: 0X001D)
		/// </summary>
		FocalLength                                         = 29,

		/// <summary>
		///    Digital zoom. (Hex: 0X001E)
		/// </summary>
		DigitalZoom                                         = 30,

		/// <summary>
		///    Saturation. (Hex: 0X001F)
		/// </summary>
		Saturation                                          = 31,

		/// <summary>
		///    Contrast. (Hex: 0X0020)
		/// </summary>
		Contrast                                            = 32,

		/// <summary>
		///    Sharpness. (Hex: 0X0021)
		/// </summary>
		Sharpness                                           = 33,

		/// <summary>
		///    Location. (Hex: 0X0022)
		/// </summary>
		Location                                            = 34,

		/// <summary>
		///    Home town. (Hex: 0X0023)
		/// </summary>
		Hometown                                            = 35,

		/// <summary>
		///    Destination. (Hex: 0X0024)
		/// </summary>
		Destination                                         = 36,

		/// <summary>
		///    Whether day saving time is active in home town. (Hex: 0X0025)
		/// </summary>
		HometownDST                                         = 37,

		/// <summary>
		///    Whether day saving time is active in destination. (Hex: 0X0026)
		/// </summary>
		DestinationDST                                      = 38,

		/// <summary>
		///    DSPFirmwareVersion. (Hex: 0X0027)
		/// </summary>
		DSPFirmwareVersion                                  = 39,

		/// <summary>
		///    CPUFirmwareVersion. (Hex: 0X0028)
		/// </summary>
		CPUFirmwareVersion                                  = 40,

		/// <summary>
		///    Frame number. (Hex: 0X0029)
		/// </summary>
		FrameNumber                                         = 41,

		/// <summary>
		///    Camera calculated light value, includes exposure compensation. (Hex: 0X002D)
		/// </summary>
		EffectiveLV                                         = 45,

		/// <summary>
		///    Image processing. (Hex: 0X0032)
		/// </summary>
		ImageProcessing                                     = 50,

		/// <summary>
		///    Picture mode. (Hex: 0X0033)
		/// </summary>
		PictureMode                                         = 51,

		/// <summary>
		///    Drive mode. (Hex: 0X0034)
		/// </summary>
		DriveMode                                           = 52,

		/// <summary>
		///    Color space. (Hex: 0X0037)
		/// </summary>
		ColorSpace                                          = 55,

		/// <summary>
		///    Image area offset. (Hex: 0X0038)
		/// </summary>
		ImageAreaOffset                                     = 56,

		/// <summary>
		///    Raw image size. (Hex: 0X0039)
		/// </summary>
		RawImageSize                                        = 57,

		/// <summary>
		///    Preview image borders. (Hex: 0X003E)
		/// </summary>
		PreviewImageBorders                                 = 62,

		/// <summary>
		///    Lens type. (Hex: 0X003F)
		/// </summary>
		LensType                                            = 63,

		/// <summary>
		///    Sensitivity adjust. (Hex: 0X0040)
		/// </summary>
		SensitivityAdjust                                   = 64,

		/// <summary>
		///    Digital filter. (Hex: 0X0041)
		/// </summary>
		DigitalFilter                                       = 65,

		/// <summary>
		///    Camera temperature. (Hex: 0X0047)
		/// </summary>
		Temperature                                         = 71,

		/// <summary>
		///    AE lock. (Hex: 0X0048)
		/// </summary>
		AELock                                              = 72,

		/// <summary>
		///    Noise reduction. (Hex: 0X0049)
		/// </summary>
		NoiseReduction                                      = 73,

		/// <summary>
		///    Flash exposure compensation. (Hex: 0X004D)
		/// </summary>
		FlashExposureCompensation                           = 77,

		/// <summary>
		///    Image tone. (Hex: 0X004F)
		/// </summary>
		ImageTone                                           = 79,

		/// <summary>
		///    Colort temperature. (Hex: 0X0050)
		/// </summary>
		ColorTemperature                                    = 80,

		/// <summary>
		///    Shake reduction information. (Hex: 0X005C)
		/// </summary>
		ShakeReduction                                      = 92,

		/// <summary>
		///    Shutter count. (Hex: 0X005D)
		/// </summary>
		ShutterCount                                        = 93,

		/// <summary>
		///    Dynamic range expansion. (Hex: 0X0069)
		/// </summary>
		DynamicRangeExpansion                               = 105,

		/// <summary>
		///    High ISO noise reduction. (Hex: 0X0071)
		/// </summary>
		HighISONoiseReduction                               = 113,

		/// <summary>
		///    AF Adjustment. (Hex: 0X0072)
		/// </summary>
		AFAdjustment                                        = 114,

		/// <summary>
		///    Black point. (Hex: 0X0200)
		/// </summary>
		BlackPoint                                          = 512,

		/// <summary>
		///    White point. (Hex: 0X0201)
		/// </summary>
		WhitePoint                                          = 513,

		/// <summary>
		///    ShotInfo. (Hex: 0X0205)
		/// </summary>
		ShotInfo                                            = 517,

		/// <summary>
		///    AEInfo. (Hex: 0X0206)
		/// </summary>
		AEInfo                                              = 518,

		/// <summary>
		///    LensInfo. (Hex: 0X0207)
		/// </summary>
		LensInfo                                            = 519,

		/// <summary>
		///    FlashInfo. (Hex: 0X0208)
		/// </summary>
		FlashInfo                                           = 520,

		/// <summary>
		///    AEMeteringSegments. (Hex: 0X0209)
		/// </summary>
		AEMeteringSegments                                  = 521,

		/// <summary>
		///    FlashADump. (Hex: 0X020A)
		/// </summary>
		FlashADump                                          = 522,

		/// <summary>
		///    FlashBDump. (Hex: 0X020B)
		/// </summary>
		FlashBDump                                          = 523,

		/// <summary>
		///    WB_RGGBLevelsDaylight. (Hex: 0X020D)
		/// </summary>
		WB_RGGBLevelsDaylight                               = 525,

		/// <summary>
		///    WB_RGGBLevelsShade. (Hex: 0X020E)
		/// </summary>
		WB_RGGBLevelsShade                                  = 526,

		/// <summary>
		///    WB_RGGBLevelsCloudy. (Hex: 0X020F)
		/// </summary>
		WB_RGGBLevelsCloudy                                 = 527,

		/// <summary>
		///    WB_RGGBLevelsTungsten. (Hex: 0X0210)
		/// </summary>
		WB_RGGBLevelsTungsten                               = 528,

		/// <summary>
		///    WB_RGGBLevelsFluorescentD. (Hex: 0X0211)
		/// </summary>
		WB_RGGBLevelsFluorescentD                           = 529,

		/// <summary>
		///    WB_RGGBLevelsFluorescentN. (Hex: 0X0212)
		/// </summary>
		WB_RGGBLevelsFluorescentN                           = 530,

		/// <summary>
		///    WB_RGGBLevelsFluorescentW. (Hex: 0X0213)
		/// </summary>
		WB_RGGBLevelsFluorescentW                           = 531,

		/// <summary>
		///    WB_RGGBLevelsFlash. (Hex: 0X0214)
		/// </summary>
		WB_RGGBLevelsFlash                                  = 532,

		/// <summary>
		///    CameraInfo. (Hex: 0X0215)
		/// </summary>
		CameraInfo                                          = 533,

		/// <summary>
		///    BatteryInfo. (Hex: 0X0216)
		/// </summary>
		BatteryInfo                                         = 534,

		/// <summary>
		///    AFInfo. (Hex: 0X021F)
		/// </summary>
		AFInfo                                              = 543,

		/// <summary>
		///    ColorInfo. (Hex: 0X0222)
		/// </summary>
		ColorInfo                                           = 546,

		/// <summary>
		///    Serial Number. (Hex: 0X0229)
		/// </summary>
		SerialNumber                                        = 553,
	}
}
