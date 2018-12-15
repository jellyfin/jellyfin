//
// Nikon3MakerNoteEntryTag.cs:
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
	///    Nikon format 3 makernote tags.
	///    Based on http://www.exiv2.org/tags-nikon.html and
	///    http://park2.wakwak.com/~tsuruzoh/Computer/Digicams/exif-e.html
	/// </summary>
	public enum Nikon3MakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Makernote version. (Hex: 0x0001)
		/// </summary>
		Version                                             = 1,

		/// <summary>
		///    ISO speed setting. (Hex: 0X0002)
		/// </summary>
		ISOSpeed                                            = 2,

		/// <summary>
		///    Color mode. (Hex: 0X0003)
		/// </summary>
		ColorMode                                           = 3,

		/// <summary>
		///    Image quality setting. (Hex: 0X0004)
		/// </summary>
		Quality                                             = 4,

		/// <summary>
		///    White balance. (Hex: 0X0005)
		/// </summary>
		WhiteBalance                                        = 5,

		/// <summary>
		///    Image sharpening setting. (Hex: 0X0006)
		/// </summary>
		Sharpening                                          = 6,

		/// <summary>
		///    Focus mode. (Hex: 0X0007)
		/// </summary>
		Focus                                               = 7,

		/// <summary>
		///    Flash setting. (Hex: 0X0008)
		/// </summary>
		FlashSetting                                        = 8,

		/// <summary>
		///    Flash device. (Hex: 0X0009)
		/// </summary>
		FlashDevice                                         = 9,

		/// <summary>
		///    Unknown. (Hex: 0X000A)
		/// </summary>
		Unknown10                                           = 10,

		/// <summary>
		///    White balance bias. (Hex: 0X000B)
		/// </summary>
		WhiteBalanceBias                                    = 11,

		/// <summary>
		///    WB RB levels. (Hex: 0X000C)
		/// </summary>
		WB_RBLevels                                         = 12,

		/// <summary>
		///    Program shift. (Hex: 0X000D)
		/// </summary>
		ProgramShift                                        = 13,

		/// <summary>
		///    Exposure difference. (Hex: 0X000E)
		/// </summary>
		ExposureDiff                                        = 14,

		/// <summary>
		///    ISO selection. (Hex: 0X000F)
		/// </summary>
		ISOSelection                                        = 15,

		/// <summary>
		///    Data dump. (Hex: 0X0010)
		/// </summary>
		DataDump                                            = 16,

		/// <summary>
		///    Offset to an IFD containing a preview image. (Hex: 0x0011)
		/// </summary>
		Preview                                             = 17,

		/// <summary>
		///    Flash compensation setting. (Hex: 0X0012)
		/// </summary>
		FlashComp                                           = 18,

		/// <summary>
		///    ISO setting. (Hex: 0X0013)
		/// </summary>
		ISOSettings                                         = 19,

		/// <summary>
		///    Image boundary. (Hex: 0X0016)
		/// </summary>
		ImageBoundary                                       = 22,

		/// <summary>
		///    Unknown. (Hex: 0X0017)
		/// </summary>
		Unknown23                                           = 23,

		/// <summary>
		///    Flash bracket compensation applied. (Hex: 0X0018)
		/// </summary>
		FlashBracketComp                                    = 24,

		/// <summary>
		///    AE bracket compensation applied. (Hex: 0X0019)
		/// </summary>
		ExposureBracketComp                                 = 25,

		/// <summary>
		///    Image processing. (Hex: 0X001A)
		/// </summary>
		ImageProcessing                                     = 26,

		/// <summary>
		///    Crop high speed. (Hex: 0X001B)
		/// </summary>
		CropHiSpeed                                         = 27,

		/// <summary>
		///    Serial Number. (Hex: 0X001D)
		/// </summary>
		SerialNumber                                        = 29,

		/// <summary>
		///    Color space. (Hex: 0X001E)
		/// </summary>
		ColorSpace                                          = 30,

		/// <summary>
		///    VR info. (Hex: 0X001F)
		/// </summary>
		VRInfo                                              = 31,

		/// <summary>
		///    Image authentication. (Hex: 0X0020)
		/// </summary>
		ImageAuthentication                                 = 32,

		/// <summary>
		///    ActiveD-lighting. (Hex: 0X0022)
		/// </summary>
		ActiveDLighting                                     = 34,

		/// <summary>
		///    Picture control. (Hex: 0X0023)
		/// </summary>
		PictureControl                                      = 35,

		/// <summary>
		///    World time. (Hex: 0X0024)
		/// </summary>
		WorldTime                                           = 36,

		/// <summary>
		///    ISO info. (Hex: 0X0025)
		/// </summary>
		ISOInfo                                             = 37,

		/// <summary>
		///    Vignette control. (Hex: 0X002A)
		/// </summary>
		VignetteControl                                     = 42,

		/// <summary>
		///    Image adjustment setting. (Hex: 0X0080)
		/// </summary>
		ImageAdjustment                                     = 128,

		/// <summary>
		///    Tone compensation. (Hex: 0X0081)
		/// </summary>
		ToneComp                                            = 129,

		/// <summary>
		///    Auxiliary lens (adapter). (Hex: 0X0082)
		/// </summary>
		AuxiliaryLens                                       = 130,

		/// <summary>
		///    Lens type. (Hex: 0X0083)
		/// </summary>
		LensType                                            = 131,

		/// <summary>
		///    Lens. (Hex: 0X0084)
		/// </summary>
		Lens                                                = 132,

		/// <summary>
		///    Manual focus distance. (Hex: 0X0085)
		/// </summary>
		FocusDistance                                       = 133,

		/// <summary>
		///    Digital zoom setting. (Hex: 0X0086)
		/// </summary>
		DigitalZoom                                         = 134,

		/// <summary>
		///    Mode of flash used. (Hex: 0X0087)
		/// </summary>
		FlashMode                                           = 135,

		/// <summary>
		///    AF info. (Hex: 0X0088)
		/// </summary>
		AFInfo                                              = 136,

		/// <summary>
		///    Shooting mode. (Hex: 0X0089)
		/// </summary>
		ShootingMode                                        = 137,

		/// <summary>
		///    Auto bracket release. (Hex: 0X008A)
		/// </summary>
		AutoBracketRelease                                  = 138,

		/// <summary>
		///    Lens FStops. (Hex: 0X008B)
		/// </summary>
		LensFStops                                          = 139,

		/// <summary>
		///    Contrast curve. (Hex: 0X008C)
		/// </summary>
		ContrastCurve                                       = 140,

		/// <summary>
		///    Color hue. (Hex: 0X008D)
		/// </summary>
		ColorHue                                            = 141,

		/// <summary>
		///    Scene mode. (Hex: 0X008F)
		/// </summary>
		SceneMode                                           = 143,

		/// <summary>
		///    Light source. (Hex: 0X0090)
		/// </summary>
		LightSource                                         = 144,

		/// <summary>
		///    Shot info. (Hex: 0X0091)
		/// </summary>
		ShotInfo                                            = 145,

		/// <summary>
		///    Hue adjustment. (Hex: 0X0092)
		/// </summary>
		HueAdjustment                                       = 146,

		/// <summary>
		///    NEF compression. (Hex: 0X0093)
		/// </summary>
		NEFCompression                                      = 147,

		/// <summary>
		///    Saturation. (Hex: 0X0094)
		/// </summary>
		Saturation                                          = 148,

		/// <summary>
		///    Noise reduction. (Hex: 0X0095)
		/// </summary>
		NoiseReduction                                      = 149,

		/// <summary>
		///    Linearization table. (Hex: 0X0096)
		/// </summary>
		LinearizationTable                                  = 150,

		/// <summary>
		///    Color balance. (Hex: 0X0097)
		/// </summary>
		ColorBalance                                        = 151,

		/// <summary>
		///    Lens data settings. (Hex: 0X0098)
		/// </summary>
		LensData                                            = 152,

		/// <summary>
		///    Raw image center. (Hex: 0X0099)
		/// </summary>
		RawImageCenter                                      = 153,

		/// <summary>
		///    Sensor pixel size. (Hex: 0X009A)
		/// </summary>
		SensorPixelSize                                     = 154,

		/// <summary>
		///    Unknown. (Hex: 0X009B)
		/// </summary>
		Unknown155                                          = 155,

		/// <summary>
		///    Scene assist. (Hex: 0X009C)
		/// </summary>
		SceneAssist                                         = 156,

		/// <summary>
		///    Retouch history. (Hex: 0X009E)
		/// </summary>
		RetouchHistory                                      = 158,

		/// <summary>
		///    Unknown. (Hex: 0X009F)
		/// </summary>
		Unknown159                                          = 159,

		/// <summary>
		///    Camera serial number, usually starts with "NO= ". (Hex: 0X00A0)
		/// </summary>
		SerialNO                                            = 160,

		/// <summary>
		///    Image data size. (Hex: 0X00A2)
		/// </summary>
		ImageDataSize                                       = 162,

		/// <summary>
		///    Unknown. (Hex: 0X00A3)
		/// </summary>
		Unknown163                                          = 163,

		/// <summary>
		///    Image count. (Hex: 0X00A5)
		/// </summary>
		ImageCount                                          = 165,

		/// <summary>
		///    Deleted image count. (Hex: 0X00A6)
		/// </summary>
		DeletedImageCount                                   = 166,

		/// <summary>
		///    Number of shots taken by camera. (Hex: 0X00A7)
		/// </summary>
		ShutterCount                                        = 167,

		/// <summary>
		///    Flash info. (Hex: 0X00A8)
		/// </summary>
		FlashInfo                                           = 168,

		/// <summary>
		///    Image optimization. (Hex: 0X00A9)
		/// </summary>
		ImageOptimization                                   = 169,

		/// <summary>
		///    Saturation. (Hex: 0X00AA)
		/// </summary>
		Saturation2                                         = 170,

		/// <summary>
		///    Program variation. (Hex: 0X00AB)
		/// </summary>
		VariProgram                                         = 171,

		/// <summary>
		///    Image stabilization. (Hex: 0X00AC)
		/// </summary>
		ImageStabilization                                  = 172,

		/// <summary>
		///    AF response. (Hex: 0X00AD)
		/// </summary>
		AFResponse                                          = 173,

		/// <summary>
		///    Multi exposure. (Hex: 0X00B0)
		/// </summary>
		MultiExposure                                       = 176,

		/// <summary>
		///    High ISO Noise Reduction. (Hex: 0X00B1)
		/// </summary>
		HighISONoiseReduction                               = 177,

		/// <summary>
		///    Toning effect. (Hex: 0X00B3)
		/// </summary>
		ToningEffect                                        = 179,

		/// <summary>
		///    AF info 2. (Hex: 0X00B7)
		/// </summary>
		AFInfo2                                             = 183,

		/// <summary>
		///    File info. (Hex: 0X00B8)
		/// </summary>
		FileInfo                                            = 184,

		/// <summary>
		///    PrintIM information. (Hex: 0X0E00)
		/// </summary>
		PrintIM                                             = 3584,

		/// <summary>
		///    Capture data. (Hex: 0X0E01)
		/// </summary>
		CaptureData                                         = 3585,

		/// <summary>
		///    Capture version. (Hex: 0X0E09)
		/// </summary>
		CaptureVersion                                      = 3593,

		/// <summary>
		///    Capture offsets. (Hex: 0X0E0E)
		/// </summary>
		CaptureOffsets                                      = 3598,

		/// <summary>
		///    Scan IFD. (Hex: 0X0E10)
		/// </summary>
		ScanIFD                                             = 3600,

		/// <summary>
		///    ICC profile. (Hex: 0X0E1D)
		/// </summary>
		ICCProfile                                          = 3613,

		/// <summary>
		///    Capture output. (Hex: 0X0E1E)
		/// </summary>
		CaptureOutput                                       = 3614,
	}
}
