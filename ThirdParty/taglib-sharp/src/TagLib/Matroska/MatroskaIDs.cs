//
// MatroskaIDs.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
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

namespace TagLib.Matroska
{
	/// <summary>
	/// Public enumeration listing Matroska specific EBML Identifiers.
	/// </summary>
	public enum MatroskaID : uint
	{
		/// <summary>
		/// Indicates a Matroska Segment EBML element.
		/// </summary>
		Segment = 0x18538067,

		/// <summary>
		/// Indicates a Matroska Segment Info EBML element.
		/// </summary>
		SegmentInfo = 0x1549A966,

		/// <summary>
		/// Indicates a Matroska Tracks EBML Element.
		/// </summary>
		Tracks = 0x1654AE6B,

		/// <summary>
		/// Indicates a Matroska Cues EBML element.
		/// </summary>
		Cues = 0x1C53BB6B,

		/// <summary>
		/// Indicates a Matroska Tags EBML element.
		/// </summary>
		Tags = 0x1254C367,

		/// <summary>
		/// Indicates a Matroska Seek Head EBML element.
		/// </summary>
		SeekHead = 0x114D9B74,

		/// <summary>
		/// Indicates a Matroska Cluster EBML element.
		/// </summary>
		Cluster = 0x1F43B675,

		/// <summary>
		/// Indicates a Matroska Attachments EBML element.
		/// </summary>
		Attachments = 0x1941A469,

		/// <summary>
		/// Indicates a Matroska Chapters EBML element.
		/// </summary>
		Chapters = 0x1043A770,

		/* IDs in the SegmentInfo master */

		/// <summary>
		/// Indicate a Matroska Code Scale EBML element.
		/// </summary>
		TimeCodeScale = 0x2AD7B1,

		/// <summary>
		/// Indicates a Matroska Duration EBML element.
		/// </summary>
		Duration = 0x4489,

		/// <summary>
		/// Indicates a Matroska Writing App EBML element.
		/// </summary>
		WrittingApp = 0x5741,

		/// <summary>
		/// Indicates a Matroska Muxing App EBML element.
		/// </summary>
		MuxingApp = 0x4D80,

		/// <summary>
		/// Indicate a Matroska Date UTC EBML element.
		/// </summary>
		DateUTC = 0x4461,

		/// <summary>
		/// Indicate a Matroska Segment UID EBML element.
		/// </summary>
		SegmentUID = 0x73A4,

		/// <summary>
		/// Indicate a Matroska Segment File Name EBML element.
		/// </summary>
		SegmentFileName = 0x7384,

		/// <summary>
		/// Indicate a Matroska Prev UID EBML element.
		/// </summary>
		PrevUID = 0x3CB923,

		/// <summary>
		/// Indicate a Matroska Prev File Name EBML element.
		/// </summary>
		PrevFileName = 0x3C83AB,

		/// <summary>
		/// Indicate a Matroska Nex UID EBML element.
		/// </summary>
		NexUID = 0x3EB923,

		/// <summary>
		/// Indicate a Matroska Nex File Name EBML element.
		/// </summary>
		NexFileName = 0x3E83BB,

		/// <summary>
		/// Indicate a Matroska Title EBML element.
		/// </summary>
		Title = 0x7BA9,

		/// <summary>
		/// Indicate a Matroska Segment Family EBML element.
		/// </summary>
		SegmentFamily = 0x4444,

		/// <summary>
		/// Indicate a Matroska Chapter Translate EBML element.
		/// </summary>
		ChapterTranslate = 0x6924,

		/* ID in the Tracks master */

		/// <summary>
		/// Indicate a Matroska Track Entry EBML element.
		/// </summary>
		TrackEntry = 0xAE,

		/* IDs in the TrackEntry master */

		/// <summary>
		/// Indicate a Matroska Track Number EBML element.
		/// </summary>
		TrackNumber = 0xD7,

		/// <summary>
		/// Indicate a Matroska Track UID EBML element.
		/// </summary>
		TrackUID = 0x73C5,

		/// <summary>
		/// Indicate a Matroska Track Type EBML element.
		/// </summary>
		TrackType = 0x83,

		/// <summary>
		/// Indicate a Matroska Track Audio EBML element.
		/// </summary>
		TrackAudio = 0xE1,

		/// <summary>
		/// Indicate a Matroska Track Video EBML element.
		/// </summary>
		TrackVideo = 0xE0,

		/// <summary>
		/// Indicate a Matroska Void EBML element.
		/// </summary>
		Void = 0xEC,

		/// <summary>
		/// Indicate a Matroska CRC-32 EBML element.
		/// </summary>
		/// <remarks>
		/// The CRC is computed on all the data of the Master-element it's in. 
		/// The CRC Element should be the first in it's parent master for easier reading. 
		/// All level 1 Elements should include a CRC-32. The CRC in use is the IEEE CRC32 Little Endian.
		/// </remarks>
		CRC32 = 0xBF,

