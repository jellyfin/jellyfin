//
// IFDEntryTag.cs:
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
	///    Entry tags occuring in a Tiff IFD, or IFD0 for Jpegs. They are mostly
	///    defined by the TIFF specification:
	///    http://partners.adobe.com/public/developer/en/tiff/TIFF6.pdf
	///    The complete overview can be obtained at:
	///    http://www.awaresystems.be/imaging/tiff.html
	/// </summary>
	public enum IFDEntryTag : ushort
	{

		/// <summary>
		///     A general indication of the kind of data contained in this subfile. (Hex: 0x00FE)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/newsubfiletype.html
		/// </summary>
		NewSubfileType                                     = 254,

		/// <summary>
		///     A general indication of the kind of data contained in this subfile. (Hex: 0x00FF)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/subfiletype.html
		/// </summary>
		SubfileType                                        = 255,

		/// <summary>
		///     The number of columns in the image, i.e., the number of pixels per row. (Hex: 0x0100)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imagewidth.html
		/// </summary>
		ImageWidth                                         = 256,

		/// <summary>
		///     The number of rows of pixels in the image. (Hex: 0x0101)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imagelength.html
		/// </summary>
		ImageLength                                        = 257,

		/// <summary>
		///     Number of bits per component. (Hex: 0x0102)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/bitspersample.html
		/// </summary>
		BitsPerSample                                      = 258,

		/// <summary>
		///     Compression scheme used on the image data. (Hex: 0x0103)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/compression.html
		/// </summary>
		Compression                                        = 259,

		/// <summary>
		///     The color space of the image data. (Hex: 0x0106)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/photometricinterpretation.html
		/// </summary>
		PhotometricInterpretation                          = 262,

		/// <summary>
		///     For black and white TIFF files that represent shades of gray, the technique used to convert from gray to black and white pixels. (Hex: 0x0107)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/threshholding.html
		/// </summary>
		Threshholding                                      = 263,

		/// <summary>
		///     The width of the dithering or halftoning matrix used to create a dithered or halftoned bilevel file. (Hex: 0x0108)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cellwidth.html
		/// </summary>
		CellWidth                                          = 264,

		/// <summary>
		///     The length of the dithering or halftoning matrix used to create a dithered or halftoned bilevel file. (Hex: 0x0109)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/celllength.html
		/// </summary>
		CellLength                                         = 265,

		/// <summary>
		///     The logical order of bits within a byte. (Hex: 0x010A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/fillorder.html
		/// </summary>
		FillOrder                                          = 266,

		/// <summary>
		///     The name of the document from which this image was scanned. (Hex: 0x010D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/documentname.html
		/// </summary>
		DocumentName                                       = 269,

		/// <summary>
		///     A string that describes the subject of the image. (Hex: 0x010E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imagedescription.html
		/// </summary>
		ImageDescription                                   = 270,

		/// <summary>
		///     The scanner manufacturer. (Hex: 0x010F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/make.html
		/// </summary>
		Make                                               = 271,

		/// <summary>
		///     The scanner model name or number. (Hex: 0x0110)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/model.html
		/// </summary>
		Model                                              = 272,

		/// <summary>
		///     For each strip, the byte offset of that strip. (Hex: 0x0111)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/stripoffsets.html
		/// </summary>
		StripOffsets                                       = 273,

		/// <summary>
		///     The orientation of the image with respect to the rows and columns. (Hex: 0x0112)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/orientation.html
		/// </summary>
		Orientation                                        = 274,

		/// <summary>
		///     The number of components per pixel. (Hex: 0x0115)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/samplesperpixel.html
		/// </summary>
		SamplesPerPixel                                    = 277,

		/// <summary>
		///     The number of rows per strip. (Hex: 0x0116)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/rowsperstrip.html
		/// </summary>
		RowsPerStrip                                       = 278,

		/// <summary>
		///     For each strip, the number of bytes in the strip after compression. (Hex: 0x0117)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/stripbytecounts.html
		/// </summary>
		StripByteCounts                                    = 279,

		/// <summary>
		///     The minimum component value used. (Hex: 0x0118)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/minsamplevalue.html
		/// </summary>
		MinSampleValue                                     = 280,

		/// <summary>
		///     The maximum component value used. (Hex: 0x0119)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/maxsamplevalue.html
		/// </summary>
		MaxSampleValue                                     = 281,

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
		///     How the components of each pixel are stored. (Hex: 0x011C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/planarconfiguration.html
		/// </summary>
		PlanarConfiguration                                = 284,

		/// <summary>
		///     The name of the page from which this image was scanned. (Hex: 0x011D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/pagename.html
		/// </summary>
		PageName                                           = 285,

		/// <summary>
		///     X position of the image. (Hex: 0x011E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/xposition.html
		/// </summary>
		XPosition                                          = 286,

		/// <summary>
		///     Y position of the image. (Hex: 0x011F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/yposition.html
		/// </summary>
		YPosition                                          = 287,

		/// <summary>
		///     For each string of contiguous unused bytes in a TIFF file, the byte offset of the string. (Hex: 0x0120)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/freeoffsets.html
		/// </summary>
		FreeOffsets                                        = 288,

		/// <summary>
		///     For each string of contiguous unused bytes in a TIFF file, the number of bytes in the string. (Hex: 0x0121)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/freebytecounts.html
		/// </summary>
		FreeByteCounts                                     = 289,

		/// <summary>
		///     The precision of the information contained in the GrayResponseCurve. (Hex: 0x0122)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/grayresponseunit.html
		/// </summary>
		GrayResponseUnit                                   = 290,

		/// <summary>
		///     For grayscale data, the optical density of each possible pixel value. (Hex: 0x0123)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/grayresponsecurve.html
		/// </summary>
		GrayResponseCurve                                  = 291,

		/// <summary>
		///     Options for Group 3 Fax compression (Hex: 0x0124)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/t4options.html
		/// </summary>
		T4Options                                          = 292,

		/// <summary>
		///     Options for Group 4 Fax compression (Hex: 0x0125)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/t6options.html
		/// </summary>
		T6Options                                          = 293,

		/// <summary>
		///     The unit of measurement for XResolution and YResolution. (Hex: 0x0128)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/resolutionunit.html
		/// </summary>
		ResolutionUnit                                     = 296,

		/// <summary>
		///     The page number of the page from which this image was scanned. (Hex: 0x0129)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/pagenumber.html
		/// </summary>
		PageNumber                                         = 297,

		/// <summary>
		///     Describes a transfer function for the image in tabular style. (Hex: 0x012D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/transferfunction.html
		/// </summary>
		TransferFunction                                   = 301,

		/// <summary>
		///     Name and version number of the software package(s) used to create the image. (Hex: 0x0131)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/software.html
		/// </summary>
		Software                                           = 305,

		/// <summary>
		///     Date and time of image creation. (Hex: 0x0132)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/datetime.html
		/// </summary>
		DateTime                                           = 306,

		/// <summary>
		///     Person who created the image. (Hex: 0x013B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/artist.html
		/// </summary>
		Artist                                             = 315,

		/// <summary>
		///     The computer and/or operating system in use at the time of image creation. (Hex: 0x013C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/hostcomputer.html
		/// </summary>
		HostComputer                                       = 316,

		/// <summary>
		///     A mathematical operator that is applied to the image data before an encoding scheme is applied. (Hex: 0x013D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/predictor.html
		/// </summary>
		Predictor                                          = 317,

		/// <summary>
		///     The chromaticity of the white point of the image. (Hex: 0x013E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/whitepoint.html
		/// </summary>
		WhitePoint                                         = 318,

		/// <summary>
		///     The chromaticities of the primaries of the image. (Hex: 0x013F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/primarychromaticities.html
		/// </summary>
		PrimaryChromaticities                              = 319,

		/// <summary>
		///     A color map for palette color images. (Hex: 0x0140)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/colormap.html
		/// </summary>
		ColorMap                                           = 320,

		/// <summary>
		///     Conveys to the halftone function the range of gray levels within a colorimetrically-specified image that should retain tonal detail. (Hex: 0x0141)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/halftonehints.html
		/// </summary>
		HalftoneHints                                      = 321,

		/// <summary>
		///     The tile width in pixels. This is the number of columns in each tile. (Hex: 0x0142)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/tilewidth.html
		/// </summary>
		TileWidth                                          = 322,

		/// <summary>
		///     The tile length (height) in pixels. This is the number of rows in each tile. (Hex: 0x0143)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/tilelength.html
		/// </summary>
		TileLength                                         = 323,

		/// <summary>
		///     For each tile, the byte offset of that tile, as compressed and stored on disk. (Hex: 0x0144)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/tileoffsets.html
		/// </summary>
		TileOffsets                                        = 324,

		/// <summary>
		///     For each tile, the number of (compressed) bytes in that tile. (Hex: 0x0145)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/tilebytecounts.html
		/// </summary>
		TileByteCounts                                     = 325,

		/// <summary>
		///     Used in the TIFF-F standard, denotes the number of 'bad' scan lines encountered by the facsimile device. (Hex: 0x0146)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/badfaxlines.html
		/// </summary>
		BadFaxLines                                        = 326,

		/// <summary>
		///     Used in the TIFF-F standard, indicates if 'bad' lines encountered during reception are stored in the data, or if 'bad' lines have been replaced by the receiver. (Hex: 0x0147)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cleanfaxdata.html
		/// </summary>
		CleanFaxData                                       = 327,

		/// <summary>
		///     Used in the TIFF-F standard, denotes the maximum number of consecutive 'bad' scanlines received. (Hex: 0x0148)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/consecutivebadfaxlines.html
		/// </summary>
		ConsecutiveBadFaxLines                             = 328,

		/// <summary>
		///     Offset to child IFDs. (Hex: 0x014A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/subifds.html
		/// </summary>
		SubIFDs                                            = 330,

		/// <summary>
		///     The set of inks used in a separated (PhotometricInterpretation=5) image. (Hex: 0x014C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/inkset.html
		/// </summary>
		InkSet                                             = 332,

		/// <summary>
		///     The name of each ink used in a separated image. (Hex: 0x014D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/inknames.html
		/// </summary>
		InkNames                                           = 333,

		/// <summary>
		///     The number of inks. (Hex: 0x014E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/numberofinks.html
		/// </summary>
		NumberOfInks                                       = 334,

		/// <summary>
		///     The component values that correspond to a 0% dot and 100% dot. (Hex: 0x0150)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/dotrange.html
		/// </summary>
		DotRange                                           = 336,

		/// <summary>
		///     A description of the printing environment for which this separation is intended. (Hex: 0x0151)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/targetprinter.html
		/// </summary>
		TargetPrinter                                      = 337,

		/// <summary>
		///     Description of extra components. (Hex: 0x0152)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/extrasamples.html
		/// </summary>
		ExtraSamples                                       = 338,

		/// <summary>
		///     Specifies how to interpret each data sample in a pixel. (Hex: 0x0153)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/sampleformat.html
		/// </summary>
		SampleFormat                                       = 339,

		/// <summary>
		///     Specifies the minimum sample value. (Hex: 0x0154)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/sminsamplevalue.html
		/// </summary>
		SMinSampleValue                                    = 340,

		/// <summary>
		///     Specifies the maximum sample value. (Hex: 0x0155)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/smaxsamplevalue.html
		/// </summary>
		SMaxSampleValue                                    = 341,

		/// <summary>
		///     Expands the range of the TransferFunction. (Hex: 0x0156)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/transferrange.html
		/// </summary>
		TransferRange                                      = 342,

		/// <summary>
		///     Mirrors the essentials of PostScript's path creation functionality. (Hex: 0x0157)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/clippath.html
		/// </summary>
		ClipPath                                           = 343,

		/// <summary>
		///     The number of units that span the width of the image, in terms of integer ClipPath coordinates. (Hex: 0x0158)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/xclippathunits.html
		/// </summary>
		XClipPathUnits                                     = 344,

		/// <summary>
		///     The number of units that span the height of the image, in terms of integer ClipPath coordinates. (Hex: 0x0159)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/yclippathunits.html
		/// </summary>
		YClipPathUnits                                     = 345,

		/// <summary>
		///     Aims to broaden the support for indexed images to include support for any color space. (Hex: 0x015A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/indexed.html
		/// </summary>
		Indexed                                            = 346,

		/// <summary>
		///     JPEG quantization and/or Huffman tables. (Hex: 0x015B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegtables.html
		/// </summary>
		JPEGTables                                         = 347,

		/// <summary>
		///     OPI-related. (Hex: 0x015F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/opiproxy.html
		/// </summary>
		OPIProxy                                           = 351,

		/// <summary>
		///     Used in the TIFF-FX standard to point to an IFD containing tags that are globally applicable to the complete TIFF file. (Hex: 0x0190)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/globalparametersifd.html
		/// </summary>
		GlobalParametersIFD                                = 400,

		/// <summary>
		///     Used in the TIFF-FX standard, denotes the type of data stored in this file or IFD. (Hex: 0x0191)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/profiletype.html
		/// </summary>
		ProfileType                                        = 401,

		/// <summary>
		///     Used in the TIFF-FX standard, denotes the 'profile' that applies to this file. (Hex: 0x0192)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/faxprofile.html
		/// </summary>
		FaxProfile                                         = 402,

		/// <summary>
		///     Used in the TIFF-FX standard, indicates which coding methods are used in the file. (Hex: 0x0193)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/codingmethods.html
		/// </summary>
		CodingMethods                                      = 403,

		/// <summary>
		///     Used in the TIFF-FX standard, denotes the year of the standard specified by the FaxProfile field. (Hex: 0x0194)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/versionyear.html
		/// </summary>
		VersionYear                                        = 404,

		/// <summary>
		///     Used in the TIFF-FX standard, denotes the mode of the standard specified by the FaxProfile field. (Hex: 0x0195)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/modenumber.html
		/// </summary>
		ModeNumber                                         = 405,

		/// <summary>
		///     Used in the TIFF-F and TIFF-FX standards, holds information about the ITULAB (PhotometricInterpretation = 10) encoding. (Hex: 0x01B1)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/decode.html
		/// </summary>
		Decode                                             = 433,

		/// <summary>
		///     Defined in the Mixed Raster Content part of RFC 2301, is the default color needed in areas where no image is available. (Hex: 0x01B2)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/defaultimagecolor.html
		/// </summary>
		DefaultImageColor                                  = 434,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0200)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegproc.html
		/// </summary>
		JPEGProc                                           = 512,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0201)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpeginterchangeformat.html
		/// </summary>
		JPEGInterchangeFormat                              = 513,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0202)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpeginterchangeformatlength.html
		/// </summary>
		JPEGInterchangeFormatLength                        = 514,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0203)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegrestartinterval.html
		/// </summary>
		JPEGRestartInterval                                = 515,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0205)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpeglosslesspredictors.html
		/// </summary>
		JPEGLosslessPredictors                             = 517,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0206)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegpointtransforms.html
		/// </summary>
		JPEGPointTransforms                                = 518,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0207)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegqtables.html
		/// </summary>
		JPEGQTables                                        = 519,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0208)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegdctables.html
		/// </summary>
		JPEGDCTables                                       = 520,

		/// <summary>
		///     Old-style JPEG compression field. TechNote2 invalidates this part of the specification. (Hex: 0x0209)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/jpegactables.html
		/// </summary>
		JPEGACTables                                       = 521,

		/// <summary>
		///     The transformation from RGB to YCbCr image data. (Hex: 0x0211)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ycbcrcoefficients.html
		/// </summary>
		YCbCrCoefficients                                  = 529,

		/// <summary>
		///     Specifies the subsampling factors used for the chrominance components of a YCbCr image. (Hex: 0x0212)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ycbcrsubsampling.html
		/// </summary>
		YCbCrSubSampling                                   = 530,

		/// <summary>
		///     Specifies the positioning of subsampled chrominance components relative to luminance samples. (Hex: 0x0213)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ycbcrpositioning.html
		/// </summary>
		YCbCrPositioning                                   = 531,

		/// <summary>
		///     Specifies a pair of headroom and footroom image data values (codes) for each pixel component. (Hex: 0x0214)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/referenceblackwhite.html
		/// </summary>
		ReferenceBlackWhite                                = 532,

		/// <summary>
		///     Defined in the Mixed Raster Content part of RFC 2301, used to replace RowsPerStrip for IFDs with variable-sized strips. (Hex: 0x022F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/striprowcounts.html
		/// </summary>
		StripRowCounts                                     = 559,

		/// <summary>
		///     XML packet containing XMP metadata (Hex: 0x02BC)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/xmp.html
		/// </summary>
		XMP                                                = 700,

		/// <summary>
		///     Rating tag used by Windows (Hex: 0x4746)
		/// </summary>
		Rating                                             = 18246,

		/// <summary>
		///     Rating tag used by Windows, value in percent (Hex: 0x4749)
		/// </summary>
		RatingPercent                                      = 18249,

		/// <summary>
		///     OPI-related. (Hex: 0x800D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imageid.html
		/// </summary>
		ImageID                                            = 32781,

		/// <summary>
		///     Annotation data, as used in 'Imaging for Windows'. (Hex: 0x80A4)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/wangannotation.html
		/// </summary>
		WangAnnotation                                     = 32932,

		/// <summary>
		///     Copyright notice. (Hex: 0x8298)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/copyright.html
		/// </summary>
		Copyright                                          = 33432,

		/// <summary>
		///     Specifies the pixel data format encoding in the Molecular Dynamics GEL file format. (Hex: 0x82A5)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdfiletag.html
		/// </summary>
		MDFileTag                                          = 33445,

		/// <summary>
		///     Specifies a scale factor in the Molecular Dynamics GEL file format. (Hex: 0x82A6)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdscalepixel.html
		/// </summary>
		MDScalePixel                                       = 33446,

		/// <summary>
		///     Used to specify the conversion from 16bit to 8bit in the Molecular Dynamics GEL file format. (Hex: 0x82A7)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdcolortable.html
		/// </summary>
		MDColorTable                                       = 33447,

		/// <summary>
		///     Name of the lab that scanned this file, as used in the Molecular Dynamics GEL file format. (Hex: 0x82A8)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdlabname.html
		/// </summary>
		MDLabName                                          = 33448,

		/// <summary>
		///     Information about the sample, as used in the Molecular Dynamics GEL file format. (Hex: 0x82A9)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdsampleinfo.html
		/// </summary>
		MDSampleInfo                                       = 33449,

		/// <summary>
		///     Date the sample was prepared, as used in the Molecular Dynamics GEL file format. (Hex: 0x82AA)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdprepdate.html
		/// </summary>
		MDPrepDate                                         = 33450,

		/// <summary>
		///     Time the sample was prepared, as used in the Molecular Dynamics GEL file format. (Hex: 0x82AB)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdpreptime.html
		/// </summary>
		MDPrepTime                                         = 33451,

		/// <summary>
		///     Units for data in this file, as used in the Molecular Dynamics GEL file format. (Hex: 0x82AC)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/mdfileunits.html
		/// </summary>
		MDFileUnits                                        = 33452,

		/// <summary>
		///     Used in interchangeable GeoTIFF files. (Hex: 0x830E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/modelpixelscaletag.html
		/// </summary>
		ModelPixelScaleTag                                 = 33550,

		/// <summary>
		///     IPTC (International Press Telecommunications Council) metadata. (Hex: 0x83BB)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/iptc.html
		/// </summary>
		IPTC                                               = 33723,

		/// <summary>
		///     Intergraph Application specific storage. (Hex: 0x847E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ingrpacketdatatag.html
		/// </summary>
		INGRPacketDataTag                                  = 33918,

		/// <summary>
		///     Intergraph Application specific flags. (Hex: 0x847F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ingrflagregisters.html
		/// </summary>
		INGRFlagRegisters                                  = 33919,

		/// <summary>
		///     Originally part of Intergraph's GeoTIFF tags, but likely understood by IrasB only. (Hex: 0x8480)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/irasbtransformationmatrix.html
		/// </summary>
		IrasBTransformationMatrix                          = 33920,

		/// <summary>
		///     Originally part of Intergraph's GeoTIFF tags, but now used in interchangeable GeoTIFF files. (Hex: 0x8482)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/modeltiepointtag.html
		/// </summary>
		ModelTiepointTag                                   = 33922,

		/// <summary>
		///     Used in interchangeable GeoTIFF files. (Hex: 0x85D8)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/modeltransformationtag.html
		/// </summary>
		ModelTransformationTag                             = 34264,

		/// <summary>
		///     Collection of Photoshop 'Image Resource Blocks'. (Hex: 0x8649)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/photoshop.html
		/// </summary>
		Photoshop                                          = 34377,

		/// <summary>
		///     A pointer to the Exif IFD. (Hex: 0x8769)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/exififd.html
		/// </summary>
		ExifIFD                                            = 34665,

		/// <summary>
		///     ICC profile data. (Hex: 0x8773)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/iccprofile.html
		/// </summary>
		ICCProfile                                         = 34675,

		/// <summary>
		///     Defined in the Mixed Raster Content part of RFC 2301, used to denote the particular function of this Image in the mixed raster scheme. (Hex: 0x87AC)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imagelayer.html
		/// </summary>
		ImageLayer                                         = 34732,

		/// <summary>
		///     Used in interchangeable GeoTIFF files. (Hex: 0x87AF)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/geokeydirectorytag.html
		/// </summary>
		GeoKeyDirectoryTag                                 = 34735,

		/// <summary>
		///     Used in interchangeable GeoTIFF files. (Hex: 0x87B0)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/geodoubleparamstag.html
		/// </summary>
		GeoDoubleParamsTag                                 = 34736,

		/// <summary>
		///     Used in interchangeable GeoTIFF files. (Hex: 0x87B1)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/geoasciiparamstag.html
		/// </summary>
		GeoAsciiParamsTag                                  = 34737,

		/// <summary>
		///     A pointer to the Exif-related GPS Info IFD. (Hex: 0x8825)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/gpsifd.html
		/// </summary>
		GPSIFD                                             = 34853,

		/// <summary>
		///     Used by HylaFAX. (Hex: 0x885C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/hylafaxfaxrecvparams.html
		/// </summary>
		HylaFAXFaxRecvParams                               = 34908,

		/// <summary>
		///     Used by HylaFAX. (Hex: 0x885D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/hylafaxfaxsubaddress.html
		/// </summary>
		HylaFAXFaxSubAddress                               = 34909,

		/// <summary>
		///     Used by HylaFAX. (Hex: 0x885E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/hylafaxfaxrecvtime.html
		/// </summary>
		HylaFAXFaxRecvTime                                 = 34910,

		/// <summary>
		///     Used by Adobe Photoshop. (Hex: 0x935C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/imagesourcedata.html
		/// </summary>
		ImageSourceData                                    = 37724,

		/// <summary>
		///     A pointer to the Exif-related Interoperability IFD. (Hex: 0xA005)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/interoperabilityifd.html
		/// </summary>
		InteroperabilityIFD                                = 40965,

		/// <summary>
		///     Used by the GDAL library, holds an XML list of name=value 'metadata' values about the image as a whole, and about specific samples. (Hex: 0xA480)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/gdal_metadata.html
		/// </summary>
		GDAL_METADATA                                      = 42112,

		/// <summary>
		///     Used by the GDAL library, contains an ASCII encoded nodata or background pixel value. (Hex: 0xA481)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/gdal_nodata.html
		/// </summary>
		GDAL_NODATA                                        = 42113,

		/// <summary>
		///     Used in the Oce scanning process. (Hex: 0xC427)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/ocescanjobdescription.html
		/// </summary>
		OceScanjobDescription                              = 50215,

		/// <summary>
		///     Used in the Oce scanning process. (Hex: 0xC428)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/oceapplicationselector.html
		/// </summary>
		OceApplicationSelector                             = 50216,

		/// <summary>
		///     Used in the Oce scanning process. (Hex: 0xC429)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/oceidentificationnumber.html
		/// </summary>
		OceIdentificationNumber                            = 50217,

		/// <summary>
		///     Used in the Oce scanning process. (Hex: 0xC42A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/oceimagelogiccharacteristics.html
		/// </summary>
		OceImageLogicCharacteristics                       = 50218,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC612)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/dngversion.html
		/// </summary>
		DNGVersion                                         = 50706,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC613)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/dngbackwardversion.html
		/// </summary>
		DNGBackwardVersion                                 = 50707,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC614)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/uniquecameramodel.html
		/// </summary>
		UniqueCameraModel                                  = 50708,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC615)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/localizedcameramodel.html
		/// </summary>
		LocalizedCameraModel                               = 50709,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC616)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cfaplanecolor.html
		/// </summary>
		CFAPlaneColor                                      = 50710,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC617)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cfalayout.html
		/// </summary>
		CFALayout                                          = 50711,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC618)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/linearizationtable.html
		/// </summary>
		LinearizationTable                                 = 50712,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC619)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/blacklevelrepeatdim.html
		/// </summary>
		BlackLevelRepeatDim                                = 50713,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/blacklevel.html
		/// </summary>
		BlackLevel                                         = 50714,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/blackleveldeltah.html
		/// </summary>
		BlackLevelDeltaH                                   = 50715,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/blackleveldeltav.html
		/// </summary>
		BlackLevelDeltaV                                   = 50716,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/whitelevel.html
		/// </summary>
		WhiteLevel                                         = 50717,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/defaultscale.html
		/// </summary>
		DefaultScale                                       = 50718,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC61F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/defaultcroporigin.html
		/// </summary>
		DefaultCropOrigin                                  = 50719,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC620)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/defaultcropsize.html
		/// </summary>
		DefaultCropSize                                    = 50720,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC621)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/colormatrix1.html
		/// </summary>
		ColorMatrix1                                       = 50721,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC622)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/colormatrix2.html
		/// </summary>
		ColorMatrix2                                       = 50722,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC623)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cameracalibration1.html
		/// </summary>
		CameraCalibration1                                 = 50723,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC624)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cameracalibration2.html
		/// </summary>
		CameraCalibration2                                 = 50724,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC625)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/reductionmatrix1.html
		/// </summary>
		ReductionMatrix1                                   = 50725,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC626)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/reductionmatrix2.html
		/// </summary>
		ReductionMatrix2                                   = 50726,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC627)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/analogbalance.html
		/// </summary>
		AnalogBalance                                      = 50727,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC628)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/asshotneutral.html
		/// </summary>
		AsShotNeutral                                      = 50728,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC629)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/asshotwhitexy.html
		/// </summary>
		AsShotWhiteXY                                      = 50729,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC62A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/baselineexposure.html
		/// </summary>
		BaselineExposure                                   = 50730,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC62B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/baselinenoise.html
		/// </summary>
		BaselineNoise                                      = 50731,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC62C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/baselinesharpness.html
		/// </summary>
		BaselineSharpness                                  = 50732,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC62D)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/bayergreensplit.html
		/// </summary>
		BayerGreenSplit                                    = 50733,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC62E)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/linearresponselimit.html
		/// </summary>
		LinearResponseLimit                                = 50734,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC62F)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/cameraserialnumber.html
		/// </summary>
		CameraSerialNumber                                 = 50735,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC630)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/lensinfo.html
		/// </summary>
		LensInfo                                           = 50736,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC631)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/chromablurradius.html
		/// </summary>
		ChromaBlurRadius                                   = 50737,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC632)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/antialiasstrength.html
		/// </summary>
		AntiAliasStrength                                  = 50738,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC634)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/dngprivatedata.html
		/// </summary>
		DNGPrivateData                                     = 50740,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC635)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/makernotesafety.html
		/// </summary>
		MakerNoteSafety                                    = 50741,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC65A)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/calibrationilluminant1.html
		/// </summary>
		CalibrationIlluminant1                             = 50778,

		/// <summary>
		///     Used in IFD 0 of DNG files. (Hex: 0xC65B)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/calibrationilluminant2.html
		/// </summary>
		CalibrationIlluminant2                             = 50779,

		/// <summary>
		///     Used in Raw IFD of DNG files. (Hex: 0xC65C)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/bestqualityscale.html
		/// </summary>
		BestQualityScale                                   = 50780,

		/// <summary>
		///     Alias Sketchbook Pro layer usage description. (Hex: 0xC660)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/aliaslayermetadata.html
		/// </summary>
		AliasLayerMetadata                                 = 50784,
	}
}
