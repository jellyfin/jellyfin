//
// ListBase.cs:
//
// Author:
//   Aaron Bockover (abockover@novell.com)
//
// Original Source:
//   tbytevectorlist.cpp from TagLib
//
// Copyright (C) 2006 Novell, Inc.
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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace TagLib {
	/// <summary>
	///    This class implements <see cref="T:System.Collections.Generic`1"/>
	///    for objects that implement <see cref="T:System.IComparable`1"/>,
	///    providing extra features used in lists in TagLib#.
	/// </summary>
	public class ListBase<T> : IList<T> where T : IComparable<T>
	{
		/// <summary>
		///    Contains the internal list.
		/// </summary>
		private List<T> data = new List<T> ();

		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="T:TagLib.ListBase`1" /> with no contents.
		/// </summary>
		public ListBase ()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="T:TagLib.ListBase`1" /> with specified contents.
		/// </summary>
		/// <param name="list">
		///   A <see cref="T:System.Collections.Generic.IEnumerable`1"
		///   /> containing objects to add to the current instance.
		/// </param>
		public ListBase(ListBase<T> list)
		{
			if (list != null)
				Add (list);
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="T:TagLib.ListBase`1" /> with specified contents.
		/// </summary>
		/// <param name="list">
		///   A <see cref="System.Array" /> containing objects to add to
		///   the current instance.
		/// </param>
		public ListBase (params T [] list)
		{
			if (list != null)
				Add (list);
		}

		#endregion

		#region Properties
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance is empty;
		///    otherwise <see langword="false" />.
		/// </value>
		public bool IsEmpty {
			get {return Count == 0;}
		}

		#endregion

		#region Methods

		/// <summary>
		///    Adds a collection of elements to the current instance.
		/// </summary>
		/// <param name="list">
		///    A <see cref="T:TagLib.ListBase`1"/> object containing
		///    elements to add to the current instance.
		/// </param>
		public void Add(ListBase<T> list)
		{
			if(list != null) {
				data.AddRange(list);
			}
		}

		/// <summary>
		///    Adds a collection of elements to the current instance.
		/// </summary>
		/// <param name="list">
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1"/> object containing
		///    elements to add to the current instance.
		/// </param>
		public void Add(IEnumerable<T> list)
		{
			if(list != null) {
				data.AddRange(list);
			}
		}

		/// <summary>
		///    Adds a collection of elements to the current instance.
		/// </summary>
		/// <param name="list">
		///    An array containing elements to add to the current
		///    instance.
		/// </param>
		public void Add(T [] list)
		{
			if(list != null) {
				data.AddRange(list);
			}
		}

		/// <summary>
		///    Performs a sorted insert of an object into the current
		///    instance, optionally only adding if the item is unique.
		/// </summary>
		/// <param name="item">
		///    An object to add to the current instance.
		/// </param>
		/// <param name="unique">
		///    If <see langword="true" />, the object will only be added
		///    if an identical value is not already contained in the
		///    current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="item" /> is <see langword="null" />.
		/// </exception>
		public virtual void SortedInsert (T item, bool unique)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			int i = 0;
			for(; i < data.Count; i++) {
				if(item.CompareTo(data[i]) == 0 && unique) {
					return;
				}

				if(item.CompareTo(data[i]) <= 0) {
					break;
				}
			}

			Insert(i, item);
		}

		/// <summary>
		///    Performs a sorted insert of an object into the current
		///    instance.
		/// </summary>
		/// <param name="item">
		///    An object to add to the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="item" /> is <see langword="null" />.
		/// </exception>
		public void SortedInsert (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			SortedInsert(item, false);
		}
		
		/// <summary>
		///    Converts the current instance to an array.
		/// </summary>
		/// <returns>
		///    A <see cref="System.Array" /> containing the contents of
		///    the current instance.
		/// </returns>
		public T [] ToArray ()
		{
			return data.ToArray();
		}

		#endregion

#region IList<T>
		
		/// <summary>
		///    Gets whether or not the current instance is read-only.
		/// </summary>
		/// <value>
		///    Always <see langword="false" />.
		/// </value>
		public bool IsReadOnly {
			get { return false; }
		}
		
		/// <summary>
		///    Gets whether or not the current instance has a fixed
		///    size.
		/// </summary>
		/// <value>
		///    Always <see langword="false" />.
		/// </value>
		public bool IsFixedSize {
			get { return false; }
		}
		
		/// <summary>
		///    Gets and sets the value as a specified index.
		/// </summary>
		public T this [int index] {
			get { return data[index]; }
			set { data[index] = value; }
		}
		
		/// <summary>
		///    Adds a single item to end of the current instance.
		/// </summary>
		/// <param name="item">
		///    An object to add to the end of the current instance.
		/// </param>
		public void Add (T item)
		{
			data.Add (item);
		}
		
		/// <summary>
		///    Clears the contents of the current instance.
		/// </summary>
		public void Clear ()
		{
			data.Clear ();
		}
		
		/// <summary>
		///    Gets whether or not the current instance contains a
		///    specified object.
		/// </summary>
		/// <param name="item">
		///    An object to look for in the current instance.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if the item could be found;
		///    otherwise <see langword="false" />.
		/// </returns>
		public bool Contains (T item)
		{
			return data.Contains (item);
		}
		
		/// <summary>
		///    Gets the index of the first occurance of a value.
		/// </summary>
		/// <param name="item">
		///    A object to find in the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> value containing the first index
		///    at which the value was found, or -1 if it was not found.
		/// </returns>
		public int IndexOf (T item)
		{
			return data.IndexOf (item);
		}
		
		/// <summary>
		///    Inserts a single value into the current instance at a
		///    specified index.
		/// </summary>
		/// <param name="index">
		///    A <see cref="int" /> value specifying the position at
		///    which to insert the value.
		/// </param>
		/// <param name="item">
		///    An object to insert into the current instance.
		/// </param>
		public void Insert (int index, T item)
		{
			data.Insert (index, item);
		}
		
		/// <summary>
		///    Removes the first occurance of an object from the current
		///    instance.
		/// </summary>
		/// <param name="item">
		///    An object to remove from the current instance.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if the value was removed;
		///    otherwise the value did not appear in the current
		///    instance and <see langword="false" /> is returned.
		/// </returns>
		public bool Remove (T item)
		{
			return data.Remove (item);
		}
		
		/// <summary>
		///    Removes the item at the specified index.
		/// </summary>
		/// <param name="index">
		///    A <see cref="int" /> value specifying the position at
		///    which to remove an item.
		/// </param>
		public void RemoveAt (int index)
		{
			data.RemoveAt (index);
		}
		
		/// <summary>
		///    Gets a string representation of the contents of the
		///    current instance, joined by a separator.
		/// </summary>
		/// <param name="separator">
		///    A <see cref="string" /> object to separate the items
		///    with.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the contents
		///    of the current instance.
		/// </returns>
		public string ToString (string separator)
		{
			StringBuilder builder = new StringBuilder();

			for(int i = 0; i < Count; i++) {
				if(i != 0) {
					builder.Append(separator);
				}

				builder.Append(this[i].ToString());
			}

			return builder.ToString ();
		}

		/// <summary>
		///    Gets a string representation of the contents of the
		///    current instance, joined by commas.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> object containing the contents
		///    of the current instance.
		/// </returns>
		public override string ToString ()
		{
			return ToString(", ");
		}

#endregion
		
		
		
#region ICollection<T>
		
		/// <summary>
		///    Gets the number of elements in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    elements in the current instance.
		/// </value>
		public int Count {
			get {return data.Count;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is synchronized.
		/// </summary>
		/// <value>
		///    Always <see langword="false" />.
		/// </value>
		public bool IsSynchronized { 
			get {return false;}
		}
		
		/// <summary>
		///    Gets the object that can be used to synchronize the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="object" /> that can be used to synchronize
		///    the current instance.
		/// </value>
		public object SyncRoot { 
			get {return this;}
		}
		
		/// <summary>
		///    Copies the current instance to an array, starting at a
		///    specified index.
		/// </summary>
		/// <param name="array">
		///    An array to copy to.
		/// </param>
		/// <param name="arrayIndex">
		///    A <see cref="int" /> value indicating the index in
		///    <paramref name="array" /> at which to start copying.
		/// </param>
		public void CopyTo (T [] array, int arrayIndex)
		{
			data.CopyTo (array, arrayIndex);
		}
		
#endregion
		
		
		
		
#region IEnumerable<T>
		
		/// <summary>
		///    Gets an enumerator for enumerating through the elements
		///    in the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the tag's data boxes.
		/// </returns>
		public IEnumerator<T> GetEnumerator()
		{
			return data.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return data.GetEnumerator();
		}

#endregion
	}
}