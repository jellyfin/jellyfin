//
// IFDRenderer.cs: Outputs an IFD structure into TIFF IFD bytes.
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

using System;
using System.Collections.Generic;
using TagLib.IFD.Entries;

namespace TagLib.IFD
{
	/// <summary>
	///     This class contains all the IFD rendering code.
	/// </summary>
	public class IFDRenderer {

#region Private Fields

		/// <summary>
		///    The IFD structure that will be rendered.
		/// </summary>
		private readonly IFDStructure structure;

		/// <summary>
		///    If IFD should be encoded in BigEndian or not.
		/// </summary>
		private readonly bool is_bigendian;

		/// <summary>
		///    A <see cref="System.UInt32"/> value with the offset of the
		///    current IFD. All offsets inside the IFD must be adjusted
		///    according to this given offset.
		/// </summary>
		private readonly uint ifd_offset;

#endregion

#region Constructors

		/// <summary>
		///    Constructor. Will render the given IFD structure.
		/// </summary>
		/// <param name="is_bigendian">
		///    If IFD should be encoded in BigEndian or not.
		/// </param>
		/// <param name="structure">
		///    The IFD structure that will be rendered.
		/// </param>
		/// <param name="ifd_offset">
		///    A <see cref="System.UInt32"/> value with the offset of the
		///    current IFD. All offsets inside the IFD must be adjusted
		///    according to this given offset.
		/// </param>
		public IFDRenderer (bool is_bigendian, IFDStructure structure, uint ifd_offset)
		{
			this.is_bigendian = is_bigendian;
			this.structure = structure;
			this.ifd_offset = ifd_offset;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> containing the rendered IFD.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector ifd_data = new ByteVector ();

			uint current_offset = ifd_offset;
			var directories = structure.directories;

			for (int index = 0; index < directories.Count; index++) {
				ByteVector data = RenderIFD (directories [index], current_offset, index == directories.Count - 1);
				current_offset += (uint) data.Count;
				ifd_data.Add (data);
			}

			return ifd_data;
		}

#endregion

#region Private Methods

		/// <summary>
		///    Renders the IFD to an ByteVector where the offset of the IFD
		///    itself is <paramref name="ifd_offset"/> and all offsets
		///    contained in the IFD are adjusted accroding it.
		/// </summary>
		/// <param name="directory">
		///    A <see cref="IFDDirectory"/> with the directory to render.
		/// </param>
		/// <param name="ifd_offset">
		///    A <see cref="System.UInt32"/> with the offset of the IFD
		/// </param>
		/// <param name="last">
		///    A <see cref="System.Boolean"/> which is true, if the IFD is
		///    the last one, i.e. the offset to the next IFD, which is
		///    stored inside the IFD, is 0. If the value is false, the
		///    offset to the next IFD is set that it starts directly after
		///    the current one.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered IFD.
		/// </returns>
		private ByteVector RenderIFD (IFDDirectory directory, uint ifd_offset, bool last)
		{
			if (directory.Count > (int)UInt16.MaxValue)
				throw new Exception (String.Format ("Directory has too much entries: {0}", directory.Count));

			// Remove empty SUB ifds.
			var tags = new List<ushort> (directory.Keys);
			foreach (var tag in tags) {
				var entry = directory [tag];
				if (entry is SubIFDEntry && (entry as SubIFDEntry).ChildCount == 0) {
					directory.Remove (tag);
				}
			}

			ushort entry_count = (ushort) directory.Count;

			// ifd_offset + size of entry_count + entries + next ifd offset
			uint data_offset = ifd_offset + 2 + 12 * (uint) entry_count + 4;

			// store the entries itself
			ByteVector entry_data = new ByteVector ();

			// store the data referenced by the entries
			ByteVector offset_data = new ByteVector ();

			entry_data.Add (ByteVector.FromUShort (entry_count, is_bigendian));

			foreach (IFDEntry entry in directory.Values)
				RenderEntryData (entry, entry_data, offset_data, data_offset);

			if (last)
				entry_data.Add ("\0\0\0\0");
			else
				entry_data.Add (ByteVector.FromUInt ((uint) (data_offset + offset_data.Count), is_bigendian));

			if (data_offset - ifd_offset != entry_data.Count)
				throw new Exception (String.Format ("Expected IFD data size was {0} but is {1}", data_offset - ifd_offset, entry_data.Count));

			entry_data.Add (offset_data);

			return entry_data;
		}

#endregion

#region Protected Methods

