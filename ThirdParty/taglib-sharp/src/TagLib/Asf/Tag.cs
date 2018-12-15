//
// Tag.cs: Provides a representation of an ASF tag which can be read from and
// written to disk.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2005-2007 Brian Nickel
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
using System.Globalization;

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="TagLib.Tag" /> to provide a
	///    representation of an ASF tag which can be read from and written
	///    to disk.
	/// </summary>
	public class Tag : TagLib.Tag, IEnumerable<ContentDescriptor>
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the content description object.
		/// </summary>
		private ContentDescriptionObject description =
			new ContentDescriptionObject ();
		
		/// <summary>
		///    Contains the extended content description object.
		/// </summary>
		private ExtendedContentDescriptionObject ext_description =
			new ExtendedContentDescriptionObject ();
		
		/// <summary>
		///    Contains the metadata library object.
		/// </summary>
		private MetadataLibraryObject metadata_library =
			new MetadataLibraryObject ();
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> with no contents.
		/// </summary>
		public Tag ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> using the children of a <see
		///    cref="HeaderObject" /> object.
		/// </summary>
		/// <param name="header">
		///    A <see cref="HeaderObject" /> object whose children are
		///    are to be used by the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="header" /> is <see langword="null" />.
		/// </exception>
		public Tag (HeaderObject header)
		{
			if (header == null)
				throw new ArgumentNullException ("header");
			
			foreach (Object child in header.Children) {
				if (child is ContentDescriptionObject)
					description =
						child as ContentDescriptionObject;
			
				if (child is ExtendedContentDescriptionObject)
					ext_description =
						child as ExtendedContentDescriptionObject;
			}
			
			foreach (Object child in header.Extension.Children)
				if (child is MetadataLibraryObject)
					metadata_library =
						child as MetadataLibraryObject;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the ASF Content Description object used by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ContentDescriptionObject" /> object
		///    containing the ASF Content Description object used by the
		///    current instance.
		/// </value>
		public ContentDescriptionObject ContentDescriptionObject {
			get {return description;}
		}
		
		/// <summary>
		///    Gets the ASF Extended Content Description object used by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ExtendedContentDescriptionObject" /> object
		///    containing the ASF Extended Content Description object
		///    used by the current instance.
		/// </value>
		public ExtendedContentDescriptionObject
			ExtendedContentDescriptionObject {
			get {return ext_description;}
		}
		
		/// <summary>
		///    Gets the ASF Metadata Library object used by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="MetadataLibraryObject" /> object containing
		///    the ASF Metadata Library object used by the current
		///    instance.
		/// </value>
		public MetadataLibraryObject MetadataLibraryObject {
			get {return metadata_library;}
		}

		#endregion



		#region Public Methods

		/// <summary>
		///    Gets the string contained in a specific descriptor in the
		///    current instance.
		/// </summary>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names of the
		///    descriptors to look for the value in.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is <see langword="null" />.
		/// </exception>
		/// <returns>
		///    A <see cref="string" /> object containing the contents of
		///    the first descriptor found in the current instance.
		/// </returns>
		public string GetDescriptorString (params string [] names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			foreach (ContentDescriptor desc in GetDescriptors (names)) {
				if (desc == null || desc.Type != DataType.Unicode)
					continue;
				
				string value = desc.ToString ();
				if (value != null)
					return value;
			}
			
			return null;
		}
		
		/// <summary>
		///    Gets the strings contained in a specific descriptor in
		///    the current instance.
		/// </summary>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names of the
		///    descriptors to look for the value in.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is <see langword="null" />.
		/// </exception>
		/// <returns>
		///    A <see cref="string" /> object containing the contents of
		///    the first descriptor found in the current instance as
		///    split by ';'.
		/// </returns>
		public string [] GetDescriptorStrings (params string [] names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			return SplitAndClean (GetDescriptorString (names));
		}
		
		/// <summary>
		///    Sets the string for a collection of descriptors in the
		///    current instance.
		/// </summary>
		/// <param name="value">
		///    A <see cref="string" /> object containing the value to
		///    store, or <see langword="null" /> to clear the value.
		/// </param>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names in which
		///    the value would be expected. For example, "WM/AlbumTitle"
		///    and "Album".
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is <see langword="null" />.
		/// </exception>
		/// <remarks>
		///    The value will be stored in the first value in <paramref
		///    name="names" /> and the rest will be cleared.
		/// </remarks>
		public void SetDescriptorString (string value,
		                                 params string [] names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			int index = 0;
			
			if (value != null && value.Trim ().Length != 0) {
				SetDescriptors (names [0],
					new ContentDescriptor (names [0], value));
				
				index ++;
			}
			
			for (; index < names.Length; index ++)
				RemoveDescriptors (names [index]);
		}
		
		/// <summary>
		///    Sets the strings for a collection of descriptors in the
		///    current instance.
		/// </summary>
		/// <param name="value">
		///    A <see cref="T:string[]" /> containing the value to store,
		///    or <see langword="null" /> to clear the value.
		/// </param>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names in which
		///    the value would be expected. For example, "WM/AlbumTitle"
		///    and "Album".
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is <see langword="null" />.
		/// </exception>
		/// <remarks>
		///    The value will be stored in the first value in <paramref
		///    name="names" /> and the rest will be cleared.
		/// </remarks>
		public void SetDescriptorStrings (string [] value,
		                                  params string [] names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			SetDescriptorString (String.Join ("; ", value), names);
		}
		
		/// <summary>
		///    Removes all descriptors with a specified name from the
		///    current instance.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    descriptor to remove from the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="name" /> is <see langword="null" />.
		/// </exception>
		public void RemoveDescriptors (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			ext_description.RemoveDescriptors (name);
		}
		
		/// <summary>
		///    Gets all descriptors with any of a collection of names
		///    from the current instance.
		/// </summary>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names of the
		///    descriptors to be retrieved.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="names" /> is <see langword="null" />.
		/// </exception>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the <see cref="ContentDescriptor" /> objects
		///    retrieved from the current instance.
		/// </returns>
		public IEnumerable<ContentDescriptor> GetDescriptors (params string [] names)
		{
			if (names == null)
				throw new ArgumentNullException ("names");
			
			return ext_description.GetDescriptors (names);
		}
		
		/// <summary>
		///    Sets the a collection of desciptors for a given name,
		///    removing the existing matching records.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    descriptors to be added.
		/// </param>
		/// <param name="descriptors">
		///    A <see cref="T:ContentDescriptor[]" /> containing
		///    descriptors to add to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="name" /> is <see langword="null" />.
		/// </exception>
		/// <remarks>
		///    All added entries in <paramref name="descriptors" />
		///    should match <paramref name="name" /> but it is not
		///    verified by the method. The descriptors will be added
		///    with their own names and not the one provided in this
		///    method, which are used for removing existing values and
		///    determining where to position the new objects.
		/// </remarks>
		public void SetDescriptors (string name,
									params ContentDescriptor [] descriptors)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			ext_description.SetDescriptors (name, descriptors);
		}
		
		/// <summary>
		///    Adds a descriptor to the current instance.
		/// </summary>
		/// <param name="descriptor">
		///    A <see cref="ContentDescriptor" /> object to add to the
		///    current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="descriptor" /> is <see langword="null"
		///    />.
		/// </exception>
		public void AddDescriptor (ContentDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException ("descriptor");
			
			ext_description.AddDescriptor (descriptor);
		}
		
		#endregion
		
		
		
		#region Private Static Methods
		
		/// <summary>
		///    Converts a raw ASF picture into an <see cref="IPicture"
		///    /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing raw ASF
		///    picture data.
		/// </param>
		/// <returns>
		///    A <see cref="IPicture" /> object to read from the raw
		///    data.
		/// </returns>
		private static IPicture PictureFromData (ByteVector data)
		{
			if (data.Count < 9)
				return null;
			
			int offset = 0;
			Picture p = new Picture ();
			
			// Get the picture type:
			
			p.Type = (PictureType) data [offset];
			offset += 1;
			
			// Get the picture size:
			
			int size = (int) data.Mid (offset, 4).ToUInt (false);
			offset += 4;
			
			// Get the mime-type:
			
			int found = data.Find (ByteVector.TextDelimiter (
				StringType.UTF16LE), offset, 2);
			if (found < 0)
				return null;
			
			p.MimeType = data.ToString (StringType.UTF16LE, offset,
				found - offset);
			offset = found + 2;
			
			// Get the description:
			
			found = data.Find (ByteVector.TextDelimiter (
				StringType.UTF16LE), offset, 2);
			if (found < 0)
				return null;
			
			p.Description = data.ToString (StringType.UTF16LE,
				offset, found - offset);
			offset = found + 2;
			
			p.Data = data.Mid (offset, size);
			
			return p;
		}
		
		/// <summary>
		///    Converts a <see cref="IPicture" /> object into raw ASF
		///    picture data.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture" /> object to convert.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing raw ASF
		///    picture data.
		/// </returns>
		private static ByteVector PictureToData (IPicture picture)
		{
			ByteVector v = new ByteVector ((byte) picture.Type);
			v.Add (Object.RenderDWord ((uint) picture.Data.Count));
			v.Add (Object.RenderUnicode (picture.MimeType));
			v.Add (Object.RenderUnicode (picture.Description));
			v.Add (picture.Data);
			return v;
		}
		
		/// <summary>
		///    Splits a string into a collection of strings by ';'.
		/// </summary>
		/// <param name="s">
		///    A <see cref="string" /> object containing the text to
		///    split.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]" /> containing the split text.
		/// </returns>
		private static string [] SplitAndClean (string s)
		{
			if (s == null || s.Trim ().Length == 0)
				return new string [0];
			
			string [] result = s.Split (';');
			
			for (int i = 0; i < result.Length; i ++)
				result [i] = result [i].Trim ();
			
			return result;
		}
		
		#endregion
		
		
		
