//
// AppleAdditionalInfoBox.cs: Provides an implementation of an Apple
// AdditionalInfoBox.
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
	///    implementation of an Apple AdditionalInfoBox.
	/// </summary>
	public class AppleAdditionalInfoBox : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the box data.
		/// </summary>
		private ByteVector data;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleAdditionalInfoBox" /> with a provided header
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
		public AppleAdditionalInfoBox (BoxHeader header, TagLib.File file, IsoHandlerBox handler) : base (header, handler)
		{
			// We do not care what is in this custom data section
			// see: https://developer.apple.com/library/mac/#documentation/QuickTime/QTFF/QTFFChap2/qtff2.html
			Data = LoadData (file);
		}
		
		/// <summary>
		/// Constructs and initializes a new instance of <see
		///    cref="AppleAdditionalInfoBox" /> using specified header, version and flags
		/// </summary>
		/// <param name="header">defines the header data</param>
		public AppleAdditionalInfoBox (ByteVector header) : base (header)
		{
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
			get {return data;}
			set {data = value != null ? value : new ByteVector ();}
		}
		
		/// <summary>
		///    Gets and sets the text contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the text
		///    contained in the current instance.
		/// </value>
		public string Text {
			get {return Data.ToString (StringType.Latin1).TrimStart ('\0');}
			set {
				Data = ByteVector.FromString (value,
					StringType.Latin1);
			}
		}
		
		#endregion
	}
}
