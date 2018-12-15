//
// File.cs: Provides tagging for GIF files
//
// Author:
//   Mike Gemuende (mike@gemuende.be)
//
// Copyright (C) 2010 Mike Gemuende
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
using System.IO;

using TagLib;
using TagLib.Image;
using TagLib.Xmp;


namespace TagLib.Gif
{

	/// <summary>
	///    This class extends <see cref="TagLib.Image.ImageBlockFile" /> to provide tagging
	///    and property support for Gif files.
	/// </summary>
	[SupportedMimeType("taglib/gif", "gif")]
	[SupportedMimeType("image/gif")]
	public class File : TagLib.Image.ImageBlockFile
	{

#region GIF specific constants

		/// <summary>
		///    Gif file signature which occurs at the begin of the file
		/// </summary>
		protected static readonly string SIGNATURE = "GIF";

		/// <summary>
		///    String which is used to indicate version the gif file format version 87a
		/// </summary>
		protected static readonly string VERSION_87A = "87a";

		/// <summary>
		///    String which is used to indicate version the gif file format version 89a
		/// </summary>
		protected static readonly string VERSION_89A = "89a";

		/// <summary>
		///    Application Extension Identifier for an XMP Block
		/// </summary>
		private static readonly string XMP_IDENTIFIER = "XMP Data";

		/// <summary>
		///    Application Authentication Extension Code for an XMP Block
		/// </summary>
		private static readonly string XMP_AUTH_CODE = "XMP";

		/// <summary>
		///    The Magic Trailer for XMP Data
		/// </summary>
		/// <remarks>
		///    The storage of XMP data in GIF does not follow the GIF specification. According to the
		///    specification, extension data is stored in so-called sub-blocks, which start with a length
		///    byte which specifies the number of data bytes contained in the sub block. So a block can at
		///    most contain 256 data bytes. After a sub-block, the next sub-block begins. The sequence ends,
		///    when a sub-block starts with 0. So readers, which are not aware of the XMP data not following
		///    this scheme, will get confused by the XMP data. To fix this, this trailer is added to the end.
		///    It has a length of 258 bytes, so that it is ensured that a reader which tries to skip the
		///    XMP data reads one of this bytes as length of a sub-block. But, each byte points with its length
		///    to the last one. Therefoe, independent of the byte, the reader reads as sub-block length, it is
		///    redirected to the last byte of the trailer and therfore to the end of the XMP data.
		/// </remarks>
		private static readonly byte [] XMP_MAGIC_TRAILER = new byte [] {
			0x01, 0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8, 0xF7, 0xF6, 0xF5, 0xF4, 0xF3, 0xF2, 0xF1,
			0xF0, 0xEF, 0xEE, 0xED, 0xEC, 0xEB, 0xEA, 0xE9, 0xE8, 0xE7, 0xE6, 0xE5, 0xE4, 0xE3, 0xE2, 0xE1,
			0xE0, 0xDF, 0xDE, 0xDD, 0xDC, 0xDB, 0xDA, 0xD9, 0xD8, 0xD7, 0xD6, 0xD5, 0xD4, 0xD3, 0xD2, 0xD1,
			0xD0, 0xCF, 0xCE, 0xCD, 0xCC, 0xCB, 0xCA, 0xC9, 0xC8, 0xC7, 0xC6, 0xC5, 0xC4, 0xC3, 0xC2, 0xC1,
			0xC0, 0xBF, 0xBE, 0xBD, 0xBC, 0xBB, 0xBA, 0xB9, 0xB8, 0xB7, 0xB6, 0xB5, 0xB4, 0xB3, 0xB2, 0xB1,
			0xB0, 0xAF, 0xAE, 0xAD, 0xAC, 0xAB, 0xAA, 0xA9, 0xA8, 0xA7, 0xA6, 0xA5, 0xA4, 0xA3, 0xA2, 0xA1,
			0xA0, 0x9F, 0x9E, 0x9D, 0x9C, 0x9B, 0x9A, 0x99, 0x98, 0x97, 0x96, 0x95, 0x94, 0x93, 0x92, 0x91,
			0x90, 0x8F, 0x8E, 0x8D, 0x8C, 0x8B, 0x8A, 0x89, 0x88, 0x87, 0x86, 0x85, 0x84, 0x83, 0x82, 0x81,
			0x80, 0x7F, 0x7E, 0x7D, 0x7C, 0x7B, 0x7A, 0x79, 0x78, 0x77, 0x76, 0x75, 0x74, 0x73, 0x72, 0x71,
			0x70, 0x6F, 0x6E, 0x6D, 0x6C, 0x6B, 0x6A, 0x69, 0x68, 0x67, 0x66, 0x65, 0x64, 0x63, 0x62, 0x61,
			0x60, 0x5F, 0x5E, 0x5D, 0x5C, 0x5B, 0x5A, 0x59, 0x58, 0x57, 0x56, 0x55, 0x54, 0x53, 0x52, 0x51,
			0x50, 0x4F, 0x4E, 0x4D, 0x4C, 0x4B, 0x4A, 0x49, 0x48, 0x47, 0x46, 0x45, 0x44, 0x43, 0x42, 0x41,
			0x40, 0x3F, 0x3E, 0x3D, 0x3C, 0x3B, 0x3A, 0x39, 0x38, 0x37, 0x36, 0x35, 0x34, 0x33, 0x32, 0x31,
			0x30, 0x2F, 0x2E, 0x2D, 0x2C, 0x2B, 0x2A, 0x29, 0x28, 0x27, 0x26, 0x25, 0x24, 0x23, 0x22, 0x21,
			0x20, 0x1F, 0x1E, 0x1D, 0x1C, 0x1B, 0x1A, 0x19, 0x18, 0x17, 0x16, 0x15, 0x14, 0x13, 0x12, 0x11,
			0x10, 0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
			0x00, 0x00
		};

#endregion

#region private fields

