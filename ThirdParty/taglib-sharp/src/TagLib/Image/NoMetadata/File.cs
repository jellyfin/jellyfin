//
// File.cs: Provides an empty wrapper for files that don't support metadata.
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

namespace TagLib.Image.NoMetadata
{
	/// <summary>
	///    This class extends <see cref="TagLib.Image.File" /> to provide tagging
	///    some sort of support for files that don't support metadata. You
	///    obviously can't write to them, but you can populate an XMP tag, for
	///    sidecar purposes.
	/// </summary>
	[SupportedMimeType("taglib/bmp", "bmp")]
	[SupportedMimeType("image/x-MS-bmp")]
	[SupportedMimeType("image/x-bmp")]
	[SupportedMimeType("taglib/ppm", "ppm")]
	[SupportedMimeType("taglib/pgm", "pgm")]
	[SupportedMimeType("taglib/pbm", "pbm")]
	[SupportedMimeType("taglib/pnm", "pnm")]
	[SupportedMimeType("image/x-portable-pixmap")]
	[SupportedMimeType("image/x-portable-graymap")]
	[SupportedMimeType("image/x-portable-bitmap")]
	[SupportedMimeType("image/x-portable-anymap")]
	[SupportedMimeType("taglib/pcx", "pcx")]
	[SupportedMimeType("image/x-pcx")]
	[SupportedMimeType("taglib/svg", "svg")]
	[SupportedMimeType("taglib/svgz", "svgz")]
	[SupportedMimeType("image/svg+xml")]
	[SupportedMimeType("taglib/kdc", "kdc")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/orf", "orf")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/srf", "srf")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/crw", "crw")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/mrw", "mrw")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/raf", "raf")]    // FIXME: Not supported yet!
	[SupportedMimeType("taglib/x3f", "x3f")]    // FIXME: Not supported yet!
	public class File : TagLib.Image.File
	{

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
			get { return null; }
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
			ImageTag = new CombinedImageTag (TagTypes.XMP);
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

	}
}
