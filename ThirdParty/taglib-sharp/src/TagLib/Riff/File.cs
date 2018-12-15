//
// File.cs:
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
using System.Collections;
using System.Collections.Generic;

namespace TagLib.Riff
{
	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide
	///    support for reading and writing tags and properties for files
	///    using the RIFF file format such as AVI and Wave files.
	/// </summary>
	[SupportedMimeType("taglib/avi", "avi")]
	[SupportedMimeType("taglib/wav", "wav")]
	[SupportedMimeType("taglib/divx", "divx")]
	[SupportedMimeType("video/avi")]
	[SupportedMimeType("video/msvideo")]
	[SupportedMimeType("video/x-msvideo")]
	[SupportedMimeType("image/avi")]
	[SupportedMimeType("application/x-troff-msvideo")]
	[SupportedMimeType("audio/avi")]
	[SupportedMimeType("audio/wav")]
	[SupportedMimeType("audio/wave")]
	[SupportedMimeType("audio/x-wav")]
	public class File : TagLib.File
	{
#region Private Fields
		
		/// <summary>
		///  Contains all the tags of the file.
		/// </summary>
		private CombinedTag tag = new CombinedTag ();
		
		/// <summary>
		///  Contains the INFO tag.
		/// </summary>
		private InfoTag info_tag = null;
		
		/// <summary>
		///  Contains the MovieID tag.
		/// </summary>
		private MovieIdTag mid_tag = null;
		
		/// <summary>
		///  Contains the DivX tag.
		/// </summary>
		private DivXTag divx_tag = null;
		
		/// <summary>
		///  Contains the Id3v2 tag.
		/// </summary>
		private Id3v2.Tag id32_tag = null;
		
		/// <summary>
		///  Contains the media properties.
		/// </summary>
		private Properties properties = null;
		
#endregion
		
		
		
#region Public Static Fields
		
