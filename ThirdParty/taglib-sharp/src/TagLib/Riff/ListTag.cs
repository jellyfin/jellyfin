//
// ListTag.cs:
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

namespace TagLib.Riff
{
	/// <summary>
	///    This abstract class extends <see cref="Tag" /> to provide support
	///    for reading and writing tags stored in the RIFF list format.
	/// </summary>
	public abstract class ListTag : Tag
	{
#region Private Fields
		
		/// <summary>
		///    Contains the <see cref="List" /> object.
		/// </summary>
		List fields;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ListTag" /> with no contents.
		/// </summary>
		protected ListTag ()
		{
			fields = new List ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MovieIdTag" /> using a specified RIFF list.
		/// </summary>
		/// <param name="fields">
		///    A <see cref="List"/> object to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="fields" /> is <see langword="null" />.
		/// </exception>
		protected ListTag (List fields)
		{
			if (fields == null)
				throw new ArgumentNullException ("fields");
			
			this.fields = fields;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ListTag" /> by reading the contents of a raw
		///    RIFF list stored in a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw RIFF list to
		///    read into the new instance.
		/// </param>
		protected ListTag (ByteVector data)
		{
			fields = new List (data);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ListTag" /> by reading the contents of a raw RIFF
		///    list from a specified position in a <see
		///    cref="TagLib.File" />.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    from which the contents of the new instance is to be
		///    read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the list.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> value specifying the number of bytes
		///    to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		protected ListTag (TagLib.File file, long position, int length)
		{
			if (file == null)
				throw new System.ArgumentNullException ("file");
 		
			if (length < 0)
				throw new ArgumentOutOfRangeException (
					"length");
			
			if (position < 0 || position > file.Length - length)
				throw new ArgumentOutOfRangeException (
					"position");
			
			fields = new List (file, position, length);
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance enclosed in the appropriate
		///    item.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public abstract ByteVector RenderEnclosed ();
		
		/// <summary>
		///    Renders the current instance enclosed in an item with a
		///    specified ID.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector"/> object containing the ID of
		///    the item to enclose the current instance in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		protected ByteVector RenderEnclosed (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			return fields.RenderEnclosed (id);
		}
		
		/// <summary>
		///    Renders the current instance as a raw RIFF list.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public ByteVector Render ()
		{
			return fields.Render ();
		}
		
		/// <summary>
		///    Gets the values for a specified item in the current
		///    instance as a <see cref="ByteVectorCollection" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the values of the specified item.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public ByteVectorCollection GetValues (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			return fields.GetValues (id);
		}
		
		/// <summary>
		///    Gets the values for a specified item in the current
		///    instance as a <see cref="T:string[]" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]" /> containing the values of the
		///    specified item.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public string [] GetValuesAsStrings (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			return fields.GetValuesAsStrings (id);
		}
		
		/// <summary>
		///    Gets the values for a specified item in the current
		///    instance as a <see cref="StringCollection" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <returns>
		///    A <see cref="StringCollection" /> object containing the
		///    values of the specified item.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		[Obsolete("Use GetValuesAsStrings(ByteVector)")]
		public StringCollection GetValuesAsStringCollection (ByteVector id)
		{
			return new StringCollection (
				fields.GetValuesAsStrings (id));
		}
		
		/// <summary>
		///    Gets the value for a specified item in the current
		///    instance as a <see cref="uint"/>.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <returns>
		///    A <see cref="uint" /> value containing the first value
		///    with the specified ID that could be converted to an
		///    integer, or zero if none could be found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public uint GetValueAsUInt (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			return fields.GetValueAsUInt (id);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see cref="T:ByteVector[]"
		///    />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="value">
		///    A <see cref="T:ByteVector[]" /> containing the values to
		///    store in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, params ByteVector [] value)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			fields.SetValue (id, value);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see
		///    cref="ByteVectorCollection" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="value">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the values to store in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, ByteVectorCollection value)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			fields.SetValue (id, value);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the value of a <see cref="uint"/>.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="value">
		///    A <see cref="uint" /> value to store in the specified
		///    item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, uint value)
		{
			fields.SetValue (id, value);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see
		///    cref="StringCollection" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="value">
		///    A <see cref="StringCollection" /> object containing the
		///    values to store in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		[Obsolete("Use SetValue(ByteVector,string[])")]
		public void SetValue (ByteVector id, StringCollection value)
		{
			fields.SetValue (id, value);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see cref="T:string[]" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="value">
		///    A <see cref="T:string[]" /> containing the values to store
		///    in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, params string [] value)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			fields.SetValue (id, value);
		}
		
		/// <summary>
		///    Removes the item with the specified ID from the current
		///    instance.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector"/> object containing the ID of
		///    the item to remove from the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void RemoveValue (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			fields.RemoveValue (id);
		}
		
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance does not
		///    any values. Otherwise <see langword="false" />.
		/// </value>
		public override bool IsEmpty {
			get {return fields.Count == 0;}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			fields.Clear ();
		}
		
#endregion
	}
}