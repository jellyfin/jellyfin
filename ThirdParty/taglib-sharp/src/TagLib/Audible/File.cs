//
// File.cs:
//
// Author:
//   Guy Taylor (s0700260@sms.ed.ac.uk) (thebigguy.co.uk@gmail.com)
//
// Original Source:
//   Ogg/File.cs from TagLib-sharp
//
// Copyright (C) 2009 Guy Taylor (Original Implementation)
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

namespace TagLib.Audible
{
	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide tagging
	///    and properties support for Audible inc's aa file format.
	/// </summary>
	[SupportedMimeType("audio/x-audible")]
	[SupportedMimeType("taglib/aa", "aa")]
	[SupportedMimeType("taglib/aax", "aax")]
	public class File : TagLib.File
	{
		
		#region Private Fields
		
		/// <summary>
		///   Contains the tags for the file.
		/// </summary>
		private TagLib.Tag tag;
		
		/// <summary>
		///    Contains the media properties.
		/// </summary>
		private Properties properties = new Properties();
		
		#endregion	
		
		#region Public Static Fields
		
		/// <summary>
		///    The offset to the tag block.
		/// </summary>
		public const short TagBlockOffset = 0xBD;
		
		/// <summary>
		///    The offset to the end of tag pointer.
		/// </summary>
		public const short OffsetToEndTagPointer = 0x38;

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
		/// <exception cref="CorruptFileException">
		///    The file is not the write length.
		/// </exception>
		public File (File.IFileAbstraction abstraction,
		             ReadStyle propertiesStyle) : base (abstraction)
		{			
			
			Mode = AccessMode.Read;
			
			try {
				// get the pointer to the end of the tag block
				// and calculate the tag block length
				Seek (OffsetToEndTagPointer);
				int tagLen = ( (int) ReadBlock(4).ToUInt(true) ) - TagBlockOffset;
				
				// read the whole tag and send to Tag class
				Seek (TagBlockOffset);
				ByteVector bv = ReadBlock(tagLen);
				
				tag = new  TagLib.Audible.Tag( bv );
				
			} finally {
				Mode = AccessMode.Closed;
			}
			
			// ??
			TagTypesOnDisk = TagTypes;
			
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
		{
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		/// <remarks>
		/// 	Currently this does not work as there is not enough
		/// 	information about the file format
		/// </remarks>
		public override void Save ()
		{
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
		/// <remarks>
		/// 	Currently this does not work as there is not enough
		/// 	information about the file format
		/// </remarks>
		public override void RemoveTags (TagLib.TagTypes types)
		{
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
			if (type == TagTypes.AudibleMetadata)
				return tag;
			
			return null;

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
		public override TagLib.Tag Tag {
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
		
	}
}
