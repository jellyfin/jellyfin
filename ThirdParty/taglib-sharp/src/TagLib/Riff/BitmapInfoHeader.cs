//
// BitmapInfoHeader.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

using System;
using System.Globalization;

namespace TagLib.Riff
{
	/// <summary>
	///    This structure provides a representation of a Microsoft
	///    BitmapInfoHeader structure.
	/// </summary>
	public struct BitmapInfoHeader : IVideoCodec
	{
#region Private Fields
		
		/// <summary>
		///    Contains the size of the header.
		/// </summary>
		uint size; 
		
		/// <summary>
		///    Contains the video width.
		/// </summary>
		uint width; 
		
		/// <summary>
		///    Contains the video height.
		/// </summary>
		uint height; 
		
		/// <summary>
		///    Contains the number of planes.
		/// </summary>
		ushort planes; 
		
		/// <summary>
		///    Contains the bit count.
		/// </summary>
		ushort bit_count; 
		
		/// <summary>
		///    Contains the compression (codec) ID.
		/// </summary>
		ByteVector compression_id; 
		
		/// <summary>
		///    Contains the size of the image.
		/// </summary>
		uint size_of_image; 
		
		/// <summary>
		///    Contains the number of X pixels per meter.
		/// </summary>
		uint x_pixels_per_meter; 
		
		/// <summary>
		///    Contains the number of Y pixels per meter.
		/// </summary>
		uint y_pixels_per_meter; 
		
		/// <summary>
		///    Contains the number of colors used.
		/// </summary>
		uint colors_used; 
		
		/// <summary>
		///    Contains the number of important colors.
		/// </summary>
		uint colors_important;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="BitmapInfoHeader" /> by reading the raw structure
		///    from the beginning of a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 40 bytes.
		/// </exception>
		[Obsolete("Use BitmapInfoHeader(ByteVector,int)")]
		public BitmapInfoHeader (ByteVector data) : this (data, 0)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="BitmapInfoHeader" /> by reading the raw structure
		///    from a specified position in a <see cref="ByteVector" />
		///    object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the index in
		///    <paramref name="data"/> at which the structure begins.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="offset" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 16 bytes at
		///    <paramref name="offset" />.
		/// </exception>
		public BitmapInfoHeader (ByteVector data, int offset)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (offset + 40 > data.Count)
				throw new CorruptFileException (
					"Expected 40 bytes.");
			
			if (offset < 0)
				throw new ArgumentOutOfRangeException (
					"offset");
			
