//
// StringList.cs: This class extends ListBase<string> for a collection
// of string objects.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Aaron Bockover (abockover@novell.com)
//
// Copyright (C) 2006 Novell, Inc.
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
using System.Runtime.InteropServices;

namespace TagLib {
	/// <summary>
	///    This class extends <see cref="T:TagLib.ListBase`1" /> for a collection of
	///    <see cref="string" /> objects.
	/// </summary>
	[ComVisible(false)]
	public class StringCollection : ListBase<string>
	{
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StringCollection" /> with no contents.
		/// </summary>
		public StringCollection ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StringCollection" /> with the contents of another
		///    instance.
		/// </summary>
		/// <param name="values">
		///    A <see cref="StringCollection" /> object whose values are
		///    to be added to the new instance.
		/// </param>
		public StringCollection (StringCollection values)
		{
			Add (values);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StringCollection" /> with the contents of a
		///    specified array.
		/// </summary>
		/// <param name="values">
		///    A <see cref="T:string[]" /> whose values are to be added to
		///    the new instance.
		/// </param>
		public StringCollection (params string [] values)
		{
			Add (values);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StringCollection" /> by converting a collection of
		///    <see cref="ByteVector" /> objects to strings with a
		///    specified encoding.
		/// </summary>
		/// <param name="vectorList">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    values to convert and add to the new instance.
		/// </param>
		/// <param name="type">
		///    A <see cref="StringType" /> specifying what encoding to
		///    use when converting the data to strings.
		/// </param>
		public StringCollection (ByteVectorCollection vectorList,
		                         StringType type)
		{
			foreach (ByteVector vector in vectorList)
				Add (vector.ToString (type));
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StringCollection" /> by converting a collection of
		///    <see cref="ByteVector" /> objects to strings using the
		///    UTF-8 encoding.
		/// </summary>
		/// <param name="vectorList">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    values to convert and add to the new instance.
		/// </param>
		public StringCollection(ByteVectorCollection vectorList)
			: this (vectorList, StringType.UTF8)
		{
		}
		
		/// <summary>
		///    Splits a single <see cref="string" /> into a <see
		///    cref="StringCollection" /> using a pattern.
		/// </summary>
		/// <param name="value">
		///    A <see cref="string" /> object to split.
		/// </param>
		/// <param name="pattern">
		///    A <see cref="string" /> object containing a pattern to
		///    use to split <paramref name="value" />.
		/// </param>
		/// <returns>
		///    A <see cref="StringCollection" /> object containing the
		///    split values.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="value" /> or <paramref name="pattern" />
		///    is <see langword="null" />.
		/// </exception>
		public static StringCollection Split (string value,
		                                      string pattern)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			
			if (pattern == null)
				throw new ArgumentNullException ("pattern");
			
			StringCollection list = new StringCollection ();
			
			int previous_position = 0;
			int position = value.IndexOf (pattern, 0);
			int pattern_length = pattern.Length;
			
			while (position != -1) {
				list.Add (value.Substring (previous_position,
					position - previous_position));
				previous_position = position + pattern_length;
				position = value.IndexOf (pattern,
					previous_position);
			}
			
			list.Add (value.Substring (previous_position));
			
			return list;
		}
	}
}