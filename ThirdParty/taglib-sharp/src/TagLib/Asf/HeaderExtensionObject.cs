//
// HeaderExtensionObject.cs: Provides a representation of an ASF Header
// Extension object which can be read from and written to disk.
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

using System.Collections.Generic;
using System;

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="Object" /> to provide a
	///    representation of an ASF Header Extension object which can be
	///    read from and written to disk.
	/// </summary>
	public class HeaderExtensionObject : Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the child objects.
		/// </summary>
		private List<Object> children = new List<Object> ();
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="HeaderExtensionObject" /> by reading the contents
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
		///    or contents.
		/// </exception>
		public HeaderExtensionObject (Asf.File file, long position)
			: base (file, position)
		{
			if (!Guid.Equals (Asf.Guid.AsfHeaderExtensionObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (file.ReadGuid () != Asf.Guid.AsfReserved1)
				throw new CorruptFileException (
					"Reserved1 GUID expected.");
			
			if (file.ReadWord () != 6)
				throw new CorruptFileException (
					"Invalid reserved WORD. Expected '6'.");
			
			uint size_remaining = file.ReadDWord ();
			position += 0x170 / 8;
			
			while (size_remaining > 0) {
				Object obj = file.ReadObject (position);
				position += (long) obj.OriginalSize;
				size_remaining -= (uint) obj.OriginalSize;
				children.Add (obj);
			}
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the child objects contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the children of the current instance.
		/// </value>
		public IEnumerable<Object> Children {
			get {return children;}
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
			
			foreach (Object child in children)
				output.Add (child.Render ());
			
			output.Insert (0, RenderDWord ((uint) output.Count));
			output.Insert (0, RenderWord (6));
			output.Insert (0, Asf.Guid.AsfReserved1.ToByteArray ());
			
			return Render (output);
		}
		
		/// <summary>
		///    Adds a child object to the current instance.
		/// </summary>
		/// <param name="obj">
		///    A <see cref="Object" /> object to add to the current
		///    instance.
		/// </param>
		public void AddObject (Object obj)
		{
			children.Add (obj);
		}
		
		/// <summary>
		///    Adds a child unique child object to the current instance,
		///    replacing and existing child if present.
		/// </summary>
		/// <param name="obj">
		///    A <see cref="Object" /> object to add to the current
		///    instance.
		/// </param>
		public void AddUniqueObject (Object obj)
		{
			for (int i = 0; i < children.Count; i ++)
				if (((Object) children [i]).Guid == obj.Guid) {
					children [i] = obj;
					return;
				}
			
			children.Add (obj);
		}
		
		#endregion
	}
}
