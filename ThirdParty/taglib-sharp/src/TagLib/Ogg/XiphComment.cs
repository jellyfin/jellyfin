//
// XiphComment.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   xiphcomment.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2003 Scott Wheeler (Original Implementation)
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
using System.Globalization;

namespace TagLib.Ogg
{
	/// <summary>
	///    This class extends <see cref="TagLib.Tag" /> and implements <see
	///    cref="T:System.Collections.Generic.IEnumerable`1" /> to provide
	///    support for reading and writing Xiph comments.
	/// </summary>
	public class XiphComment : TagLib.Tag, IEnumerable<string>
	{
#region Private Fields
		
		/// <summary>
		///    Contains the comment fields.
		/// </summary>
		private Dictionary<string, string[]> field_list =
			new Dictionary<string, string[]> ();

		/// <summary>
		///    Contains the vendor ID.
		/// </summary>
		private string vendor_id;

		/// <summary>
		///    Saves BeatsPerMinute tag as either "Tempo" or "BPM"
		///    based on which was last read.
		/// </summary>
		private static bool SaveBeatsPerMinuteAsTempo = true;

		/// <summary>
		///    Picture instances parsed from the fields.
		/// </summary>
		private IPicture[] pictures = null;

		/// <summary>
		///    true if the picture fields in <see cref="field_list" />
		///    should be updated from the <see cref="pictures"/> array.
		/// </summary>
		private bool picture_fields_dirty = false;

		/// <summary>
		///    Name of picture fields as defined in the norm.
		/// </summary>
		private static readonly string[] PICTURE_FIELDS = new string[] {"COVERART", "METADATA_BLOCK_PICTURE"};

		/// <summary>
		///    Cached empty pictures array.
		/// </summary>
		private static readonly IPicture[] EMPTY_PICTURES = new IPicture[0];

#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="XiphComment" /> with no contents.
		/// </summary>
		public XiphComment ()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="XiphComment" /> by reading the contents of a raw
		///    Xiph Comment from a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing a raw Xiph
		///    comment.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public XiphComment (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			Parse (data);
		}

		#endregion



		#region Public Methods

		/// <summary>
		///    Gets the field data for a given field identifier.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string"/> object containing the field
		///    identifier.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]"/> containing the field data or an
		///    empty array if the field was not found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public string [] GetField (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			key = key.ToUpper (CultureInfo.InvariantCulture);

			EnsurePictureFieldsClean(key);
			
			if (!field_list.ContainsKey (key))
				return new string [0];
			
