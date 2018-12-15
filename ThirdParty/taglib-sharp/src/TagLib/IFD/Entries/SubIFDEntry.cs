//
// SubIFDEntry.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
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
	///    Contains a Sub IFD.
	/// </summary>
	public class SubIFDEntry : IFDEntry
	{

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag { get; private set; }

		/// <value>
		///    The type of the IFD entry.
		/// </value>
		public ushort Type { get; private set; }

		/// <value>
		///    The count of the IFD entry.
		/// </value>
		public uint Count { get; private set; }

		/// <value>
		///    The structure of the sub-ifd which is stored by the current
		///    instance
		/// </value>
		public IFDStructure Structure { get; private set; }

		/// <value>
		///    The number of entries in the entire IFD.
		/// </value>
		public int ChildCount {
			get {
				int sum = 0;
				foreach (var directory in Structure.Directories)
					sum += directory.Count;
				return sum;
			}
		}

		/// <summary>
		///    Construcor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> with the type of the IFD entry.
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the IFD entry.
		/// </param>
		/// <param name="structure">
		///    A <see cref="IFDStructure"/> to be stored
		/// </param>
		public SubIFDEntry (ushort tag, ushort type, uint count, IFDStructure structure)
		{
			Tag = tag;
			Type = type;
			Count = count;
			Structure = structure;
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
			type = (ushort) Type;
			count = 1;

			count = Count;
			return new IFDRenderer (is_bigendian, Structure, offset).Render ();
		}

#endregion

	}
}
