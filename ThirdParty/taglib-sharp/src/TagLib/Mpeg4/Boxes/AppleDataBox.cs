//
// AppleDataBox.cs: Provides an implementation of an Apple DataBox.
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
	///    This class extends <see cref="FullBox" /> to provide an
	///    implementation of an Apple DataBox.
	/// </summary>
	public class AppleDataBox : FullBox
	{
		#region Enums
		
		/// <summary>
		///    Specifies the type of data contained in a box.
		/// </summary>
		public enum FlagType {
			/// <summary>
			///    The box contains UTF-8 text.
			/// </summary>
			ContainsText = 0x01,
			
			/// <summary>
			///    The box contains binary data.
			/// </summary>
			ContainsData = 0x00,
			
			/// <summary>
			///    The box contains data for a tempo box.
			/// </summary>
			ForTempo = 0x15,
			
			/// <summary>
			///    The box contains a raw JPEG image.
			/// </summary>
			ContainsJpegData = 0x0D,
			
			/// <summary>
			///    The box contains a raw PNG image.
			/// </summary>
			ContainsPngData = 0x0E,

			/// <summary>
			///    The box contains a raw BMP image.
			/// </summary>
			ContainsBmpData = 0x1B

		}

		#endregion



		#region Private Fields

		/// <summary>
		///    Contains the box data.
		/// </summary>
		private ByteVector data;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleDataBox" /> with a provided header and handler
		///    by reading the contents from a specified file.
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
		public AppleDataBox (BoxHeader header, TagLib.File file,
		                     IsoHandlerBox handler)
			: base (header, file, handler)
		{
			Data = LoadData (file);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleDataBox" /> with specified data and flags.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the data to
		///    store in the new instance.
		/// </param>
		/// <param name="flags">
		///    A <see cref="uint" /> value containing flags to use for
		///    the new instance.
		/// </param>
		public AppleDataBox (ByteVector data, uint flags)
			: base ("data", 0, flags)
		{
			Data = data;
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
		///    contained in the current instance, or <see
		///    langword="null" /> if the box is not flagged as
		///    containing text.
		/// </value>
		public string Text {
			get {
				return ((Flags & (int)
					FlagType.ContainsText) != 0) ?
					Data.ToString (StringType.UTF8) : null;
			}
			set {
				Flags = (int) FlagType.ContainsText;
				Data = ByteVector.FromString (value,
					StringType.UTF8);
			}
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Renders the current instance, including its children, to
		///    a new <see cref="ByteVector" /> object, preceeding the
		///    contents with a specified block of data.
		/// </summary>
		/// <param name="topData">
		///    A <see cref="ByteVector" /> object containing box
		///    specific header data to preceed the content.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		protected override ByteVector Render (ByteVector topData)
		{
			ByteVector output = new ByteVector (4);
			output.Add (topData);
			return base.Render (output);
		}
		
		#endregion
	}
}
