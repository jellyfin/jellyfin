//
// IsoSampleDescriptionBox.cs: Provides an implementation of a ISO/IEC 14496-12
// SampleDescriptionBox.
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
	///    This class extends <see cref="FullBox" /> to provide an
	///    implementation of a ISO/IEC 14496-12 SampleDescriptionBox.
	/// </summary>
	public class IsoSampleDescriptionBox : FullBox
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the number of entries at the beginning of the
		///    children that will be of type <see cref="IsoSampleEntry"
		///    />, regardless of their box type.
		/// </summary>
		private uint entry_count;
		
		/// <summary>
		///    Contains the children of the box.
		/// </summary>
		private IEnumerable<Box> children;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoSampleDescriptionBox" /> with a provided header
		///    and handler by reading the contents from a specified
		///    file.
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
		public IsoSampleDescriptionBox (BoxHeader header,
		                                TagLib.File file,
		                                IsoHandlerBox handler)
			: base (header, file, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			entry_count = file.ReadBlock (4).ToUInt ();
			children = LoadChildren (file);
		}
		
		#endregion
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the position of the data contained in the current
		///    instance, after any box specific headers.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the position of
		///    the data contained in the current instance.
		/// </value>
		protected override long DataPosition {
			get {return base.DataPosition + 4;}
		}
		
		/// <summary>
		///    Gets the number of boxes at the begining of the children
		///    that will be stored as <see cref="IsoAudioSampleEntry" />
		///    of <see cref="IsoVisualSampleEntry" /> objects, depending
		///    on the handler.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    children that will appear as sample entries.
		/// </value>
		public uint EntryCount {
			get {return entry_count;}
		}
		
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
