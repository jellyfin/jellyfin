//
// BaseTiffFile.cs:
//
// Author:
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2010 Mike Gemuende
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

using TagLib.Image;
using TagLib.IFD;


namespace TagLib.Tiff
{

	/// <summary>
	///    This class extends <see cref="TagLib.Image.File" /> to provide some basic behavior
	///    for Tiff based file formats.
	/// </summary>
	public abstract class BaseTiffFile : TagLib.Image.File
	{

#region Public Properties

		/// <summary>
		///    Indicates if the current file is in big endian or little endian format.
		/// </summary>
		/// <remarks>
		///    The method <see cref="ReadHeader()"/> must be called from a subclass to
		///    properly initialize this property.
		/// </remarks>
		public bool IsBigEndian {
			get; private set;
		}

#endregion

#region Protected Properties

		/// <summary>
		///    The identifier used to recognize the file. This is 42 for most TIFF files.
		/// </summary>
		protected ushort Magic {
			get; set;
		}

#endregion

#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		protected BaseTiffFile (string path) : base (path)
		{
			Magic = 42;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		protected BaseTiffFile (IFileAbstraction abstraction) : base (abstraction)
		{
			Magic = 42;
		}

#endregion

#region Protected Methods

		/// <summary>
		///    Reads and validates the TIFF header at the current position.
		/// </summary>
		/// <returns>
		///    A <see cref="System.UInt32"/> with the offset value to the first
		///    IFD contained in the file.
		/// </returns>
		/// <remarks>
		///    This method should only be called, when the current read position is
		///    the beginning of the file.
		/// </remarks>
		protected uint ReadHeader ()
		{
			// TIFF header:
			//
			// 2 bytes         Indicating the endianess (II or MM)
			// 2 bytes         Tiff Magic word (usually 42)
			// 4 bytes         Offset to first IFD

			ByteVector header = ReadBlock (8);

			if (header.Count != 8)
				throw new CorruptFileException ("Unexpected end of header");

			string order = header.Mid (0, 2).ToString ();

			if (order == "II") {
				IsBigEndian = false;
			} else if (order == "MM") {
				IsBigEndian = true;
			} else {
				throw new CorruptFileException ("Unknown Byte Order");
			}

			if (header.Mid (2, 2).ToUShort (IsBigEndian) != Magic)
				throw new CorruptFileException (String.Format ("TIFF Magic ({0}) expected", Magic));

			uint first_ifd_offset = header.Mid (4, 4).ToUInt (IsBigEndian);

			return first_ifd_offset;
		}


		/// <summary>
		///    Reads IFDs starting from the given offset.
		/// </summary>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the IFD offset to start
		///    reading from.
		/// </param>
		protected void ReadIFD (uint offset)
		{
			ReadIFD (offset, -1);
		}


		/// <summary>
		///    Reads a certain number of IFDs starting from the given offset.
		/// </summary>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the IFD offset to start
		///    reading from.
		/// </param>
		/// <param name="ifd_count">
		///    A <see cref="System.Int32"/> with the number of IFDs to read.
		/// </param>
		protected void ReadIFD (uint offset, int ifd_count)
		{
			long length = 0;
			try {
				length = Length;
			} catch (Exception) {
				// Use a safety-value of 4 gigabyte.
				length = 1073741824L * 4;
			}
			var ifd_tag = GetTag (TagTypes.TiffIFD, true) as IFDTag;
			var reader = CreateIFDReader (this, IsBigEndian, ifd_tag.Structure, 0, offset, (uint) length);

			reader.Read (ifd_count);
		}

		/// <summary>
		///    Creates an IFD reader to parse the file.
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
		protected virtual IFDReader CreateIFDReader (BaseTiffFile file, bool is_bigendian, IFDStructure structure, long base_offset, uint ifd_offset, uint max_offset)
		{
			return new IFDReader (file, is_bigendian, structure, base_offset, ifd_offset, max_offset);

		}

		/// <summary>
		///    Renders a TIFF header with the given offset to the first IFD.
		///    The returned data has length 8.
		/// </summary>
		/// <param name="first_ifd_offset">
		///    A <see cref="System.UInt32"/> with the offset to the first IFD
		///    to be included in the header.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered header of length 8.
		/// </returns>
		protected ByteVector RenderHeader (uint first_ifd_offset)
		{
			ByteVector data = new ByteVector ();

			if (IsBigEndian)
				data.Add ("MM");
			else
				data.Add ("II");

			data.Add (ByteVector.FromUShort (Magic, IsBigEndian));
			data.Add (ByteVector.FromUInt (first_ifd_offset, IsBigEndian));

			return data;
		}

#endregion

	}
}
