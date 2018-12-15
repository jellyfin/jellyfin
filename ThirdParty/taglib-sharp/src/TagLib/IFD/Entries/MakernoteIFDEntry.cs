//
// MakernoteIFDEntry.cs:
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
	///    An enum to represent the manufactor of the makernote
	///    The information of the makernote types is from:
	///    http://exiv2.org/makernote.html
	/// </summary>
	public enum MakernoteType {

		/// <summary>
		///    The manufactor could not be determined
		/// </summary>
		Unknown,

		/// <summary>
		///    Canon makernote.
		///    Standard IFD without a special prefix.
		/// </summary>
		Canon,

		/// <summary>
		///    Panasonic makernote.
		///    "Panasonic\0\0\0" prefix and IFD starting at offset 12.
		///    The next-IFD pointer is missing
		/// </summary>
		Panasonic,

		/// <summary>
		///    Leica makernote.
		///    "LEICA\0\0\0" prefix and IFD starting at offset 10.
		/// </summary>
		Leica,

		/// <summary>
		///    Pentax makernote.
		///    "AOC\0" + 2 unknown bytes as prefix. The IFD starts at
		///    offset 6.
		/// </summary>
		Pentax,

		/// <summary>
		///    Nikon makernote (type 1).
		///    Standard IFD without a special prefix.
		/// </summary>
		Nikon1,

		/// <summary>
		///    Nikon makernote (type 2).
		///    "Nikon\0" + 2 unknown bytes prefix. The IFD starts at
		///    offset 8.
		/// </summary>
		Nikon2,

		/// <summary>
		///    Nikon makernote (type 3).
		///    "Nikon\0" + 4 bytes with verison code + Tiff header.
		///    The IFD starts usually at offset 18. The offsets of the IFD
		///    are relative to start of the Tiff header (byte 10)
		/// </summary>
		Nikon3,

		/// <summary>
		///    Olympus makernote (type 1).
		///    "OLYMP\0" + 2 unknown bytes as prefix. The IFD starts at
		///    offset 8.
		/// </summary>
		Olympus1,

		/// <summary>
		///    Olympus makernote (type 2)
		///    "OLYMPUS\0II" + 2 unknown bytes as prefix. The IFD starts at
		///    offset 12. The offsets of the IFD are relative to the
		///    beginning of the makernote.
		/// </summary>
		Olympus2,

		/// <summary>
		///    Sony makernote (type 1).
		///    "SONY DSC \0\0\0" as prefix. The IFD starts at offset 12. A
		///    next-IFD pointer is missing.
		/// </summary>
		Sony
	}


	/// <summary>
	///    Contains a Makernote IFD.
	/// </summary>
	/// <remarks>
	///    Makernote IFDs are mostly of the same form. They start with and
	///    Manufactor specific prefix indicating the type and contain then
	///    a IFD structure.
	///    It must be distinguished, where the offsets in the IFD belongs to.
	///    For some makernotes the offset refers to the beginning of the
	///    surrounding metadata IFD structure, for others they refer to the
	///    start of the makernote.
	///    In addition the endianess of the makernote can be different to the
	///    endianess of the surrounding metadata.
	///    This class takes care about all those things.
	/// </remarks>
	public class MakernoteIFDEntry : IFDEntry
	{

#region Private Fields

		/// <value>
		///    Stores the prefix of the makernote
		/// </value>
		private ByteVector prefix;

		/// <value>
		///    Stores the offset of the IFD contained in makernote
		/// </value>
		private uint ifd_offset;

		/// <value>
		///    Indicates, if the offsets are relative to the current makernote
		///    or absolut to the base_offset of the surrounding IFD.
		/// </value>
		private bool absolute_offset;

		/// <value>
		///    Stores, if the makernote is encoded in big- or little endian.
		///    If the field is <see langword="null"/>, the endianess of the
		///    surrounding IFD is used.
		/// </value>
		private bool? is_bigendian;

#endregion

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag { get; private set; }

		/// <value>
		///    The type of the makernote the current instance represents
		/// </value>
		public MakernoteType MakernoteType { get; private set; }

		/// <value>
		///    The pure <see cref="IFDStructure"/> which is stored by the
		///    makernote.
		/// </value>
		public IFDStructure Structure { get; private set; }

#endregion

#region Constructors

		/// <summary>
		///    Construcor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="structure">
		///    A <see cref="IFDStructure"/> with the IFD structure, which is stored by this
		///    instance
		/// </param>
		/// <param name="makernote_type">
		///    A <see cref="MakernoteType"/> with the type of the makernote.
		/// </param>
		/// <param name="prefix">
		///    A <see cref="ByteVector"/> containing the prefix, which should be rendered
		///    before the real IFD.
		/// </param>
		/// <param name="ifd_offset">
		///    A <see cref="System.UInt32"/> with the offset in addition to the relative
		///    offsets in the IFD
		/// </param>
		/// <param name="absolute_offset">
		///    A <see cref="System.Boolean"/> indicating if the offsets of the IFD are relative
		///    to the <paramref name="ifd_offset"/>, or absolut to the base offset of the
		///    surrounding IFD.
		/// </param>
		/// <param name="is_bigendian">
		///    A <see cref="System.Nullable"/> indicating if the current IFD is encoded in
		///    big- or little endian. It it is <see langword="null"/>, the endianess of the
		///    surrounding IFD is used.
		/// </param>
		public MakernoteIFDEntry (ushort tag, IFDStructure structure, MakernoteType makernote_type, ByteVector prefix, uint ifd_offset, bool absolute_offset, bool? is_bigendian)
		{
			Tag = tag;
			Structure = structure;
			MakernoteType = makernote_type;
			this.prefix = prefix;
			this.ifd_offset = ifd_offset;
			this.absolute_offset = absolute_offset;
			this.is_bigendian = is_bigendian;
		}

		/// <summary>
		///    Constructor. Creates a makernote instance just containing an IFD and
		///    without any special prefix or offset behavior.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="structure">
		///    A <see cref="IFDStructure"/> with the IFD structure, which is stored by this
		///    instance
		/// </param>
		/// <param name="makernote_type">
		///    A <see cref="MakernoteType"/> with the type of the makernote.
		/// </param>
		public MakernoteIFDEntry (ushort tag, IFDStructure structure, MakernoteType makernote_type)
			: this (tag, structure, makernote_type, null, 0, true, null) {}

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
			type = (ushort) IFDEntryType.Undefined;

			var renderer =
				new IFDRenderer (this.is_bigendian ?? is_bigendian, Structure, absolute_offset ? offset + ifd_offset : ifd_offset);

			ByteVector data = renderer.Render ();
			data.Insert (0, prefix);
			count = (uint) data.Count;
			return data;
		}

#endregion

	}
}
