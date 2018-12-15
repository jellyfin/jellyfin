//
// List.cs:
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Serialization;

namespace TagLib.Riff {
	/// <summary>
	///    This class extends <see
	///    cref="T:System.Collections.Generic.Dictionary`2" /> to provide
	///    support for reading and writing RIFF lists.
	/// </summary>
	[Serializable]
	[ComVisible(false)]
	public class List : Dictionary <ByteVector,ByteVectorCollection>
	{
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="List" /> with no contents.
		/// </summary>
		public List ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="List" /> by reading the contents of a raw RIFF
		///    list stored in a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw RIFF list to
		///    read into the new instance.
		/// </param>
		public List (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			Parse (data);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="List" /> by reading the contents of a raw RIFF list
		///    from a specified position in a <see cref="TagLib.File"/>.
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
		public List (TagLib.File file, long position, int length)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			if (length < 0)
				throw new ArgumentOutOfRangeException (
					"length");
			
			if (position < 0 || position > file.Length - length)
				throw new ArgumentOutOfRangeException (
					"position");
			
			file.Seek (position);
			Parse (file.ReadBlock (length));
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="List" /> from a specified serialization info and
		///    streaming context.
		/// </summary>
		/// <param name="info">
		///    A <see cref="SerializationInfo" /> object containing the
		///    serialized data to be used for the new instance.
		/// </param>
		/// <param name="context">
		///    A <see cref="StreamingContext" /> object containing the
		///    streaming context information for the new instance.
		/// </param>
		/// <remarks>
		///    This constructor is implemented because <see
		///    cref="List" /> implements the <see cref="ISerializable"
		///    /> interface.
		/// </remarks>
		protected List (SerializationInfo info,
		                StreamingContext context)
			: base (info, context)
		{
		}
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw RIFF list.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector data = new ByteVector ();
			
			foreach (ByteVector id in Keys)
				foreach (ByteVector value in this [id]) {
					if (value.Count == 0)
						continue;
					
					data.Add (id);
					data.Add (ByteVector.FromUInt (
						(uint) value.Count, false));
					data.Add (value);
					
					if (value.Count % 2 == 1)
						data.Add (0);
				}
			
			return data;
		}
		
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
		public ByteVector RenderEnclosed (ByteVector id)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			ByteVector data = Render ();
			
			if (data.Count <= 8)
				return new ByteVector ();
			
			ByteVector header = new ByteVector ("LIST");
			header.Add (ByteVector.FromUInt (
				(uint) (data.Count + 4), false));
			header.Add (id);
			data.Insert (0, header);
			return data;
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
			
			ByteVectorCollection value;
			
			return TryGetValue (id, out value) ?
				value : new ByteVectorCollection ();
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
			
			ByteVectorCollection values = GetValues (id);
			
			string [] result = new string [values.Count];
			
			for (int i = 0; i < result.Length; i ++) {
				ByteVector data = values [i];
				
				if (data == null) {
					result [i] = string.Empty;
					continue;
				}
				
				int length = data.Count;
				while (length > 0 && data [length - 1] == 0)
					length --;
				
				result [i] = data
					.ToString (StringType.UTF8, 0, length);
			}
			
			return result;
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
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			return new StringCollection (GetValuesAsStrings (id));
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
			
			foreach (string text in GetValuesAsStrings (id)) {
				uint value;
				if (uint.TryParse (text, out value))
					return value;
			}
			
			return 0;
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see
		///    cref="T:System.Collections.Generic.IEnumerable`1" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="values">
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1"
		///    /> containing the <see cref="ByteVector"/> objects to
		///    store in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id,
		                      IEnumerable<ByteVector> values)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			if (values == null)
				RemoveValue (id);
			else if (ContainsKey (id))
				this [id] = new ByteVectorCollection (values);
			else
				Add (id, new ByteVectorCollection (values));
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
		/// <param name="values">
		///    A <see cref="T:ByteVector[]" /> containing the values to
		///    store in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, params ByteVector [] values)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			if (values == null || values.Length == 0)
				RemoveValue (id);
			else
				SetValue (id, values as IEnumerable<ByteVector>);
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
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			if (value == 0)
				RemoveValue (id);
			else
				SetValue (id, value.ToString (
					CultureInfo.InvariantCulture));
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see
		///    cref="T:System.Collections.Generic.IEnumerable`1" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="values">
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1"
		///    /> containing the <see cref="string"/> objects to store
		///    in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, IEnumerable<string> values)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			if (values == null) {
				RemoveValue (id);
				return;
			}
			
			ByteVectorCollection l = new ByteVectorCollection ();
			foreach (string value in values) {
				if (string.IsNullOrEmpty (value))
					continue;
				
				ByteVector data = ByteVector.FromString (value,
					StringType.UTF8);
				data.Add (0);
				l.Add (data);
			}
			
			if (l.Count == 0)
				RemoveValue (id);
			else
				SetValue (id, l);
		}
		
		/// <summary>
		///    Sets the value for a specified item in the current
		///    instance to the contents of a <see cref="T:string[]" />.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the ID of
		///    the item to set.
		/// </param>
		/// <param name="values">
		///    A <see cref="T:string[]" /> containing the values to store
		///    in the specified item.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="id" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="id" /> isn't exactly four bytes long.
		/// </exception>
		public void SetValue (ByteVector id, params string [] values)
		{
			if (id == null)
				throw new ArgumentNullException ("id");
			
			if (id.Count != 4)
				throw new ArgumentException (
					"ID must be 4 bytes long.", "id");
			
			if (values == null || values.Length == 0)
				RemoveValue (id);
			else
				SetValue (id, values as IEnumerable<string>);
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
			
			if (ContainsKey (id))
				Remove (id);
		}
		
#endregion
		
		
		
#region Private Methods
		
		/// <summary>
		///    Populates the current instance by reading in the contents
		///    of a raw RIFF list stored in a <see cref="ByteVector" />
		///    object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw RIFF list to
		///    read into the current instance.
		/// </param>
		private void Parse (ByteVector data)
		{
			int offset = 0;
			while (offset + 8 < data.Count) {
				ByteVector id = data.Mid (offset, 4);
				int length = (int) data.Mid (offset + 4, 4)
					.ToUInt (false);
				
				if (!ContainsKey (id))
					Add (id, new ByteVectorCollection ());
				
				this [id].Add (data.Mid (offset + 8, length));
				
				if (length % 2 == 1)
					length ++;
				
				offset += 8 + length;
			}
		}
#endregion
	}
}