		/// <summary>
		/// Indicate a Matroska Track Encoding EBML element.
		/// </summary>
		ContentEncodings = 0x6D80,

		/// <summary>
		/// Indicate a Matroska Codec ID EBML element.
		/// </summary>
		CodecID = 0x86,

		/// <summary>
		/// Indicate a Matroska Codec Private EBML element.
		/// </summary>
		CodecPrivate = 0x63A2,

		/// <summary>
		/// Indicate a Matroska Codec Name EBML element.
		/// </summary>
		CodecName = 0x258688,

		/// <summary>
		/// Indicate a Matroska Track Name EBML element.
		/// </summary>
		TrackName = 0x536E,

		/// <summary>
		/// Indicate a Matroska Track Language EBML element.
		/// </summary>
		TrackLanguage = 0x22B59C,

		/// <summary>
		/// Indicate a Matroska Track Enabled EBML element.
		/// </summary>
		TrackFlagEnabled = 0xB9,

		/// <summary>
		/// Indicate a Matroska Track Flag Default EBML element.
		/// </summary>
		TrackFlagDefault = 0x88,

		/// <summary>
		/// Indicate a Matroska Track Flag Forced EBML element.
		/// </summary>
		TrackFlagForced = 0x55AA,

		/// <summary>
		/// Indicate a Matroska Track Flag Lacing EBML element.
		/// </summary>
		TrackFlagLacing = 0x9C,

		/// <summary>
		/// Indicate a Matroska Track Min Cache EBML element.
		/// </summary>
		TrackMinCache = 0x6DE7,

		/// <summary>
		/// Indicate a Matroska Track Max Cache EBML element.
		/// </summary>
		TrackMaxCache = 0x6DF8,

		/// <summary>
		/// Indicate a Matroska Track Default Duration EBML element.
		/// </summary>
		TrackDefaultDuration = 0x23E383,

		/// <summary>
		/// Indicate a Matroska Track Time Code Scale EBML element.
		/// </summary>
		TrackTimeCodeScale = 0x23314F,

		/// <summary>
		/// Indicate a Matroska Track Max Block Addition EBML element.
		/// </summary>
		MaxBlockAdditionID = 0x55EE,

		/// <summary>
		/// Indicate a Matroska Track Attachment Link EBML element.
		/// </summary>
		TrackAttachmentLink = 0x7446,

		/// <summary>
		/// Indicate a Matroska Track Overlay EBML element.
		/// </summary>
		TrackOverlay = 0x6FAB,

		/// <summary>
		/// Indicate a Matroska Track Translate EBML element.
		/// </summary>
		TrackTranslate = 0x6624,

		/// <summary>
		/// Indicate a Matroska Track Offset element.
		/// </summary>
		TrackOffset = 0x537F,

		/// <summary>
		/// Indicate a Matroska Codec Settings EBML element.
		/// </summary>
		CodecSettings = 0x3A9697,

		/// <summary>
		/// Indicate a Matroska Codec Info URL EBML element.
		/// </summary>
		CodecInfoUrl = 0x3B4040,

		/// <summary>
		/// Indicate a Matroska Codec Download URL EBML element.
		/// </summary>
		CodecDownloadUrl = 0x26B240,

		/// <summary>
		/// Indicate a Matroska Codec Decode All EBML element.
		/// </summary>
		CodecDecodeAll = 0xAA,

		/* IDs in the TrackVideo master */
		/* NOTE: This one is here only for backward compatibility.
		* Use _TRACKDEFAULDURATION */

		/// <summary>
		/// Indicate a Matroska Video Frame Rate EBML element.
		/// </summary>
		VideoFrameRate = 0x2383E3,

		/// <summary>
		/// Indicate a Matroska Video Display Width EBML element.
		/// </summary>
		VideoDisplayWidth = 0x54B0,

		/// <summary>
		/// Indicate a Matroska Video Display Height EBML element.
		/// </summary>
		VideoDisplayHeight = 0x54BA,

		/// <summary>
		/// Indicate a Matroska Video Display Unit EBML element.
		/// </summary>
		VideoDisplayUnit = 0x54B2,

		/// <summary>
		/// Indicate a Matroska Video Pixel Width EBML element.
		/// </summary>
		VideoPixelWidth = 0xB0,

		/// <summary>
		/// Indicate a Matroska Video Pixel Height EBML element.
		/// </summary>
		VideoPixelHeight = 0xBA,

		/// <summary>
		/// Indicate a Matroska Video Pixel Crop Bottom EBML element.
		/// </summary>
		VideoPixelCropBottom = 0x54AA,

		/// <summary>
		/// Indicate a Matroska Video Pixel Crop Top EBML element.
		/// </summary>
		VideoPixelCropTop = 0x54BB,

		/// <summary>
		/// Indicate a Matroska Video Pixel Crop Left EBML element.
		/// </summary>
		VideoPixelCropLeft = 0x54CC,

		/// <summary>
		/// Indicate a Matroska Video Pixel Crop Right EBML element.
		/// </summary>
		VideoPixelCropRight = 0x54DD,

