//
// File.cs: Provides tagging and properties support for Microsoft's ASF files.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
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

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide tagging
	///    and properties support for Microsoft's ASF files.
	/// </summary>
	[SupportedMimeType("taglib/wma", "wma")]
	[SupportedMimeType("taglib/wmv", "wmv")]
	[SupportedMimeType("taglib/asf", "asf")]
	[SupportedMimeType("audio/x-ms-wma")]
	[SupportedMimeType("audio/x-ms-asf")]
	[SupportedMimeType("video/x-ms-asf")]
	public class File : TagLib.File
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the file's tag.
		/// </summary>
		private Asf.Tag asf_tag = null;
		
		/// <summary>
		///    Contains the file's properties.
		/// </summary>
		private Properties properties = null;
		
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
			: base (path)
		{
			Read (propertiesStyle);
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
			Read (propertiesStyle);
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
			get {return asf_tag;}
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
				HeaderObject header = new HeaderObject (this, 0);
				
				if (asf_tag == null) {
					header.RemoveContentDescriptors ();
					TagTypesOnDisk &= ~ TagTypes.Asf;
				} else {
					TagTypesOnDisk |= TagTypes.Asf;
					header.AddUniqueObject (
						asf_tag.ContentDescriptionObject);
					header.AddUniqueObject (
						asf_tag.ExtendedContentDescriptionObject);
					header.Extension.AddUniqueObject (
						asf_tag.MetadataLibraryObject);
				}
				
				ByteVector output = header.Render ();
				long diff = output.Count - (long) header.OriginalSize;
				Insert (output, 0, (long) header.OriginalSize);
				
				InvariantStartPosition += diff;
				InvariantEndPosition += diff;
			} finally {
				Mode = AccessMode.Closed;
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
		public override TagLib.Tag GetTag (TagTypes type, bool create)
		{
			if (type == TagTypes.Asf)
				return asf_tag;
			
			return null;
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
			if ((types & TagTypes.Asf) == TagTypes.Asf)
				asf_tag.Clear ();
		}
		
		/// <summary>
		///    Reads a 2-byte WORD from the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ushort" /> value containing the WORD read
		///    from the current instance.
		/// </returns>
		public ushort ReadWord ()
		{
			return ReadBlock (2).ToUShort (false);
		}
		
		/// <summary>
		///    Reads a 4-byte DWORD from the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="uint" /> value containing the DWORD read
		///    from the current instance.
		/// </returns>
		public uint ReadDWord ()
		{
			return ReadBlock (4).ToUInt (false);
		}
		
		/// <summary>
		///    Reads a 8-byte QWORD from the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ulong" /> value containing the QWORD read
		///    from the current instance.
		/// </returns>
		public ulong ReadQWord ()
		{
			return ReadBlock (8).ToULong (false);
		}
		
		/// <summary>
		///    Reads a 16-byte GUID from the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="System.Guid" /> value containing the GUID
		///    read from the current instance.
		/// </returns>
		public System.Guid ReadGuid ()
		{
			return new System.Guid (ReadBlock (16).Data);
		}
		
		/// <summary>
		///    Reads a Unicode (UTF-16LE) string of specified length
		///    from the current instance.
		/// </summary>
		/// <param name="length">
		///    A <see cref="int" /> value specifying the number of bytes
		///    to read. This should always be an even number.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the Unicode
		///    string read from the current instance.
		/// </returns>
		public string ReadUnicode (int length)
		{
			ByteVector data = ReadBlock (length);
			string output = data.ToString (StringType.UTF16LE);
			int i = output.IndexOf ('\0');
			return (i >= 0) ? output.Substring (0, i) : output;
		}
		
		/// <summary>
		///    Reads a collection of objects from the current instance.
		/// </summary>
		/// <param name="count">
		///    A <see cref="uint" /> value specifying the number of
		///    objects to read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <returns>
		///    A new <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the <see cref="Object" /> objects read from the
		///    current instance.
		/// </returns>
		public IEnumerable<Object> ReadObjects (uint count,
		                                        long position)
		{
			for (int i = 0; i < (int) count; i ++) {
				Object obj = ReadObject (position);
				position += (long) obj.OriginalSize;
				yield return obj;
			}
		}
		
		/// <summary>
		///    Reads a <see cref="Object" /> from the current instance.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <returns>
		///    A new <see cref="Object" /> object of appropriate type as
		///    read from the current instance.
		/// </returns>
		public Object ReadObject (long position)
		{
			Seek (position);
			System.Guid id = ReadGuid ();
			
			if (id.Equals (Guid.AsfFilePropertiesObject))
				return new FilePropertiesObject (this,
					position);
			
			if (id.Equals (Guid.AsfStreamPropertiesObject))
				return new StreamPropertiesObject (this,
					position);
			
			if (id.Equals (Guid.AsfContentDescriptionObject))
				return new ContentDescriptionObject (this,
					position);
			
			if (id.Equals (
				Guid.AsfExtendedContentDescriptionObject))
				return new ExtendedContentDescriptionObject (
					this, position);
			
			if (id.Equals (Guid.AsfPaddingObject))
				return new PaddingObject (this, position);
			
			if (id.Equals (Guid.AsfHeaderExtensionObject))
				return new HeaderExtensionObject (this,
					position);
			
			if (id.Equals (Guid.AsfMetadataLibraryObject))
				return new MetadataLibraryObject (this,
					position);
			
			return new UnknownObject (this, position);
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Reads the contents of the current instance.
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
				HeaderObject header = new HeaderObject (this, 0);
				
				if (header.HasContentDescriptors)
					TagTypesOnDisk |= TagTypes.Asf;
				
				asf_tag = new Asf.Tag (header);
				
				InvariantStartPosition = (long) header.OriginalSize;
				InvariantEndPosition = Length;
				
				if ((propertiesStyle & ReadStyle.Average) != 0)
					properties = header.Properties;
			} finally {
				Mode = AccessMode.Closed;
			}
		}
		
		#endregion
	}
}
