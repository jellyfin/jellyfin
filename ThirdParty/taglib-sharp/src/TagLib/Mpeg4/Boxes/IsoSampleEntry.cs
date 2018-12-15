//
// IsoSampleEntry.cs: Provides an implementation of a ISO/IEC 14496-12
// SampleEntry.
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
	///    implementation of a ISO/IEC 14496-12 SampleEntry.
	/// </summary>
	public class IsoSampleEntry : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the data reference index.
		/// </summary>
		private ushort data_reference_index;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoSampleEntry" /> with a provided header and
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
		public IsoSampleEntry (BoxHeader header, TagLib.File file,
		                       IsoHandlerBox handler)
			: base (header, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Seek (base.DataPosition + 6);
			data_reference_index = file.ReadBlock (2).ToUShort ();
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
			get {return base.DataPosition + 8;}
		}
		
		/// <summary>
		///    Gets the data reference index of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value containing the data
		///    reference index of the current instance.
		/// </value>
		public ushort DataReferenceIndex {
			get {return data_reference_index;}
		}
		
		#endregion
	}
}