			return (string []) field_list [key].Clone ();
		}
		
		/// <summary>
		///    Gets the first field for a given field identifier.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string"/> object containing the field
		///    identifier.
		/// </param>
		/// <returns>
		///    A <see cref="string"/> containing the field data or <see
		///    langword="null" /> if the field was not found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public string GetFirstField (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");

			EnsurePictureFieldsClean(key);
			
			string [] values = GetField (key);
			return (values.Length > 0) ? values [0] : null;
		}

		/// <summary>
		///    Sets the contents of a specified field to a number.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string"/> object containing the field
		///    identifier.
		/// </param>
		/// <param name="number">
		///    A <see cref="uint" /> value to set the field to.
		/// </param>
		/// <param name="format">
		///    A <see cref="string" /> value representing the format
		///    to be used to repreesent the <paramref name="number"/>.
		///    Default: simple decimal number ("0").
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void SetField (string key, uint number, string format = "0")
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (number == 0)
				RemoveField (key);
			else
				SetField (key, number.ToString (
					format,
					CultureInfo.InvariantCulture));
		}
		
		/// <summary>
		///    Sets the contents of a specified field to the contents of
		///    a <see cref="T:string[]" />.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string"/> object containing the field
		///    identifier.
		/// </param>
		/// <param name="values">
		///    A <see cref="T:string[]"/> containing the values to store
		///    in the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void SetField (string key, params string [] values)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			key = key.ToUpper (CultureInfo.InvariantCulture);
			
			if (values == null || values.Length == 0) {
				RemoveField (key);
				return;
			}
			
			List <string> result = new List<string> ();
			foreach (string text in values)
				if (text != null && text.Trim ().Length != 0)
					result.Add (text);
			
			if (result.Count == 0)
				RemoveField (key);
			else if (field_list.ContainsKey (key))
				field_list [key] = result.ToArray ();
			else
				field_list.Add (key, result.ToArray ());

			// Update picture state if this field name is a picture field
			ResetPicturesState (key);
		}
		
		/// <summary>
		///    Removes a field and all its values from the current
		///    instance.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string"/> object containing the field
		///    identifier.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void RemoveField (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			key = key.ToUpper (CultureInfo.InvariantCulture);
			
			field_list.Remove (key);

			// Update picture state if this field name is a picture field
			ResetPicturesState (key);
		}

		/// <summary>
		///    Renders the current instance as a raw Xiph comment,
		///    optionally adding a framing bit.
		/// </summary>
		/// <param name="addFramingBit">
		///    If <see langword="true" />, a framing bit will be added to
		///    the end of the content.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public ByteVector Render (bool addFramingBit)
		{
			ByteVector data = new ByteVector ();

			// Before storing the fields, ensure the pictures array has
			// been stored to the field list
			if (picture_fields_dirty)
				StorePictures ();

			// Add the vendor ID length and the vendor ID.  It's
			// important to use the length of the data(String::UTF8)
			// rather than the lenght of the the string since this
			// is UTF8 text and there may be more characters in the
			// data than in the UTF16 string.
			
			ByteVector vendor_data = ByteVector.FromString (
				vendor_id, StringType.UTF8);

			data.Add (ByteVector.FromUInt ((uint) vendor_data.Count,
				false));
			data.Add (vendor_data);

			// Add the number of fields.

			data.Add (ByteVector.FromUInt (FieldCount, false));

			foreach (KeyValuePair<string,string[]> entry in field_list) {
				// And now iterate over the values of the
				// current list.

				foreach (string value in entry.Value) {
					ByteVector field_data =
						ByteVector.FromString (
							entry.Key, StringType.UTF8);
					field_data.Add ((byte) '=');
					field_data.Add (ByteVector.FromString (
						value, StringType.UTF8));
					
					data.Add (ByteVector.FromUInt ((uint)
						field_data.Count, false));
					data.Add (field_data);
				}
			}

			// Append the "framing bit".
			if (addFramingBit)
				data.Add ((byte) 1);

			return data;
		}

#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the number of fields contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    fields in the current instance.
		/// </value>
		public uint FieldCount {
			get
			{
				uint count = 0;
				foreach (string[] values in field_list.Values)
					count += (uint) values.Length;

				// If the pictures array is loaded and not in sync
				// with the underlying fields, adjust the field count
				if (pictures != null && picture_fields_dirty) {
					foreach (string fieldName in PICTURE_FIELDS) {
						string[] fieldValue;
						if (field_list.TryGetValue (fieldName, out fieldValue)) {
							count -= (uint) fieldValue.Length;
						}
					}

					count += (uint) pictures.Length;
				}
				
				return count;
			}
		}
		
		/// <summary>
		///    Gets the vendor ID for the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the vendor ID
		///    for current instance.
		/// </value>
		public string VendorId {
			get {return vendor_id;}
		}
		
#endregion
		
		
		
#region Protected Methods
		
		/// <summary>
		///    Populates and initializes a new instance of <see
		///    cref="XiphComment" /> by reading the contents of a raw
		///    Xiph Comment from a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing a raw Xiph
		///    comment.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		protected void Parse (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			// Reset picture state before parsing
			picture_fields_dirty = false;
			pictures = null;

			// The first thing in the comment data is the vendor ID
			// length, followed by a UTF8 string with the vendor ID.
			int pos = 0;
			int vendor_length = (int) data.Mid (pos, 4)
				.ToUInt (false);
			pos += 4;

			vendor_id = data.ToString (StringType.UTF8, pos,
				vendor_length);
			pos += vendor_length;

			// Next the number of fields in the comment vector.

			int comment_fields = (int) data.Mid (pos, 4)
				.ToUInt (false);
			pos += 4;

			for(int i = 0; i < comment_fields; i++) {
				// Each comment field is in the format
				// "KEY=value" in a UTF8 string and has 4 bytes
				// before the text starts that gives the length.

				int comment_length = (int) data.Mid (pos, 4)
					.ToUInt (false);
				pos += 4;

				string comment = data.ToString (StringType.UTF8,
					pos, comment_length);
				pos += comment_length;

				int comment_separator_position = comment
					.IndexOf ('=');

				if (comment_separator_position < 0)
					continue;

				string key = comment.Substring (0,
					comment_separator_position)
					.ToUpper (
						CultureInfo.InvariantCulture);
				string value = comment.Substring (
					comment_separator_position + 1);
				string [] values;
				
				if (field_list.TryGetValue (key, out values)) {
					Array.Resize <string> (ref values,
						values.Length + 1);
					values [values.Length - 1] = value;
					field_list [key] = values;
				} else {
					SetField (key, value);
				}
			}
		}
		