		/// <summary>
		///    The identifier used to recognize a RIFF files.
		/// </summary>
		/// <value>
		///    "RIFF"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "RIFF";
		
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
		public File (string path, ReadStyle propertiesStyle)
			: this (new File.LocalFileAbstraction (path),
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
			uint riff_size;
			long tag_start, tag_end;

			Mode = AccessMode.Read;
			try {
				Read (true, propertiesStyle, out riff_size,
					out tag_start, out tag_end);
			} finally {
				Mode = AccessMode.Closed;
			}

			TagTypesOnDisk = TagTypes;

			GetTag (TagTypes.Id3v2, true);
			GetTag (TagTypes.RiffInfo, true);
			GetTag (TagTypes.MovieId, true);
			GetTag (TagTypes.DivX, true);
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
		public File (File.IFileAbstraction abstraction)
			: this (abstraction, ReadStyle.Average)
		{}
		
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
		public override Tag Tag {
			get {return tag;}
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
		public override TagLib.Properties Properties {
			get {return properties;}
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
				ByteVector data = new ByteVector ();
				
				// Enclose the Id3v2 tag in an "id3 " item and
				// embed it as the first tag.
				if (id32_tag != null) {
					ByteVector tag_data = id32_tag.Render ();
					if (tag_data.Count > 10) {
						if (tag_data.Count % 2 == 1)
							tag_data.Add (0);
						data.Add("id3 ");
						data.Add (ByteVector.FromUInt (
							(uint) tag_data.Count,
							false));
						data.Add (tag_data);
					}
				}
				
				// Embed "INFO" as the second tag.
				if (info_tag != null)
					data.Add (info_tag.RenderEnclosed ());

				// Embed "MID " as the third tag.
				if (mid_tag != null)
					data.Add (mid_tag.RenderEnclosed ());

				// Embed the DivX tag in "IDVX and embed it as
				// the fourth tag.
				if (divx_tag != null && !divx_tag.IsEmpty) {
					ByteVector tag_data = divx_tag.Render ();
					data.Add ("IDVX");
					data.Add (ByteVector.FromUInt (
						(uint) tag_data.Count, false));
					data.Add (tag_data);
				}
				
				// Read the file to determine the current RIFF
				// size and the area tagging does in.
				uint riff_size;
				long tag_start, tag_end;
				Read (false, ReadStyle.None, out riff_size,
					out tag_start, out tag_end);
				
				// If tagging info cannot be found, place it at
				// the end of the file.
				if (tag_start < 12 || tag_end < tag_start)
					tag_start = tag_end = Length;
				
				int length = (int)(tag_end - tag_start);
				
				// If the tag isn't at the end of the file,
				// try appending using padding to improve
				// write time now or for subsequent writes.
				if (tag_end != Length) {
					int padding_size = length - data.Count - 8;
					if (padding_size < 0)
						padding_size = 1024;
					
					
					data.Add ("JUNK");
					data.Add (ByteVector.FromUInt (
						(uint)padding_size, false));
					data.Add (new ByteVector (padding_size));
				}
				
				// Insert the tagging data.
				Insert (data, tag_start, length);
				
				// If the data size changed, and the tagging
				// data is within the RIFF portion of the file,
				// update the riff size.
				if (data.Count - length != 0 &&
					tag_start <= riff_size)
					Insert (ByteVector.FromUInt ((uint)
						(riff_size + data.Count - length),
						false), 4, 4);
				
				// Update the tag types.
				TagTypesOnDisk = TagTypes;
			} finally {
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
		public override void RemoveTags (TagTypes types)
		{
			if ((types & TagLib.TagTypes.Id3v2) != TagLib.TagTypes.None)
				id32_tag = null;
			if ((types & TagLib.TagTypes.RiffInfo) != TagLib.TagTypes.None)
				info_tag = null;
			if ((types & TagLib.TagTypes.MovieId) != TagLib.TagTypes.None)
				mid_tag  = null;
			if ((types & TagLib.TagTypes.DivX) != TagLib.TagTypes.None)
				divx_tag = null;

			tag.SetTags (id32_tag, info_tag, mid_tag, divx_tag);
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
		public override TagLib.Tag GetTag (TagTypes type, bool create)
		{
			TagLib.Tag tag = null;

			switch (type)
			{
			case TagTypes.Id3v2:
				if (id32_tag == null && create) {
					id32_tag = new Id3v2.Tag ();
					id32_tag.Version = 4;
					id32_tag.Flags |= Id3v2.HeaderFlags
						.FooterPresent;
					this.tag.CopyTo (id32_tag, true);
				}
				
				tag = id32_tag;
				break;
				
			case TagTypes.RiffInfo:
				if (info_tag == null && create) {
					info_tag = new InfoTag ();
					this.tag.CopyTo (info_tag, true);
				}
				
				tag = info_tag;
				break;
				
			case TagTypes.MovieId:
				if (mid_tag == null && create) {
					mid_tag = new MovieIdTag ();
					this.tag.CopyTo (mid_tag, true);
				}
				
				tag = mid_tag;
				break;
				
			case TagTypes.DivX:
				if (divx_tag == null && create) {
					divx_tag = new DivXTag ();
					this.tag.CopyTo (divx_tag, true);
				}
				
				tag = divx_tag;
				break;
			}

			this.tag.SetTags (id32_tag, info_tag, mid_tag, divx_tag);
			return tag;
		}
		
#endregion
		
		
		
#region Private Methods
		
		/// <summary>
		///    Reads the contents of the current instance determining
		///    the size of the riff data, the area the tagging is in,
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
		/// <param name="riff_size">
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
		private void Read (bool read_tags, ReadStyle style,
		                   out uint riff_size, out long tag_start,
		                   out long tag_end)
		{
			Seek (0);
			if (ReadBlock (4) != FileIdentifier)
				throw new CorruptFileException (
					"File does not begin with RIFF identifier");
			
			riff_size = ReadBlock (4).ToUInt (false);
			ByteVector stream_format = ReadBlock (4);
			tag_start = -1;
			tag_end   = -1;
			
			long position = 12;
			long length = Length;
			uint size = 0;
			TimeSpan duration = TimeSpan.Zero;
			ICodec [] codecs = new ICodec [0];
			
			// Read until there are less than 8 bytes to read.
			do {
				bool tag_found = false;

				// Check if the current position is an odd number and increment it so it is even
				// This is done when the previous chunk size was an odd number.
				// If this is not done, the chunk being read after the odd chunk will not be read.
				if (position > 12 && (position & 1) != 0) { position++; }

				Seek (position);
				string fourcc = ReadBlock (4).ToString (StringType.UTF8);
				size = ReadBlock (4).ToUInt (false);

				switch (fourcc)
				{
				
				// "fmt " is used by Wave files to hold the
				// WaveFormatEx structure.
				case "fmt ":
					if (style == ReadStyle.None ||
						stream_format != "WAVE")
						break;
					
					Seek (position + 8);
					codecs = new ICodec [] {
						new WaveFormatEx (ReadBlock (18), 0)
					};
					break;
				
				// "data" contains the audio data for wave
				// files. It's contents represent the invariant
				// portion of the file and is used to determine
				// the duration of a file. It should always
				// appear after "fmt ".
				case "data":
					if (stream_format != "WAVE")
						break;
					
					InvariantStartPosition = position;
					InvariantEndPosition = position + size;
					
					if (style == ReadStyle.None ||
						codecs.Length != 1 ||
						!(codecs [0] is WaveFormatEx))
						break;
					
					duration += TimeSpan.FromSeconds (
						(double) size / (double)
						((WaveFormatEx) codecs [0])
							.AverageBytesPerSecond);
					
					break;
				
				// Lists are used to store a variety of data
				// collections. Read the type and act on it.
				case "LIST":
				{
					switch (ReadBlock (4).ToString (StringType.UTF8))
					{
					
					// "hdlr" is used by AVI files to hold
					// a media header and BitmapInfoHeader
					// and WaveFormatEx structures.
					case "hdrl":
						if (style == ReadStyle.None ||
							stream_format != "AVI ")
							continue;
						
						AviHeaderList header_list =
							new AviHeaderList (this,
								position + 12,
								(int) (size - 4));
						duration = header_list.Header.Duration;
						codecs = header_list.Codecs;
						break;
					
					// "INFO" is a tagging format handled by
					// the InfoTag class.
					case "INFO":
						if (read_tags && info_tag == null)
							info_tag = new InfoTag (
								this,
								position + 12,
								(int) (size - 4));
						
						tag_found = true;
						break;
					
					// "MID " is a tagging format handled by
					// the MovieIdTag class.
					case "MID ":
						if (read_tags && mid_tag == null)
							mid_tag = new MovieIdTag (
								this,
								position + 12,
								(int) (size - 4));
						
						tag_found = true;
						break;
					
					// "movi" contains the media data for
					// and AVI and its contents represent
					// the invariant portion of the file.
					case "movi":
						if (stream_format != "AVI ")
							break;
						
						InvariantStartPosition = position;
						InvariantEndPosition = position + size;
						break;
					}
					break;
				}

				// "ID32" is a custom box for this format that
				// contains an ID3v2 tag.
				// "ID3 " and "id3 " have become standard (de facto)
				case "id3 ":
				case "ID3 ":
				case "ID32":
					if (read_tags && id32_tag == null)
						id32_tag = new Id3v2.Tag (this,
							position + 8, style);
					
					tag_found = true;
					break;
				
				// "IDVX" is used by DivX and holds an ID3v1-
				// style tag.
				case "IDVX":
					if (read_tags && divx_tag == null)
						divx_tag = new DivXTag (this,
							position + 8);
					
					tag_found = true;
					break;
				
				// "JUNK" is a padding element that could be
				// associated with tag data.
				case "JUNK":
					if (tag_end == position)
						tag_end = position + 8 + size;
					break;
				}
				
				// Determine the region of the file that
				// contains tags.
				if (tag_found) {
					if (tag_start == -1) {
						tag_start = position;
						tag_end = position + 8 + size;
					} else if (tag_end == position) {
						tag_end = position + 8 + size;
					}
				}
				
				// Move to the next item.
			} while ((position += 8L + size) + 8 < length);
			
			// If we're reading properties, and one were found,
			// throw an exception. Otherwise, create the Properties
			// object.
			if (style != ReadStyle.None) {
				if (codecs.Length == 0)
					throw new UnsupportedFormatException (
						"Unsupported RIFF type.");
				
				properties = new Properties (duration, codecs);
			}
			
			// If we're reading tags, update the combined tag.
			if (read_tags)
				tag.SetTags (id32_tag, info_tag, mid_tag, divx_tag);
		}
		
#endregion
	}
}
