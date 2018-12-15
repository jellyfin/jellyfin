//
// File.cs: Provides tagging and properties support for Monkey's Audio APE
// files.
//
// Author:
//   Helmut Wahrmann
//
// Copyright (C) 2007 Helmut Wahrmann
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

namespace TagLib.Ape {
	/// <summary>
	///    This class extends <see cref="TagLib.NonContainer.File" /> to
	///    provide tagging and properties support for Monkey's Audio APE
	///    files.
	/// </summary>
	/// <remarks>
	///    A <see cref="TagLib.Ape.Tag" /> will be added automatically to
	///    any file that doesn't contain one. This change does not effect
	///    the physical file until <see cref="TagLib.File.Save" /> is called and can be
	///    reversed using the following method:
	///    <code>file.RemoveTags (file.TagTypes &amp; ~file.TagTypesOnDisk);</code>
	/// </remarks>
	[SupportedMimeType("taglib/ape", "ape")]
	[SupportedMimeType("audio/x-ape")]
	[SupportedMimeType("audio/ape")]
	[SupportedMimeType("application/x-ape")]
	public class File : TagLib.NonContainer.File
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the block with the audio header.
		/// </summary>
		private ByteVector header_block = null;
		
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
			: base (path, propertiesStyle)
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
		public File (string path)
			: base (path)
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
		             ReadStyle propertiesStyle)
			: base (abstraction, propertiesStyle)
		{
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
			: base (abstraction)
		{
		}
		
		#endregion
		
		
		
		#region Public Methods
		
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
		/// <remarks>
		///    If a <see cref="TagLib.Id3v2.Tag" /> is added to the
		///    current instance, it will be placed at the start of the
		///    file. On the other hand, <see cref="TagLib.Id3v1.Tag" />
		///    <see cref="TagLib.Ape.Tag" /> will be added to the end of
		///    the file. All other tag types will be ignored.
		/// </remarks>
		public override TagLib.Tag GetTag(TagTypes type, bool create)
		{
			TagLib.Tag t = (Tag as TagLib.NonContainer.Tag)
				.GetTag (type);
			
			if (t != null || !create)
				return t;
			
			switch (type)
			{
			case TagTypes.Id3v1:
				return EndTag.AddTag (type, Tag);
			
			case TagTypes.Id3v2:
				return StartTag.AddTag (type, Tag);
			
			case TagTypes.Ape:
				return EndTag.AddTag (type, Tag);
			
			default:
				return null;
			}
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Reads format specific information at the start of the
		///    file.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value containing the seek position
		///    at which the tags end and the media data begins.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		protected override void ReadStart (long start,
		                                   ReadStyle propertiesStyle)
		{
			if (header_block != null &&
				(propertiesStyle & ReadStyle.Average) == 0)
				return;
			
			Seek(start);
				header_block = ReadBlock ((int)StreamHeader.Size);
		}
		
		/// <summary>
		///    Reads format specific information at the end of the
		///    file.
		/// </summary>
		/// <param name="end">
		///    A <see cref="long" /> value containing the seek position
		///    at which the media data ends and the tags begin.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		protected override void ReadEnd (long end,
		                                 ReadStyle propertiesStyle)
		{
			// Make sure we have an APE tag.
			GetTag (TagTypes.Ape, true);
		}
		
		/// <summary>
		///    Reads the audio properties from the file represented by
		///    the current instance.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value containing the seek position
		///    at which the tags end and the media data begins.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value containing the seek position
		///    at which the media data ends and the tags begin.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <returns>
		///    A <see cref="TagLib.Properties" /> object describing the
		///    media properties of the file represented by the current
		///    instance.
		/// </returns>
		protected override Properties ReadProperties (long start,
		                                              long end,
		                                              ReadStyle propertiesStyle)
		{
			StreamHeader header = new StreamHeader (header_block,
				end - start);
			
			return new Properties(TimeSpan.Zero, header);
		}
		
		#endregion
	}
}
