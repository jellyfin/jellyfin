//
// File.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2009 Ruben Vermeersch
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
using System.IO;

using TagLib.Image;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using TagLib.Xmp;

namespace TagLib.Tiff
{
	/// <summary>
	///    This class extends <see cref="TagLib.Tiff.BaseTiffFile" /> to provide tagging
	///    and properties support for Tiff files.
	/// </summary>
	[SupportedMimeType("taglib/tiff", "tiff")]
	[SupportedMimeType("taglib/tif", "tif")]
	[SupportedMimeType("image/tiff")]
	public class File : BaseTiffFile
	{
#region Private Fields

		/// <summary>
		///    Contains the media properties.
		/// </summary>
		private Properties properties;

#endregion

#region Constructors

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
		public File (string path) : base (path)
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
			ImageTag = new CombinedImageTag (TagTypes.TiffIFD | TagTypes.XMP);

			Mode = AccessMode.Read;
			try {
				Read (propertiesStyle);
				TagTypesOnDisk = TagTypes;
			} finally {
				Mode = AccessMode.Closed;
			}
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
		protected File (IFileAbstraction abstraction) : base (abstraction)
		{
		}

#endregion

#region Public Properties

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

#endregion

#region Public Methods

		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		public override void Save ()
		{
			// Boilerplate
			PreSave();

			Mode = AccessMode.Write;
			try {
				WriteFile ();

				TagTypesOnDisk = TagTypes;
			} finally {
				Mode = AccessMode.Closed;
			}
		}

#endregion

#region Private Methods

		/// <summary>
		///    Render the whole file and write it back.
		/// </summary>
		private void WriteFile ()
		{
			// Check, if IFD0 is contained
			IFDTag exif = ImageTag.Exif;
			if (exif == null)
				throw new Exception ("Tiff file without tags");

			UpdateTags (exif);

			// first IFD starts at 8
			uint first_ifd_offset = 8;
			ByteVector data = RenderHeader (first_ifd_offset);

			var renderer = new IFDRenderer (IsBigEndian, exif.Structure, first_ifd_offset);

			data.Add (renderer.Render ());

			Insert (data, 0, Length);
		}

		/// <summary>
		///    Update the XMP stored in the Tiff IFD
		/// </summary>
		/// <param name="exif">
		///    A <see cref="IFDTag"/> The Tiff IFD to update the entries
		/// </param>
		private void UpdateTags (IFDTag exif)
		{
			// update the XMP entry
			exif.Structure.RemoveTag (0, (ushort) IFDEntryTag.XMP);

			XmpTag xmp = ImageTag.Xmp;
			if (xmp != null)
				exif.Structure.AddEntry (0, new ByteVectorIFDEntry ((ushort) IFDEntryTag.XMP, xmp.Render ()));
		}

		/// <summary>
		///    Reads the file with a specified read style.
		/// </summary>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		protected void Read (ReadStyle propertiesStyle)
		{
			Mode = AccessMode.Read;
			try {
				uint first_ifd_offset = ReadHeader ();
				ReadIFD (first_ifd_offset);

				// Find XMP data
				var xmp_entry = ImageTag.Exif.Structure.GetEntry (0, (ushort) IFDEntryTag.XMP) as ByteVectorIFDEntry;
				if (xmp_entry != null) {
					ImageTag.AddTag (new XmpTag (xmp_entry.Data.ToString (), this));
				}

				if ((propertiesStyle & ReadStyle.Average) == 0)
					return;

				properties = ExtractProperties ();
			} finally {
				Mode = AccessMode.Closed;
			}
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
		protected virtual Properties ExtractProperties ()
		{
			int width = 0, height = 0;

			IFDTag tag = GetTag (TagTypes.TiffIFD) as IFDTag;
			IFDStructure structure = tag.Structure;

			width = (int) (structure.GetLongValue (0, (ushort) IFDEntryTag.ImageWidth) ?? 0);
			height = (int) (structure.GetLongValue (0, (ushort) IFDEntryTag.ImageLength) ?? 0);

			if (width > 0 && height > 0) {
				return new Properties (TimeSpan.Zero, CreateCodec (width, height));
			}

			return null;
		}

		/// <summary>
		///    Create a codec that describes the photo properties.
		/// </summary>
		/// <returns>
		///    A <see cref="Codec" /> object.
		/// </returns>
		protected virtual Codec CreateCodec (int width, int height)
		{
			return new Codec (width, height);
		}

#endregion
	}
}
