//
// MetadataLibraryObject.cs: Provides a representation of an ASF Metadata
// Library object which can be read from and written to disk.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
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

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="Object" /> to provide a
	///    representation of an ASF Metadata Library object which can be
	///    read from and written to disk.
	/// </summary>
	public class MetadataLibraryObject : Object,
		IEnumerable<DescriptionRecord>
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the description records.
		/// </summary>
		private List<DescriptionRecord> records =
			new List<DescriptionRecord> ();
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MetadataLibraryObject" /> by reading the contents
		///    from a specified position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="Asf.File" /> object containing the file from
		///    which the contents of the new instance are to be read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the object.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The object read from disk does not have the correct GUID
		///    or smaller than the minimum size.
		/// </exception>
		public MetadataLibraryObject (Asf.File file, long position)
			: base (file, position)
		{
			if (!Guid.Equals (Asf.Guid.AsfMetadataLibraryObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 26)
				throw new CorruptFileException (
					"Object size too small.");
			
			ushort count = file.ReadWord ();
			
			for (ushort i = 0; i < count; i ++) {
				DescriptionRecord rec = new DescriptionRecord (
					file);
				AddRecord (rec);
			}
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MetadataLibraryObject" /> with no contents.
		/// </summary>
		public MetadataLibraryObject ()
			: base (Asf.Guid.AsfMetadataLibraryObject)
		{
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance doesn't
		///    contain any <see cref="DescriptionRecord" /> objects.
		///    Otherwise <see langword="false" />.
		/// </value>
		public bool IsEmpty {
			get {return records.Count == 0;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw ASF object.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public override ByteVector Render ()
		{
			ByteVector output = new ByteVector ();
			ushort count = 0;
			
			foreach (DescriptionRecord rec in records) {
				count ++;
				output.Add (rec.Render ());
			}
			
			return Render (RenderWord (count) + output);
		}
		
		/// <summary>
		///    Removes all records with a given language, stream, and
		///    name from the current instance.
		/// </summary>
		/// <param name="languageListIndex">
		///    A <see cref="ushort" /> value containing the language
		///    list index of the records to be removed.
		/// </param>
		/// <param name="streamNumber">
		///    A <see cref="ushort" /> value containing the stream
		///    number of the records to be removed.
		/// </param>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    records to be removed.
		/// </param>
		public void RemoveRecords (ushort languageListIndex,
		                           ushort streamNumber,
		                           string name)
		{
			for (int i = records.Count - 1; i >= 0; i --) {
				DescriptionRecord rec = records [i];
				if (rec.LanguageListIndex == languageListIndex &&
					rec.StreamNumber == streamNumber &&
					rec.Name == name)
					records.RemoveAt (i);
			}
		}

		/// <summary>
		///    Gets all records with a given language, stream, and any
		///    of a collection of names from the current instance.
		/// </summary>
		/// <param name="languageListIndex">
		///    A <see cref="ushort" /> value containing the language
		///    list index of the records to be retrieved.
		/// </param>
		/// <param name="streamNumber">
		///    A <see cref="ushort" /> value containing the stream
		///    number of the records to be retrieved.
		/// </param>
		/// <param name="names">
		///    A <see cref="T:string[]" /> containing the names of the
		///    records to be retrieved.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the <see cref="DescriptionRecord" /> objects
		///    retrieved from the current instance.
		/// </returns>
		public IEnumerable<DescriptionRecord> GetRecords (ushort languageListIndex,
		                                                  ushort streamNumber,
		                                                  params string [] names)
		{
			foreach (DescriptionRecord rec in records) {
				if (rec.LanguageListIndex != languageListIndex ||
					rec.StreamNumber != streamNumber)
					continue;
				
				foreach (string name in names)
					if (rec.Name == name)
						yield return rec;
			}
		}
		
		/// <summary>
		///    Adds a record to the current instance.
		/// </summary>
		/// <param name="record">
		///    A <see cref="DescriptionRecord" /> object to add to the
		///    current instance.
		/// </param>
		public void AddRecord (DescriptionRecord record)
		{
			records.Add (record);
		}
		
		/// <summary>
		///    Sets the a collection of records for a given language,
		///    stream, and name, removing the existing matching records.
		/// </summary>
		/// <param name="languageListIndex">
		///    A <see cref="ushort" /> value containing the language
		///    list index of the records to be added.
		/// </param>
		/// <param name="streamNumber">
		///    A <see cref="ushort" /> value containing the stream
		///    number of the records to be added.
		/// </param>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    records to be added.
		/// </param>
		/// <param name="records">
		///    A <see cref="T:DescriptionRecord[]" /> containing records
		///    to add to the new instance.
		/// </param>
		/// <remarks>
		///    All added entries in <paramref name="records" /> should
		///    match <paramref name="languageListIndex" />, <paramref
		///    name="streamNumber" /> and <paramref name="name" /> but
		///    it is not verified by the method. The records will be
		///    added with their own values and not those provided in
		///    this method, which are used for removing existing values
		///    and determining where to position the new object.
		/// </remarks>
		public void SetRecords (ushort languageListIndex,
		                        ushort streamNumber, string name,
		                        params DescriptionRecord [] records)
		{
			int position = this.records.Count;
			for (int i = this.records.Count - 1; i >= 0; i --) {
				DescriptionRecord rec = this.records [i];
				if (rec.LanguageListIndex == languageListIndex &&
					rec.StreamNumber == streamNumber &&
					rec.Name == name) {
					this.records.RemoveAt (i);
					position = i;
				}
			}
			this.records.InsertRange (position, records);
		}
		
		#endregion
		
		
		
#region IEnumerable
		
		/// <summary>
		///    Gets an enumerator for enumerating through the
		///    description records.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the description records.
		/// </returns>
		public IEnumerator<DescriptionRecord> GetEnumerator ()
		{
			return records.GetEnumerator ();
		}
		
		System.Collections.IEnumerator
			System.Collections.IEnumerable.GetEnumerator ()
		{
			return records.GetEnumerator ();
		}
		
#endregion
	}
}