		/// <summary>
		///    Adds the data of a single entry to <paramref name="entry_data"/>.
		/// </summary>
		/// <param name="entry_data">
		///    A <see cref="ByteVector"/> to add the entry to.
		/// </param>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag of the entry.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> with the type of the entry.
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the data count of the entry,
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset field of the entry.
		/// </param>
		protected void RenderEntry (ByteVector entry_data, ushort tag, ushort type, uint count, uint offset)
		{
			entry_data.Add (ByteVector.FromUShort (tag, is_bigendian));
			entry_data.Add (ByteVector.FromUShort (type, is_bigendian));
			entry_data.Add (ByteVector.FromUInt (count, is_bigendian));
			entry_data.Add (ByteVector.FromUInt (offset, is_bigendian));
		}

		/// <summary>
		///    Renders a complete entry together with the data. The entry itself
		///    is stored in <paramref name="entry_data"/> and the data of the
		///    entry is stored in <paramref name="offset_data"/> if it cannot be
		///    stored in the offset. This method is called for every <see
		///    cref="IFDEntry"/> of this IFD and can be overwritten in subclasses
		///    to provide special behavior.
		/// </summary>
		/// <param name="entry">
		///    A <see cref="IFDEntry"/> with the entry to render.
		/// </param>
		/// <param name="entry_data">
		///    A <see cref="ByteVector"/> to add the entry to.
		/// </param>
		/// <param name="offset_data">
		///    A <see cref="ByteVector"/> to add the entry data to if it cannot be
		///    stored in the offset field.
		/// </param>
		/// <param name="data_offset">
		///    A <see cref="System.UInt32"/> with the offset, were the data of the
		///    entries starts. It is needed to adjust the offsets of the entries
		///    itself.
		/// </param>
		protected virtual void RenderEntryData (IFDEntry entry, ByteVector entry_data, ByteVector offset_data, uint data_offset)
		{
			ushort tag = (ushort) entry.Tag;
			uint offset = (uint) (data_offset + offset_data.Count);

			ushort type;
			uint count;
			ByteVector data = entry.Render (is_bigendian, offset, out type, out count);

			// store data in offset, if it is smaller than 4 byte
			if (data.Count <= 4) {

				while (data.Count < 4)
					data.Add ("\0");

				offset = data.ToUInt (is_bigendian);
				data = null;
			}

			// preserve word boundary of offsets
			if (data != null && data.Count % 2 != 0)
				data.Add ("\0");

			RenderEntry (entry_data, tag, type, count, offset);
			offset_data.Add (data);
		}

		/// <summary>
		///    Constructs a new IFD Renderer used to render a <see cref="SubIFDEntry"/>.
		/// </summary>
		/// <param name="is_bigendian">
		///    If IFD should be encoded in BigEndian or not.
		/// </param>
		/// <param name="structure">
		///    The IFD structure that will be rendered.
		/// </param>
		/// <param name="ifd_offset">
		///    A <see cref="System.UInt32"/> value with the offset of the
		///    current IFD. All offsets inside the IFD must be adjusted
		///    according to this given offset.
		/// </param>
		protected virtual IFDRenderer CreateSubRenderer (bool is_bigendian, IFDStructure structure, uint ifd_offset)
		{
			return new IFDRenderer (is_bigendian, structure, ifd_offset);
		}

#endregion

	}
}
