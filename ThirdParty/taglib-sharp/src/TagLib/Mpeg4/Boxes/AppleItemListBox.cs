//
// AppleItemListBox.cs: Provides an implementation of an Apple ItemListBox.
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

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="Box" /> to provide an
	///    implementation of an Apple ItemListBox.
	/// </summary>
	public class AppleItemListBox : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the children of the box.
		/// </summary>
		private IEnumerable<Box> children;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleItemListBox" /> with a provided header and
		///    handler by reading the contents from a specified file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		public AppleItemListBox (BoxHeader header, TagLib.File file,
		                         IsoHandlerBox handler)
			: base (header, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			children = LoadChildren (file);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleItemListBox" /> with no children.
		/// </summary>
		public AppleItemListBox () : base ("ilst")
		{
			children = new List<Box> ();
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the children of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    children of the current instance.
		/// </value>
		public override IEnumerable<Box> Children {
			get {return children;}
		}
		
		#endregion
	}
}
