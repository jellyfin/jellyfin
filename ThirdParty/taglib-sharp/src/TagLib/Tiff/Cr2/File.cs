//
// File.cs: Provides tagging for Canon CR2 files
//
// Author:
//   Mike Gemuende (mike@gemuende.be)
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
using System.Collections.Generic;

using TagLib;
using TagLib.Image;
using TagLib.IFD;
using TagLib.IFD.Tags;

namespace TagLib.Tiff.Cr2
{

	/// <summary>
	///    This class extends <see cref="TagLib.Tiff.BaseTiffFile" /> to provide tagging
	///    for CR2 image files.
	/// </summary>
	[SupportedMimeType("taglib/cr2", "cr2")]
	[SupportedMimeType("image/cr2")]
	[SupportedMimeType("image/x-canon-cr2")]
	public class File : TagLib.Tiff.BaseTiffFile
	{
#region private fields

		/// <summary>
		///    The Properties of the image
		/// </summary>
		private Properties properties;

#endregion

#region public Properties

		/// <summary>
		///    Gets the media properties of the file represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Properties" /> object containing the
		///    media properties of the file represented by the current
		///    instance.
		/// </value>
		public override TagLib.Properties Properties {
			get { return properties; }
		}

		/// <summary>
		///    Indicates if tags can be written back to the current file or not
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> which is true if tags can be written to the
		///    current file, otherwise false.
		/// </value>
		public override bool Writeable {
			get { return false; }
		}


#endregion

#region constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system and specified read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path, ReadStyle propertiesStyle)
			: this (new File.LocalFileAbstraction (path),
				propertiesStyle)
		{
		}

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
		public File (string path) : this (path, ReadStyle.Average)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction and
		///    specified read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction,
		             ReadStyle propertiesStyle) : base (abstraction)
		{
			Read (propertiesStyle);
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
		protected File (IFileAbstraction abstraction)
			: this (abstraction, ReadStyle.Average)
		{
		}

#endregion

#region Public Methods

		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		public override void Save ()
		{
			throw new NotSupportedException ();
		}

#endregion

#region private methods

		/// <summary>
		///    Reads the information from file with a specified read style.
		/// </summary>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		private void Read (ReadStyle propertiesStyle)
		{
			Mode = AccessMode.Read;
			try {
				ImageTag = new CombinedImageTag (TagTypes.TiffIFD);

				ReadFile ();

				TagTypesOnDisk = TagTypes;

				if ((propertiesStyle & ReadStyle.Average) != 0)
					properties = ExtractProperties ();

			} finally {
				Mode = AccessMode.Closed;
			}
		}

		/// <summary>
		///    Parses the CR2 file
		/// </summary>
		private void ReadFile ()
		{
			// A CR2 file starts with a Tiff header followed by a CR2 header
			uint first_ifd_offset = ReadHeader ();
			uint raw_ifd_offset = ReadAdditionalCR2Header ();

			ReadIFD (first_ifd_offset, 3);
			ReadIFD (raw_ifd_offset, 1);
		}

		/// <summary>
		///   Reads and validates the CR2 header started at the current position.
		/// </summary>
		/// <returns>
		///    A <see cref="System.UInt32"/> with the offset to the IFD with the RAW data.
		/// </returns>
		private uint ReadAdditionalCR2Header ()
		{
			// CR2 Header
			//
			// CR2 Information:
			//
			// 2 bytes         CR2 Magic word (CR)
			// 1 byte          CR2 major version (2)
			// 1 byte          CR2 minor version (0)
			// 4 bytes         Offset to RAW IFD
			//

			ByteVector header = ReadBlock (8);

			if (header.Count != 8)
				throw new CorruptFileException ("Unexpected end of CR2 header");

			if (header.Mid (0, 2).ToString () != "CR")
				throw new CorruptFileException("CR2 Magic (CR) expected");

			byte major_version = header [2];
			byte minor_version = header [3];

			if (major_version != 2 || minor_version != 0)
				throw new UnsupportedFormatException ("Only major version 2 and minor version 0 are supported");

			uint raw_ifd_offset = header.Mid (4, 4).ToUInt (IsBigEndian);

			return raw_ifd_offset;
		}

		/// <summary>
		///    Attempts to extract the media properties of the main
		///    photo.
		/// </summary>
		/// <returns>
		///    A <see cref="Properties" /> object with a best effort guess
		///    at the right values. When no guess at all can be made,
		///    <see langword="null" /> is returned.
		/// </returns>
		private Properties ExtractProperties ()
		{
			int width = 0, height = 0;

			IFDTag tag = GetTag (TagTypes.TiffIFD) as IFDTag;

			width = (int) (tag.ExifIFD.GetLongValue (0, (ushort) ExifEntryTag.PixelXDimension) ?? 0);
			height = (int) (tag.ExifIFD.GetLongValue (0, (ushort) ExifEntryTag.PixelYDimension) ?? 0);

			if (width > 0 && height > 0) {
				return new Properties (TimeSpan.Zero, new Codec (width, height, "Canon RAW File"));
			}

			return null;
		}

#endregion


	}
}
