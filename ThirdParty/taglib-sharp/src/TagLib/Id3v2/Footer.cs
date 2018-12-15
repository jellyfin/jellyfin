//
// Footer.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v2header.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002,2003 Scott Wheeler (Original Implementation)
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

namespace TagLib.Id3v2
{
	
	/// <summary>
	///    This structure provides a representation of an ID3v2 tag footer
	///    which can be read from and written to disk.
	/// </summary>
	public struct Footer
	{
#region Private Fields
		
		/// <summary>
		///    Contains the tag's major version.
		/// </summary>
		private byte major_version;
		
		/// <summary>
		///    Contains the tag's version revision.
		/// </summary>
		private byte revision_number;
		
		/// <summary>
		///    Contains tag's flags.
		/// </summary>
		private HeaderFlags flags;
		
		/// <summary>
		///    Contains the tag size.
		/// </summary>
		private uint tag_size;
		
#endregion
		
		
		
#region Public Fields
		
		/// <summary>
		///    The size of a ID3v2 footer.
		/// </summary>
		public const uint Size = 10;
		
		/// <summary>
		///    The identifier used to recognize a ID3v2 footer.
		/// </summary>
		/// <value>
		///    "3DI"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "3DI";
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Footer" /> by reading it from raw footer data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data to build the new instance from.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is smaller than <see
		///    cref="Size" />, does not begin with <see
		///    cref="FileIdentifier" />, contains invalid flag data,
		///    or contains invalid size data.
		/// </exception>
		public Footer (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < Size)
				throw new CorruptFileException (
					"Provided data is smaller than object size.");
			
			if (!data.StartsWith (FileIdentifier))
				throw new CorruptFileException (
					"Provided data does not start with the file identifier");
			
			major_version = data [3];
			revision_number = data [4];
			flags = (HeaderFlags) data [5];
			
			if (major_version == 2 && ((int) flags & 127) != 0)
				throw new CorruptFileException (
					"Invalid flags set on version 2 tag.");
			
			if (major_version == 3 && ((int) flags & 15) != 0)
				throw new CorruptFileException (
					"Invalid flags set on version 3 tag.");
			
			if (major_version == 4 && ((int) flags & 7) != 0)
				throw new CorruptFileException (
					"Invalid flags set on version 4 tag.");
			
			for (int i = 6; i < 10; i ++)
				if (data [i] >= 128)
					throw new CorruptFileException (
						"One of the bytes in the header was greater than the allowed 128.");
			
			tag_size = SynchData.ToUInt (data.Mid (6, 4));
		}
		
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="Footer" /> by reading in the contents of the header
		///    object used for the same tag.
		/// </summary>
		/// <param name="header">
		///    A <see cref="Header" /> object to base the new instance
		///    off of.
		/// </param>
		public Footer (Header header)
		{
			major_version = header.MajorVersion;
			revision_number = header.RevisionNumber;
			flags = header.Flags | HeaderFlags.FooterPresent;
			tag_size = header.TagSize;
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets and sets the major version of the tag described by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value specifying the ID3v2 version
		///    of tag described by the current instance.
		/// </value>
		/// <remarks>
		///    When the version is set, unsupported header flags will
		///    automatically be removed from the tag.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="value" /> is not 4.
		/// </exception>
		public byte MajorVersion {
			get {
				return major_version == 0 ? Tag.DefaultVersion :
					major_version;
			}
			set {
				if (value != 4)
					throw new ArgumentException (
						"Version unsupported.");
				
				major_version = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the version revision number of the tag
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value containing the version
		///    revision number of the tag represented by the current
		///    instance.
		/// </value>
		/// <remarks>
		///    This value should always be zeroed. A non-zero value
		///    indicates an experimental or new version of the format
		///    which may not be completely understood by the current
		///    implementation. Some software may refuse to read tags
		///    with a non-zero value.
		/// </remarks>
		public byte RevisionNumber {
			get {return revision_number;}
			set {revision_number = value;}
		}
		
		/// <summary>
		///    Gets and sets the flags applied to the current instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="HeaderFlags" /> value
		///    containing the flags applied to the current instance.
		/// </value>
		/// <exception cref="ArgumentException">
		///    <paramref name="value" /> contains a flag not supported
		///    by the the ID3v2 version of the current instance.
		/// </exception>
		public HeaderFlags Flags {
			get {return flags;}
			set {
				if (0 != (value & (HeaderFlags.ExtendedHeader |
					HeaderFlags.ExperimentalIndicator)) &&
					MajorVersion < 3)
					throw new ArgumentException (
						"Feature only supported in version 2.3+",
						"value");
				
				if (0 != (value & HeaderFlags.FooterPresent) &&
					MajorVersion < 3)
					throw new ArgumentException (
						"Feature only supported in version 2.4+",
						"value");
				
				flags = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the size of the tag described by the
		///    current instance, minus the header and footer.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    tag described by the current instance.
		/// </value>
		public uint TagSize {
			get {return tag_size;}
			set {tag_size = value;}
		}
		
		/// <summary>
		///    Gets the complete size of the tag described by the
		///    current instance, including the header and footer.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the complete size
		///    of the tag described by the current instance.
		/// </value>
		public uint CompleteTagSize {
			get {return TagSize + Header.Size + Size;}
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw ID3v2 header.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered header.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector v = new ByteVector ();
			v.Add (FileIdentifier);
			v.Add (MajorVersion);
			v.Add (RevisionNumber);
			v.Add ((byte)flags);
			v.Add (SynchData.FromUInt (TagSize));
			return v;
		}
		
#endregion
	}
}
