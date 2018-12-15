//
// File.cs: Provides tagging for DNG files
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

using TagLib;
using TagLib.Image;
using TagLib.IFD;
using TagLib.IFD.Tags;
using TagLib.IFD.Entries;

namespace TagLib.Tiff.Dng
{

	/// <summary>
	///    This class extends <see cref="TagLib.Tiff.File" /> to provide tagging
	///    for DNG image files.
	/// </summary>
	[SupportedMimeType("taglib/dng", "dng")]
	[SupportedMimeType("image/dng")]
	[SupportedMimeType("image/x-adobe-dng")]
	public class File : TagLib.Tiff.File
	{

#region public Properties

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
		             ReadStyle propertiesStyle) : base (abstraction, propertiesStyle)
		{
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

		/// <summary>
		///    Attempts to extract the media properties of the main
		///    photo.
		/// </summary>
		/// <returns>
		///    A <see cref="Properties" /> object with a best effort guess
		///    at the right values. When no guess at all can be made,
		///    <see langword="null" /> is returned.
		/// </returns>
		protected override Properties ExtractProperties ()
		{
			int width = 0, height = 0;

			IFDTag tag = GetTag (TagTypes.TiffIFD) as IFDTag;
			IFDStructure structure = tag.Structure;

			// DNG uses SubIFDs for images, the one with SubfileType = 0 is the RAW data.
			var sub_ifds = structure.GetEntry (0, (ushort) IFDEntryTag.SubIFDs) as SubIFDArrayEntry;
			if (sub_ifds == null) {
				return base.ExtractProperties ();
			}

			foreach (var entry in sub_ifds.Entries) {
				var type = entry.GetLongValue (0, (ushort) IFDEntryTag.NewSubfileType);
				if (type == 0) {
					width = (int) (entry.GetLongValue (0, (ushort) IFDEntryTag.ImageWidth) ?? 0);
					height = (int) (entry.GetLongValue (0, (ushort) IFDEntryTag.ImageLength) ?? 0);
					break; // No need to iterate the other SubIFDs
				}
			}

			if (width > 0 && height > 0) {
				return new Properties (TimeSpan.Zero, CreateCodec (width, height));
			}

			// Fall back to normal detection.
			return base.ExtractProperties ();
		}

		/// <summary>
		///    Create a codec that describes the photo properties.
		/// </summary>
		/// <returns>
		///    A <see cref="Codec" /> object.
		/// </returns>
		protected override Codec CreateCodec (int width, int height)
		{
			return new Codec (width, height, "Adobe Digital Negative File");
		}
	}
}
