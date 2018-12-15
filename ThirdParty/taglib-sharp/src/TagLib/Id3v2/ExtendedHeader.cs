//
// ExtendedHeader.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v2extendedheader.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002,2003 Scott Wheeler (Original Implementation)
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
	///    This class is a filler until support for reading and writing the
	///    ID3v2 extended header is implemented.
	/// </summary>
	public class ExtendedHeader : ICloneable
	{
		/// <summary>
		///    Contains the size of the read header.
		/// </summary>
		private uint size;
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ExtendedHeader"/> with no contents.
		/// </summary>
		public ExtendedHeader ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ExtendedHeader" /> by reading the raw contents from
		///    a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    extended header structure.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value indicating the ID3v2 version.
		/// </param>
		public ExtendedHeader (ByteVector data, byte version)
		{
			Parse (data, version);
		}
		
		/// <summary>
		///    Gets the size of the data on disk in bytes.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    data on disk.
		/// </value>
		public uint Size {
			get {return size;}
		}
		
		/// <summary>
		///    Populates the current instance with the contents of the
		///    raw ID3v2 frame.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    extended header structure.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value indicating the ID3v2 version.
		/// </param>
		protected void Parse (ByteVector data, byte version)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			size = (version == 3 ? 4u : 0u) + SynchData.ToUInt (data.Mid (0, 4));
		}
		
#region ICloneable
		
		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="ExtendedHeader" /> object identical to
		///    the current instance.
		/// </returns>
		public ExtendedHeader Clone ()
		{
			ExtendedHeader header = new ExtendedHeader ();
			header.size = size;
			return header;
		}
		
		object ICloneable.Clone ()
		{
			return Clone ();
		}
		
#endregion
	}
}
