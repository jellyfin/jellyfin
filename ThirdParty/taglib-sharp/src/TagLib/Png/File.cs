//
// File.cs: Provides tagging for PNG files
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TagLib;
using TagLib.Image;
using TagLib.Xmp;

namespace TagLib.Png
{

	/// <summary>
	///    This class extends <see cref="TagLib.Image.ImageBlockFile" /> to provide tagging
	///    for PNG image files.
	/// </summary>
	/// <remarks>
	///    This implementation is based on http://www.w3.org/TR/PNG
	/// </remarks>
	[SupportedMimeType("taglib/png", "png")]
	[SupportedMimeType("image/png")]
	public class File : TagLib.Image.ImageBlockFile
	{

#region GIF specific constants

		/// <summary>
		///    The PNG Header every png file starts with.
		/// </summary>
		private readonly byte [] HEADER = new byte [] {137, 80, 78, 71, 13, 10, 26, 10};

		/// <summary>
		///    byte sequence to indicate a IHDR Chunk
		/// </summary>
		private readonly byte [] IHDR_CHUNK_TYPE = new byte [] {73, 72, 68, 82};

		/// <summary>
		///    byte sequence to indicate a IEND Chunk
		/// </summary>
		private readonly byte [] IEND_CHUNK_TYPE = new byte [] {73, 69, 78, 68};

		/// <summary>
		///    byte sequence to indicate a iTXt Chunk
		/// </summary>
		private readonly byte [] iTXt_CHUNK_TYPE = new byte [] {105, 84, 88, 116};

		/// <summary>
		///    byte sequence to indicate a tEXt Chunk
		/// </summary>
		private readonly byte [] tEXt_CHUNK_TYPE = new byte [] {116, 69, 88, 116};

		/// <summary>
		///    byte sequence to indicate a zTXt Chunk
		/// </summary>
		private readonly byte [] zTXt_CHUNK_TYPE = new byte [] {122, 84, 88, 116};

		/// <summary>
		///    header of a iTXt which contains XMP data.
		/// </summary>
		private readonly byte [] XMP_CHUNK_HEADER = new byte [] {
			// Keyword ("XML:com.adobe.xmp")
			0x58, 0x4D, 0x4C, 0x3A, 0x63, 0x6F, 0x6D, 0x2E, 0x61, 0x64, 0x6F, 0x62, 0x65, 0x2E, 0x78, 0x6D, 0x70,

			// Null Separator
			0x00,

			// Compression Flag
			0x00,

			// Compression Method
			0x00,

			// Language Tag Null Separator
			0x00,

			// Translated Keyword Null Separator
			0x00
		};

#endregion

#region private fields

		/// <summary>
		///    The height of the image
		/// </summary>
		private int height;

		/// <summary>
		///    The width of the image
		/// </summary>
		private int width;

		/// <summary>
		///    The Properties of the image
		/// </summary>
		private Properties properties;

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

#region private methods

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
				ImageTag = new CombinedImageTag (TagTypes.XMP | TagTypes.Png);

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
		///    Validates the header of a PNG file. Therfore, the current position to
		///    read must be the start of the file.
		/// </summary>
		private void ValidateHeader ()
		{
			ByteVector data = ReadBlock (8);

			if (data.Count != 8)
				throw new CorruptFileException ("Unexpected end of header");

			if (! data.Equals (new ByteVector (HEADER)))
				throw new CorruptFileException ("PNG Header was expected");
		}


		/// <summary>
		///    Reads the length of data of a chunk from the current position
		/// </summary>
		/// <returns>
		///    A <see cref="System.Int32"/> with the length of data.
		/// </returns>
		/// <remarks>
		///    The length is stored in a 4-byte unsigned integer in the file,
		///    but due to the PNG specification this value does not exceed
		///    2^31-1 and can therfore be safely returned as an signed integer.
		///    This prevents unsafe casts for using the length as parameter
		///    for other methods.
		/// </remarks>
		private int ReadChunkLength ()
		{
			ByteVector data = ReadBlock (4);

			if (data.Count != 4)
				throw new CorruptFileException ("Unexpected end of Chunk Length");

			uint length = data.ToUInt (true);

			if (length > Int32.MaxValue)
				throw new CorruptFileException ("PNG limits the Chunk Length to 2^31-1");

			return (int) length;
		}


