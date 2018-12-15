//
// ExtendedContentDescriptionObject.cs: Provides a representation of an ASF
// Extended Content Description object which can be read from and written to
// disk.
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
	///    representation of an ASF Extended Content Description object
	///    which can be read from and written to disk.
	/// </summary>
	public class ExtendedContentDescriptionObject : Object,
		IEnumerable<ContentDescriptor>
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the content descriptors.
		/// </summary>
		private List<ContentDescriptor> descriptors =
			new List<ContentDescriptor> ();
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ExtendedContentDescriptionObject" /> by reading the
		///    contents from a specified position in a specified file.
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
		public ExtendedContentDescriptionObject (Asf.File file,
		                                         long position)
			: base (file, position)
		{
			if (!Guid.Equals (
				Asf.Guid.AsfExtendedContentDescriptionObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 26)
				throw new CorruptFileException (
					"Object size too small.");
			
			ushort count = file.ReadWord ();
			
			for (ushort i = 0; i < count; i ++)
				AddDescriptor (new ContentDescriptor (file));
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ExtendedContentDescriptionObject" /> with no
		///    contents.
		/// </summary>
		public ExtendedContentDescriptionObject ()
			: base (Asf.Guid.AsfExtendedContentDescriptionObject)
		{
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance doesn't
		///    contain any <see cref="ContentDescriptor" /> objects.
		///    Otherwise <see langword="false" />.
		/// </value>
		public bool IsEmpty {
			get {return descriptors.Count == 0;}
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
			
			foreach (ContentDescriptor desc in descriptors) {
				count ++;
				output.Add (desc.Render ());
			}
			
			return Render (RenderWord (count) + output);
		}
		
		/// <summary>
		///    Removes all descriptors with a given name from the
		///    current instance.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    descriptors to be removed.
		/// </param>
		public void RemoveDescriptors (string name)
		{
			for (int i = descriptors.Count - 1; i >= 0; i --)
				if (name == descriptors [i].Name)
					descriptors.RemoveAt (i);
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
			
			foreach (string name in names)
				foreach (ContentDescriptor desc in descriptors)
					if (desc.Name == name)
						yield return desc;
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
			
			descriptors.Add (descriptor);
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
			
			int position = this.descriptors.Count;
			for (int i = this.descriptors.Count - 1; i >= 0; i --) {
				if (name == this.descriptors [i].Name) {
					this.descriptors.RemoveAt (i);
					position = i;
				}
			}
			this.descriptors.InsertRange (position, descriptors);
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
			return descriptors.GetEnumerator ();
		}
		
		System.Collections.IEnumerator
			System.Collections.IEnumerable.GetEnumerator ()
		{
			return descriptors.GetEnumerator ();
		}
		
#endregion
	}
}
