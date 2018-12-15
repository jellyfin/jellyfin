//
// File.cs: Provides tagging and properties support for the DSD (Direct Stream Digital) DSF  
// file Format.
//
// Author:
//   Helmut Wahrmann
//
// Copyright (C) 2014 Helmut Wahrmann
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
using TagLib.Id3v2;

namespace TagLib.Dsf
{
	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide
	///    support for reading and writing tags and properties for files
	///    using the AIFF file format.
	/// </summary>
	[SupportedMimeType("taglib/dsf", "dsf")]
	[SupportedMimeType("audio/x-dsf")]
	[SupportedMimeType("audio/dsf")]
	[SupportedMimeType("sound/dsf")]
	[SupportedMimeType("application/x-dsf")]
  public class File : TagLib.File
	{
		#region Private Fields

		/// <summary>
		///    Contains the address of the DSF header block.
		/// </summary>
		private ByteVector header_block = null;

		/// <summary>
		///  Contains the Id3v2 tag.
		/// </summary>
		private Id3v2.Tag tag = null;

		/// <summary>
		///  Contains the media properties.
		/// </summary>
		private Properties properties = null;

		/// <summary>
		/// Contains the size of the DSF File
		/// </summary>
		private uint dsf_size = 0;

		/// <summary>
		/// Contains the start position of the Tag
		/// </summary>
		private long tag_start;

		/// <summary>
		/// Contains the end position of the Tag
		/// </summary>
		private long tag_end;

		#endregion

		#region Public Static Fields

		/// <summary>
		///    The identifier used to recognize a DSF file.
		/// </summary>
		/// <value>
		///    "DSD "
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "DSD ";

		/// <summary>
		///    The identifier used to recognize a Format chunk.
		/// </summary>
		/// <value>
		///    "fmt "
		/// </value>
		public static readonly ReadOnlyByteVector FormatIdentifier = "fmt ";

		/// <summary>
		///    The identifier used to recognize a DSF ID3 chunk.
		/// </summary>
		/// <value>
		///    "ID3 "
		/// </value>
		public static readonly ReadOnlyByteVector ID3Identifier = "ID3";

		#endregion