		/// <summary>
		///    Reads the type of a chunk from the current position.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with 4 bytes containing the type of
		///    the Chunk.
		/// </returns>
		private ByteVector ReadChunkType ()
		{
			ByteVector data = ReadBlock (4);

			if (data.Count != 4)
				throw new CorruptFileException ("Unexpected end of Chunk Type");

			return data;
		}


		/// <summary>
		///    Reads the CRC value for a chunk from the current position.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with 4 bytes with the CRC value.
		/// </returns>
		private ByteVector ReadCRC ()
		{
			ByteVector data = ReadBlock (4);

			if (data.Count != 4)
				throw new CorruptFileException ("Unexpected end of CRC");

			return data;
		}


		/// <summary>
		///    Reads the whole Chunk data starting from the current position.
		/// </summary>
		/// <param name="data_length">
		///    A <see cref="System.Int32"/> with the length of the Chunk Data.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the Chunk Data which is read.
		/// </returns>
		private ByteVector ReadChunkData (int data_length)
		{
			ByteVector data = ReadBlock (data_length);

			if (data.Count != data_length)
				throw new CorruptFileException (String.Format ("Chunk Data of Length {0} expected", data_length));

			return data;
		}


		/// <summary>
		///    Reads a null terminated string from the given data from given position.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> with teh data to read the string from
		/// </param>
		/// <param name="start_index">
		///    A <see cref="System.Int32"/> with the index to start reading
		/// </param>
		/// <param name="terminator_index">
		///    A <see cref="System.Int32"/> with the index of the null byte
		/// </param>
		/// <returns>
		///    A <see cref="System.String"/> with the read string. The null byte
		///    is not included.
		/// </returns>
		private string ReadTerminatedString (ByteVector data, int start_index, out int terminator_index)
		{
			if (start_index >= data.Count)
				throw new CorruptFileException ("Unexpected End of Data");

			terminator_index = data.Find ("\0", start_index);

			if (terminator_index < 0)
				throw new CorruptFileException ("Cannot find string terminator");

			return data.Mid (start_index, terminator_index - start_index).ToString ();
		}


		/// <summary>
		///    Reads a null terminated keyword from he given data from given position.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> with teh data to read the string from
		/// </param>
		/// <param name="start_index">
		///    A <see cref="System.Int32"/> with the index to start reading
		/// </param>
		/// <param name="terminator_index">
		///    A <see cref="System.Int32"/> with the index of the null byte
		/// </param>
		/// <returns>
		///    A <see cref="System.String"/> with the read keyword. The null byte
		///    is not included.
		/// </returns>
		private string ReadKeyword (ByteVector data, int start_index, out int terminator_index)
		{
			string keyword = ReadTerminatedString (data, start_index, out terminator_index);

			if (String.IsNullOrEmpty (keyword))
				throw new CorruptFileException ("Keyword cannot be empty");

			return keyword;
		}


		/// <summary>
		///    Skips the Chunk Data and CRC Data. The read position must be at the
		///    beginning of the Chunk data.
		/// </summary>
		/// <param name="data_size">
		///    A <see cref="System.Int32"/> with the length of the chunk data read
		///    before.
		/// </param>
		private void SkipChunkData (int data_size)
		{
			long position = Tell;

			if (position + data_size >= Length)
				throw new CorruptFileException (String.Format ("Chunk Data of Length {0} expected", data_size));

			Seek (Tell + data_size);
			ReadCRC ();
		}


		/// <summary>
		///    Reads the whole metadata from file. The current position must be set to
		///    the first Chunk which is contained in the file.
		/// </summary>
		private void ReadMetadata ()
		{
			int data_length = ReadChunkLength ();
			ByteVector type = ReadChunkType ();

			// File should start with a header chunk
			if (! type.StartsWith (IHDR_CHUNK_TYPE))
				throw new CorruptFileException (
					String.Format ("IHDR Chunk was expected, but Chunk {0} was found", type.ToString ()));

			ReadIHDRChunk (data_length);

			// Read all following chunks
			while (true) {

				data_length = ReadChunkLength ();
				type = ReadChunkType ();

				if (type.StartsWith (IEND_CHUNK_TYPE))
					return;
				else if (type.StartsWith (iTXt_CHUNK_TYPE))
					ReadiTXtChunk (data_length);
				else if (type.StartsWith (tEXt_CHUNK_TYPE))
					ReadtEXtChunk (data_length);
				else if (type.StartsWith (zTXt_CHUNK_TYPE))
					ReadzTXtChunk (data_length);
				else
					SkipChunkData (data_length);

			}
		}