#endregion
		
		
		

#region Private methods

		/// <summary>
		///    If needed, update the pictures field from the value of the
		///    pictures array.
		/// </summary>
		/// <param name="fieldName">
		///    Name of the field being queried by the user.
		///    If the field name is not a picture field name, no update will take place.
		/// </param>
		private void EnsurePictureFieldsClean (string fieldName)
		{
			if (IsPictureField (fieldName) && picture_fields_dirty)
				StorePictures ();
		}

		/// <summary>
		///    Parses the pictures from the COVERART and METADATA_BLOCK_PICTURE
		///    fields contained in the <see cref="field_list" /> variable.
		/// </summary>
		private void ParsePictures ()
		{
			string[] coverArtStrings = GetField ("COVERART"),
				blockPictureStrings = GetField ("METADATA_BLOCK_PICTURE");

			IPicture[] pictures = new IPicture[coverArtStrings.Length + blockPictureStrings.Length];

			// Read old-format COVERART
			for (int i = 0; i < coverArtStrings.Length; i++) {
				ByteVector data = new ByteVector (Convert.FromBase64String (coverArtStrings[i]));
				pictures[i] = new Picture (data);
			}

			// Read new-format METADATA_BLOCK_PICTURE
			for (int i = 0; i < blockPictureStrings.Length; i++) {
				ByteVector data = new ByteVector (Convert.FromBase64String (blockPictureStrings[i]));
				pictures[i + coverArtStrings.Length] = new Flac.Picture (data);
			}

			this.pictures = pictures;
			// Pictures array loaded from picture field, reset dirty flag
			picture_fields_dirty = false;
		}

		/// <summary>
		///    Stores the pictures in the pictures array in the
		///    METADATA_BLOCK_PICTURE field. Conversion to Flac.Picture is done
		///    as needed.
		/// </summary>
		private void StorePictures ()
		{
			// Remove all picture fields
			foreach (string pictureField in PICTURE_FIELDS)
				field_list.Remove (pictureField);

			// Store the pictures array in METADATA_BLOCK_PICTURE
			if (pictures != null &&
				pictures.Length > 0) {
				string[] flacPictures = new string[pictures.Length];

				for (int i = 0; i < pictures.Length; ++i) {
					flacPictures[i] = Convert.ToBase64String (new Flac.Picture (pictures[i]).Render ().Data);
				}

				field_list.Add ("METADATA_BLOCK_PICTURE", flacPictures);
			}

			// The picture fields are now up to date with the pictures array
			picture_fields_dirty = false;
		}

		/// <summary>
		///    If the given parameter represents a Xiph field containing
		///    picture information, clear the currently parsed pictures
		///    array, so it will be loaded from the field value again
		///    when the Pictures property is accessed.
		/// </summary>
		/// <param name="key">Name of the Xiph field being changed</param>
		private void ResetPicturesState(string key)
		{
			if (IsPictureField(key)) {
				picture_fields_dirty = false;
				pictures = null;
			}
		}

		/// <summary>
		///    Returns a value indicating if a field name is a picture field.
		/// </summary>
		/// <param name="fieldName">Name of the field</param>
		/// <returns>
		///    true if the field represents a field that contains picture art data,
		///    false otherwise.
		/// </returns>
		private static bool IsPictureField (string fieldName)
		{
			foreach (string pictureFieldName in PICTURE_FIELDS)
				if (string.Equals (fieldName, pictureFieldName))
					return true;
			return false;
		}

#endregion

