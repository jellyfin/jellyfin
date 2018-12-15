//
// SubIFDArrayEntry.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2010 Ruben Vermeersch
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

namespace TagLib.IFD.Entries
{
	/// <summary>
	///    Contains the SubIFDs IFD entry.
	/// </summary>
	public class SubIFDArrayEntry : IFDEntry
	{

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag {
			get;
			set;
		}

		/// <value>
		///    The structures of the IFDs to which this entry points.
		/// </value>
		public IFDStructure [] Entries { get; private set; }

#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="entries">
		///    A set of ifd structures.
		/// </param>
		public SubIFDArrayEntry (ushort tag, List<IFDStructure> entries)
		{
			Tag = tag;
			Entries = entries.ToArray ();
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
			throw new NotImplementedException ("Not implemented yet!");
		}

#endregion

	}
}
