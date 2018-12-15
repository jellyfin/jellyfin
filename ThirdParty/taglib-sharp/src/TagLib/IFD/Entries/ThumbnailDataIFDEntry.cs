//
// ThumbnailDataIFDEntry.cs:
//
// Author:
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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

namespace TagLib.IFD.Entries
{
	/// <summary>
	///    Contains the data of a Thumbnail. Since the thumbnail is
	///    referenced by two long entries (offset to the data and length)
	///    we need to take care of this special case.
	///    This entry acts as the offset-entry but holds also the
	///    thumbail data. When rendering the entry, we have to render the
	///    data but write a long entry.
	/// </summary>
	public class ThumbnailDataIFDEntry : IFDEntry
	{

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag { get; private set; }

		/// <value>
		///    The data of the thumbnail which is stored by the current instance
		/// </value>
		public ByteVector Data { get; private set; }

#endregion

#region Constructors

		/// <summary>
		///    Construcor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector"/> with the thumbnail data to be stored
		/// </param>
		public ThumbnailDataIFDEntry (ushort tag, ByteVector data)
		{
			Tag = tag;
			Data = data;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>
		/// </summary>
		/// <param name="is_bigendian">
		///    A <see cref="System.Boolean"/> indicating the endianess for rendering.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset, the data is stored.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> the ID of the type, which is rendered
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the values which are
		///    rendered.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered data.
		/// </returns>
		public ByteVector Render (bool is_bigendian, uint offset, out ushort type, out uint count)
		{
			// the entry is a single long entry where the value is an offset to the data
			// the offset is automatically updated by the renderer.
			type = (ushort) IFDEntryType.Long;
			count = 1;

			return Data;
		}

#endregion

	}
}
