//
// PaddingObject.cs: Provides a representation of an ASF Padding object which
// can be read from and written to disk.
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

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="Object" /> to provide a
	///    representation of an ASF Padding object which can be read from
	///    and written to disk.
	/// </summary>
	public class PaddingObject : Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the size of the current instance.
		/// </summary>
		private ulong size;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PaddingObject" /> by reading the contents from a
		///    specified position in a specified file.
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
		public PaddingObject (Asf.File file, long position)
			: base (file, position)
		{
			if (!Guid.Equals (Asf.Guid.AsfPaddingObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 24)
				throw new CorruptFileException (
					"Object size too small.");
			
			size = OriginalSize;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PaddingObject" /> of a specified size.
		/// </summary>
		/// <param name="size">
		///    A <see cref="uint" /> value specifying the number of
		///    bytes the new instance is to take up on disk.
		/// </param>
		public PaddingObject (uint size)
			: base (Asf.Guid.AsfPaddingObject)
		{
			this.size = size;
		}
		
		#endregion
		
		
		
		#region Prublic Properties
		
		/// <summary>
		///    Gets and sets the number of bytes the current instance
		///    will take up on disk.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> value containing the size of the
		///    current instance on disk.
		/// </value>
		public ulong Size {
			get {return size;}
			set {size = value;}
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
			return Render (new ByteVector ((int) (size - 24)));
		}
		
		#endregion
	}
}