		/// <summary>
		///    Reads the IHDR Chunk from file and extracts some image information
		///    like width and height. The current position must be set to the start
		///    of the Chunk Data.
		/// </summary>
		/// <param name="data_length">
		///     A <see cref="System.Int32"/> with the length of the Chunk Data.
		/// </param>
		private void ReadIHDRChunk (int data_length)
		{
			// IHDR Chunk
			//
			// 4 Bytes     Width
			// 4 Bytes     Height
			// 1 Byte      Bit depth
			// 1 Byte      Colour type
			// 1 Byte      Compression method
			// 1 Byte      Filter method
			// 1 Byte      Interlace method
			//
			// Followed by 4 Bytes CRC data

			if (data_length != 13)
				throw new CorruptFileException ("IHDR chunk data length must be 13");

			ByteVector data = ReadChunkData (data_length);

			CheckCRC (IHDR_CHUNK_TYPE, data, ReadCRC ());

			// The PNG specification limits the size of 4-byte unsigned integers to 2^31-1.
			// That allows us to safely cast them to an signed integer.
			uint width = data.Mid (0, 4).ToUInt (true);
			uint height = data.Mid (4, 4).ToUInt (true);

			if (width > Int32.MaxValue || height > Int32.MaxValue)
				throw new CorruptFileException ("PNG limits width and heigth to 2^31-1");

			this.width = (int) width;
			this.height = (int) height;
		}


		/// <summary>
		///    Reads an iTXt Chunk from file. The current position must be set
		///    to the start of the Chunk Data. Such a Chunk may contain XMP data
		///    or translated keywords.
		/// </summary>
		/// <param name="data_length">
		///    A <see cref="System.Int32"/> with the length of the Chunk Data.
		/// </param>
		private void ReadiTXtChunk (int data_length)
		{
			long position = Tell;

			// iTXt Chunk
			//
			// N Bytes     Keyword
			// 1 Byte      Null Separator
			// 1 Byte      Compression Flag (0 for uncompressed data)
			// 1 Byte      Compression Method
			// N Bytes     Language Tag
			// 1 Byte      Null Separator
			// N Bytes     Translated Keyword
			// 1 Byte      Null Terminator
			// N Bytes     Txt
			//
			// Followed by 4 Bytes CRC data

			ByteVector data = ReadChunkData (data_length);

			CheckCRC (iTXt_CHUNK_TYPE, data, ReadCRC ());

			// handle XMP, which has a fixed header
			if (data.StartsWith (XMP_CHUNK_HEADER)) {
				ImageTag.AddTag (new XmpTag (data.Mid (XMP_CHUNK_HEADER.Length).ToString (StringType.UTF8), this));

				AddMetadataBlock (position - 8, data_length + 8 + 4);

				return;
			}

			int terminator_index;
			string keyword = ReadKeyword (data, 0, out terminator_index);

			if (terminator_index + 2 >= data_length)
				throw new CorruptFileException ("Compression Flag and Compression Method byte expected");

			byte compression_flag = data[terminator_index + 1];
			byte compression_method = data[terminator_index + 2];

			//string language = ReadTerminatedString (data, terminator_index + 3, out terminator_index);
			//string translated_keyword = ReadTerminatedString (data, terminator_index + 1, out terminator_index);

			ByteVector txt_data = data.Mid (terminator_index + 1);

			if (compression_flag != 0x00) {
				txt_data = Decompress (compression_method, txt_data);

				// ignore unknown compression methods
				if (txt_data == null)
					return;
			}

			string value = txt_data.ToString ();
			PngTag png_tag = GetTag (TagTypes.Png, true) as PngTag;

			if (png_tag.GetKeyword (keyword) == null)
				png_tag.SetKeyword (keyword, value);

			AddMetadataBlock (position - 8, data_length + 8 + 4);
		}


