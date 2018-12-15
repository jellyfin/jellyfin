//
// IsoUserDataBox.cs: Provides an implementation of a ISO/IEC 14496-12
// UserDataBox.
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
	///    implementation of a ISO/IEC 14496-12 UserDataBox.
	/// </summary>
	public class IsoUserDataBox : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the children of the box.
		/// </summary>
		private IEnumerable<Box> children;

		/// <summary>
		///    Contains the box headers from the top of the file to the
		///    current udta box.
		/// </summary>
		private BoxHeader [] parent_tree;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoUserDataBox" /> with a provided header and
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
		public IsoUserDataBox (BoxHeader header, TagLib.File file,
		                       IsoHandlerBox handler)
			: base (header, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			children = LoadChildren (file);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoUserDataBox" /> with no children.
		/// </summary>
		public IsoUserDataBox () : base ("udta")
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
		
		/// <summary>
		///    Gets the box headers for the current "<c>udta</c>" box and
		///    all parent boxes up to the top of the file.
		/// </summary>
		/// <value>
		///    A <see cref="T:BoxHeader[]" /> containing the headers for
		///    the current "<c>udta</c>" box and its parent boxes up to
		///    the top of the file, in the order they appear, or <see
		///    langword="null" /> if none is present.
		/// </value>
		public BoxHeader [] ParentTree {
			get {return parent_tree;}
			set {parent_tree = value;}
		}

		#endregion
	}
}
