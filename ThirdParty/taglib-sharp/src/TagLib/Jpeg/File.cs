//
// File.cs: Provides tagging for Jpeg files
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//   Stephane Delcroix (stephane@delcroix.org)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
// Copyright (c) 2009 Stephane Delcroix
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
using System.Collections.Generic;
using System.IO;

using TagLib.Image;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.Xmp;

namespace TagLib.Jpeg
{

	/// <summary>
	///    This class extends <see cref="TagLib.Image.ImageBlockFile" /> to provide tagging
	///    and properties support for Jpeg files.
	/// </summary>
	[SupportedMimeType("taglib/jpg", "jpg")]
	[SupportedMimeType("taglib/jpeg", "jpeg")]
	[SupportedMimeType("taglib/jpe", "jpe")]
	[SupportedMimeType("taglib/jif", "jif")]
	[SupportedMimeType("taglib/jfif", "jfif")]
	[SupportedMimeType("taglib/jfi", "jfi")]
	[SupportedMimeType("image/jpeg")]
	public class File : TagLib.Image.ImageBlockFile
	{

		/// <summary>
		///    The magic bits used to recognize an Exif segment
		/// </summary>
		private static readonly string EXIF_IDENTIFIER = "Exif\0\0";

		/// <summary>
		/// The magic strings used to identifiy an IPTC-IIM section
		/// </summary>
		private static readonly string IPTC_IIM_IDENTIFIER = "Photoshop 3.0\u00008BIM\u0004\u0004";

		/// <summary>
		///    Standard (empty) JFIF header to add, if no one is contained
		/// </summary>
		private static readonly byte [] BASIC_JFIF_HEADER = new byte [] {
			// segment maker
			0xFF, (byte) Marker.APP0,

			// segment size
			0x00, 0x10,

			// segment data
			0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01,
			0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00
		};


#region Private Fields

		/// <summary>
		///    Contains the media properties.
		/// </summary>
		private Properties properties;

		/// <summary>
		///    For now, we do not allow to change the jfif header. As long as this is
		///    the case, the header is kept as it is.
		/// </summary>
		private ByteVector jfif_header = null;

		/// <summary>
		///    The image width, as parsed from the Frame
		/// </summary>
		ushort width;

		/// <summary>
		///    The image height, as parsed from the Frame
		/// </summary>
		ushort height;

