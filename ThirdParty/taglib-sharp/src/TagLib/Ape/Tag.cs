//
// Tag.cs: Provides a representation of an APEv2 tag which can be read from and
// written to disk.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   apetag.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2004 Allan Sandfeld Jensen (Original Implementation)
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

namespace TagLib.Ape {
	/// <summary>
	///    This class extends <see cref="TagLib.Tag" /> and implements <see
	///    cref="T:System.Collections.Generic.IEnumerable`1" /> to provide a representation of an APEv2
	///    tag which can be read from and written to disk.
	/// </summary>
	public class Tag : TagLib.Tag, IEnumerable<string>
	{
		
#region Private Static Fields
		
		/// <summary>
		///    Contains names of picture fields, indexed to correspond
		///    to their picture item names.
		/// </summary>
		private static string [] picture_item_names = new string [] {
			"Cover Art (other)",
			"Cover Art (icon)",
			"Cover Art (other icon)",
			"Cover Art (front)",
			"Cover Art (back)",
			"Cover Art (leaflet)",
			"Cover Art (media)",
			"Cover Art (lead)",
			"Cover Art (artist)",
			"Cover Art (conductor)",
			"Cover Art (band)",
			"Cover Art (composer)",
			"Cover Art (lyricist)",
			"Cover Art (studio)",
			"Cover Art (recording)",
			"Cover Art (performance)",
			"Cover Art (movie scene)",
			"Cover Art (colored fish)",
			"Cover Art (illustration)",
			"Cover Art (band logo)",
			"Cover Art (publisher logo)",
			"Embedded Object"
		};
		
#endregion
		
		
		
#region Private Fields
		
		/// <summary>
		///    Contains the tag footer.
		/// </summary>
		private Footer footer = new Footer ();
		
		/// <summary>
		///    Contains the items in the tag.
		/// </summary>
		private List<Item> items = new List<Item> ();
		
		#endregion
		
		
		
		#region Public Static Properties
		
		/// <summary>
		///    Specifies the identifier used find an APEv2 tag in a
		///    file.
		/// </summary>
		/// <value>
		///    "<c>APETAGEX</c>"
		/// </value>
		[Obsolete("Use Footer.FileIdentifer")]
		public static readonly ReadOnlyByteVector FileIdentifier =
			Footer.FileIdentifier;
		
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
		///    cref="Tag" /> by reading the contents from a specified
		///    position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    from which the contents of the new instance is to be
		///    read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the tag.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		public Tag (TagLib.File file, long position)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			if (position < 0 ||
				position > file.Length - Footer.Size)
				throw new ArgumentOutOfRangeException (
					"position");
			
			Read (file, position);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> by reading the contents of a raw tag in a
		///    specified <see cref="ByteVector"/> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    tag.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is too small to contain a tag,
		///    has a header where the footer should be, or is smaller
		///    than the tag it is supposed to contain.
		/// </exception>
		public Tag (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < Footer.Size)
				throw new CorruptFileException (
					"Does not contain enough footer data.");
			
			footer = new Footer (
				data.Mid ((int) (data.Count - Footer.Size)));
			
			if (footer.TagSize == 0)
				throw new CorruptFileException (
					"Tag size out of bounds.");
			
			// If we've read a header at the end of the block, the
			// block is invalid.
			if ((footer.Flags & FooterFlags.IsHeader) != 0)
				throw new CorruptFileException (
					"Footer was actually header.");
			
			if (data.Count < footer.TagSize)
				throw new CorruptFileException (
					"Does not contain enough tag data.");
			