		/// <summary>
		///    The width of the image
		/// </summary>
		private int width;

		/// <summary>
		///    The height of the image
		/// </summary>
		private int height;

		/// <summary>
		///    The Properties of the image
		/// </summary>
		private Properties properties;

		/// <summary>
		///    The version of the file format
		/// </summary>
		private string version;

		/// <summary>
		///    The start of the first block in file after the header.
		/// </summary>
		private long start_of_blocks = -1;

#endregion

#region public Properties

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

#region constructors

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


#region Public Methods

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
				SaveMetadata ();

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
				ImageTag = new CombinedImageTag (TagTypes.XMP | TagTypes.GifComment);

				ReadHeader ();
				ReadMetadata ();

				TagTypesOnDisk = TagTypes;

				if ((propertiesStyle & ReadStyle.Average) != 0)
					properties = ExtractProperties ();

			} finally {
				Mode = AccessMode.Closed;
			}
		}


		/// <summary>
		///   Reads a single byte form file. This is needed often for Gif files.
		/// </summary>
		/// <returns>
		///   A <see cref="System.Byte"/> with the read data.
		/// </returns>
		private byte ReadByte ()
		{
			ByteVector data = ReadBlock (1);

			if (data.Count != 1)
				throw new CorruptFileException ("Unexpected end of file");

			return data[0];
		}


		/// <summary>
		///    Reads the Header and the Logical Screen Descriptor of the GIF file and,
		///    if there is one, skips the global color table. It also extracts the
		///    image width and height from it.
		/// </summary>
		private void ReadHeader ()
		{
			// The header consists of:
			//
			// 3 Bytes        Signature
			// 3 Bytes        Version
			//
			// The Logical Screen Descriptor of:
			//
			// 2 Bytes        Width  (little endian)
			// 2 Bytes        Height (little endian)
			// 1 Byte         Screen and Color Map (packed field)
			// 1 Byte         Background Color
			// 1 Byte         Aspect Ratio
			//
			// Whereas the bits of the packed field contains some special information.
			ByteVector data = ReadBlock (13);

			if (data.Count != 13)
				throw new CorruptFileException ("Unexpected end of Header");

			if (data.Mid (0, 3).ToString () != SIGNATURE)
				throw new CorruptFileException (String.Format ("Expected a GIF signature at start of file, but found: {0}", data.Mid (0, 3).ToString ()));

			// We do not care about the version here, because we can read both versions in the same way.
			// We just care when writing metadata, that, if necessary, the version is increased to 89a.
			var read_version = data.Mid (3, 3).ToString ();
			if (read_version == VERSION_87A || read_version == VERSION_89A)
				version = read_version;
			else
				throw new UnsupportedFormatException (
					String.Format ("Only GIF versions 87a and 89a are currently supported, but not: {0}", read_version));

			// Read Image Size (little endian)
			width = data.Mid (6, 2).ToUShort (false);
			height = data.Mid (8, 2).ToUShort (false);

			// Skip optional global color table
			SkipColorTable (data [10]);
		}


		/// <summary>
		///    Reads the metadata from file. The current position must point to the
		///    start of the first block after the Header and Logical Screen
		///    Descriptor (and, if there is one, the Global Color Table)
		/// </summary>
		private void ReadMetadata ()
		{
			start_of_blocks = Tell;

			// Read Blocks until end of file is reached.
			while (true) {
				byte identifier = ReadByte ();

				switch (identifier) {
				case 0x2c:
					SkipImage ();
					break;

				case 0x21:
					ReadExtensionBlock ();
					break;

				case 0x3B:
					return;

				default:
					throw new CorruptFileException (
						String.Format ("Do not know what to do with byte 0x{0:X2} at the beginning of a block ({1}).", identifier, Tell - 1));
				}
			}
		}

		/// <summary>
		///    Reads an Extension Block at the current position. The current position must
		///    point to the 2nd byte of the comment block. (The other byte is usually
		///    read before to identify the comment block)
		/// </summary>
		private void ReadExtensionBlock ()
		{
			// Extension Block
			//
			// 1 Byte       Extension Introducer (0x21)
			// 1 Byte       Extension Identifier
			// ....
			//
			// Note, the Extension Introducer was read before to
			// identify the Extension Block. Therefore, it has not
			// to be handled here.
			byte identifier = ReadByte ();

			switch (identifier) {
			case 0xFE:
				ReadCommentBlock ();
				break;

			case 0xFF:
				ReadApplicationExtensionBlock ();
				break;

			// Control Extension Block, ...
			case 0xF9:
			// ... Plain Text Extension ...
			case 0x01:
			// ... and all other unknown blocks can be skipped by just
			// reading sub-blocks.
			default:
				SkipSubBlocks ();
				break;
			}
		}


		/// <summary>
		///    Reads an Application Extension Block at the current position. The current
		///    position must point to the 3rd byte of the comment block. (The other 2 bytes
		///    are usually read before to identify the comment block)
		/// </summary>
		private void ReadApplicationExtensionBlock ()
		{
			// Application Extension Block
			//
			// 1 Byte       Extension Introducer (0x21)
			// 1 Byte       Application Extension Label (0xFF)
			// 1 Byte       Block Size (0x0B - 11)
			// 8 Bytes      Application Identifier
			// 3 Bytes      Application Auth. Code
			// N Bytes      Application Data (sub blocks)
			// 1 Byte       Block Terminator (0x00)
			//
			// Note, the first 2 bytes are still read to identify the Comment Block.
			// Therefore, we only need to read the sub blocks and extract the data.
			long position = Tell;
			ByteVector data = ReadBlock (12);

			if (data.Count != 12)
				throw new CorruptFileException ("");

			// Contains XMP data
			if (data.Mid (1, 8) == XMP_IDENTIFIER &&
			    data.Mid (9, 3) == XMP_AUTH_CODE) {
				// XMP Data is not organized in sub-blocks

				// start of xmp data
				long data_start = Tell;

				// start of trailer start
				// FIXME: Since File.Find is still buggy, the following call does not work to find the end of the
				// XMP data. Therfore, we use here a different way for now.
				//long xmp_trailer_start = Find (new ByteVector (0x00), data_start);

				// Since searching just one byte is save, we search for the end of the xmp trailer which
				// consists of two 0x00 bytes and compute the expected start.
				long xmp_trailer_start = Find (new byte [] {0x00}, data_start) - XMP_MAGIC_TRAILER.Length + 2;

				Seek (data_start, SeekOrigin.Begin);

				if (xmp_trailer_start <= data_start)
					throw new CorruptFileException ("No End of XMP data found");

				// length of xmp data
				int data_length = (int) (xmp_trailer_start - data_start);

				ByteVector xmp_data = ReadBlock (data_length);
				ImageTag.AddTag (new XmpTag (xmp_data.ToString (StringType.UTF8), this));

				// 2 bytes where read before
				AddMetadataBlock (position - 2, 14 + data_length + XMP_MAGIC_TRAILER.Length);

				// set position behind the XMP block
				Seek (xmp_trailer_start + XMP_MAGIC_TRAILER.Length, SeekOrigin.Begin);

			} else {
				SkipSubBlocks ();
			}
		}


		/// <summary>
		///    Reads a Comment Block at the current position. The current position must
		///    point to the 3rd byte of the comment block. (The other 2 bytes are usually
		///    read before to identify the comment block)
		/// </summary>
		private void ReadCommentBlock ()
		{
			long position = Tell;

			// Comment Extension
			//
			// 1 Byte       Extension Introducer (0x21)
			// 1 Byte       Comment Label (0xFE)
			// N Bytes      Comment Data (Sub Blocks)
			// 1 Byte       Block Terminator (0x00)
			//
			// Note, the first 2 bytes are still read to identify the Comment Block.
			// Therefore, we only need to read the sub blocks and extract the data.

			string comment = ReadSubBlocks ();

			// Only add the tag, if no one is still contained.
			if ((TagTypes & TagTypes.GifComment) == 0x00) {
				ImageTag.AddTag (new GifCommentTag (comment));

				// 2 bytes where read before
				AddMetadataBlock (position - 2, Tell - position + 2);
			}
		}


		/// <summary>
		///    Skips the color table if there is one
		/// </summary>
		/// <param name="packed_data">
		///    A <see cref="System.Byte"/> with the packed data which is
		///    contained Logical Screen Descriptor or in the Image Descriptor.
		/// </param>
		/// <remarks>
		///    The data contained in the packed data is different for the Logical
		///    Screen Descriptor and for the Image Descriptor. But fortunately,
		///    the bits which are used do identifying the exitstance and the size
		///    of the color table are at the same position.
		/// </remarks>
		private void SkipColorTable (byte packed_data)
		{
			// Packed Field (Information with Bit 0 is  LSB)
			//
			// Bit 0-2      Size of Color Table
			// Bit 3-6      Other stuff
			// Bit 7        (Local|Global) Color Table Flag
			//
			// We are interested in Bit 7 which indicates if a global color table is
			// present or not and the Bits 0-2 which indicate the size of the color
			// table.
			if ((packed_data & 0x80) == 0x80) {
				// 2^(size + 1) for each color.
				int table_size = 3 * (1 << ((packed_data & 0x07) + 1));

				// and simply skip the color table
				ByteVector color_table = ReadBlock (table_size);

				if (color_table.Count != table_size)
					throw new CorruptFileException ("Unexpected end of Color Table");

			}
		}


		/// <summary>
		///    Skip over the image data at the current position. The current position must
		///    point to 2nd byte of the Image Descriptor. (First byte is usually read before
		///    to identify the image descriptor.)
		/// </summary>
		private void SkipImage ()
		{
			// Image Descriptor
			//
			// 1 Byte         Separator (0x2C)
			// 2 Bytes        Image Left Position   (little endian)
			// 2 Bytes        Image Right Position  (little endian)
			// 2 Bytes        Image Witdh           (little endian)
			// 2 Bytes        Image Height          (little endian)
			// 1 Byte         Packed Data
			//
			// Note, the Separator was read before to identify the Image Block
			// Therefore, we only need to read 9 bytes here.
			ByteVector data = ReadBlock (9);

			if (data.Count != 9)
				throw new CorruptFileException ("Unexpected end of Image Descriptor");

			// Skip an optional local color table
			SkipColorTable (data [8]);


			// Image Data
			//
			// 1 Byte         LZW Minimum Code Size
			// N Bytes        Image Data (Sub-Blocks)
			//
			// Before the image data, one byte for LZW encoding information is used.
			// This byte is read first, then the sub-blocks are skipped.
			ReadBlock (1);
			SkipSubBlocks ();
		}


		/// <summary>
		///    Reads a sequence of sub-blocks from the current position and concatenates the data
		///    from the sub-blocks to a string. The current position must point to the size-byte
		///    of the first subblock to skip.
		/// </summary>
		/// <returns>
		///    A <see cref="System.String"/> with the data contained in the sub-blocks.
		/// </returns>
		private string ReadSubBlocks ()
		{
			// Sub Block
			// Starts with one byte with the number of data bytes
			// following. The last sub block is terminated by length 0
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();

			byte length = 0;

			do {

				if (length >= 0)
					builder.Append (ReadBlock (length).ToString ());

				// read new length byte
				length = ReadByte ();

			// The sub-blocks are terminated with 0
			} while (length != 0);

			return builder.ToString ();
		}


		/// <summary>
		///    Skips over a sequence of sub-blocks from the current position in the file.
		///    The current position must point to the size-byte of the first subblock to skip.
		/// </summary>
		private void SkipSubBlocks ()
		{
			// Sub Block
			// Starts with one byte with the number of data bytes
			// following. The last sub block is terminated by length 0
			byte length = 0;

			do {

				if (Tell + length >= Length)
					throw new CorruptFileException ("Unexpected end of Sub-Block");

				// Seek to end of sub-block and update the position
				Seek (Tell + length, SeekOrigin.Begin);


				// read new length byte
				length = ReadByte ();

			// The sub-blocks are terminated with 0
			} while (length != 0);
		}


		/// <summary>
		///    Save the metadata to file.
		/// </summary>
		private void SaveMetadata ()
		{
			ByteVector comment_block = RenderGifCommentBlock ();
			ByteVector xmp_block = RenderXMPBlock ();

			// If we write metadata and the version is not 89a, bump the format version
			// because application extension blocks and comment extension blocks are
			// specified in 89a.
			// If we do not write metadata or if metadata is deleted, we do not care
			// about the version, because it may be wrong before.
			if (comment_block != null && xmp_block != null && version != VERSION_89A) {
				Insert (VERSION_89A, 3, VERSION_89A.Length);
			}

			// now, only metadata is stored at the beginning of the file, and we can overwrite it.
			ByteVector metadata_blocks = new ByteVector ();
			metadata_blocks.Add (comment_block);
			metadata_blocks.Add (xmp_block);

			SaveMetadata (metadata_blocks, start_of_blocks);
		}


		/// <summary>
		///    Renders the XMP data to a Application Extension Block which can be
		///    embedded in a Gif file.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the Application Extension Block for the
		///    XMP data, or <see langword="null" /> if the file does not have XMP data.
		/// </returns>
		private ByteVector RenderXMPBlock ()
		{
			// Check, if XmpTag is contained
			XmpTag xmp = ImageTag.Xmp;
			if (xmp == null)
				return null;

			ByteVector xmp_data = new ByteVector ();

			// Add Extension Introducer (0x21), Application Extension Label (0xFF) and
			// the Block Size (0x0B
			xmp_data.Add (new byte [] {0x21, 0xFF, 0x0B});

			// Application Identifier and Appl. Auth. Code
			xmp_data.Add (XMP_IDENTIFIER);
			xmp_data.Add (XMP_AUTH_CODE);

			// Add XMP data and Magic Trailer
			// For XMP, we do not need to store the data in sub-blocks, therfore we
			// can just add the whole rendered data. (The trailer fixes this)
			xmp_data.Add (xmp.Render ());
			xmp_data.Add (XMP_MAGIC_TRAILER);

			return xmp_data;
		}


		/// <summary>
		///    Renders the Gif Comment to a Comment Extension Block which can be
		///    embedded in a Gif file.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the Comment Extension Block for the
		///    Gif Comment, or <see langword="null" /> if the file does not have
		///    a Gif Comment.
		/// </returns>
		private ByteVector RenderGifCommentBlock ()
		{
			// Check, if GifCommentTag is contained
			GifCommentTag comment_tag = GetTag (TagTypes.GifComment) as GifCommentTag;
			if (comment_tag == null)
				return null;

			string comment = comment_tag.Comment;
			if (comment == null)
				return null;

			ByteVector comment_data = new ByteVector ();

			// Add Extension Introducer (0x21) and Comment Label (0xFE)
			comment_data.Add (new byte [] {0x21, 0xFE});

			// Add data of comment in sub-blocks of max length 256.
			ByteVector comment_bytes = new ByteVector (comment);
			byte block_max = 255;
			for (int start = 0; start < comment_bytes.Count; start += block_max) {
				byte block_length = (byte) Math.Min (comment_bytes.Count - start, block_max);

				comment_data.Add (block_length);
				comment_data.Add (comment_bytes.Mid (start, block_length));
			}
			comment_data.Add (new byte [] {0x00});

			return comment_data;
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
				return new Properties (TimeSpan.Zero, new Codec (width, height));

			return null;

		}

#endregion

	}
}