#region IEnumerable
		
		/// <summary>
		///    Gets an enumerator for enumerating through the the field
		///    identifiers.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the field identifiers.
		/// </returns>
		public IEnumerator<string> GetEnumerator ()
		{
			return field_list.Keys.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return field_list.Keys.GetEnumerator();
		}
		
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Xiph" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.Xiph;}
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
		///    This property is implemented using the "TITLE" field.
		/// </remarks>
		public override string Title {
			get {return GetFirstField ("TITLE");}
			set {SetField ("TITLE", value);}
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
		///    This property is implemented using the "TITLESORT"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string TitleSort {
			get {return GetFirstField ("TITLESORT");}
			set {SetField ("TITLESORT", value);}
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
		///    This property is implemented using the "SUBTITLE"
		///    non-standard field.
		/// </remarks>
		public override string Subtitle
		{
			get { return GetFirstField("SUBTITLE"); }
			set { SetField("SUBTITLE", value); }
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
		///    This property is implemented using the "DESCRIPTION"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string Description
		{
			get { return GetFirstField("DESCRIPTION"); }
			set { SetField("DESCRIPTION", value); }
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
		///    This property is implemented using the "ARTIST" field.
		/// </remarks>
		public override string [] Performers {
			get {return GetField ("ARTIST");}
			set {SetField ("ARTIST", value);}
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
		///    This property is implemented using the "ARTISTSORT" field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string [] PerformersSort {
			get {return GetField ("ARTISTSORT");}
			set {SetField ("ARTISTSORT", value);}
		}

		/// <summary>
		///    Gets and sets the Charaters for a video media, or
		///    instruments played for music media. 
		///    This should match the <see cref="Performers"/> array (for
		///    each person correspond one/more role). Several roles for
		///    the same artist/actor can be made up with semicolons. 
		///    For example, "Marty McFly; Marty McFly Jr.; Marlene McFly".
		/// </summary>
		/// <remarks>
		///    <para> This is typically usefull for movies, although the
		///    instrument played by each artist in a music may be of
		///    relevance.
		///    </para>
		///    <para>It is highly important to match each role to the 
		///    performers. This means that a role may be <see 
		///    langword="null"/> to keep the match between a
		///    Performers[i] and PerformersRole[i].
		///    </para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "ARTISTROLE" 
		///    non-standard field.
		/// </remarks>
		public override string[] PerformersRole
		{
			get { return GetField("ARTISTROLE"); }
			set { SetField("ARTISTROLE", value); }
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
		///    This property is implemented using the "ALBUMARTIST"
		///    field.
		/// </remarks>
		public override string [] AlbumArtists {
			get {
				// First try to get AlbumArtist, if that comment is not present try: 
				// ENSEMBLE: set by TAG & RENAME
				// ALBUM ARTIST: set by The GodFather
				string[] value = GetField("ALBUMARTIST");
				if (value != null && value.Length > 0)
				  return value;

				value = GetField("ALBUM ARTIST");
				if (value != null && value.Length > 0)
				  return value;

				return GetField ("ENSEMBLE"); 
			}
			set {SetField ("ALBUMARTIST", value);}
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
		///    This property is implemented using the "ALBUMARTISTSORT"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string [] AlbumArtistsSort {
			get {return GetField ("ALBUMARTISTSORT");}
			set {SetField ("ALBUMARTISTSORT", value);}
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
		///    This property is implemented using the "COMPOSER" field.
		/// </remarks>
		public override string [] Composers {
			get {return GetField ("COMPOSER");}
			set {SetField ("COMPOSER", value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names for the composers of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the composer of the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "COMPOSERSORT"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string [] ComposersSort {
			get {return GetField ("COMPOSERSORT");}
			set {SetField ("COMPOSERSORT", value);}
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
		///    This property is implemented using the "ALBUM" field.
		/// </remarks>
		public override string Album {
			get {return GetFirstField ("ALBUM");}
			set {SetField ("ALBUM", value);}
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
		///    This property is implemented using the "ALBUMSORT"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string AlbumSort {
			get {return GetFirstField ("ALBUMSORT");}
			set {SetField ("ALBUMSORT", value);}
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
		///    This property is implemented using the "COMMENT" field.
		/// </remarks>
		public override string Comment {
			get { return GetFirstField("COMMENT"); }
			set { SetField("COMMENT", value); }
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
		///    This property is implemented using the "GENRE" field.
		/// </remarks>
		public override string [] Genres {
			get {return GetField ("GENRE");}
			set {SetField ("GENRE", value);}
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
		///    This property is implemented using the "DATE" field. If a
		///    value greater than 9999 is set, this property will be
		///    cleared.
		/// </remarks>
		public override uint Year {
			get {
				string text = GetFirstField ("DATE");
				uint value;
				return (text != null && uint.TryParse (
					text.Length > 4 ? text.Substring (0, 4)
					: text, out value)) ? value : 0;
			}
			set {SetField ("DATE", value);}
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
		///    This property is implemented using the "TRACKNUMER"
		///    field.
		/// </remarks>
		public override uint Track {
			get {
				string text = GetFirstField ("TRACKNUMBER");
				string [] values;
				uint value;
				
				if (text != null && (values = text.Split ('/'))
					.Length > 0 && uint.TryParse (
						values [0], out value))
					return value;
				
				return 0;
			}
			set {
				SetField ("TRACKTOTAL", TrackCount);
				SetField ("TRACKNUMBER", value, "00");
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
		///    This property is implemented using the "TRACKTOTAL" field
		///    but is capable of reading from "TRACKNUMBER" if the total
		///    is stored in {track}/{count} format.
		/// </remarks>
		public override uint TrackCount {
			get {
				string text;
				string [] values;
				uint value;
				
				if ((text = GetFirstField ("TRACKTOTAL")) !=
					null && uint.TryParse (text, out value))
					return value;
				
				if ((text = GetFirstField ("TRACKNUMBER")) !=
					null && (values = text.Split ('/'))
					.Length > 1 && uint.TryParse (
						values [1], out value))
					return value;
				
				return 0;
			}
			set {SetField ("TRACKTOTAL", value);}
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
		///    This property is implemented using the "DISCNUMBER"
		///    field.
		/// </remarks>
		public override uint Disc {
			get {
				string text = GetFirstField ("DISCNUMBER");
				string [] values;
				uint value;
				
				if (text != null && (values = text.Split ('/'))
					.Length > 0 && uint.TryParse (
						values [0], out value))
					return value;
				
				return 0;
			}
			set {
				SetField ("DISCTOTAL", DiscCount);
				SetField ("DISCNUMBER", value);
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
		///    This property is implemented using the "DISCTOTAL" field
		///    but is capable of reading from "DISCNUMBER" if the total
		///    is stored in {disc}/{count} format.
		/// </remarks>
		public override uint DiscCount {
			get {
				string text;
				string [] values;
				uint value;

				if ((text = GetFirstField ("DISCTOTAL")) != null
					&& uint.TryParse (text, out value))
					return value;
				
				if ((text = GetFirstField ("DISCNUMBER")) !=
					null && (values = text.Split ('/'))
					.Length > 1 && uint.TryParse (
						values [1], out value))
					return value;
				
				return 0;
			}
			set {SetField ("DISCTOTAL", value);}
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
		///    This property is implemented using the "LYRICS" field.
		/// </remarks>
		public override string Lyrics {
			get {return GetFirstField ("LYRICS");}
			set {SetField ("LYRICS", value);}
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
		///    This property is implemented using the "GROUPING" field.
		/// </remarks>
		public override string Grouping {
			get {return GetFirstField ("GROUPING");}
			set {SetField ("GROUPING", value);}
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
		///    This property is implemented using the "TEMPO" field.
		///    Since there is no official definition, this property is
		///    also implemented using the "BPM" field.
		/// </remarks>
		public override uint BeatsPerMinute {
			get {
				SaveBeatsPerMinuteAsTempo = true;
				string text = GetFirstField ("TEMPO");
				if (string.IsNullOrEmpty(text))
				{
					text = GetFirstField("BPM");
					if (!string.IsNullOrEmpty(text))
					{
						SaveBeatsPerMinuteAsTempo = false;
					}
				}
				double value;
				return (text != null &&
					double.TryParse (text, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out value) &&
					value > 0) ? (uint) Math.Round (value) :
					0;
			}
			set {
				if (SaveBeatsPerMinuteAsTempo) SetField("TEMPO", value);
				else SetField("BPM", value);
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
		///    This property is implemented using the "CONDUCTOR" field.
		/// </remarks>
		public override string Conductor {
			get {return GetFirstField ("CONDUCTOR");}
			set {SetField ("CONDUCTOR", value);}
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
		///    This property is implemented using the "COPYRIGHT" field.
		/// </remarks>
		public override string Copyright {
			get {return GetFirstField ("COPYRIGHT");}
			set {SetField ("COPYRIGHT", value);}
		}


		/// <summary>
		///    Gets and sets the date at which the tag has been written.
		/// </summary>
		/// <value>
		///    A nullable <see cref="DateTime" /> object containing the 
		///    date at which the tag has been written, or <see 
		///    langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "DATETAGGED" 
		///    non-standard field. It used the the ISO 8601 standard:
		///    YYYY-MM-DDTHH:MM:SS
		///    <see url="https://wiki.xiph.org/VorbisComment#Date_and_time"/> 
		/// </remarks>
		public override DateTime? DateTagged
		{
			get
			{
				string value = GetFirstField("DATETAGGED");
				if (value != null)
				{
					value = value.Replace('T', ' ');
					DateTime date;
					if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out date))
					{
						return date;
					}
				}
				return null;
			}
			set
			{
				string date = null;
				if (value != null)
				{
					date = string.Format("{0:yyyy-MM-dd HH:mm:ss}", value);
					date = date.Replace(' ', 'T');
				}
				SetField("DATETAGGED", date);
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Artist ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ArtistID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ARTISTID" field.
		/// </remarks>
		public override string MusicBrainzArtistId {
			get {return GetFirstField ("MUSICBRAINZ_ARTISTID");}
			set {SetField ("MUSICBRAINZ_ARTISTID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Group ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseGroupID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_RELEASEGROUPID" field.
		/// </remarks>
		public override string MusicBrainzReleaseGroupId {
			get { return GetFirstField("MUSICBRAINZ_RELEASEGROUPID"); }
			set { SetField("MUSICBRAINZ_RELEASEGROUPID", value); }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMID" field.
		/// </remarks>
		public override string MusicBrainzReleaseId {
			get {return GetFirstField ("MUSICBRAINZ_ALBUMID");}
			set {SetField ("MUSICBRAINZ_ALBUMID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Artist ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseArtistID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMARTISTID" field.
		/// </remarks>
		public override string MusicBrainzReleaseArtistId {
			get {return GetFirstField ("MUSICBRAINZ_ALBUMARTISTID");}
			set {SetField ("MUSICBRAINZ_ALBUMARTISTID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Track ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    TrackID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_TRACKID" field.
		/// </remarks>
		public override string MusicBrainzTrackId {
			get {return GetFirstField ("MUSICBRAINZ_TRACKID");}
			set {SetField ("MUSICBRAINZ_TRACKID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Disc ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    DiscID for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_DISCID" field.
		/// </remarks>
		public override string MusicBrainzDiscId {
			get {return GetFirstField ("MUSICBRAINZ_DISCID");}
			set {SetField ("MUSICBRAINZ_DISCID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicIP PUID
		///    for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICIP_PUID" field.
		/// </remarks>
		public override string MusicIpId {
			get {return GetFirstField ("MUSICIP_PUID");}
			set {SetField ("MUSICIP_PUID", value);}
		}

		/// <summary>
		///    Gets and sets the Amazon ID for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the AmazonID
		///    for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ASIN" field.
		/// </remarks>
		public override string AmazonId {
			get {return GetFirstField ("ASIN");}
			set {SetField ("ASIN", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseStatus for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMSTATUS" field.
		/// </remarks>
		public override string MusicBrainzReleaseStatus {
			get {return GetFirstField ("MUSICBRAINZ_ALBUMSTATUS");}
			set {SetField ("MUSICBRAINZ_ALBUMSTATUS", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseType for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMTYPE" field.
		/// </remarks>
		public override string MusicBrainzReleaseType {
			get {return GetFirstField ("MUSICBRAINZ_ALBUMTYPE");}
			set {SetField ("MUSICBRAINZ_ALBUMTYPE", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseCountry for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "RELEASECOUNTRY" field.
		/// </remarks>
		public override string MusicBrainzReleaseCountry {
			get {return GetFirstField ("RELEASECOUNTRY");}
			set {SetField ("RELEASECOUNTRY", value);}
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
		///    <para>This property is implemented using the COVERART
		///    field.</para>
		/// </remarks>
		public override IPicture[] Pictures {
			get
			{
				// Load pictures on demand from the fields
				if (pictures == null) {
					ParsePictures ();
				}
				
				return pictures;
			}
			set
			{
				if (value == null) {
					// Set pictures to a 0-length array to prevent
					// re-parsing by the getter on the next access
					pictures = EMPTY_PICTURES;
				}
				else {
					pictures = value;
				}
				
				// The pictures fields are not up to date with the pictures array anymore
				picture_fields_dirty = true;
			}
		}

		/// <summary>
		///    Gets and sets whether or not the album described by the
		///    current instance is a compilation.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    album described by the current instance is a compilation.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "COMPILATION" field.
		/// </remarks>
		public bool IsCompilation {
			get {
				string text;
				int value;

				if ((text = GetFirstField ("COMPILATION")) !=
					null && int.TryParse (text, out value)) {
						return value == 1;
				}
				return false;
			}
			set {
				if (value) {
					SetField ("COMPILATION", "1");
				} else {
					RemoveField ("COMPILATION");
				}
			}
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
		public override double ReplayGainTrackGain {
			get {
				string text = GetFirstField ("REPLAYGAIN_TRACK_GAIN");
				double value;
				
				if (text == null) {
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db")) {
					text = text.Substring (0, text.Length - 2).Trim();
				}
				
				if (double.TryParse (text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value)) {
					return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					RemoveField ("REPLAYGAIN_TRACK_GAIN");
				} else {
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetField ("REPLAYGAIN_TRACK_GAIN", text);
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
		public override double ReplayGainTrackPeak {
			get {
				string text;
				double value;

				if ((text = GetFirstField ("REPLAYGAIN_TRACK_PEAK")) !=
					null && double.TryParse (text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value)) {
						return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					RemoveField ("REPLAYGAIN_TRACK_PEAK");
				} else {
					string text = value.ToString ("0.000000", CultureInfo.InvariantCulture);
					SetField ("REPLAYGAIN_TRACK_PEAK", text);
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
		public override double ReplayGainAlbumGain {
			get {
				string text = GetFirstField ("REPLAYGAIN_ALBUM_GAIN");
				double value;

				if (text == null) {
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db")) {
					text = text.Substring (0, text.Length - 2).Trim();
				}
				
				if (double.TryParse (text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value)) {
					return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					RemoveField ("REPLAYGAIN_ALBUM_GAIN");
				} else {
					string text = value.ToString ("0.00 dB",
						CultureInfo.InvariantCulture);
					SetField ("REPLAYGAIN_ALBUM_GAIN", text);
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
		public override double ReplayGainAlbumPeak {
			get {
				string text;
				double value;

				if ((text = GetFirstField ("REPLAYGAIN_ALBUM_PEAK")) !=
					null && double.TryParse (text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value)) {
						return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					RemoveField ("REPLAYGAIN_ALBUM_PEAK");
				} else {
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetField ("REPLAYGAIN_ALBUM_PEAK", text);
				}
			}
		}

		/// <summary>
		///    Gets and sets the initial key of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the initial key of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "INITIALKEY" field.
		/// </remarks>
		public override string InitialKey
		{
			get { return GetFirstField("INITIALKEY"); }
			set { SetField("INITIALKEY", value); }
		}

		/// <summary>
		///    Gets and sets the remixer of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the remixer of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "REMIXEDBY" field.
		/// </remarks>
		public override string RemixedBy
		{
			get { return GetFirstField("REMIXEDBY"); }
			set { SetField("REMIXEDBY", value); }
		}

		/// <summary>
		///    Gets and sets the publisher of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the publisher of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ORGANIZATION" field.
		/// </remarks>
		public override string Publisher
		{
			get { return GetFirstField("ORGANIZATION"); }
			set { SetField("ORGANIZATION", value); }
		}

		/// <summary>
		///    Gets and sets the ISRC (International Standard Recording Code) of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the ISRC of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ISRC" field.
		/// </remarks>
		public override string ISRC
		{
			get { return GetFirstField("ISRC"); }
			set { SetField("ISRC", value); }
		}

		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance does not
		///    any values. Otherwise <see langword="false" />.
		/// </value>
		public override bool IsEmpty {
			get { return FieldCount == 0; }
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			field_list.Clear ();

			// clear pictures
			pictures = new IPicture[0];
			picture_fields_dirty = false;
		}
		
#endregion
	}
}