#region IEnumerable
		
		/// <summary>
		///    Gets an enumerator for enumerating through the content
		///    descriptors.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the content descriptors.
		/// </returns>
		public IEnumerator<ContentDescriptor> GetEnumerator ()
		{
			return ext_description.GetEnumerator ();
		}
		
		System.Collections.IEnumerator
			System.Collections.IEnumerable.GetEnumerator ()
		{
			return ext_description.GetEnumerator ();
		}
		
#endregion
		
		
		
		#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Asf" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.Asf;}
		}
		
		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the title stored in
		///    the ASF Content Description Object.
		/// </remarks>
		public override string Title {
			get {return description.Title;}
			set {description.Title = value;}
		}


		/// <summary>
		///    Gets and sets a short description, one-liner. 
		///    It represents the tagline of the Video/music.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>This field gives a nice/short precision to 
		///    the title, which is typically below the title on the
		///    front cover of a media.
		///    For example, for "Back to the future", this would be 
		///    "It's About Time". 
		///    </para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "WM/SubTitle"
		///    field.
		///    https://msdn.microsoft.com/en-us/library/windows/desktop/dd757997(v=vs.85).aspx
		/// </remarks>
		public override string Subtitle
		{
			get
			{
				return GetDescriptorString("WM/SubTitle");
			}
			set
			{
				SetDescriptorString(value, "WM/SubTitle");
			}
		}



		/// <summary>
		///    Gets and sets the sort names for the Track Title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort name of 
		///    the Track Title of the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/TitleSortOrder"
		///    field.
		///    http://msdn.microsoft.com/en-us/library/aa386866(VS.85).aspx
		/// </remarks>
		public override string TitleSort {
			get {
				return GetDescriptorString ("WM/TitleSortOrder");
			}
			set {
				SetDescriptorString (value, "WM/TitleSortOrder");
			}
		}

		/// <summary>
		///    Gets and sets a short description of the media.
		///    For a music, this could be the comment that the artist
		///    made of its artwork. For a video, this should be a 
		///    short summary of the story/plot, but a spoiler. This
		///    should give the impression of what to expect in the
		///    media.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the description stored
		///    in the ASF Content Description Object.
		/// </remarks>
		public override string Description
		{
			get { return description.Description; }
			set { description.Description = value; }
		}


		/// <summary>
		///    Gets and sets the performers or artists who performed in
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the performers or
		///    artists who performed in the media described by the
		///    current instance or an empty array if no value is
		///    present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the author stored in
		///    the ASF Content Description Object.
		/// </remarks>
		public override string [] Performers {
			get {return SplitAndClean (description.Author);}
			set {description.Author = string.Join ("; ", value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the performers or artists
		///    who performed in the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/ArtistSortOrder" field.
		///    http://msdn.microsoft.com/en-us/library/aa386866(VS.85).aspx
		/// </remarks>
		public override string [] PerformersSort {
			get {
				return GetDescriptorStrings ("WM/ArtistSortOrder");
			}
			set {
				SetDescriptorStrings (value, "WM/ArtistSortOrder");
			}
		}
		
		/// <summary>
		///    Gets and sets the band or artist who is credited in the
		///    creation of the entire album or collection containing the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the band or artist
		///    who is credited in the creation of the entire album or
		///    collection containing the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/AlbumArtist"
		///    and "AlbumArtist" Content Descriptors.
		/// </remarks>
		public override string [] AlbumArtists {
			get {
				return GetDescriptorStrings ("WM/AlbumArtist",
					"AlbumArtist");
			}
			set {
				SetDescriptorStrings (value, "WM/AlbumArtist",
					"AlbumArtist");
			}
		}
		
		/// <summary>
		///    Gets and sets the sort names for the band or artist who
		///    is credited in the creation of the entire album or
		///    collection containing the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the band or artist who is credited in the creation
		///    of the entire album or collection containing the media
		///    described by the current instance or an empty array if
		///    no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/AlbumArtistSortOrder"
		///    field.
		///    http://msdn.microsoft.com/en-us/library/aa386866(VS.85).aspx
		/// </remarks>
		public override string [] AlbumArtistsSort {
			get {
				return GetDescriptorStrings ("WM/AlbumArtistSortOrder");
			}
			set {
				SetDescriptorStrings (value, "WM/AlbumArtistSortOrder");
			}
		}
		
		/// <summary>
		///    Gets and sets the composers of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the composers of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Composer"
		///    and "Composer" Content Descriptors.
		/// </remarks>
		public override string [] Composers {
			get {
				return GetDescriptorStrings ("WM/Composer",
					"Composer");
			}
			set {
				SetDescriptorStrings (value, "WM/Composer",
					"Composer");
			}
		}
		
		/// <summary>
		///    Gets and sets the album of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the album of
		///    the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/AlbumTitle"
		///    and "Album" Content Descriptors.
		/// </remarks>
		public override string Album {
			get {
				return GetDescriptorString ("WM/AlbumTitle",
					"Album");
			}
			set {
				SetDescriptorString (value, "WM/AlbumTitle",
					"Album");
			}
		}
		
		/// <summary>
		///    Gets and sets the sort names for the Album Title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort name of 
		///    the Album Title of the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/AlbumSortOrder"
		///    field.
		///    http://msdn.microsoft.com/en-us/library/aa386866(VS.85).aspx
		/// </remarks>
		public override string AlbumSort {
			get {
				return GetDescriptorString ("WM/AlbumSortOrder");
			}
			set {
				SetDescriptorString (value, "WM/AlbumSortOrder");
			}
		}

		/// <summary>
		///    Gets and sets a user comment on the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing user comments
		///    on the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Text" field.
		/// </remarks>
		public override string Comment {
			get {
				return GetDescriptorString("WM/Text");
			}
			set {
				SetDescriptorString(value, "WM/Text");
			}
		}

		/// <summary>
		///    Gets and sets the genres of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the genres of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Genre",
		///    "WM/GenreID", and "Genre" Content Descriptors.
		/// </remarks>
		public override string [] Genres {
			get {
				string value = GetDescriptorString ("WM/Genre",
					"WM/GenreID", "Genre");
				
				if (value == null || value.Trim ().Length == 0)
					return new string [] {};
				
				string [] result = value.Split (';');
				
				for (int i = 0; i < result.Length; i ++) {
					string genre = result [i].Trim ();
					
					byte genre_id;
					int closing = genre.IndexOf (')');
					if (closing > 0 && genre[0] == '(' &&
						byte.TryParse (genre.Substring (
						1, closing - 1), out genre_id))
						genre = TagLib.Genres
							.IndexToAudio (genre_id);
					
					result [i] = genre;
				}
				
				return result;
			}
			set {
				SetDescriptorString (String.Join ("; ", value),
					"WM/Genre", "Genre", "WM/GenreID");
			}
		}
		
		/// <summary>
		///    Gets and sets the year that the media represented by the
		///    current instance was recorded.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the year that the media
		///    represented by the current instance was created or zero
		///    if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Year" Content
		///    Descriptor.
		/// </remarks>
		public override uint Year {
			get {
				string text = GetDescriptorString ("WM/Year");
				
				if (text == null || text.Length < 4)
					return 0;
				
				uint value;
				if (uint.TryParse (text.Substring (0, 4),
					NumberStyles.Integer,
					CultureInfo.InvariantCulture,
					out value))
					return value;
				
				return 0;
			}
			set {
				if (value == 0) {
					RemoveDescriptors ("WM/Year");
					return;
				}
				
				SetDescriptorString (
					value.ToString (
						CultureInfo.InvariantCulture),
					"WM/Year");
			}
		}
		
		/// <summary>
		///    Gets and sets the position of the media represented by
		///    the current instance in its containing album.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the position of the
		///    media represented by the current instance in its
		///    containing album or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/TrackNumber"
		///    Content Descriptor.
		/// </remarks>
		public override uint Track {
			get {
				foreach (ContentDescriptor desc in
					GetDescriptors ("WM/TrackNumber")) {
						uint value = desc.ToDWord ();
						if (value != 0)
							return value;
				}
				
				return 0;
			}
			set {
				if (value == 0)
					RemoveDescriptors ("WM/TrackNumber");
				else
					SetDescriptors ("WM/TrackNumber",
						new ContentDescriptor (
							"WM/TrackNumber",
							value));
			}
		}
		
		/// <summary>
		///    Gets and sets the number of tracks in the album
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of tracks in
		///    the album containing the media represented by the current
		///    instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TrackTotal"
		///    Content Descriptor.
		/// </remarks>
		public override uint TrackCount {
			get {
				foreach (ContentDescriptor desc in
					GetDescriptors ("TrackTotal")) {
						uint value = desc.ToDWord ();
						if (value != 0)
							return value;
				}
				
				return 0;
			}
			set {
				if (value == 0)
					RemoveDescriptors ("TrackTotal");
				else
					SetDescriptors ("TrackTotal",
						new ContentDescriptor (
							"TrackTotal", value));
			}
		}
		
		/// <summary>
		///    Gets and sets the number of the disc containing the media
		///    represented by the current instance in the boxed set.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of the disc
		///    containing the media represented by the current instance
		///    in the boxed set.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/PartOfSet"
		///    Content Descriptor.
		/// </remarks>
		public override uint Disc {
			get {
				string text = GetDescriptorString (
					"WM/PartOfSet");
				
				if (text == null)
					return 0;
				
				string [] texts = text.Split ('/');
				uint value;
				
				if (texts.Length < 1)
					return 0;
				
				return uint.TryParse (texts [0],
					NumberStyles.Integer,
					CultureInfo.InvariantCulture,
					out value) ? value : 0;
			}
			set {
				uint count = DiscCount;
				if (value == 0 && count == 0) {
					RemoveDescriptors ("WM/PartOfSet");
					return;
				}
				
				if (count != 0) {
					SetDescriptorString (string.Format (
						CultureInfo.InvariantCulture,
						"{0}/{1}", value, count),
						"WM/PartOfSet");
					return;
				}
				
				SetDescriptorString (value.ToString (
					CultureInfo.InvariantCulture),
					"WM/PartOfSet");
			}
		}
		
		/// <summary>
		///    Gets and sets the number of discs in the boxed set
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of discs in
		///    the boxed set containing the media represented by the
		///    current instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/PartOfSet"
		///    Content Descriptor.
		/// </remarks>
		public override uint DiscCount {
			get {
				string text = GetDescriptorString (
					"WM/PartOfSet");
				
				if (text == null)
					return 0;
				
				string [] texts = text.Split ('/');
				uint value;
				
				if (texts.Length < 2)
					return 0;
				
				return uint.TryParse (texts [1],
					NumberStyles.Integer,
					CultureInfo.InvariantCulture,
					out value) ? value : 0;
			}
			set {
				uint disc = Disc;
				if (disc == 0 && value == 0) {
					RemoveDescriptors ("WM/PartOfSet");
					return;
				}
				
				if (value != 0) {
					SetDescriptorString (string.Format (
						CultureInfo.InvariantCulture,
						"{0}/{1}", disc, value),
						"WM/PartOfSet");
					return;
				}
				
				SetDescriptorString (disc.ToString (
					CultureInfo.InvariantCulture),
					"WM/PartOfSet");
			}
		}
		
		/// <summary>
		///    Gets and sets the lyrics or script of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the lyrics or
		///    script of the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Lyrics"
		///    Content Descriptor.
		/// </remarks>
		public override string Lyrics {
			get {return GetDescriptorString ("WM/Lyrics");}
			set {SetDescriptorString (value, "WM/Lyrics");}
		}
		
		/// <summary>
		///    Gets and sets the grouping on the album which the media
		///    in the current instance belongs to.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the grouping on
		///    the album which the media in the current instance belongs
		///    to or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the
		///    "WM/ContentGroupDescription" Content Descriptor.
		/// </remarks>
		public override string Grouping {
			get {
				return GetDescriptorString (
					"WM/ContentGroupDescription");
			}
			set {
				SetDescriptorString (value,
					"WM/ContentGroupDescription");
			}
		}
		
		/// <summary>
		///    Gets and sets the number of beats per minute in the audio
		///    of the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of beats per
		///    minute in the audio of the media represented by the
		///    current instance, or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the
		///    "WM/BeatsPerMinute" Content Descriptor.
		/// </remarks>
		public override uint BeatsPerMinute {
			get {
				foreach (ContentDescriptor desc in
					GetDescriptors ("WM/BeatsPerMinute")) {
						uint value = desc.ToDWord ();
						if (value != 0)
							return value;
				}
				
				return 0;
			}
			set {
				if (value == 0) {
					RemoveDescriptors ("WM/BeatsPerMinute");
					return;
				}
				
				SetDescriptors ("WM/BeatsPerMinute",
					new ContentDescriptor (
						"WM/BeatsPerMinute", value));
			}
		}
		
		/// <summary>
		///    Gets and sets the conductor or director of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the conductor
		///    or director of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Conductor"
		///    Content Descriptor.
		/// </remarks>
		public override string Conductor {
			get {return GetDescriptorString ("WM/Conductor");}
			set {SetDescriptorString (value, "WM/Conductor");}
		}
		
		/// <summary>
		///    Gets and sets the copyright information for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the copyright stored
		///    in the ASF Content Description Object.
		/// </remarks>
		public override string Copyright {
			get {return description.Copyright;}
			set {description.Copyright = value;}
		}
		
		/// <summary>
		///    Gets and sets the MusicBrainz Artist ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ArtistID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Artist Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzArtistId {
			get {return GetDescriptorString ("MusicBrainz/Artist Id");}
			set {SetDescriptorString (value, "MusicBrainz/Artist Id");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Group ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseGroupID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Release Group Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseGroupId
		{
			get { return GetDescriptorString("MusicBrainz/Release Group Id"); }
			set { SetDescriptorString(value, "MusicBrainz/Release Group Id"); }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Album Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseId {
			get {return GetDescriptorString ("MusicBrainz/Album Id");}
			set {SetDescriptorString (value, "MusicBrainz/Album Id");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Artist ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseArtistID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Album Artist Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseArtistId {
			get {return GetDescriptorString ("MusicBrainz/Album Artist Id");}
			set {SetDescriptorString (value, "MusicBrainz/Album Artist Id");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Track ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    TrackID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Track Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzTrackId {
			get {return GetDescriptorString ("MusicBrainz/Track Id");}
			set {SetDescriptorString (value, "MusicBrainz/Track Id");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Disc ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    DiscID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Disc Id"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzDiscId {
			get {return GetDescriptorString ("MusicBrainz/Disc Id");}
			set {SetDescriptorString (value, "MusicBrainz/Disc Id");}
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicIPPUID 
		///    for the media described by the current instance or
		///    null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicIP/PUID"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicIpId {
			get {return GetDescriptorString ("MusicIP/PUID");}
			set {SetDescriptorString (value, "MusicIP/PUID");}
		}

		// <summary>
		//    Gets and sets the AmazonID of
		//    the media described by the current instance.
		// </summary>
		// <value>
		//    A <see cref="string" /> containing the AmazonID 
		//    for the media described by the current instance or
		//    null if no value is present.  
		// </value>
		// <remarks>
		//    A definition on where to store the ASIN for
		//    Windows Media is not currently defined
		// </remarks>
		//public override string AmazonId {
		//    get { return null; }
		//    set {}
		//}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseStatus for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Album Status"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseStatus {
			get {return GetDescriptorString ("MusicBrainz/Album Status");}
			set {SetDescriptorString (value, "MusicBrainz/Album Status");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseType for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Album Type"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseType {
			get {return GetDescriptorString ("MusicBrainz/Album Type");}
			set {SetDescriptorString (value, "MusicBrainz/Album Type");}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseCountry for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MusicBrainz/Album Release Country"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseCountry {
			get {return GetDescriptorString ("MusicBrainz/Album Release Country");}
			set {SetDescriptorString (value, "MusicBrainz/Album Release Country");}
		}

		/// <summary>
		///    Gets and sets the ReplayGain track gain in dB.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value in dB for the track gain as
		///    per the ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the 
		///    "REPLAYGAIN_TRACK_GAIN" field. Set the value to double.NaN
		///    to clear the field.
		/// </remarks>
		public override double ReplayGainTrackGain
		{
			get
			{
				string text = GetDescriptorString("ReplayGain/Track");
				double value;

				if (text == null)
				{
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db"))
				{
					text = text.Substring(0, text.Length - 2).Trim();
				}

				if (double.TryParse(text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				if (double.IsNaN(value))
				{
					RemoveDescriptors("ReplayGain/Track");
				}
				else
				{
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetDescriptorString(text, "ReplayGain/Track");
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain track peak sample.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value for the track peak as per the
		///    ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the 
		///    "REPLAYGAIN_TRACK_PEAK" field. Set the value to double.NaN
		///    to clear the field.
		/// </remarks>
		public override double ReplayGainTrackPeak
		{
			get
			{
				string text;
				double value;

				if ((text = GetDescriptorString("ReplayGain/Track Peak")) !=
					null && double.TryParse(text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				if (double.IsNaN(value))
				{
					RemoveDescriptors("ReplayGain/Track Peak");
				}
				else
				{
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetDescriptorString(text, "ReplayGain/Track Peak");
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain album gain in dB.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value in dB for the album gain as
		///    per the ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the 
		///    "REPLAYGAIN_ALBUM_GAIN" field. Set the value to double.NaN
		///    to clear the field.
		/// </remarks>
		public override double ReplayGainAlbumGain
		{
			get
			{
				string text = GetDescriptorString("ReplayGain/Album");
				double value;

				if (text == null)
				{
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db"))
				{
					text = text.Substring(0, text.Length - 2).Trim();
				}

				if (double.TryParse(text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				if (double.IsNaN(value))
				{
					RemoveDescriptors("ReplayGain/Album");
				}
				else
				{
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetDescriptorString(text, "ReplayGain/Album");
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain album peak sample.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value for the album peak as per the
		///    ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the 
		///    "REPLAYGAIN_ALBUM_PEAK" field. Set the value to double.NaN
		///    to clear the field.
		/// </remarks>
		public override double ReplayGainAlbumPeak
		{
			get
			{
				string text;
				double value;

				if ((text = GetDescriptorString("ReplayGain/Album Peak")) !=
					null && double.TryParse(text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				if (double.IsNaN(value))
				{
					RemoveDescriptors("ReplayGain/Album Peak");
				}
				else
				{
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetDescriptorString(text, "ReplayGain/Album Peak");
				}
			}
		}

		/// <summary>
		///    Gets and sets a collection of pictures associated with
		///    the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:IPicture[]" /> containing a collection of
		///    pictures associated with the media represented by the
		///    current instance or an empty array if none are present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "WM/Picture"
		///    Content Descriptor and Description Record.
		/// </remarks>
		public override IPicture [] Pictures {
			get {
				List<IPicture> l = new List<IPicture> ();
				
				foreach (ContentDescriptor descriptor in
					GetDescriptors ("WM/Picture")) {
					IPicture p = PictureFromData (
						descriptor.ToByteVector ());
					if (p != null)
						l.Add (p);
				}
				
				foreach (DescriptionRecord record in
					metadata_library.GetRecords (0, 0,
						"WM/Picture")) {
					IPicture p = PictureFromData (
						record.ToByteVector ());
					if (p != null)
						l.Add (p);
				}
				
				return l.ToArray ();
			}
			set {
				if (value == null || value.Length == 0) {
					RemoveDescriptors ("WM/Picture");
					metadata_library.RemoveRecords (0, 0,
						"WM/Picture");
					return;
				}
				
				List<ByteVector> pics = new List<ByteVector> ();
				
				bool big_pics = false;
				
				foreach (IPicture pic in value) {
					ByteVector data = PictureToData (pic);
					pics.Add (data);
					if (data.Count > 0xFFFF)
						big_pics = true;
				}
				
				if (big_pics) {
					DescriptionRecord [] records =
						new DescriptionRecord [pics.Count];
					for (int i = 0; i < pics.Count; i ++)
						records [i] = new DescriptionRecord (
							0, 0, "WM/Picture", pics [i]);
					RemoveDescriptors ("WM/Picture");
					metadata_library.SetRecords (0, 0,
						"WM/Picture", records);
				} else {
					ContentDescriptor [] descs =
						new ContentDescriptor [pics.Count];
					for (int i = 0; i < pics.Count; i ++)
						descs [i] = new ContentDescriptor (
							"WM/Picture", pics [i]);
					metadata_library.RemoveRecords (0, 0,
						"WM/Picture");
					SetDescriptors ("WM/Picture", descs);
				}
			}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance does not
		///    any values. Otherwise <see langword="false" />.
		/// </value>
		public override bool IsEmpty {
			get {
				return description.IsEmpty &&
					ext_description.IsEmpty;
			}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			description = new ContentDescriptionObject ();
			ext_description =
				new ExtendedContentDescriptionObject ();
			metadata_library.RemoveRecords (0, 0, "WM/Picture");
		}
		
		#endregion
	}
}