		/// <summary>
		/// Indicate a Matroska Video Flag Interlaced EBML element.
		/// </summary>
		VideoFlagInterlaced = 0x9A,

		/// <summary>
		/// Indicate a Matroska Video Stereo Mode EBML element.
		/// </summary>
		VideoStereoMode = 0x53B8,

		/// <summary>
		/// Indicate a Matroska Video Aspect Ratio Type EBML element.
		/// </summary>
		VideoAspectRatioType = 0x54B3,

		/// <summary>
		/// Indicate a Matroska Video Colour Space EBML element.
		/// </summary>
		VideoColourSpace = 0x2EB524,

		/// <summary>
		/// Indicate a Matroska Video Gamma Value EBML element.
		/// </summary>
		VideoGammaValue = 0x2FB523,

		/* in the Matroska Seek Head master */

		/// <summary>
		/// Indicate a Matroska Seek Entry (Master).
		/// </summary>
		Seek = 0x4DBB,

		/// <summary>
		/// Indicate a Matroska Seek ID (Binary).
		/// </summary>
		SeekID = 0x53AB,

		/// <summary>
		/// Indicate a Matroska Seek Position (uint).
		/// </summary>
		SeekPosition = 0x53AC,


		/* IDs in the TrackAudio master */

		/// <summary>
		/// Indicate a Matroska Audio Sampling Freq EBML element.
		/// </summary>
		AudioSamplingFreq = 0xB5,

		/// <summary>
		/// Indicate a Matroska Audio Bit Depth EBML element.
		/// </summary>
		AudioBitDepth = 0x6264,

		/// <summary>
		/// Indicate a Matroska Audio Channels EBML element.
		/// </summary>
		AudioChannels = 0x9F,

		/// <summary>
		/// Indicate a Matroska Audio Channels Position EBML element.
		/// </summary>
		AudioChannelsPositions = 0x7D7B,

		/// <summary>
		/// Indicate a Matroska Audio Output Sampling Freq EBML element.
		/// </summary>
		AudioOutputSamplingFreq = 0x78B5,

		/* IDs in the Tags master */

		/// <summary>
		/// Indicate a Matroska Tag EBML element.
		/// </summary>
		Tag = 0x7373,

		/* in the Tag master */

		/// <summary>
		/// Indicate a Matroska Simple Tag EBML element.
		/// </summary>
		SimpleTag = 0x67C8,

		/// <summary>
		/// Indicate a Matroska Targets EBML element.
		/// </summary>
		Targets = 0x63C0,

		/* in the SimpleTag master */

		/// <summary>
		/// Indicate a Matroska Tag Name EBML element.
		/// </summary>
		TagName = 0x45A3,

		/// <summary>
		/// Indicate a Matroska Tag String EBML element.
		/// </summary>
		TagString = 0x4487,

		/// <summary>
		/// Indicate a Matroska Tag Language EBML element.
		/// </summary>
		TagLanguage = 0x447A,

		/// <summary>
		/// Indicate a Matroska Tag Default EBML element.
		/// </summary>
		TagDefault = 0x4484,

		/// <summary>
		/// Indicate a Matroska Tag Binary EBML element.
		/// </summary>
		TagBinary = 0x4485,

		/* in the Targets master */

		/// <summary>
		/// Indicate a Matroska Target Type Value  EBML element (UINT).
		/// </summary>
		TargetTypeValue = 0x68CA,

		/// <summary>
		/// Indicate a Matroska Target Type EBML element (string).
		/// </summary>
		TargetType = 0x63CA,

		/// <summary>
		/// Indicate a Matroska Target Tag Track UID EBML element (UINT).
		/// </summary>
		TagTrackUID = 0x63C5,

		/// <summary>
		/// Indicate a Matroska Target Tag Edition UID EBML element (UINT).
		/// </summary>
		TagEditionUID = 0x63C9,

		/// <summary>
		/// Indicate a Matroska Target Tag Chapter UID EBML element (UINT).
		/// </summary>
		TagChapterUID = 0x63C4,

		/// <summary>
		/// Indicate a Matroska Target Tag Attachment UID EBML element (UINT).
		/// </summary>
		TagAttachmentUID = 0x63C6,


		/* in the Attachments master */

		/// <summary>
		/// Indicate a Matroska attached file.
		/// </summary>
		AttachedFile = 0x61A7,

		/// <summary>
		/// Indicate a Matroska human-friendly name for the attached file.
		/// </summary>
		FileDescription = 0x467E,

		/// <summary>
		/// Indicate a Matroska Filename of the attached file.
		/// </summary>
		FileName = 0x466E,

		/// <summary>
		/// Indicate a Matroska MIME type of the file. 
		/// </summary>
		FileMimeType = 0x4660,

		/// <summary>
		/// Indicate a Matroska data of the file. 
		/// </summary>
		FileData = 0x465C,

		/// <summary>
		/// Indicate a Matroska Unique ID representing the file, as random as possible.
		/// </summary>
		FileUID = 0x46AE

	}
}
