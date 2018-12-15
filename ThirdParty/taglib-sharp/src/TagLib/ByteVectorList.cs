//
// ByteVectorList.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Aaron Bockover (abockover@novell.com)
//
// Original Source:
//   tbytevectorlist.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TagLib {
	/// <summary>
	///    This class extends <see cref="T:TagLib.ListBase`1"/> to represent
	///    a collection of <see cref="ByteVector" /> objects.
	/// </summary>
	[ComVisible(false)]
	public class ByteVectorCollection : ListBase<ByteVector>
	{
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ByteVectorCollection" /> with no contents.
		/// </summary>
		public ByteVectorCollection ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ByteVectorCollection" /> with specified contents.
		/// </summary>
		/// <param name="list">
		///   A <see cref="T:System.Collections.Generic.IEnumerable`1"
		///   /> containing <see cref="ByteVector" /> objects to add to
		///   the current instance.
		/// </param>
		public ByteVectorCollection(IEnumerable<ByteVector> list)
		{
			if (list != null)
				Add (list);
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ByteVectorCollection" /> with specified contents.
		/// </summary>
		/// <param name="list">
		///   A <see cref="T:ByteVector[]" /> containing objects to add to
		///   the current instance.
		/// </param>
		public ByteVectorCollection (params ByteVector[] list)
		{
			if (list != null)
				Add (list);
		}
		
		/// <summary>
		///    Performs a sorted insert of a <see cref="ByteVector" />
		///    object into the current instance, optionally only adding
		///    if the item is unique.
		/// </summary>
		/// <param name="item">
		///    A <see cref="ByteVector" /> object to add to the current
		///    instance.
		/// </param>
		/// <param name="unique">
		///    If <see langword="true" />, the object will only be added
		///    if an identical value is not already contained in the
		///    current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="item" /> is <see langword="null" />.
		/// </exception>
		public override void SortedInsert (ByteVector item, bool unique)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			// FIXME: This is not used, but if it is a faster
			// method could be used.
			int i = 0;
			for(; i < Count; i++) {
				if (item == this[i] && unique)
					return;

				if (item >= this[i])
					break;
			}

			Insert (i + 1, item);
		}
		
		/// <summary>
		///    Converts the current instance to a <see cref="ByteVector"
		///    /> by joining the contents together with a specified
		///    separator.
		/// </summary>
		/// <param name="separator">
		///    A <see cref="ByteVector"/> object to separate the
		///    combined contents of the current instance.
		/// </param>
		/// <returns>
		///    A new <see cref="ByteVector"/> object containing the
		///    joined contents of the current instance.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="separator" /> is <see langword="null" />.
		/// </exception>
		public ByteVector ToByteVector (ByteVector separator)
		{
			if (separator == null)
				throw new ArgumentNullException ("separator");
			
			ByteVector vector = new ByteVector();
			
			for(int i = 0; i < Count; i++) {
				if(i != 0 && separator.Count > 0)
					vector.Add(separator);
				
				vector.Add(this[i]);
			}
			
			return vector;
		}
		
		/// <summary>
		///    Splits a <see cref="ByteVector" /> object using a
		///    pattern.
		/// </summary>
		/// <param name="vector">
		///    A <see cref="ByteVector"/> object to split.
		/// </param>
		/// <param name="pattern">
		///    A <see cref="ByteVector"/> object to use to split
		///    <paramref name="vector" /> with.
		/// </param>
		/// <param name="byteAlign">
		///    A <see cref="int" /> specifying the byte align to use
		///    when splitting. In order to split when a pattern is
		///    encountered, the index at which it is found must be
		///    divisible by <paramref name="byteAlign" />.
		/// </param>
		/// <param name="max">
		///    A <see cref="int" /> value specifying the maximum number
		///    of objects to return, or zero to not to limit the number.
		///    If that that number is reached, the last value will
		///    contain the remainder of the file even if it contains
		///    more instances of <paramref name="pattern" />.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the split contents of the current instance.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="vector" /> or <paramref name="pattern" />
		///    is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="byteAlign" /> is less than 1.
		/// </exception>
		public static ByteVectorCollection Split (ByteVector vector,
		                                          ByteVector pattern,
		                                          int byteAlign,
		                                          int max)
		{
			if (vector == null)
				throw new ArgumentNullException ("vector");
			
			if (pattern == null)
				throw new ArgumentNullException ("pattern");
			
			if (byteAlign < 1)
				throw new ArgumentOutOfRangeException (
					"byteAlign",
					"byteAlign must be at least 1.");
			
			ByteVectorCollection list = new ByteVectorCollection ();
			int previous_offset = 0;
			
			for (int offset = vector.Find(pattern, 0, byteAlign);
				offset != -1 && (max < 1 ||
					max > list.Count + 1);
				offset = vector.Find (pattern,
					offset + pattern.Count, byteAlign)) {
				list.Add (vector.Mid (previous_offset,
					offset - previous_offset));
				previous_offset = offset + pattern.Count;
			}
			
			if (previous_offset < vector.Count)
				list.Add (vector.Mid (previous_offset,
					vector.Count - previous_offset));
			
			return list;
		}
		
		/// <summary>
		///    Splits a <see cref="ByteVector" /> object using a
		///    pattern.
		/// </summary>
		/// <param name="vector">
		///    A <see cref="ByteVector"/> object to split.
		/// </param>
		/// <param name="pattern">
		///    A <see cref="ByteVector"/> object to use to split
		///    <paramref name="vector" /> with.
		/// </param>
		/// <param name="byteAlign">
		///    A <see cref="int" /> specifying the byte align to use
		///    when splitting. In order to split when a pattern is
		///    encountered, the index at which it is found must be
		///     divisible by <paramref name="byteAlign" />.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the split contents of the current instance.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="vector" /> or <paramref name="pattern" />
		///    is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="byteAlign" /> is less than 1.
		/// </exception>
		public static ByteVectorCollection Split (ByteVector vector,
		                                          ByteVector pattern,
		                                          int byteAlign)
		{
			return Split(vector, pattern, byteAlign, 0);
		}
		
		/// <summary>
		///    Splits a <see cref="ByteVector" /> object using a
		///    pattern.
		/// </summary>
		/// <param name="vector">
		///    A <see cref="ByteVector"/> object to split.
		/// </param>
		/// <param name="pattern">
		///    A <see cref="ByteVector"/> object to use to split
		///    <paramref name="vector" /> with.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the split contents of the current instance.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="vector" /> or <paramref name="pattern" />
		///    is <see langword="null" />.
		/// </exception>
		public static ByteVectorCollection Split (ByteVector vector,
		                                          ByteVector pattern)
		{
			return Split(vector, pattern, 1);
		}
	}
}

