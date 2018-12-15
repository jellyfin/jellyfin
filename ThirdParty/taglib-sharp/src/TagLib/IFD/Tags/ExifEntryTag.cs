//
// ExifEntryTag.cs:
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
	///    Entry tags occuring in the Exif IFD
	///    The complete overview can be obtained at:
	///    http://www.awaresystems.be/imaging/tiff.html
	/// </summary>
	public enum ExifEntryTag : ushort
	{
		/// <summary>
		///     Contains two values representing the minimum rows and columns
		///     to define the repeating patterns of the color filter array.
		///     (Hex: 0x828D)
		/// </summary>
		CFARepeatPatternDim                                = 33421,

		/// <summary>
		///     Contains two values representing the minimum rows and columns
		///     to define the repeating patterns of the color filter array.
		///     (Hex: 0x828E)
		/// </summary>
		CFAPattern                                         = 33422,

		/// <summary>
		///     Exposure time, given in seconds. (Hex: 0x829A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposuretime.html
		/// </summary>
		ExposureTime                                       = 33434,

		/// <summary>
		///     The F number. (Hex: 0x829D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/fnumber.html
		/// </summary>
		FNumber                                            = 33437,

		/// <summary>
		///     The class of the program used by the camera to set exposure when the picture is taken. (Hex: 0x8822)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposureprogram.html
		/// </summary>
		ExposureProgram                                    = 34850,

		/// <summary>
		///     Indicates the spectral sensitivity of each channel of the camera used. (Hex: 0x8824)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/spectralsensitivity.html
		/// </summary>
		SpectralSensitivity                                = 34852,

		/// <summary>
		///     Indicates the ISO Speed and ISO Latitude of the camera or input device as specified in ISO 12232. (Hex: 0x8827)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/isospeedratings.html
		/// </summary>
		ISOSpeedRatings                                    = 34855,

		/// <summary>
		///     Indicates the Opto-Electric Conversion Function (OECF) specified in ISO 14524. (Hex: 0x8828)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/oecf.html
		/// </summary>
		OECF                                               = 34856,

		/// <summary>
		///     The version of the supported Exif standard. (Hex: 0x9000)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exifversion.html
		/// </summary>
		ExifVersion                                        = 36864,

		/// <summary>
		///     The date and time when the original image data was generated. (Hex: 0x9003)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/datetimeoriginal.html
		/// </summary>
		DateTimeOriginal                                   = 36867,

		/// <summary>
		///     The date and time when the image was stored as digital data. (Hex: 0x9004)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/datetimedigitized.html
		/// </summary>
		DateTimeDigitized                                  = 36868,

		/// <summary>
		///     Specific to compressed data; specifies the channels and complements PhotometricInterpretation (Hex: 0x9101)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/componentsconfiguration.html
		/// </summary>
		ComponentsConfiguration                            = 37121,

		/// <summary>
		///     Specific to compressed data; states the compressed bits per pixel. (Hex: 0x9102)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/compressedbitsperpixel.html
		/// </summary>
		CompressedBitsPerPixel                             = 37122,

		/// <summary>
		///     Shutter speed. (Hex: 0x9201)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/shutterspeedvalue.html
		/// </summary>
		ShutterSpeedValue                                  = 37377,

		/// <summary>
		///     The lens aperture. (Hex: 0x9202)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/aperturevalue.html
		/// </summary>
		ApertureValue                                      = 37378,

		/// <summary>
		///     The value of brightness. (Hex: 0x9203)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/brightnessvalue.html
		/// </summary>
		BrightnessValue                                    = 37379,

		/// <summary>
		///     The exposure bias. (Hex: 0x9204)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposurebiasvalue.html
		/// </summary>
		ExposureBiasValue                                  = 37380,

		/// <summary>
		///     The smallest F number of the lens. (Hex: 0x9205)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/maxaperturevalue.html
		/// </summary>
		MaxApertureValue                                   = 37381,

		/// <summary>
		///     The distance to the subject, given in meters. (Hex: 0x9206)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectdistance.html
		/// </summary>
		SubjectDistance                                    = 37382,

		/// <summary>
		///     The metering mode. (Hex: 0x9207)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/meteringmode.html
		/// </summary>
		MeteringMode                                       = 37383,

		/// <summary>
		///     The kind of light source. (Hex: 0x9208)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/lightsource.html
		/// </summary>
		LightSource                                        = 37384,

		/// <summary>
		///     Indicates the status of flash when the image was shot. (Hex: 0x9209)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/flash.html
		/// </summary>
		Flash                                              = 37385,

		/// <summary>
		///     The actual focal length of the lens, in mm. (Hex: 0x920A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/focallength.html
		/// </summary>
		FocalLength                                        = 37386,

		/// <summary>
		///     Indicates the location and area of the main subject in the overall scene. (Hex: 0x9214)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectarea.html
		/// </summary>
		SubjectArea                                        = 37396,

		/// <summary>
		///     Manufacturer specific information. (Hex: 0x927C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/makernote.html
		/// </summary>
		MakerNote                                          = 37500,

		/// <summary>
		///     Keywords or comments on the image; complements ImageDescription. (Hex: 0x9286)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/usercomment.html
		/// </summary>
		UserComment                                        = 37510,

		/// <summary>
		///     A tag used to record fractions of seconds for the DateTime tag. (Hex: 0x9290)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subsectime.html
		/// </summary>
		SubsecTime                                         = 37520,

		/// <summary>
		///     A tag used to record fractions of seconds for the DateTimeOriginal tag. (Hex: 0x9291)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subsectimeoriginal.html
		/// </summary>
		SubsecTimeOriginal                                 = 37521,

		/// <summary>
		///     A tag used to record fractions of seconds for the DateTimeDigitized tag. (Hex: 0x9292)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subsectimedigitized.html
		/// </summary>
		SubsecTimeDigitized                                = 37522,

		/// <summary>
		///     The Flashpix format version supported by a FPXR file. (Hex: 0xA000)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/flashpixversion.html
		/// </summary>
		FlashpixVersion                                    = 40960,

		/// <summary>
		///     The color space information tag is always recorded as the color space specifier. (Hex: 0xA001)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/colorspace.html
		/// </summary>
		ColorSpace                                         = 40961,

		/// <summary>
		///     Specific to compressed data; the valid width of the meaningful image. (Hex: 0xA002)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/pixelxdimension.html
		/// </summary>
		PixelXDimension                                    = 40962,

		/// <summary>
		///     Specific to compressed data; the valid height of the meaningful image. (Hex: 0xA003)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/pixelydimension.html
		/// </summary>
		PixelYDimension                                    = 40963,

		/// <summary>
		///     Used to record the name of an audio file related to the image data. (Hex: 0xA004)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/relatedsoundfile.html
		/// </summary>
		RelatedSoundFile                                   = 40964,

		/// <summary>
		///     Indicates the strobe energy at the time the image is captured, as measured in Beam Candle Power Seconds (Hex: 0xA20B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/flashenergy.html
		/// </summary>
		FlashEnergy                                        = 41483,

		/// <summary>
		///     Records the camera or input device spatial frequency table and SFR values in the direction of image width, image height, and diagonal direction, as specified in ISO 12233. (Hex: 0xA20C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/spatialfrequencyresponse.html
		/// </summary>
		SpatialFrequencyResponse                           = 41484,

		/// <summary>
		///     Indicates the number of pixels in the image width (X) direction per FocalPlaneResolutionUnit on the camera focal plane. (Hex: 0xA20E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/focalplanexresolution.html
		/// </summary>
		FocalPlaneXResolution                              = 41486,

		/// <summary>
		///     Indicates the number of pixels in the image height (Y) direction per FocalPlaneResolutionUnit on the camera focal plane. (Hex: 0xA20F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/focalplaneyresolution.html
		/// </summary>
		FocalPlaneYResolution                              = 41487,

		/// <summary>
		///     Indicates the unit for measuring FocalPlaneXResolution and FocalPlaneYResolution. (Hex: 0xA210)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/focalplaneresolutionunit.html
		/// </summary>
		FocalPlaneResolutionUnit                           = 41488,

		/// <summary>
		///     Indicates the location of the main subject in the scene. (Hex: 0xA214)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectlocation.html
		/// </summary>
		SubjectLocation                                    = 41492,

		/// <summary>
		///     Indicates the exposure index selected on the camera or input device at the time the image is captured. (Hex: 0xA215)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposureindex.html
		/// </summary>
		ExposureIndex                                      = 41493,

		/// <summary>
		///     Indicates the image sensor type on the camera or input device. (Hex: 0xA217)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/sensingmethod.html
		/// </summary>
		SensingMethod                                      = 41495,

		/// <summary>
		///     Indicates the image source. (Hex: 0xA300)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/filesource.html
		/// </summary>
		FileSource                                         = 41728,

		/// <summary>
		///     Indicates the type of scene. (Hex: 0xA301)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/scenetype.html
		/// </summary>
		SceneType                                          = 41729,

		/// <summary>
		///     Indicates the color filter array (CFA) geometric pattern of the image sensor when a one-chip color area sensor is used. (Hex: 0xA302)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/cfapattern.html
		/// </summary>
		CFAPattern2                                        = 41730,

		/// <summary>
		///     Indicates the use of special processing on image data, such as rendering geared to output. (Hex: 0xA401)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/customrendered.html
		/// </summary>
		CustomRendered                                     = 41985,

		/// <summary>
		///     Indicates the exposure mode set when the image was shot. (Hex: 0xA402)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposuremode.html
		/// </summary>
		ExposureMode                                       = 41986,

		/// <summary>
		///     Indicates the white balance mode set when the image was shot. (Hex: 0xA403)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/whitebalance.html
		/// </summary>
		WhiteBalance                                       = 41987,

		/// <summary>
		///     Indicates the digital zoom ratio when the image was shot. (Hex: 0xA404)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/digitalzoomratio.html
		/// </summary>
		DigitalZoomRatio                                   = 41988,

		/// <summary>
		///     Indicates the equivalent focal length assuming a 35mm film camera, in mm. (Hex: 0xA405)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/focallengthin35mmfilm.html
		/// </summary>
		FocalLengthIn35mmFilm                              = 41989,

		/// <summary>
		///     Indicates the type of scene that was shot. (Hex: 0xA406)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/scenecapturetype.html
		/// </summary>
		SceneCaptureType                                   = 41990,

		/// <summary>
		///     Indicates the degree of overall image gain adjustment. (Hex: 0xA407)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/gaincontrol.html
		/// </summary>
		GainControl                                        = 41991,

		/// <summary>
		///     Indicates the direction of contrast processing applied by the camera when the image was shot. (Hex: 0xA408)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/contrast.html
		/// </summary>
		Contrast                                           = 41992,

		/// <summary>
		///     Indicates the direction of saturation processing applied by the camera when the image was shot. (Hex: 0xA409)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/saturation.html
		/// </summary>
		Saturation                                         = 41993,

		/// <summary>
		///     Indicates the direction of sharpness processing applied by the camera when the image was shot. (Hex: 0xA40A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/sharpness.html
		/// </summary>
		Sharpness                                          = 41994,

		/// <summary>
		///     This tag indicates information on the picture-taking conditions of a particular camera model. (Hex: 0xA40B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/devicesettingdescription.html
		/// </summary>
		DeviceSettingDescription                           = 41995,

		/// <summary>
		///     Indicates the distance to the subject. (Hex: 0xA40C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/subjectdistancerange.html
		/// </summary>
		SubjectDistanceRange                               = 41996,

		/// <summary>
		///     Indicates an identifier assigned uniquely to each image. (Hex: 0xA420)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/imageuniqueid.html
		/// </summary>
		ImageUniqueID                                      = 42016,
	}
}