		/// <summary>
		///    Reads an tEXt Chunk from file. The current position must be set
		///    to the start of the Chunk Data. Such a Chunk contains plain
		///    keywords.
		/// </summary>
		/// <param name="data_length">
		///    A <see cref="System.Int32"/> with the length of the Chunk Data.
		/// </param>
		private void ReadtEXtChunk (int data_length)
		{
			long position = Tell;

			// tEXt Chunk
			//
			// N Bytes     Keyword
			// 1 Byte      Null Separator
			// N Bytes     Txt
			//
			// Followed by 4 Bytes CRC data

			ByteVector data = ReadChunkData (data_length);

			CheckCRC (tEXt_CHUNK_TYPE, data, ReadCRC ());

			int keyword_terminator;
			string keyword = ReadKeyword (data, 0, out keyword_terminator);

			string value = data.Mid (keyword_terminator + 1).ToString ();

			PngTag png_tag = GetTag (TagTypes.Png, true) as PngTag;

			if (png_tag.GetKeyword (keyword) == null)
				png_tag.SetKeyword (keyword, value);

			AddMetadataBlock (position - 8, data_length + 8 + 4);
		}


		/// <summary>
		///    Reads an zTXt Chunk from file. The current position must be set
		///    to the start of the Chunk Data. Such a Chunk contains compressed
		///    keywords.
		/// </summary>
		/// <param name="data_length">
		///    A <see cref="System.Int32"/> with the length of the Chunk Data.
		/// </param>
		/// <remarks>
		///    The Chunk may also contain compressed Exif data which is written
		///    by other tools. But, since the PNG specification does not support
		///    Exif data, we ignore it here.
		/// </remarks>
		private void ReadzTXtChunk (int data_length)
		{
			long position = Tell;

			// zTXt Chunk
			//
			// N Bytes     Keyword
			// 1 Byte      Null Separator
			// 1 Byte      Compression Method
			// N Bytes     Txt
			//
			// Followed by 4 Bytes CRC data

			ByteVector data = ReadChunkData (data_length);

			CheckCRC (zTXt_CHUNK_TYPE, data, ReadCRC ());

			int terminator_index;
			string keyword = ReadKeyword (data, 0, out terminator_index);

			if (terminator_index + 1 >= data_length)
				throw new CorruptFileException ("Compression Method byte expected");

			byte compression_method = data[terminator_index + 1];

			ByteVector plain_data = Decompress (compression_method, data.Mid (terminator_index + 2));

			// ignore unknown compression methods
			if (plain_data == null)
				return;

			string value = plain_data.ToString ();

			PngTag png_tag = GetTag (TagTypes.Png, true) as PngTag;

			if (png_tag.GetKeyword (keyword) == null)
				png_tag.SetKeyword (keyword, value);

			AddMetadataBlock (position - 8, data_length + 8 + 4);
		}


		/// <summary>
		///    Save the metadata to file.
		/// </summary>
		private void SaveMetadata ()
		{
			ByteVector metadata_chunks = new ByteVector ();

			metadata_chunks.Add (RenderXMPChunk ());
			metadata_chunks.Add (RenderKeywordChunks ());

			// Metadata is stored after the PNG header and the IDHR chunk.
			SaveMetadata (metadata_chunks, HEADER.Length + 13 + 4 + 4 + 4);
		}


		/// <summary>
		///    Creates a Chunk containing the XMP data.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the XMP data chunk
		///    or <see langword="null" /> if no XMP data is contained.
		/// </returns>
		private ByteVector RenderXMPChunk ()
		{
			// Check, if XmpTag is contained
			XmpTag xmp = ImageTag.Xmp;
			if (xmp == null)
				return null;

			ByteVector chunk = new ByteVector ();

			// render the XMP data itself
			ByteVector xmp_data = xmp.Render ();

			// TODO check uint size.
			chunk.Add (ByteVector.FromUInt ((uint) xmp_data.Count + (uint) XMP_CHUNK_HEADER.Length));
			chunk.Add (iTXt_CHUNK_TYPE);
			chunk.Add (XMP_CHUNK_HEADER);
			chunk.Add (xmp_data);
			chunk.Add (ComputeCRC (iTXt_CHUNK_TYPE, XMP_CHUNK_HEADER, xmp_data));

			return chunk;
		}


