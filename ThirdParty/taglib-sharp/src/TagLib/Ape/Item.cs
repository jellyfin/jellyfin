//
// Item.cs: Provides a representation of an APEv2 tag item which can be read
// from and written to disk.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   apeitem.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2004 by Allan Sandfeld Jensen (Original Implementation)
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
	///    Indicates the type of data stored in a <see cref="Item" />
	///    object.
	/// </summary>
	public enum ItemType {
		/// <summary>
		///    The item contains Unicode text.
		/// </summary>
		Text = 0,
		
		/// <summary>
		///    The item contains binary data.
		/// </summary>
		Binary = 1,
		
		/// <summary>
		///    The item contains a locator (file path/URL) for external
		///    information.
		/// </summary>
		Locator = 2
	}
	
	/// <summary>
	///    This class provides a representation of an APEv2 tag item which
	///    can be read from and written to disk.
	/// </summary>
	public class Item : ICloneable
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the type of data stored in the item.
		/// </summary>
		private ItemType type = ItemType.Text;
		
		/// <summary>
		///    Contains the item key.
		/// </summary>
		private string key = null;
		
		/// <summary>
		///    Contains the item value.
		/// </summary>
		private ReadOnlyByteVector data = null;
		
		/// <summary>
		///    Contains the item text.
		/// </summary>
		private string [] text = null;
		
		/// <summary>
		///    Indicates whether or not the item is read only.
		/// </summary>
		private bool read_only = false;
		
		/// <summary>
		///    Contains the size of the item on disk.
		/// </summary>
		private int size_on_disk;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Item" />  by reading in a raw APEv2 item.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the item to
		///    read.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the offset in
		///    <paramref name="data" /> at which the item data begins.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="offset" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    A complete item could not be read.
		/// </exception>
		public Item (ByteVector data, int offset)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			Parse (data, offset);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Item" /> with a specified key and value.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key to use
		///    for the current instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="string" /> object containing the value to
		///    store in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> or <paramref name="value" /> is
		///    <see langword="null" />.
		/// </exception>
		public Item (string key, string value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (value == null)
				throw new ArgumentNullException ("value");
			
			this.key = key;
			this.text = new string [] {value};
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Item" /> with a specified key and collection of
		///    values.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key to use
		///    for the current instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="T:string[]" /> containing the values to store
		///    in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> or <paramref name="value" /> is
		///    <see langword="null" />.
		/// </exception>
		public Item (string key, params string [] value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (value == null)
				throw new ArgumentNullException ("value");
			
			this.key = key;
			this.text = (string[]) value.Clone ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Item" /> with a specified key and collection of
		///    values.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key to use
		///    for the current instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="StringCollection" /> object containing the
		///    values to store in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> or <paramref name="value" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <seealso cref="Item(string,string[])" />
		[Obsolete("Use Item(string,string[])")]
		public Item (string key, StringCollection value)
		{
			if (key == null)
				throw new ArgumentNullException ("key");
			
			if (value == null)
				throw new ArgumentNullException ("value");
			
			this.key = key;
			this.text = value.ToArray ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Item" /> with a specified key and raw data.
		/// </summary>
		/// <param name="key">
		///    A <see cref="string" /> object containing the key to use
		///    for the current instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="StringCollection" /> object containing the
		///    values to store in the new instance.
		/// </param>
		/// <remarks>
		///    This constructor automatically marks the new instance as
		///    <see cref="ItemType.Binary" />.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="key" /> or <paramref name="value" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <seealso cref="Item(string,string[])" />
		public Item (string key, ByteVector value)
		{
			this.key = key;
			this.type = ItemType.Binary;
			
			data = value as ReadOnlyByteVector;
			if (data == null)
				data = new ReadOnlyByteVector (value);
		}
		
		private Item (Item item)
		{
			type = item.type;
			key = item.key;
			if (item.data != null)
				data = new ReadOnlyByteVector (item.data);
			if (item.text != null)
				text = (string[]) item.text.Clone ();
			read_only = item.read_only;
			size_on_disk = item.size_on_disk;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the key used to identify the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the key used to
		///    identify the current instance.
		/// </value>
		/// <remarks>
		///    This value is used for specifying the contents of the
		///    item in a common and consistant fashion. For example,
		///    <c>"TITLE"</c> specifies that the item contains the title
		///    of the track.
		/// </remarks>
		public string Key {
			get {return key;}
		}
		
		/// <summary>
		///    Gets the binary value stored in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the binary
		///    value stored in the current instance, or <see
		///    langword="null" /> if the item contains text.
		/// </value>
		public ByteVector Value {
			get {return (type == ItemType.Binary) ? data : null;}
		}
		
		/// <summary>
		///    Gets the size of the current instance as it last appeared
		///    on disk.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the size of the
		///    current instance as it last appeared on disk.
		/// </value>
		public int Size {
			get {return size_on_disk;}
		}
		
		/// <summary>
		///    Gets and sets the type of value contained in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ItemType" /> value indicating the type of
		///    value contained in the current instance.
		/// </value>
		public ItemType Type {
			get {return type;}
			set {type = value;}
		}
		
		/// <summary>
		///    Gets and sets whether or not the current instance is
		///    flagged as read-only on disk.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is flagged as read-only on disk.
		/// </value>
		public bool ReadOnly {
			get {return read_only;}
			set {read_only = value;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance contains no value.
		/// </value>
		public bool IsEmpty {
			get {
				if (type != ItemType.Binary)
					return text == null || text.Length == 0;
				else
					return data == null || data.IsEmpty;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets the contents of the current instance as a <see
		///    cref="string" />.
		/// </summary>
		/// <returns>
		///    <para>A <see cref="string" /> object containing the text
		///    stored in the current instance, or <see langword="null"
		///    /> if the item is empty of contains binary data.</para>
		///    <para>If the current instance contains multiple string
		///    values, they will be returned as a comma separated
		///    value.</para>
		/// </returns>
		public override string ToString ()
		{
			if (type == ItemType.Binary || text == null)
				return null;
			else
				return string.Join (", ", text);
		}
		
		/// <summary>
		///    Gets the contents of the current instance as a <see
		///    cref="string" /> array.
		/// </summary>
		/// <returns>
		///    A <see cref="T:string[]" /> containing the text stored in
		///    the current instance, or an empty array if the item
		///    contains binary data.
		/// </returns>
		public string [] ToStringArray ()
		{
			if (type == ItemType.Binary || text == null)
				return new string [0];
			
			return text;
		}
		
		/// <summary>
		///    Renders the current instance as an APEv2 item.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public ByteVector Render ()
		{
			uint flags = (uint) ((ReadOnly) ? 1 : 0) |
				((uint) Type << 1);
			
			if (IsEmpty)
				return new ByteVector ();
			
			ByteVector result = null;
			
			if (type == ItemType.Binary) {
				if (text == null && data != null)
					result = data;
			}
			
			if (result == null && text != null) {
				result = new ByteVector ();
				
				for (int i = 0; i < text.Length; i ++) {
					if (i != 0)
						result.Add ((byte) 0);
					
					result.Add (ByteVector.FromString (
						text [i], StringType.UTF8));
				}
			}
			
			// If no data is stored, don't write the item.
			if (result == null || result.Count == 0)
				return new ByteVector ();
			
			ByteVector output = new ByteVector ();
			output.Add (ByteVector.FromUInt ((uint) result.Count,
				false));
			output.Add (ByteVector.FromUInt (flags, false));
			output.Add (ByteVector.FromString (key, StringType.UTF8));
			output.Add ((byte) 0);
			output.Add (result);
			
			size_on_disk = output.Count;
			
			return output;
		}
		
		#endregion
		
		#region Protected Methods
		
		/// <summary>
		///    Populates the current instance by reading in a raw APEv2
		///    item.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the item to
		///    read.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the offset in
		///    <paramref name="data" /> at which the item data begins.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="offset" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    A complete item could not be read.
		/// </exception>
		protected void Parse (ByteVector data, int offset)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset");
			
			
			// 11 bytes is the minimum size for an APE item
			if(data.Count < offset + 11)
				throw new CorruptFileException (
					"Not enough data for APE Item");
			
			uint value_length = data.Mid (offset, 4).ToUInt (false);
			uint flags = data.Mid (offset + 4, 4).ToUInt (false);
			
			ReadOnly = (flags & 1) == 1;
			Type = (ItemType) ((flags >> 1) & 3);
			
			int pos = data.Find (ByteVector.TextDelimiter (
				StringType.UTF8), offset + 8);
			
			key = data.ToString (StringType.UTF8,
				offset + 8, pos - offset - 8);
			
			if (value_length > data.Count - pos - 1)
				throw new CorruptFileException (
					"Invalid data length.");
			
			size_on_disk = pos + 1 + (int) value_length - offset;
			
			if (Type == ItemType.Binary)
				this.data = new ReadOnlyByteVector (
					data.Mid (pos + 1, (int) value_length));
			else
				this.text = data.Mid (pos + 1,
					(int) value_length).ToStrings (
						StringType.UTF8, 0);
		}
		
#endregion
		
		
		
#region ICloneable
		
		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Item"/> object identical to the current
		///    instance.
		/// </returns>
		public Item Clone ()
		{
			return new Item (this);
		}
		
		object ICloneable.Clone ()
		{
			return Clone ();
		}
		
#endregion
	}
}