			Parse (data.Mid ((int) (data.Count - footer.TagSize),
				(int) (footer.TagSize - Footer.Size)));
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets whether or not the current instance has a
		///    header when rendered.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance has a header when rendered.
		/// </value>
		public bool HeaderPresent {
			get {
				return (footer.Flags &
					FooterFlags.HeaderPresent) != 0;
			}
			set {
				if (value)
					footer.Flags |= FooterFlags.HeaderPresent;
				else
					footer.Flags &= ~FooterFlags.HeaderPresent;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Adds a number to the value stored in a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="number">
		///    A <see cref="uint" /> value containing the number to
		///    store.
		/// </param>
		/// <param name="count">
		///    A <see cref="uint" /> value representing a total which
		///    <paramref name="number" /> is a part of, or zero if
		///    <paramref name="number" /> is not part of a set.
		/// </param>
		/// <remarks>
		///    If both <paramref name="number" /> and <paramref
		///    name="count" /> are equal to zero, the value will not be
		///    added. If <paramref name="count" /> is zero, <paramref
		///    name="number" /> by itself will be stored. Otherwise, the
		///    values will be stored as "<paramref name="number"
		///    />/<paramref name="count" />".
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void AddValue (string key, uint number, uint count)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (number == 0 && count == 0)
				return;
			else if (count != 0)
				AddValue (key, string.Format (
					CultureInfo.InvariantCulture, "{0}/{1}",
					number, count));
			else
				AddValue (key, number.ToString (
					CultureInfo.InvariantCulture));
		}
		
		/// <summary>
		///    Stores a number in a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="number">
		///    A <see cref="uint" /> value containing the number to
		///    store.
		/// </param>
		/// <param name="count">
		///    A <see cref="uint" /> value representing a total which
		///    <paramref name="number" /> is a part of, or zero if
		///    <paramref name="number" /> is not part of a set.
		/// </param>
		/// <remarks>
		///    If both <paramref name="number" /> and <paramref
		///    name="count" /> are equal to zero, the value will be
		///    cleared. If <paramref name="count" /> is zero, <paramref
		///    name="number" /> by itself will be stored. Otherwise, the
		///    values will be stored as "<paramref name="number"
		///    />/<paramref name="count" />".
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void SetValue (string key, uint number, uint count)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (number == 0 && count == 0)
				RemoveItem (key);
			else if (count != 0)
				SetValue (key, string.Format (
					CultureInfo.InvariantCulture, "{0}/{1}",
					number, count));
			else
				SetValue (key, number.ToString (
					CultureInfo.InvariantCulture));
		}
		
		/// <summary>
		///    Adds the contents of a <see cref="string" /> to the value
		///    stored in a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> object containing the text to
		///    add.
		/// </param>
		/// <remarks>
		///    If <paramref name="value" /> is <see langword="null" />
		///    or empty, the value will not be added.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void AddValue (string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (string.IsNullOrEmpty (value))
				return;
			
