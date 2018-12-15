//
// IsoFreeSpaceBox.cs: Provides an implementation of a ISO/IEC 14496-12
// FreeSpaceBox.
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
namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="Box" /> to provide an
	///    implementation of a ISO/IEC 14496-12 FreeSpaceBox.
	/// </summary>
	public class IsoFreeSpaceBox : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the size of the padding.
		/// </summary>
		private long padding;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoMetaBox" /> with a provided header and
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
		public IsoFreeSpaceBox (BoxHeader header, TagLib.File file,
		                        IsoHandlerBox handler)
			: base (header, handler)
		{
			padding = DataSize;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoFreeSpaceBox" /> to occupy a specified number of
		///    bytes.
		/// </summary>
		/// <param name="padding">
		///    A <see cref="long" /> value specifying the number of
		///    bytes the new instance should occupy when rendered.
		/// </param>
		public IsoFreeSpaceBox (long padding) : base ("free")
		{
			PaddingSize = padding;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the data contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the data
		///    contained in the current instance.
		/// </value>
		public override ByteVector Data {
			get {return new ByteVector ((int) padding);}
			set {padding = (value != null) ? value.Count : 0;}
		}
		
		/// <summary>
		///    Gets and sets the size the current instance will occupy
		///    when rendered.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the size the
		///    current instance will occupy when rendered.
		/// </value>
		public long PaddingSize {
			get {return padding + 8;}
			set {padding = value - 8;}
		}
		
		#endregion
	}
}
