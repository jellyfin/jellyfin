//
// UnknownFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   unknownframe.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002 Scott Wheeler (Original Implementation)
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

using System.Collections;
using System;

namespace TagLib.Id3v2
{
	/// <summary>
	///    This class extends <see cref="Frame" /> to provide a fallback
	///    type when no other frame class works for a given frame.
	/// </summary>
	public class UnknownFrame : Frame
	{
#region Private Properties
		
		/// <summary>
		///    Contains the field data.
		/// </summary>
		private ByteVector field_data = null;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownFrame" /> with a specified type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing an ID3v2.4
		///    frame identifier.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector"/> object containing the contents
		///    of the frame.
		/// </param>
		public UnknownFrame (ByteVector type, ByteVector data)
			: base (type, 4)
		{
			field_data = data;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownFrame" /> with a specified type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing an ID3v2.4
		///    frame identifier.
		/// </param>
		public UnknownFrame (ByteVector type) : this (type, null)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownFrame" /> by reading its raw data in a
		///    specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public UnknownFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownFrame" /> by reading its raw data in a
		///    specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		protected internal UnknownFrame (ByteVector data, int offset,
		                                 FrameHeader header,
		                                 byte version)
			: base(header)
		{
			SetData (data, offset, version, false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets and sets the field data in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> containing the field data.
		/// </value>
		public ByteVector Data {
			get {return field_data;}
			set {field_data = value;}
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Gets a string representation of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> object describing the current
		///    instance.
		/// </returns>
		public override string ToString ()
		{
			return base.ToString ();
		}
		
#endregion
				
		
		
#region Protected Methods
		
		/// <summary>
		///    Populates the values in the current instance by parsing
		///    its field data in a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is encoded in.
		/// </param>
		protected override void ParseFields (ByteVector data,
		                                     byte version)
		{
			field_data = data;
		}
		
		/// <summary>
		///    Renders the values in the current instance into field
		///    data for a specified version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is to be encoded in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered field data.
		/// </returns>
		protected override ByteVector RenderFields (byte version)
		{
			return field_data ?? new ByteVector ();
		}
		
#endregion
	}
}