		/// <summary>
		///    Quality of the image, stored as we parse the file
		/// </summary>
		int quality;

#endregion

#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system and specified read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path, ReadStyle propertiesStyle)
			: this (new File.LocalFileAbstraction (path),
				propertiesStyle)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path) : this (path, ReadStyle.Average)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction and
		///    specified read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction,
		             ReadStyle propertiesStyle) : base (abstraction)
		{
			Read (propertiesStyle);
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		protected File (IFileAbstraction abstraction)
			: this (abstraction, ReadStyle.Average)
		{
		}

#endregion

#region Public Properties

		/// <summary>
		///    Gets the media properties of the file represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Properties" /> object containing the
		///    media properties of the file represented by the current
		///    instance.
		/// </value>
		public override TagLib.Properties Properties {
			get { return properties; }
		}

#endregion

#region Public Methods

		/// <summary>
		///  Gets a tag of a specified type from the current instance, optionally creating a
		/// new tag if possible.
		/// </summary>
		public override TagLib.Tag GetTag (TagLib.TagTypes type, bool create)
		{
			if (type == TagTypes.XMP) {
				foreach (Tag tag in ImageTag.AllTags) {
					if ((tag.TagTypes & type) == type || (tag.TagTypes & TagTypes.IPTCIIM) != 0)
						return tag;
				}
			}
			if (type == TagTypes.IPTCIIM && create)
			  {
			    // FIXME: don't know how to create IPTCIIM tags
			    return base.GetTag (type, false);
			  }

			return base.GetTag (type, create);
		}

		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		public override void Save ()
		{
			// Boilerplate
			PreSave();

			Mode = AccessMode.Write;
			try {
				WriteMetadata ();

				TagTypesOnDisk = TagTypes;
			} finally {
				Mode = AccessMode.Closed;
			}
		}

#endregion

#region Private Methods

		/// <summary>
		///    Reads the information from file with a specified read style.
		/// </summary>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		private void Read (ReadStyle propertiesStyle)
		{
			Mode = AccessMode.Read;
			try {
				ImageTag = new CombinedImageTag (TagTypes.XMP | TagTypes.TiffIFD | TagTypes.JpegComment | TagTypes.IPTCIIM);

				ValidateHeader ();
				ReadMetadata ();

				TagTypesOnDisk = TagTypes;

				if ((propertiesStyle & ReadStyle.Average) != 0)
					properties = ExtractProperties ();

			} finally {
				Mode = AccessMode.Closed;
			}
		}

		/// <summary>
		///    Attempts to extract the media properties of the main
		///    photo.
		/// </summary>
		/// <returns>
		///    A <see cref="Properties" /> object with a best effort guess
		///    at the right values. When no guess at all can be made,
		///    <see langword="null" /> is returned.
		/// </returns>
		private Properties ExtractProperties ()
		{
			if (width > 0 && height > 0)
				return new Properties (TimeSpan.Zero, new Codec (width, height, quality));

			return null;

		}

		/// <summary>
		///    Validates if the opened file is actually a JPEG.
		/// </summary>
		private void ValidateHeader ()
		{
			ByteVector segment = ReadBlock (2);
			if (segment.ToUShort () != 0xFFD8)
				throw new CorruptFileException ("Expected SOI marker at the start of the file.");
		}


		/// <summary>
		///    Reads a segment marker for a segment starting at current position.
		///    The second byte of the marker is returned, since the first is equal
		///    to 0xFF in every case.
		/// </summary>
		/// <returns>
		///    A <see cref="TagLib.Jpeg.Marker"/> with the second byte of the segment marker.
		/// </returns>
		private Marker ReadSegmentMarker ()
		{
			ByteVector segment_header = ReadBlock (2);

			if (segment_header.Count != 2)
				throw new CorruptFileException ("Could not read enough bytes for segment maker");

			if (segment_header[0] != 0xFF)
				throw new CorruptFileException ("Start of Segment expected at " + (Tell - 2));

			return (Marker)segment_header[1];
		}


		/// <summary>
		///    Reads the size of a segment at the current position.
		/// </summary>
		/// <returns>
		///    A <see cref="System.UInt16"/> with the size of the current segment.
		/// </returns>
		private ushort ReadSegmentSize ()
		{
			long position = Tell;

			ByteVector segment_size_bytes = ReadBlock (2);

			if (segment_size_bytes.Count != 2)
				throw new CorruptFileException ("Could not read enough bytes to determine segment size");

			ushort segment_size = segment_size_bytes.ToUShort ();

			// the size itself must be contained in the segment size
			// so the smallest (theoretically) possible number of bytes if 2
			if (segment_size < 2)
				throw new CorruptFileException (String.Format ("Invalid segment size ({0} bytes)", segment_size));

			long length = 0;
			try {
				length = Length;
			} catch (Exception) {
				// Probably not supported by stream.
			}

			if (length > 0 && position + segment_size >= length)
				throw new CorruptFileException ("Segment size exceeds file size");

			return segment_size;
		}


		/// <summary>
		///    Extracts the metadata from the current file by reading every segment in file.
		///    Method should be called with read position at first segment marker.
		/// </summary>
		private void ReadMetadata ()
		{
			// loop while marker is not EOI and not the data segment
			while (true) {
				Marker marker = ReadSegmentMarker ();

				// we stop parsing when the end of file (EOI) or the begin of the
				// data segment is reached (SOS)
				// the second case is a trade-off between tolerant and fast parsing
				if (marker == Marker.EOI || marker == Marker.SOS)
					break;

				long position = Tell;
				ushort segment_size = ReadSegmentSize ();

				// segment size contains 2 bytes of the size itself, so the
				// pure data size is this (and the cast is save)
				ushort data_size = (ushort) (segment_size - 2);

				switch (marker) {
				case Marker.APP0:	// possibly JFIF header
					ReadJFIFHeader (data_size);
					break;

				case Marker.APP1:	// possibly Exif or Xmp data found
					ReadAPP1Segment (data_size);
					break;

				case Marker.APP13: // possibly IPTC-IIM
					ReadAPP13Segment (data_size);
					break;

				case Marker.COM:	// Comment segment found
					ReadCOMSegment (data_size);
					break;

				case Marker.SOF0:
				case Marker.SOF1:
				case Marker.SOF2:
				case Marker.SOF3:
				case Marker.SOF9:
				case Marker.SOF10:
				case Marker.SOF11:
					ReadSOFSegment (data_size, marker);
					break;

				case Marker.DQT:	// Quantization table(s), use it to guess quality
					ReadDQTSegment (data_size);
					break;
				}

				// set position to next segment and start with next segment marker
				Seek (position + segment_size, SeekOrigin.Begin);
			}
		}

		/// <summary>
		///    Reads a JFIF header at current position
		/// </summary>
		private void ReadJFIFHeader (ushort length)
		{
			// JFIF header should be contained as first segment
			// SOI marker + APP0 Marker + segment size = 6 bytes
			if (Tell != 6)
				return;

			if (ReadBlock (5).ToString ().Equals ("JFIF\0")) {

				// store the JFIF header as it is
				Seek (2, SeekOrigin.Begin);
				jfif_header = ReadBlock (length + 2 + 2);

				AddMetadataBlock (2, length + 2 + 2);
			}

		}

		/// <summary>
		///    Reads an APP1 segment to find EXIF or XMP metadata.
		/// </summary>
		/// <param name="length">
		///    The length of the segment that will be read.
		/// </param>
		private void ReadAPP1Segment (ushort length)
		{
			long position = Tell;
			ByteVector data = null;

			// for an Exif segment, the data block consists of 14 bytes of:
			//    * 6 bytes Exif identifier string
			//    * 2 bytes bigendian indication MM (or II)
			//    * 2 bytes Tiff magic number (42)
			//    * 4 bytes offset of the first IFD in this segment
			//
			//    the last two points are alreay encoded according to
			//    big- or littleendian
			int exif_header_length = 14;

			// could be an Exif segment
			if ((ImageTag.TagTypes & TagLib.TagTypes.TiffIFD) == 0x00 && length >= exif_header_length) {

				data = ReadBlock (exif_header_length);

				if (data.Count == exif_header_length
				    && data.Mid (0, 6).ToString ().Equals (EXIF_IDENTIFIER)) {

					bool is_bigendian = data.Mid (6, 2).ToString ().Equals ("MM");

					ushort magic = data.Mid (8, 2).ToUShort (is_bigendian);
					if (magic != 42)
						throw new Exception (String.Format ("Invalid TIFF magic: {0}", magic));

					uint ifd_offset = data.Mid (10, 4).ToUInt (is_bigendian);

					var exif = new IFDTag ();
					var reader = new IFDReader (this, is_bigendian, exif.Structure, position + 6, ifd_offset, (uint) (length - 6));
					reader.Read ();
					ImageTag.AddTag (exif);

					AddMetadataBlock (position - 4, length + 4);

					return;
				}
			}

			int xmp_header_length = XmpTag.XAP_NS.Length + 1;

			// could be an Xmp segment
			if ((ImageTag.TagTypes & TagLib.TagTypes.XMP) == 0x00 && length >= xmp_header_length) {

				// if already data is read for determining the Exif segment,
				// just read the remaining bytes.
				// NOTE: that (exif_header_length < xmp_header_length) holds
				if (data == null)
					data = ReadBlock (xmp_header_length);
				else
					data.Add (ReadBlock (xmp_header_length - exif_header_length));

				if (data.ToString ().Equals (XmpTag.XAP_NS + "\0")) {
					ByteVector xmp_data = ReadBlock (length - xmp_header_length);

					ImageTag.AddTag (new XmpTag (xmp_data.ToString (), this));

					AddMetadataBlock (position - 4, length + 4);
				}
			}
		}

		/// <summary>
		///    Reads an APP13 segment to find IPTC-IIM metadata.
		/// </summary>
		/// <param name="length">
		///    The length of the segment that will be read.
		/// </param>
		/// <remarks>More info and specs for IPTC-IIM:
		/// - Guidelines for Handling Image Metadata (http://www.metadataworkinggroup.org/specs/)
		/// - IPTC Standard Photo Metadata (July 2010) (http://www.iptc.org/std/photometadata/specification/IPTC-PhotoMetadata-201007_1.pdf)
		/// - Extracting IPTC header information from JPEG images (http://www.codeproject.com/KB/graphics/iptc.aspx?fid=2301&amp;df=90&amp;mpp=25&amp;noise=3&amp;prof=False&amp;sort=Position&amp;view=Quick&amp;fr=51#xx0xx)
		/// - Reading IPTC APP14 Segment Header Information from JPEG Images (http://www.codeproject.com/KB/graphics/ReadingIPTCAPP14.aspx?q=iptc)
		/// </remarks>
		private void ReadAPP13Segment (ushort length)
		{
			// TODO: if both IPTC-IIM and XMP metadata is contained in a file, we should read
			// a IPTC-IIM checksum and compare that with the checksum built over the IIM block.
			// Depending on the result we should prefer the information from XMP or IIM.
			// Right now we always prefer XMP.

			var data = ReadBlock (length);

			// The APP13 segment consists of:
			// - the string "Photoshop 3.0\u0000"
			// - followed by "8BIM"
			// - and then the section type "\u0004\u0004".
			// There might be multiple 8BIM sections with different types, but we're following
			// YAGNI for now and only deal with the one we're interested in (and hope that it's
			// the first one).
			var iptc_iim_length = IPTC_IIM_IDENTIFIER.Length;
			if (length < iptc_iim_length || data.Mid (0, iptc_iim_length) != IPTC_IIM_IDENTIFIER)
				return;

			// PS6 introduced a new header with variable length text
			var headerInfoLen = data.Mid (iptc_iim_length, 1).ToUShort();
			int lenToSkip;
			if (headerInfoLen > 0) {
				// PS6 header: 1 byte headerinfolen + headerinfo + 2 bytes 00 padding (?) + 2 bytes length
				lenToSkip = 1 + headerInfoLen + 4;
			} else {
				//old style: 4 bytes 00 padding (?) + 2 bytes length
				lenToSkip = 6;
			}
			data.RemoveRange (0, iptc_iim_length + lenToSkip);

			var reader = new IIM.IIMReader (data);
			var tag = reader.Process ();
			if (tag != null)
				ImageTag.AddTag (tag);
		}

		/// <summary>
		///    Writes the metadata back to file. All metadata is stored in the first segments
		///    of the file.
		/// </summary>
		private void WriteMetadata ()
		{
			// first render all metadata segments to a ByteVector before the
			// file is touched ...
			ByteVector data = new ByteVector ();

			// existing jfif header is retained, otherwise a standard one
			// is created
			if (jfif_header != null)
				data.Add (jfif_header);
			else
				data.Add (BASIC_JFIF_HEADER);

			data.Add (RenderExifSegment ());
			data.Add (RenderXMPSegment ());
			data.Add (RenderCOMSegment ());

			SaveMetadata (data, 2);
		}

		/// <summary>
		///    Creates a <see cref="ByteVector"/> for the Exif segment of this file
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the whole Exif segment, if exif tags
		///    exists, otherwise null.
		/// </returns>
		private ByteVector RenderExifSegment ()
		{
			// Check, if IFD0 is contained
			IFDTag exif = ImageTag.Exif;
			if (exif == null)
				return null;

			// first IFD starts at 8
			uint first_ifd_offset = 8;

			// Render IFD0
			// FIXME: store endianess and use it here
			var renderer = new IFDRenderer (true, exif.Structure, first_ifd_offset);
			ByteVector exif_data = renderer.Render ();

			uint segment_size = (uint) (first_ifd_offset + exif_data.Count + 2 + 6);

			// do not render data segments, which cannot fit into the possible segment size
			if (segment_size > ushort.MaxValue)
				throw new Exception ("Exif Segment is too big to render");

			// Create whole segment
			ByteVector data = new ByteVector (new byte [] { 0xFF, (byte) Marker.APP1 });
			data.Add (ByteVector.FromUShort ((ushort) segment_size));
			data.Add ("Exif\0\0");
			data.Add (ByteVector.FromString ("MM", StringType.Latin1));
			data.Add (ByteVector.FromUShort (42));
			data.Add (ByteVector.FromUInt (first_ifd_offset));

			// Add ifd data itself
			data.Add (exif_data);

			return data;
		}


		/// <summary>
		///    Creates a <see cref="ByteVector"/> for the Xmp segment of this file
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the whole Xmp segment, if xmp tags
		///    exists, otherwise null.
		/// </returns>
		private ByteVector RenderXMPSegment ()
		{
			// Check, if XmpTag is contained
			XmpTag xmp = ImageTag.Xmp;
			if (xmp == null)
				return null;

			ByteVector xmp_data = XmpTag.XAP_NS + "\0";
			xmp_data.Add (xmp.Render ());

			uint segment_size = (uint) (2 + xmp_data.Count);

			// do not render data segments, which cannot fit into the possible segment size
			if (segment_size > ushort.MaxValue)
				throw new Exception ("XMP Segment is too big to render");

			// Create whole segment
			ByteVector data = new ByteVector (new byte [] { 0xFF, (byte) Marker.APP1 });
			data.Add (ByteVector.FromUShort ((ushort) segment_size));
			data.Add (xmp_data);

			return data;
		}


		/// <summary>
		///    Reads a COM segment to find the JPEG comment.
		/// </summary>
		/// <param name="length">
		///    The length of the segment that will be read.
		/// </param>
		private void ReadCOMSegment (int length)
		{
			if ((ImageTag.TagTypes & TagLib.TagTypes.JpegComment) != 0x00)
				return;

			long position = Tell;

			JpegCommentTag com_tag;

			if (length == 0) {
				 com_tag = new JpegCommentTag ();
			} else {
				ByteVector data = ReadBlock (length);

				int terminator = data.Find ("\0", 0);

				if (terminator < 0)
					com_tag = new JpegCommentTag (data.ToString ());
				else
					com_tag = new JpegCommentTag (data.Mid (0, terminator).ToString ());
			}

			ImageTag.AddTag (com_tag);
			AddMetadataBlock (position - 4, length + 4);
		}

		/// <summary>
		///    Creates a <see cref="ByteVector"/> for the comment segment of this file
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the whole comment segment, if a comment tag
		///    exists, otherwise null.
		/// </returns>
		private ByteVector RenderCOMSegment ()
		{
			// check, if Comment is contained
			JpegCommentTag com_tag = GetTag (TagTypes.JpegComment) as JpegCommentTag;
			if (com_tag == null)
				return null;

			// create comment data
			ByteVector com_data =
				ByteVector.FromString (com_tag.Value + "\0", StringType.Latin1);

			uint segment_size = (uint) (2 + com_data.Count);

			// do not render data segments, which cannot fit into the possible segment size
			if (segment_size > ushort.MaxValue)
				throw new Exception ("Comment Segment is too big to render");

			// create segment
			ByteVector data = new ByteVector (new byte [] { 0xFF, (byte) Marker.COM });
			data.Add (ByteVector.FromUShort ((ushort) segment_size));

			data.Add (com_data);

			return data;
		}

		/// <summary>
		///    Reads and parse a SOF segment
		/// </summary>
		/// <param name="length">
		///    The length of the segment that will be read.
		/// </param>
		/// <param name="marker">
		///    The SOFx marker.
		/// </param>
		void ReadSOFSegment (int length, Marker marker)
		{
#pragma warning disable 219 // Assigned, never read
			byte p = ReadBlock (1)[0];	//precision
#pragma warning restore 219

			//FIXME: according to specs, height could be 0 here, and should be retrieved from the DNL marker
			height = ReadBlock (2).ToUShort ();
			width = ReadBlock (2).ToUShort ();
		}

		/// <summary>
		///    Reads the DQT Segment, and Guesstimate the image quality from it
		/// </summary>
		/// <param name="length">
		///    The length of the segment that will be read
		/// </param>
		void ReadDQTSegment (int length)
		{
			// See CCITT Rec. T.81 (1992 E), B.2.4.1 (p39) for DQT syntax
			while (length > 0) {

				byte pqtq = ReadBlock (1)[0]; length --;
				byte pq = (byte)(pqtq >> 4);	//0 indicates 8-bit Qk, 1 indicates 16-bit Qk
				byte tq = (byte)(pqtq & 0x0f);	//table index;
				int [] table = null;
				switch (tq) {
				case 0:
					table = Table.StandardLuminanceQuantization;
					break;
				case 1:
					table = Table.StandardChrominanceQuantization;
					break;
				}

				bool allones = true; //check for all-ones tables (q=100)
				double cumsf = 0.0;
				//double cumsf2 = 0.0;
				for (int row = 0; row < 8; row ++) {
					for (int col = 0; col < 8; col++) {
						ushort val = ReadBlock (pq == 1 ? 2 : 1).ToUShort (); length -= (pq + 1);
						if (table != null) {
							double x = 100.0 * (double)val / (double)table [row*8+col]; //Scaling factor in percent
							cumsf += x;
							//cumsf2 += x*x;
							allones = allones && (val == 1);
						}
					}
				}

				if (table != null) {
					double local_q;
					cumsf /= 64.0;		// mean scale factor
					//cumfs2 /= 64.0;
					//double variance = cumsf2 - (cumsf * cumsf);

					if (allones)
						local_q = 100.0;
					else if (cumsf <= 100.0)
						local_q = (200.0 - cumsf) / 2.0;
					else
						local_q = 5000.0 / cumsf;
					quality = Math.Max (quality, (int)local_q);
				}
			}
		}

#endregion
	}
}
