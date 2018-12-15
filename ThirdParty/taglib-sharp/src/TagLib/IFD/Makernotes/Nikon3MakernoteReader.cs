//
// Nikon3MakernoteReader.cs: Reads Nikon Makernotes.
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
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

using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace TagLib.IFD.Makernotes
{
	/// <summary>
	///     This class contains Nikon3 makernote specific reading logic.
	/// </summary>
	public class Nikon3MakernoteReader : IFDReader {
#region Constructors

		/// <summary>
		///    Constructor. Reads an IFD from given file, using the given endianness.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File"/> to read from.
		/// </param>
		/// <param name="is_bigendian">
		///     A <see cref="System.Boolean"/>, it must be true, if the data of the IFD should be
		///     read as bigendian, otherwise false.
		/// </param>
		/// <param name="structure">
		///    A <see cref="IFDStructure"/> that will be populated.
		/// </param>
		/// <param name="base_offset">
		///     A <see cref="System.Int64"/> value describing the base were the IFD offsets
		///     refer to. E.g. in Jpegs the IFD are located in an Segment and the offsets
		///     inside the IFD refer from the beginning of this segment. So <paramref
		///     name="base_offset"/> must contain the beginning of the segment.
		/// </param>
		/// <param name="ifd_offset">
		///     A <see cref="System.UInt32"/> value with the beginning of the IFD relative to
		///     <paramref name="base_offset"/>.
		/// </param>
		/// <param name="max_offset">
		/// 	A <see cref="System.UInt32"/> value with maximal possible offset. This is to limit
		///     the size of the possible data;
		/// </param>
		public Nikon3MakernoteReader (File file, bool is_bigendian, IFDStructure structure, long base_offset, uint ifd_offset, uint max_offset) :
			base (file, is_bigendian, structure, base_offset, ifd_offset, max_offset)
		{
		}

#endregion

#region Protected Methods

		/// <summary>
		///    Try to parse the given IFD entry, used to discover format-specific entries.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag of the entry.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> with the type of the entry.
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the data count of the entry.
		/// </param>
		/// <param name="base_offset">
		///    A <see cref="System.Int64"/> with the base offset which every offsets in the
		///    IFD are relative to.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset of the entry.
		/// </param>
		/// <returns>
		///    A <see cref="IFDEntry"/> with the given parameters, or null if none was parsed, after
		///    which the normal TIFF parsing is used.
		/// </returns>
		protected override IFDEntry ParseIFDEntry (ushort tag, ushort type, uint count, long base_offset, uint offset)
		{
			if (tag == (ushort) Nikon3MakerNoteEntryTag.Preview) {
				// SubIFD with Preview Image
				// The entry itself is usually a long
				// TODO: handle JPEGInterchangeFormat and JPEGInterchangeFormatLength correctly

				// The preview field contains a long with an offset to an IFD
				// that contains the preview image. We need to be careful
				// though: this IFD does not contain a valid next-offset
				// pointer. For this reason, we only read the first IFD and
				// ignore the rest (which is preview image data, directly
				// starting after the IFD entries).

				type = (ushort) IFDEntryType.IFD;

				IFDStructure ifd_structure = new IFDStructure ();
				IFDReader reader = CreateSubIFDReader (file, is_bigendian, ifd_structure, base_offset, offset, max_offset);

				reader.Read (1);
				return new SubIFDEntry (tag, type, (uint) ifd_structure.Directories.Length, ifd_structure);
			}
			return base.ParseIFDEntry (tag, type, count, base_offset, offset);
		}

#endregion

	}
}