			AddValue (key, new string [] {value});
		}
		
		/// <summary>
		///    Stores the contents of a <see cref="string" /> in a
		///    specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> object containing the text to
		///    store.
		/// </param>
		/// <remarks>
		///    If <paramref name="value" /> is <see langword="null" />
		///    or empty, the value will be cleared.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void SetValue (string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (string.IsNullOrEmpty (value))
				RemoveItem (key);
			else
				SetValue (key, new string [] {value});
		}
		
		/// <summary>
		///    Adds the contents of a <see cref="T:string[]" /> to the
		///    value stored in a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="value">
		///    A <see cref="T:string[]" /> containing the text to add.
		/// </param>
		/// <remarks>
		///    If <paramref name="value" /> is <see langword="null" />
		///    or empty, the value will not be added.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void AddValue (string key, string [] value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (value == null || value.Length == 0)
				return;
			
			int index = GetItemIndex (key);
			
			List<string> values = new List<string> ();
			
			if (index >= 0)
				values.AddRange (items [index].ToStringArray ());
			
			values.AddRange (value);
			
			Item item = new Item (key, values.ToArray ());
			
			if (index >= 0)
				items [index] = item;
			else
				items.Add (item);
		}
		
		/// <summary>
		///    Stores the contents of a <see cref="T:string[]" /> in a
		///    specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to store the value in.
		/// </param>
		/// <param name="value">
		///    A <see cref="T:string[]" /> containing the text to store.
		/// </param>
		/// <remarks>
		///    If <paramref name="value" /> is <see langword="null" />
		///    or empty, the value will be cleared.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public void SetValue (string key, string [] value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (value == null || value.Length == 0) {
				RemoveItem (key);
				return;
			}
			
			Item item = new Item (key, value);
			
			int index = GetItemIndex (key);
			if (index >= 0)
				items [index] = item;
			else
				items.Add (item);
			
		}
		
		/// <summary>
		///    Gets a specified item from the current instance.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to get from the current instance.
		/// </param>
		/// <returns>
		///    The item with the matching name contained in the current
		///    instance, or <see langword="null" /> if a matching object
		///    was not found.
		/// </returns>
		public Item GetItem (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			StringComparison comparison =
				StringComparison.InvariantCultureIgnoreCase;
			
			foreach (Item item in items)
				if (key.Equals (item.Key, comparison))
					return item;
			
			return null;
		}
		
		/// <summary>
		///    Adds an item to the current instance, replacing the
		///    existing one of the same name.
		/// </summary>
		/// <param name="item">
		///    A <see cref="Item" /> object to add to the current
		///    instance.
		/// </param>
		public void SetItem (Item item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			int index = GetItemIndex (item.Key);
			if (index >= 0)
				items [index] = item;
			else
				items.Add (item);
		}
		
		/// <summary>
		///    Removes the item with a specified key from the current
		///    instance.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to remove from the current instance.
		/// </param>
		public void RemoveItem (string key)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			StringComparison comparison =
				StringComparison.InvariantCultureIgnoreCase;
			
			for (int i = items.Count - 1; i >= 0; i --)
				if (key.Equals (items [i].Key, comparison))
					items.RemoveAt (i);
		}
		
		/// <summary>
		/// Checks if an item exists.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to check.
		/// </param>
		/// <returns>
		///    Returns <see langword="true"/> if the <paramref name="key"/>
		///    exists - else <see langword="false"/> is returned.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> is <see langword="null" />.
		/// </exception>
		public bool HasItem(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			
			return GetItemIndex(key) >= 0;
		}
		
		/// <summary>
		///    Renders the current instance as a raw APEv2 tag.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered tag.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector data = new ByteVector ();
			uint item_count = 0;
			
			foreach (Item item in items) {
				data.Add (item.Render ());
				item_count ++;
			}
			
			footer.ItemCount = item_count;
			footer.TagSize = (uint) (data.Count + Footer.Size);
			HeaderPresent = true;
			
			data.Insert (0, footer.RenderHeader ());
			data.Add (footer.RenderFooter ());
			return data;
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Populates the current instance be reading in a tag from
		///    a specified position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the tag from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to read the tag.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than 0 or greater
		///    than the size of the file.
		/// </exception>
		protected void Read (TagLib.File file, long position)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Mode = File.AccessMode.Read;
			
			if (position < 0 || position > file.Length - Footer.Size)
				throw new ArgumentOutOfRangeException (
					"position");
			
			file.Seek (position);
			footer = new Footer (file.ReadBlock ((int)Footer.Size));
			
			if (footer.TagSize == 0)
				throw new CorruptFileException (
					"Tag size out of bounds.");
			
			// If we've read a header, we don't have to seek to read
			// the content. If we've read a footer, we need to move
			// back to the start of the tag.
			if ((footer.Flags & FooterFlags.IsHeader) == 0)
				file.Seek (position + Footer.Size - footer.TagSize);
			
			Parse (file.ReadBlock ((int)(footer.TagSize - Footer.Size)));
		}
		
		/// <summary>
		///    Populates the current instance by parsing the contents of
		///    a raw APEv2 tag, minus the header and footer.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the content
		///    of an APEv2 tag, minus the header and footer.
		/// </param>
		/// <remarks>
		///    This method must only be called after the internal
		///    footer has been read from the file, otherwise the data
		///    cannot be parsed correctly.
		/// </remarks>
		protected void Parse (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			int pos = 0;
			
			try {
				// 11 bytes is the minimum size for an APE item
				for (uint i = 0; i < footer.ItemCount &&
					pos <= data.Count - 11; i++) {
					Item item = new Item (data, pos);
					SetItem (item);
					pos += item.Size;
				}
			} catch (CorruptFileException) {
				// A corrupt item was encountered, considered
				// the tag finished with what has been read.
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Gets the index of an item in the current instance.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key to look
		///    for in the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> value containing the index in <see
		///    cref="items" /> at which the item appears, or -1 if the
		///    item was not found.
		/// </returns>
		/// <remarks>
		///    Keys are compared in a case insensitive manner.
		/// </remarks>
		private int GetItemIndex (string key)
		{
			StringComparison comparison =
				StringComparison.InvariantCultureIgnoreCase;
			
			for (int i = 0; i < items.Count; i ++)
				if (key.Equals (items [i].Key, comparison))
					return i;
			
			return -1;
		}
		
		/// <summary>
		///    Gets the text value from a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to get the value from.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the text of the
		///    specified frame, or <see langword="null" /> if no value
		///    was found.
		/// </returns>
		private string GetItemAsString (string key)
		{
				Item item = GetItem (key);
				return item != null ? item.ToString () : null;
		}
		
		/// <summary>
		///    Gets the text values from a specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to get the value from.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]" /> containing the text of the
		///    specified frame, or an empty array if no values were
		///    found.
		/// </returns>
		private string [] GetItemAsStrings (string key)
		{
			Item item = GetItem (key);
			return item != null ?
				item.ToStringArray () : new string [0];
		}
		
		/// <summary>
		///    Gets an integer value from a "/" delimited list in a
		///    specified item.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key of the
		///    item to get the value from.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value specifying the index in the
		///    integer list of the value to return.
		/// </param>
		/// <returns>
		///    A <see cref="uint" /> value read from the list in the
		///    frame, or 0 if the value wasn't found.
		/// </returns>
		private uint GetItemAsUInt32 (string key, int index)
		{
			string text = GetItemAsString (key);
			
			if (text == null)
				return 0;
			
			string [] values = text.Split (new char [] {'/'},
				index + 2);
			
			if (values.Length < index + 1)
				return 0;
			
			uint result;
			if (uint.TryParse (values [index], out result))
				return result;
			
			return 0;
		}
		
		#endregion
		
		
		
		#region IEnumerable
		
		/// <summary>
		///    Gets the enumerator for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerator`1" /> object enumerating through
		///    the item keys stored in the current instance.
		/// </returns>
		public IEnumerator<string> GetEnumerator ()
		{
			foreach (Item item in items)
				yield return item.Key;
		}
		
		/// <summary>
		///    Gets the enumerator for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="IEnumerator" /> object enumerating through
		///    the item keys stored in the current instance.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator ();
		}
		
		#endregion
		
		
		
		#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Ape" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.Ape;}
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
		///    This property is implemented using the "Title" item.
		/// </remarks>
		public override string Title {
			get { return GetItemAsString ("Title"); }
			set {SetValue ("Title", value);}
		}

		/// <summary>
		///    Gets and sets the sort names of the Title of the
		///    media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names for
		///    the Title of the media described by the current instance,
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TitleSort" item.
		/// </remarks>
		public override string TitleSort {
			get { return GetItemAsString ("TitleSort"); }
			set { SetValue ("TitleSort", value); }
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
		///    This property is implemented using the "Subtitle" item.
		/// </remarks>
		public override string Subtitle
		{
			get { return GetItemAsString("Subtitle"); }
			set { SetValue("Subtitle", value); }
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
		///    <para>This is especially relevant for a movie.
		///    For example, for "Back to the Future 2", this could be
		///    "After visiting 2015, Marty McFly must repeat his visit 
		///    to 1955 to prevent disastrous changes to 1985...without
		///    interfering with his first trip".
		///    </para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "Description" item.
		/// </remarks>
		public override string Description
		{
			get { return GetItemAsString("Description"); }
			set { SetValue("Description", value); }
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
		///    This property is implemented using the "Artist" item.
		/// </remarks>
		public override string [] Performers {
			get {return GetItemAsStrings ("Artist");}
			set {SetValue ("Artist", value);}
		}

		/// <summary>
		///    Gets and sets the sort names of the performers or artists
		///    who performed in the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ArtistSort" field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string[] PerformersSort {
			get { return GetItemAsStrings ("ArtistSort"); }
			set { SetValue ("ArtistSort", value); }
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
		///    This property is implemented using the "TMCL" Text
		///    Information Frame: The 'Musician credits list' is 
		///    intended as a mapping between instruments and the 
		///    musician that played it.Every odd field is an 
		///    instrument and every even is an artist or a comma 
		///    delimited list of artists.
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "PerformersRole" field.
		/// </remarks>
		public override string[] PerformersRole
		{
			get { return GetItemAsStrings("PerformersRole"); }
			set { SetValue("PerformersRole", value); }
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
		///    This property is implemented using the "Album Artist"
		///    item, and "AlbumArtist" as a backup property if it exists.
		/// </remarks>
		public override string [] AlbumArtists {
			get {
				string[] list = GetItemAsStrings("Album Artist");
				if (list.Length == 0)
					list = GetItemAsStrings("AlbumArtist");
				return list;
			}
			set {
				SetValue("Album Artist", value);
				// compatibility
				if (HasItem("AlbumArtist"))
					SetValue("AlbumArtist", value);
				}
		}

		/// <summary>
		///    Gets and sets the sort names for the band or artist who
		///    is credited in the creation of the entire album or
		///    collection containing the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the sort names
		///    for the band or artist who is credited in the creation
		///    of the entire album or collection containing the media
		///    described by the current instance or an empty array if
		///    no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "AlbumArtistSort"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string[] AlbumArtistsSort {
			get { return GetItemAsStrings ("AlbumArtistSort"); }
			set { SetValue ("AlbumArtistSort", value); }
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
		///    This property is implemented using the "Composer" item.
		/// </remarks>
		public override string [] Composers {
			get {return GetItemAsStrings ("Composer");}
			set {SetValue ("Composer", value);}
		}

		/// <summary>
		///    Gets and sets the sort names for the composers of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the sort names
		///    for the composer of the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ComposerSort"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string[] ComposersSort {
			get { return GetItemAsStrings ("ComposerSort"); }
			set { SetValue ("ComposerSort", value); }
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
		///    This property is implemented using the "Album" item.
		/// </remarks>
		public override string Album {
			get {return GetItemAsString ("Album");}
			set {SetValue ("Album", value);}
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
		///    This property is implemented using the "AlbumSort"
		///    field.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string AlbumSort {
			get { return GetItemAsString ("AlbumSort"); }
			set { SetValue ("AlbumSort", value); }
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
		///    This property is implemented using the "Comment" item.
		/// </remarks>
		public override string Comment {
			get {return GetItemAsString ("Comment");}
			set {SetValue ("Comment", value);}
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
		///    This property is implemented using the "Genre" item.
		/// </remarks>
		public override string [] Genres {
			get {return GetItemAsStrings ("Genre");}
			set {SetValue ("Genre", value);}
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
		///    This property is implemented using the "Year" item.
		/// </remarks>
		public override uint Year {
			get {
				string text = GetItemAsString ("Year");
				
				if (text == null || text.Length == 0)
					return 0;
				
				uint value;
				if (uint.TryParse (text, out value) ||
					(text.Length >= 4 && uint.TryParse (
						text.Substring (0, 4),
						out value)))
					return value;
				
				return 0;
			}
			set {SetValue ("Year", value, 0);}
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
		///    This property is implemented using the "Track" item.
		/// </remarks>
		public override uint Track {
			get {return GetItemAsUInt32 ("Track", 0);}
			set {SetValue ("Track", value, TrackCount);}
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
		///    This property is implemented using the "Track" item.
		/// </remarks>
		public override uint TrackCount {
			get {return GetItemAsUInt32 ("Track", 1);}
			set {SetValue ("Track", Track, value);}
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
		///    This property is implemented using the "Disc" item.
		/// </remarks>
		public override uint Disc {
			get {return GetItemAsUInt32 ("Disc", 0);}
			set {SetValue ("Disc", value, DiscCount);}
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
		///    This property is implemented using the "Disc" item.
		/// </remarks>
		public override uint DiscCount {
			get {return GetItemAsUInt32 ("Disc", 1);}
			set {SetValue ("Disc", Disc, value);}
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
		///    This property is implemented using the "Lyrics" item.
		/// </remarks>
		public override string Lyrics {
			get {return GetItemAsString ("Lyrics");}
			set {SetValue ("Lyrics", value);}
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
		///    This property is implemented using the "Grouping" item.
		/// </remarks>
		public override string Grouping {
			get {return GetItemAsString ("Grouping");}
			set {SetValue ("Grouping", value);}
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
		///    This property is implemented using the "BPM" item.
		/// </remarks>
		public override uint BeatsPerMinute {
			get {
				string text = GetItemAsString ("BPM");
				
				if (text == null)
					return 0;
				
				double value;
				
				if (double.TryParse (text, out value))
					return (uint) Math.Round (value);
				
				return 0;
			}
			set {SetValue ("BPM", value, 0);}
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
		///    This property is implemented using the "Conductor" item.
		/// </remarks>
		public override string Conductor {
			get {return GetItemAsString ("Conductor");}
			set {SetValue ("Conductor", value);}
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
		///    This property is implemented using the "Copyright" item.
		/// </remarks>
		public override string Copyright {
			get {return GetItemAsString ("Copyright");}
			set {SetValue ("Copyright", value);}
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
		///    This property is implemented using the "DateTagged" item.
		///    Format used is: yyyy-MM-dd HH:mm:ss
		/// </remarks>
		public override DateTime? DateTagged
		{
			get
			{
				string value = GetItemAsString("DateTagged");
				if (value != null)
				{
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
				}
				SetValue("DateTagged", date);
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Artist ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ArtistID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ARTISTID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzArtistId {
			get {return GetItemAsString ("MUSICBRAINZ_ARTISTID");}
			set {SetValue ("MUSICBRAINZ_ARTISTID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Group ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseGroupID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_RELEASEGROUPID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseGroupId
		{
			get { return GetItemAsString("MUSICBRAINZ_RELEASEGROUPID"); }
			set { SetValue("MUSICBRAINZ_RELEASEGROUPID", value); }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseId {
			get {return GetItemAsString ("MUSICBRAINZ_ALBUMID");}
			set {SetValue ("MUSICBRAINZ_ALBUMID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Artist ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseArtistID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMARTISTID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseArtistId {
			get {return GetItemAsString ("MUSICBRAINZ_ALBUMARTISTID");}
			set {SetValue ("MUSICBRAINZ_ALBUMARTISTID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Track ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    TrackID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_TRACKID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzTrackId {
			get {return GetItemAsString ("MUSICBRAINZ_TRACKID");}
			set {SetValue ("MUSICBRAINZ_TRACKID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Disc ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    DiscID for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_DISCID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzDiscId {
			get {return GetItemAsString ("MUSICBRAINZ_DISCID");}
			set {SetValue ("MUSICBRAINZ_DISCID", value);}
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicIPPUID
		///    for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICIP_PUID" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicIpId {
			get {return GetItemAsString ("MUSICIP_PUID");}
			set {SetValue ("MUSICIP_PUID", value);}
		}

		/// <summary>
		///    Gets and sets the Amazon ID of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the AmazonID
		///    for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ASIN" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string AmazonId {
			get {return GetItemAsString ("ASIN");}
			set {SetValue ("ASIN", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseStatus for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMSTATUS" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseStatus {
			get {return GetItemAsString ("MUSICBRAINZ_ALBUMSTATUS");}
			set {SetValue ("MUSICBRAINZ_ALBUMSTATUS", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseType for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "MUSICBRAINZ_ALBUMTYPE" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseType {
			get {return GetItemAsString ("MUSICBRAINZ_ALBUMTYPE");}
			set {SetValue ("MUSICBRAINZ_ALBUMTYPE", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseCountry of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the MusicBrainz
		///    ReleaseCountry for the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "RELEASECOUNTRY" item.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseCountry {
			get {return GetItemAsString ("RELEASECOUNTRY");}
			set {SetValue ("RELEASECOUNTRY", value);}
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
				string text = GetItemAsString("REPLAYGAIN_TRACK_GAIN");
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
					RemoveItem("REPLAYGAIN_TRACK_GAIN");
				}
				else
				{
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetValue("REPLAYGAIN_TRACK_GAIN", text);
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

				if ((text = GetItemAsString("REPLAYGAIN_TRACK_PEAK")) !=
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
					RemoveItem("REPLAYGAIN_TRACK_PEAK");
				}
				else
				{
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetValue("REPLAYGAIN_TRACK_PEAK", text);
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
				string text = GetItemAsString("REPLAYGAIN_ALBUM_GAIN");
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
					RemoveItem("REPLAYGAIN_ALBUM_GAIN");
				}
				else
				{
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetValue("REPLAYGAIN_ALBUM_GAIN", text);
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

				if ((text = GetItemAsString("REPLAYGAIN_ALBUM_PEAK")) !=
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
					RemoveItem("REPLAYGAIN_ALBUM_PEAK");
				}
				else
				{
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetValue("REPLAYGAIN_ALBUM_PEAK", text);
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
		///    This property is implemented using the "Cover Art" items
		///    and supports only one picture per type.
		/// </remarks>
		public override IPicture [] Pictures {
			get {
				List<IPicture> pictures = new List<IPicture> ();
				StringComparison comparison =
					StringComparison.InvariantCultureIgnoreCase;

				foreach (Item item in items) {

					if (item == null ||
						item.Type != ItemType.Binary)
						continue;

					int i;
					for (i = 0; i < picture_item_names.Length; i++) {
						if (picture_item_names[i].Equals(item.Key, comparison))
							break;
					}
					

					if (i >= picture_item_names.Length)
						continue;

					int index = item.Value.Find (
						ByteVector.TextDelimiter (
							StringType.UTF8));
					
					if (index < 0)
						continue;
					
					Picture pic = new Picture (
						item.Value.Mid (index + 1));
					
					pic.Description = item.Value
						.ToString (StringType.UTF8, 0,
							index);
					
					pic.Type = i < picture_item_names.Length - 1 ?
						(PictureType) i : PictureType.NotAPicture;
					
					pictures.Add (pic);
				}
				
				return pictures.ToArray ();
			}
			set {
				foreach (string item_name in picture_item_names)
					RemoveItem (item_name);
				
				if (value == null || value.Length == 0)
					return;
				
				foreach (IPicture pic in value) {
					int type = (int) pic.Type;
					
					if (type >= picture_item_names.Length)
						type = picture_item_names.Length - 1;

					string name = picture_item_names [type];
					
					if (GetItem (name) != null)
						continue;
					
					ByteVector data = ByteVector
						.FromString (
							pic.Description,
							StringType.UTF8);
					data.Add (ByteVector.TextDelimiter (
						StringType.UTF8));
					data.Add (pic.Data);
					
					SetItem (new Item (name, data));
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
			get {return items.Count == 0;}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			items.Clear ();
		}
		
		/// <summary>
		///    Copies the values from the current instance to another
		///    <see cref="TagLib.Tag" />, optionally overwriting
		///    existing values.
		/// </summary>
		/// <param name="target">
		///    A <see cref="TagLib.Tag" /> object containing the target
		///    tag to copy values to.
		/// </param>
		/// <param name="overwrite">
		///    A <see cref="bool" /> specifying whether or not to copy
		///    values over existing one.
		/// </param>
		/// <remarks>
		///    <para>If <paramref name="target" /> is of type <see
		///    cref="TagLib.Ape.Tag" /> a complete copy of all values
		///    will be performed. Otherwise, only standard values will
		///    be copied.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="target" /> is <see langword="null" />.
		/// </exception>
		public override void CopyTo (TagLib.Tag target, bool overwrite)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			TagLib.Ape.Tag match = target as TagLib.Ape.Tag;
			
			if (match == null) {
				base.CopyTo (target, overwrite);
				return;
			}
			
			foreach (Item item in items) {
				if (!overwrite &&
					match.GetItem (item.Key) != null)
					continue;
				
				match.items.Add (item.Clone ());
			}
		}
		
#endregion
	}
}