			size               = data.Mid (offset +  0, 4).ToUInt (false);
			width              = data.Mid (offset +  4, 4).ToUInt (false);
			height             = data.Mid (offset +  8, 4).ToUInt (false);
			planes             = data.Mid (offset + 12, 2).ToUShort (false);
			bit_count          = data.Mid (offset + 14, 2).ToUShort (false);
			compression_id     = data.Mid (offset + 16, 4);
			size_of_image      = data.Mid (offset + 20, 4).ToUInt (false);
			x_pixels_per_meter = data.Mid (offset + 24, 4).ToUInt (false);
			y_pixels_per_meter = data.Mid (offset + 28, 4).ToUInt (false);
			colors_used        = data.Mid (offset + 32, 4).ToUInt (false);
			colors_important   = data.Mid (offset + 36, 4).ToUInt (false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the size of the structure in bytes.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    bytes in the structure.
		/// </value>
		public uint HeaderSize {
			get {return size;}
		}
		
		/// <summary>
		///    Gets the number of planes in the image.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value containing the number of
		///    planes.
		/// </value>
		public ushort Planes {
			get {return planes;}
		}
		
		/// <summary>
		///    Gets the number of bits per pixel.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value containing the number of
		///    bits per pixel, equivalent to the log base 2 of the
		///    maximum number of colors.
		/// </value>
		public ushort BitCount {
			get {return bit_count;}
		}
		
		/// <summary>
		///    Gets the compression ID for image.
		/// </summary>
		/// <value>
		///    A four-byte <see cref="ByteVector" /> object containing
		///    the ID of the compression system (codec) used by the
		///    image.
		/// </value>
		public ByteVector CompressionId {
			get {return compression_id;}
		}
		
		/// <summary>
		///    Gets the size of the image in bytes.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    bytes in the image.
		/// </value>
		public uint ImageSize {
			get {return size_of_image;}
		}
		
		/// <summary>
		///    Gets the horizontal resolution of the target device.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    pixels-per-meter in the hoizontal direction for the
		///    target device.
		/// </value>
		public uint XPixelsPerMeter {
			get {return x_pixels_per_meter;}
		}
		
		/// <summary>
		///    Gets the vertical resolution of the target device.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    pixels-per-meter in the vertical direction for the
		///    target device.
		/// </value>
		public uint YPixelsPerMeter {
			get {return y_pixels_per_meter;}
		}
		
		/// <summary>
		///    Gets the number of colors in the image.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    colors.
		/// </value>
		public uint ColorsUsed {
			get {return colors_used;}
		}
		
		/// <summary>
		///    Gets the number of colors important in displaying the
		///    image.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    important colors.
		/// </value>
		public uint ImportantColors {
			get {return colors_important;}
		}
		
#endregion
		
		
		
#region IVideoCodec
		
		/// <summary>
		///    Gets the width of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    video represented by the current instance.
		/// </value>
		public int VideoWidth  {
			get {return (int)width;}
		}
		
		/// <summary>
		///    Gets the height of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    video represented by the current instance.
		/// </value>
		public int VideoHeight {
			get {return (int)height;}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Video" />.
		/// </value>
		public MediaTypes MediaTypes {
			get {return MediaTypes.Video;}
		}
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TimeSpan.Zero" />.
		/// </value>
		public TimeSpan Duration {
			get {return TimeSpan.Zero;}
		}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public string Description {
			get {
				string id = CompressionId.ToString (StringType.UTF8)
					.ToUpper (CultureInfo.InvariantCulture);
				switch (id)
				{
				case "AEMI":
					return "Array VideoONE MPEG1-I capture";
				case "ALPH":
					return "Ziracom Video";
				case "AMPG":
					return "Array VideoONE capture/compression";
				case "ANIM":
					return "Intel RDX";
				case "AP41":
					return "Microsoft Corporation Video";
				case "AUR2":
					return "AuraVision Aura 2 codec";
				case "AURA":
					return "AuraVision Aura 1 codec";
				case "AUVX":
					return "USH GmbH AUVX video codec";
				case "BT20":
					return "Brooktree MediaStream codec";
				case "BTCV":
					return "Brooktree composite video codec";
				case "CC12":
					return "Intel YUV12 codec";
				case "CDVC":
					return "Canopus DV codec";
				case "CGDI":
					return "Microsoft CamCorder in Office 97 (screen capture codec)";
				case "CHAM":
					return "Winnov Caviara Champagne";
				case "CM10":
					return "CyberLink Corporation MediaShow 1.0";
				case "CPLA":
					return "Weitek 4:2:0 YUV planar";
				case "CT10":
					return "CyberLink Corporation TalkingShow 1.0";
				case "CVID":
					return "Cinepak by SuperMac";
				case "CWLT":
					return "Microsoft Corporation Video";
				case "CYUV":
					return "Creative Labs YUV";
				case "DIV3":
				case "MP43":
					return "Microsoft MPEG-4 Version 3 Video";
				case "DIV4":
					return "Microsoft Corporation Video";
				case "DIVX":
					return "DivX Video";
				case "DJPG":
					return "Broadway 101 Motion JPEG codec";
				case "DP16":
					return "YUV411 with DPCM 6-bit compression";
				case "DP18":
					return "YUV411 with DPCM 8-bit compression";
				case "DP26":
					return "YUV422 with DPCM 6-bit compression";
				case "DP28":
					return "YUV422 with DPCM 8-bit compression";
				case "DP96":
					return "YVU9 with DPCM 6-bit compression";
				case "DP98":
					return "YVU9 with DPCM 8-bit compression";
				case "DP9L":
					return "YVU9 with DPCM 6-bit compression and thinned-out";
				case "DUCK":
					return "The Duck Corporation TrueMotion 1.0";
				case "DV25":
					return "SMPTE 314M 25Mb/s compressed";
				case "DV50":
					return "SMPTE 314M 50Mb/s compressed";
				case "DVE2":
					return "DVE-2 videoconferencing codec";
				case "DVH1":
					return "DVC Pro HD";
				case "DVHD":
					return "DV data as defined in Part 3 of the Specification of Consumer-use Digital VCRs";
				case "DVNM":
					return "Matsushita Electric Industrial Co., Ltd. Video";
				case "DVSD":
					return "DV data as defined in Part 2 of the Specification of Consumer-use Digital VCRs";
				case "DVSL":
					return "DV data as defined in Part 6 of Specification of Consumer-use Digital VCRs";
				case "DVX1":
					return "Lucent DVX1000SP video decoder.";
				case "DVX2":
					return "Lucent DVX2000S video decoder";
				case "DVX3":
					return "Lucent DVX3000S video decoder";
				case "DXTC":
					return "DirectX texture compression";
				case "DX50":
					return "DivX Version 5 Video";
				case "EMWC":
					return "EverAd Marquee WMA codec";
				case "ETV1":
				case "ETV2":
				case "ETVC":
					return "eTreppid video codec";
				case "FLJP":
					return "Field-encoded motion JPEG with LSI bitstream format";
				case "FRWA":
					return "Softlab-Nsk Ltd. Forward alpha";
				case "FRWD":
					return "Softlab-Nsk Ltd. Forward JPEG";
				case "FRWT":
					return "Softlab-Nsk Ltd. Forward JPEG+alpha";
				case "FVF1":
					return "Iterated Systems, Inc. Fractal video frame";
				case "FXT1":
					return "3dfx Interactive, Inc. Video";
				case "GWLT":
					return "Microsoft Corporation Video";
				case "H260":
				case "H261":
				case "H262":
				case "H263":
				case "H264":
				case "H265":
				case "H266":
				case "H267":
				case "H268":
				case "H269":
					return "Intel " + CompressionId.ToString (StringType.UTF8) + " Conferencing codec";
				case "I263":
					return "Intel I263";
				case "I420":
					return "Intel Indeo 4 codec";
				case "IAN":
					return "Intel RDX";
				case "ICLB":
					return "InSoft, Inc. CellB videoconferencing codec";
				case "IFO9":
					return "Intel intermediate YUV9";
				case "ILVC":
					return "Intel layered Video";
				case "ILVR":
					return "ITU-T's H.263+ compression standard";
				case "IMAC":
					return "Intel hardware motion compensation";
				case "IPDV":
					return "IEEE 1394 digital video control and capture board format";
				case "IRAW":
					return "Intel YUV uncompressed";
				case "ISME":
					return "Intel's next-generation video codec";
				case "IUYV":
					return "UYVY interlaced (even, then odd lines)";
				case "IV30":
				case "IV31":
				case "IV32":
				case "IV33":
				case "IV34":
				case "IV35":
				case "IV36":
				case "IV37":
				case "IV38":
				case "IV39":
					return "Intel Indeo Video Version 3";
				case "IV40":
				case "IV41":
				case "IV42":
				case "IV43":
				case "IV44":
				case "IV45":
				case "IV46":
				case "IV47":
				case "IV48":
				case "IV49":
					return "Intel Indeo Video Version 4";
				case "IV50":
					return "Intel Indeo Video Version 5";
				case "IY41":
					return "LEAD Technologies, Inc. Y41P interlaced (even, then odd lines)";
				case "IYU1":
					return "IEEE 1394 Digital Camera 1.04 Specification: mode 2, 12-bit YUV (4:1:1)";
				case "IYU2":
					return "IEEE 1394 Digital Camera 1.04 Specification: mode 2, 24 bit YUV (4:4:4)";
				case "JPEG":
					return "Microsoft Corporation Still image JPEG DIB.";
				case "LEAD":
					return "LEAD Technologies, Inc. Proprietary MCMP compression";
				case "LIA1":
					return "Liafail";
				case "LJPG":
					return "LEAD Technologies, Inc. Lossless JPEG compression";
				case "LSV0":
					return "Infinop Inc. Video";
				case "LSVC":
					return "Infinop Lightning Strike constant bit rate video codec";
				case "LSVW":
					return "Infinop Lightning Strike multiple bit rate video codec";
				case "M101":
					return "Matrox Electronic Systems, Ltd. Uncompressed field-based YUY2";
				case "M4S2":
					return "Microsoft ISO MPEG-4 video V1.1";
				case "MJPG":
					return "Motion JPEG";
				case "MMES":
					return "Matrox MPEG-2 elementary video stream";
				case "MMIF":
					return "Matrox MPEG-2 elementary I-frame-only video stream";
				case "MP2A":
					return "Media Excel Inc. MPEG-2 audio";
				case "MP2T":
					return "Media Excel Inc. MPEG-2 transport";
				case "MP2V":
					return "Media Excel Inc. MPEG-2 video";
				case "MP42":
					return "Microsoft MPEG-4 video codec V2";
				case "MP4A":
					return "Media Excel Inc. MPEG-4 audio";
				case "MP4S":
					return "Microsoft ISO MPEG-4 video V1.0";
				case "MP4T":
					return "Media Excel Inc. MPEG-4 transport";
				case "MP4V":
					return "Media Excel Inc. MPEG-4 video";
				case "MPEG":
					return "Chromatic Research, Inc. MPEG-1 video, I frame";
				case "MPG4":
					return "Microsoft MPEG-4 Version 1 Video";
				case "MRCA":
					return "FAST Multimedia AG Mrcodec";
				case "MRLE":
					return "Microsoft Run length encoding";
				case "MSS1":
					return "Microsoft screen codec V1";
				case "MSV1":
					return "Microsoft video codec V1";
				case "MSVC":
					return "Microsoft Video 1";
				case "MV10":
				case "MV11":
				case "MV12":
				case "MV99":
				case "MVC1":
				case "MVC2":
				case "MVC9":
					return "Nokia MVC video codec";
				case "NTN1":
					return "Nogatech video compression 1";
				case "NY12":
					return "Nogatech YUV 12 format";
				case "NYUV":
					return "Nogatech YUV 422 format";
				case "PCL2":
					return "Pinnacle RL video codec";
				case "PCLE":
					return "Pinnacle Studio 400 video codec";
				case "PHMO":
					return "IBM Corporation Photomotion";
				case "QPEG":
					return "Q-Team QPEG 1.1 format video codec";
				case "RGBT":
					return "Computer Concepts Ltd. 32-bit support";
				case "RIVA":
					return "NVIDIA Corporation Swizzled texture format";
				case "RLND":
					return "Roland Corporation Video";
				case "RT21":
					return "Intel Indeo 2.1";
				case "RVX":
					return "Intel RDX";
				case "S263":
					return "Sorenson Vision H.263";
				case "SCCD":
					return "Luminositi SoftCam codec";
				case "SDCC":
					return "Sun Digital Camera codec";
				case "SFMC":
					return "Crystal Net SFM codec";
				case "SMSC":
				case "SMSD":
					return "Radius Proprietary";
				case "SPLC":
					return "Splash Studios ACM audio codec";
				case "SQZ2":
					return "Microsoft VXtreme video codec V2";
				case "STVA":
					return "ST CMOS Imager Data (Bayer)";
				case "STVB":
					return "ST CMOS Imager Data (Nudged Bayer)";
				case "STVC":
					return "ST CMOS Imager Data (Bunched)";
				case "SV10":
					return "Sorenson Video R1";
				case "SV3M":
					return "Sorenson SV3 module decoder";
				case "TLMS":
				case "TLST":
					return "TeraLogic motion intraframe codec";
				case "TM20":
					return "The Duck Corporation TrueMotion 2.0";
				case "TMIC":
					return "TeraLogic motion intraframe codec";
				case "TMOT":
					return "TrueMotion video compression algorithm";
				case "TR20":
					return "The Duck Corporation TrueMotion RT 2.0";
				case "ULTI":
					return "IBM Corporation Ultimotion";
				case "UYVP":
					return "Evans & Sutherland YCbCr 4:2:2 extended precision, 10 bits per component (U0Y0V0Y1)";
				case "V261":
					return "Lucent VX3000S video codec";
				case "V422":
					return "VITEC Multimedia 24-bit YUV 4:2:2 format (CCIR 601)";
				case "V655":
					return "VITEC Multimedia 16-bit YUV 4:2:2 format";
				case "VCR1":
					return "ATI VCR 1.0";
				case "VCWV":
					return "VideoCon wavelet";
				case "VDCT":
					return "VITEC Multimedia Video Maker Pro DIB";
				case "VIDS":
					return "VITEC Multimedia YUV 4:2:2 CCIR 601 for v422";
				case "VGPX":
					return "Alaris VGPixel video";
				case "VIVO":
					return "Vivo H.263 video codec";
				case "VIXL":
					return "miro Computer Products AG";
				case "VJPG":
					return "Video Communication Systems - A JPEG-based compression scheme for RGB bitmaps";
				case "VLV1":
					return "VideoLogic Systems VLCAP.DRV";
				case "VQC1":
					return "ViewQuest Technologies Inc. 0x31435156";
				case "VQC2":
					return "ViewQuest Technologies Inc. 0x32435156";
				case "VQJP":
					return "ViewQuest Technologies Inc. VQ630 dual-mode digital camera";
				case "VQS4":
					return "ViewQuest Technologies Inc. VQ110 digital video camera";
				case "VX1K":
					return "Lucent VX1000S video codec";
				case "VX2K":
					return "Lucent VX2000S video codec";
				case "VXSP":
					return "Lucent VX1000SP video codec9";
				case "WBVC":
					return "Winbond Electronics Corporation W9960";
				case "WINX":
					return "Winnov, Inc. Video";
				case "WJPG":
					return "Winbond motion JPEG bitstream format";
				case "WMV1":
					return "Microsoft Windows Media Video Version 7";
				case "WMV2":
					return "Microsoft Windows Media Video Version 8";
				case "WMV3":
					return "Microsoft Windows Media Video Version 9";
				case "WNV1":
				case "WPY2":
					return "Winnov, Inc. Video";
				case "WZCD":
					return "CORE Co. Ltd. iScan";
				case "WZDC":
					return "CORE Co. Ltd. iSnap";
				case "XJPG":
					return "Xirlink JPEG-like compressor";
				case "XLV0":
					return "XL video decoder";
				case "XVID":
					return "XviD Video";
				case "YC12":
					return "Intel YUV12 Video";
				case "YCCK":
					return "Uncompressed YCbCr Video with key data";
				case "YU92":
					return "Intel YUV Video";
				case "YUV8":
					return "Winnov Caviar YUV8 Video";
				case "YUV9":
					return "Intel YUV Video";
				case "YUYP":
					return "Evans & Sutherland YCbCr 4:2:2 extended precision, 10 bits per component Video";
				case "YUYV":
					return "Canopus YUYV Compressor Video";
				case "ZPEG":
					return "Metheus Corporation Video Zipper";
				case "ZPG1":
				case "ZPG2":
				case "ZPG3":
				case "ZPG4":
					return "VoDeo Solutions Video";
				default:
					return string.Format (
						CultureInfo.InvariantCulture,
						"Unknown Image ({0})",
						CompressionId);
				}
			}
		}
		
#endregion
		
		
		
#region IEquatable
		
		/// <summary>
		///    Generates a hash code for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> value containing the hash code for
		///    the current instance.
		/// </returns>
		public override int GetHashCode ()
		{
			unchecked {
				return (int) (size ^ width ^ height ^ planes ^
					bit_count ^ compression_id.ToUInt () ^
					size_of_image ^ x_pixels_per_meter ^
					y_pixels_per_meter ^ colors_used ^
					colors_important);
			}
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another object.
		/// </summary>
		/// <param name="other">
		///    A <see cref="object" /> to compare to the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public override bool Equals (object other)
		{
			if (!(other is BitmapInfoHeader))
				return false;
			
			return Equals ((BitmapInfoHeader) other);
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another instance of <see cref="BitmapInfoHeader" />.
		/// </summary>
		/// <param name="other">
		///    A <see cref="BitmapInfoHeader" /> object to compare to
		///    the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public bool Equals (BitmapInfoHeader other)
		{
			return size == other.size && width == other.width &&
				height == other.height && planes == other.planes &&
				bit_count == other.bit_count &&
				compression_id == other.compression_id &&
				size_of_image == other.size_of_image &&
				x_pixels_per_meter == other.x_pixels_per_meter &&
				y_pixels_per_meter == other.y_pixels_per_meter &&
				colors_used == other.colors_used &&
				colors_important == other.colors_important;
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="WaveFormatEx" /> are equal to eachother.
		/// </summary>
		/// <param name="first">
		///    A <see cref="BitmapInfoHeader" /> object to compare.
		/// </param>
		/// <param name="second">
		///    A <see cref="BitmapInfoHeader" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    equal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator == (BitmapInfoHeader first,
		                                BitmapInfoHeader second)
		{
			return first.Equals (second);
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="BitmapInfoHeader" /> differ.
		/// </summary>
		/// <param name="first">
		///    A <see cref="BitmapInfoHeader" /> object to compare.
		/// </param>
		/// <param name="second">
		///    A <see cref="BitmapInfoHeader" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    unequal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator != (BitmapInfoHeader first,
		                                BitmapInfoHeader second)
		{
			return !first.Equals (second);
		}
#endregion
	}
}