		/// <summary>
		///    Creates a list of Chunks containing the PNG keywords
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> with the list of chunks, or
		///    or <see langword="null" /> if no PNG Keywords are contained.
		/// </returns>
		private ByteVector RenderKeywordChunks ()
		{
			// Check, if PngTag is contained
			PngTag png_tag = GetTag (TagTypes.Png, true) as PngTag;
			if (png_tag == null)
				return null;

			ByteVector chunks = new ByteVector ();

			foreach (KeyValuePair<string, string> keyword in png_tag) {
				ByteVector data = new ByteVector ();
				data.Add (keyword.Key);
				data.Add ("\0");
				data.Add (keyword.Value);

				chunks.Add (ByteVector.FromUInt ((uint) data.Count));
				chunks.Add (tEXt_CHUNK_TYPE);
				chunks.Add (data);
				chunks.Add (ComputeCRC (tEXt_CHUNK_TYPE, data));
			}

			return chunks;
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

#region Utility Stuff


		/// <summary>
		///    Checks the CRC for a Chunk.
		/// </summary>
		/// <param name="chunk_type">
		///    A <see cref="ByteVector"/> whith the Chunk type
		/// </param>
		/// <param name="chunk_data">
		///    A <see cref="ByteVector"/> with the Chunk data.
		/// </param>
		/// <param name="crc_data">
		///    A <see cref="ByteVector"/> with the read CRC data.
		/// </param>
		private static void CheckCRC (ByteVector chunk_type, ByteVector chunk_data, ByteVector crc_data)
		{
			ByteVector computed_crc = ComputeCRC (chunk_type, chunk_data);

			if (computed_crc != crc_data)
				throw new CorruptFileException (
					String.Format ("CRC check failed for {0} Chunk (expected: 0x{1:X4}, read: 0x{2:X4}",
					               chunk_type.ToString (), computed_crc.ToUInt (), crc_data.ToUInt ()));
		}


		/// <summary>
		///    Computes a 32bit CRC for the given data.
		/// </summary>
		/// <param name="datas">
		///    A <see cref="T:ByteVector[]"/> with data to compute
		///    the CRC for.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with 4 bytes (32bit) containing the CRC.
		/// </returns>
		private static ByteVector ComputeCRC (params ByteVector [] datas)
		{
			uint crc = 0xFFFFFFFF;

			if (crc_table == null)
				BuildCRCTable ();

			foreach (var data in datas) {

				foreach (byte b in data) {
					crc = crc_table[(crc ^ b) & 0xFF] ^ (crc >> 8);
				}
			}

			// Invert
			return ByteVector.FromUInt (crc ^ 0xFFFFFFFF);
		}


		/// <summary>
		///    Table for faster computation of CRC.
		/// </summary>
		private static uint[] crc_table;


		/// <summary>
		///    Initializes the CRC Table.
		/// </summary>
		private static void BuildCRCTable ()
		{
			uint polynom = 0xEDB88320;

			crc_table = new uint [256];

			for (int i = 0; i < 256; i++) {

				uint c = (uint) i;
				for (int k = 0; k < 8; k++) {
					if ((c & 0x00000001) != 0x00)
						c = polynom ^ (c >> 1);
					else
						c = c >> 1;
				}
				crc_table[i] = c;
			}
		}

		private static ByteVector Inflate (ByteVector data)
		{
			using (MemoryStream out_stream = new System.IO.MemoryStream ())
			using (var input = new MemoryStream (data.Data)) {
				input.Seek (2, SeekOrigin.Begin); // First 2 bytes are properties deflate does not need (or handle)
				using (var zipstream = new DeflateStream (input, CompressionMode.Decompress)) {
					//zipstream.CopyTo (out_stream); Cleaner with .NET 4
					byte[] buffer = new byte[1024];
					int written_bytes;

					while ((written_bytes = zipstream.Read (buffer, 0, 1024)) > 0)
						out_stream.Write (buffer, 0, written_bytes);

					return new ByteVector (out_stream.ToArray());
				}
			}
		}


		private static ByteVector Decompress (byte compression_method, ByteVector compressed_data)
		{
			// there is currently just one compression method specified
			// for PNG.
			switch (compression_method) {
			case 0:
				return Inflate (compressed_data);
			default:
				return null;
			}
		}

#endregion

	}
}
