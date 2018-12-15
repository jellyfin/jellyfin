//
// SonyMakerNoteEntryTag.cs:
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
	///    Label tags for Sony Makernote.
	///    Based on http://www.exiv2.org/tags-sony.html
	/// </summary>
	public enum SonyMakerNoteEntryTag : ushort
	{
		/// <summary>
		///    Image quality. (Hex: 0X0102)
		/// </summary>
		Quality                                             = 258,

		/// <summary>
		///    Flash exposure compensation in EV. (Hex: 0X0104)
		/// </summary>
		FlashExposureComp                                   = 260,

		/// <summary>
		///    Teleconverter Model. (Hex: 0X0105)
		/// </summary>
		Teleconverter                                       = 261,

		/// <summary>
		///    White Balance Fine Tune Value. (Hex: 0X0112)
		/// </summary>
		WhiteBalanceFineTune                                = 274,

		/// <summary>
		///    Camera Settings. (Hex: 0X0114)
		/// </summary>
		CameraSettings                                      = 276,

		/// <summary>
		///    White balance. (Hex: 0X0115)
		/// </summary>
		WhiteBalance                                        = 277,

		/// <summary>
		///    PrintIM information. (Hex: 0X0E00)
		/// </summary>
		PrintIM                                             = 3584,

		/// <summary>
		///    Multi Burst Mode. (Hex: 0X1000)
		/// </summary>
		MultiBurstMode                                      = 4096,

		/// <summary>
		///    Multi Burst Image Width. (Hex: 0X1001)
		/// </summary>
		MultiBurstImageWidth                                = 4097,

		/// <summary>
		///    Multi Burst Image Height. (Hex: 0X1002)
		/// </summary>
		MultiBurstImageHeight                               = 4098,

		/// <summary>
		///    Panorama. (Hex: 0X1003)
		/// </summary>
		Panorama                                            = 4099,

		/// <summary>
		///    Preview Image. (Hex: 0X2001)
		/// </summary>
		PreviewImage                                        = 8193,

		/// <summary>
		///    Auto High Definition Range. (Hex: 0X200A)
		/// </summary>
		AutoHDR                                             = 8202,

		/// <summary>
		///    Shot Information. (Hex: 0X3000)
		/// </summary>
		ShotInfo                                            = 12288,

		/// <summary>
		///    File Format. (Hex: 0XB000)
		/// </summary>
		FileFormat                                          = 45056,

		/// <summary>
		///    Sony Model ID. (Hex: 0XB001)
		/// </summary>
		SonyModelID                                         = 45057,

		/// <summary>
		///    Color Reproduction. (Hex: 0XB020)
		/// </summary>
		ColorReproduction                                   = 45088,

		/// <summary>
		///    Color Temperature. (Hex: 0XB021)
		/// </summary>
		ColorTemperature                                    = 45089,

		/// <summary>
		///    Color Compensation Filter: negative is green, positive is magenta. (Hex: 0XB022)
		/// </summary>
		ColorCompensationFilter                             = 45090,

		/// <summary>
		///    Scene Mode. (Hex: 0XB023)
		/// </summary>
		SceneMode                                           = 45091,

		/// <summary>
		///    Zone Matching. (Hex: 0XB024)
		/// </summary>
		ZoneMatching                                        = 45092,

		/// <summary>
		///    Dynamic Range Optimizer. (Hex: 0XB025)
		/// </summary>
		DynamicRangeOptimizer                               = 45093,

		/// <summary>
		///    Image stabilization. (Hex: 0XB026)
		/// </summary>
		ImageStabilization                                  = 45094,

		/// <summary>
		///    Lens identifier. (Hex: 0XB027)
		/// </summary>
		LensID                                              = 45095,

		/// <summary>
		///    Minolta MakerNote. (Hex: 0XB028)
		/// </summary>
		MinoltaMakerNote                                    = 45096,

		/// <summary>
		///    Color Mode. (Hex: 0XB029)
		/// </summary>
		ColorMode                                           = 45097,

		/// <summary>
		///    Full Image Size. (Hex: 0XB02B)
		/// </summary>
		FullImageSize                                       = 45099,

		/// <summary>
		///    Preview Image Size. (Hex: 0XB02C)
		/// </summary>
		PreviewImageSize                                    = 45100,

		/// <summary>
		///    Macro. (Hex: 0XB040)
		/// </summary>
		Macro                                               = 45120,

		/// <summary>
		///    Exposure Mode. (Hex: 0XB041)
		/// </summary>
		ExposureMode                                        = 45121,

		/// <summary>
		///    Focus mode. (Hex: 0XB042)
		/// </summary>
		FocusMode                                           = 45122,

		/// <summary>
		///    AF Mode. (Hex: 0XB043)
		/// </summary>
		AFMode                                              = 45123,

		/// <summary>
		///    AF Illuminator. (Hex: 0XB044)
		/// </summary>
		AFIlluminator                                       = 45124,

		/// <summary>
		///    Quality. (Hex: 0XB047)
		/// </summary>
		Quality2                                            = 45127,

		/// <summary>
		///    Flash Level. (Hex: 0XB048)
		/// </summary>
		FlashLevel                                          = 45128,

		/// <summary>
		///    Release Mode. (Hex: 0XB049)
		/// </summary>
		ReleaseMode                                         = 45129,

		/// <summary>
		///    Shot number in continous burst mode. (Hex: 0XB04A)
		/// </summary>
		SequenceNumber                                      = 45130,

		/// <summary>
		///    Anti-Blur. (Hex: 0XB04B)
		/// </summary>
		AntiBlur                                            = 45131,

		/// <summary>
		///    Long Exposure Noise Reduction. (Hex: 0XB04E)
		/// </summary>
		LongExposureNoiseReduction                          = 45134,

		/// <summary>
		///    Dynamic Range Optimizer. (Hex: 0XB04F)
		/// </summary>
		DynamicRangeOptimizer2                              = 45135,

		/// <summary>
		///    Intelligent Auto. (Hex: 0XB052)
		/// </summary>
		IntelligentAuto                                     = 45138,

		/// <summary>
		///    White Balance. (Hex: 0XB054)
		/// </summary>
		WhiteBalance2                                       = 45140,
	}
}