		#region Public Constructors

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
		public File(string path, ReadStyle propertiesStyle)
			: this(new File.LocalFileAbstraction(path),
			       propertiesStyle)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system with an average read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File(string path)
			: this(path, ReadStyle.Average)
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
		public File(File.IFileAbstraction abstraction,
		            ReadStyle propertiesStyle)
			: base(abstraction)
		{
			Mode = AccessMode.Read;
			try
			{
				Read(true, propertiesStyle, out dsf_size,
				     out tag_start, out tag_end);
			}
			finally
			{
				Mode = AccessMode.Closed;
			}

			TagTypesOnDisk = TagTypes;
			GetTag(TagTypes.Id3v2, true);
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction with an
		///    average read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File(File.IFileAbstraction abstraction)
			: this(abstraction, ReadStyle.Average)
		{
		}

		#endregion

		#region Public Properties

		/// <summary>
		///    Gets a abstract representation of all tags stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Tag" /> object representing all tags
		///    stored in the current instance.
		/// </value>
		public override Tag Tag
		{
			get { return tag; }
		}

		/// <summary>
		///    Gets the media properties of the file represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Properties" /> object containing the
		///    media properties of the file represented by the current
		///    instance.
		/// </value>
		public override TagLib.Properties Properties
		{
			get { return properties; }
		}

		#endregion

		#region Public Methods

		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		public override void Save()
		{
			// Boilerplate
			PreSave();

			Mode = AccessMode.Write;
			try
			{
				long original_tag_length = tag_end - tag_start;
				ByteVector data = new ByteVector();

				if (tag == null)
				{
					// The tag has been removed
					RemoveBlock(tag_start, original_tag_length);
					Insert(ByteVector.FromULong((ulong)(0),
												false), 20, 8);
				}
				else
				{
					data = tag.Render();

				// If tagging info cannot be found, place it at
				// the end of the file.
				if (tag_start == 0 || tag_end < tag_start)
				{
					tag_start = tag_end = Length;
					// Update the New Tag start
					Insert(ByteVector.FromULong((ulong)(tag_start),
												false), 20, 8);
				}

				// Insert the tagging data.
				Insert(data, tag_start, data.Count);
				}

				long length = dsf_size + data.Count - original_tag_length;

				// If the data size changed update the dsf  size.
				if (data.Count - original_tag_length != 0 &&
					tag_start <= dsf_size)
				{
					Insert(ByteVector.FromULong((ulong)(length),
												false), 12, 8);
				}
				// Update the tag types.
				TagTypesOnDisk = TagTypes;
			}
			finally
			{
				Mode = AccessMode.Closed;
			}
		}

		/// <summary>
		///    Removes a set of tag types from the current instance.
		/// </summary>
		/// <param name="types">
		///    A bitwise combined <see cref="TagLib.TagTypes" /> value
		///    containing tag types to be removed from the file.
		/// </param>
		/// <remarks>
		///    In order to remove all tags from a file, pass <see
		///    cref="TagTypes.AllTags" /> as <paramref name="types" />.
		/// </remarks>
		public override void RemoveTags(TagTypes types)
		{
			if (types == TagLib.TagTypes.Id3v2 ||
			    types == TagLib.TagTypes.AllTags)
			{
				tag = null;
			}
		}

		/// <summary>
		///    Gets a tag of a specified type from the current instance,
		///    optionally creating a new tag if possible.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagLib.TagTypes" /> value indicating the
		///    type of tag to read.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    try and create the tag if one is not found.
		/// </param>
		/// <returns>
		///    A <see cref="Tag" /> object containing the tag that was
		///    found in or added to the current instance. If no
		///    matching tag was found and none was created, <see
		///    langword="null" /> is returned.
		/// </returns>
		public override TagLib.Tag GetTag(TagTypes type, bool create)
		{
			TagLib.Tag id32_tag = null;

			switch (type)
			{
				case TagTypes.Id3v2:
					if (tag == null && create)
					{
						tag = new Id3v2.Tag();
						tag.Version = 2;
					}

					id32_tag = tag;
					break;
			}

			return id32_tag;
		}

		#endregion

		#region Private Methods

		/// <summary>
		///    Reads the contents of the current instance determining
		///    the size of the dsf data, the area the tagging is in,
		///    and optionally reading in the tags and media properties.
		/// </summary>
		/// <param name="read_tags">
		///    If <see langword="true" />, any tags found will be read
		///    into the current instance.
		/// </param>
		/// <param name="style">
		///    A <see cref="ReadStyle"/> value specifying how the media
		///    data is to be read into the current instance.
		/// </param>
		/// <param name="dsf_size">
		///    A <see cref="uint"/> value reference to be filled with
		///    the size of the RIFF data as read from the file.
		/// </param>
		/// <param name="tag_start">
		///    A <see cref="long" /> value reference to be filled with
		///    the absolute seek position at which the tagging data
		///    starts.
		/// </param>
		/// <param name="tag_end">
		///    A <see cref="long" /> value reference to be filled with
		///    the absolute seek position at which the tagging data
		///    ends.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    The file does not begin with <see cref="FileIdentifier"
		///    />.
		/// </exception>
		private void Read(bool read_tags, ReadStyle style,
							out uint dsf_size, out long tag_start,
							out long tag_end)
		{
			Seek(0);
			if (ReadBlock(4) != FileIdentifier)
				throw new CorruptFileException(
					"File does not begin with DSF identifier");

			Seek(12);
			dsf_size = ReadBlock(8).ToUInt(false);

			tag_start = (long)ReadBlock(8).ToULong(false);
			tag_end = -1;

			// Get the properties of the file
			if (header_block == null &&
				style != ReadStyle.None)
			{
				long fmt_chunk_pos = Find(FormatIdentifier, 0);

				if (fmt_chunk_pos == -1)
				{
					throw new CorruptFileException(
						"No Format chunk available in DSF file.");
				}

				Seek(fmt_chunk_pos);
				header_block = ReadBlock((int) StreamHeader.Size);

				StreamHeader header = new StreamHeader(header_block, dsf_size);
				properties = new Properties(TimeSpan.Zero, header);
			}

			// Now position to the ID3 chunk, which we read before
			if (tag_start > 0)
			{
				Seek(tag_start);
				if (ReadBlock(3) == ID3Identifier)
				{
					if (read_tags && tag == null)
					{
						tag = new Id3v2.Tag(this, tag_start, style);
					}

					// Get the length of the tag out of the ID3 chunk
					Seek(tag_start + 6);
					uint tag_size = SynchData.ToUInt(ReadBlock(4)) + 10;

					InvariantStartPosition = tag_start;
					tag_end = InvariantEndPosition = tag_start + tag_size;
				}
			}
		}
		#endregion
	}
}
