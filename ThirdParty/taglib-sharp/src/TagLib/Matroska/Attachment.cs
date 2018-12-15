//
// SimpleTag.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2017 Starwer
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
using System.IO;

namespace TagLib.Matroska
{
	/// <summary>
	/// Describes a Matroska Attachment. 
	/// Attachments may be pictures, but also any other content type.
	/// </summary>
	public class Attachment : PictureLazy, IUIDElement
	{
		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Attachment" /> with no data or values.
		/// </summary>
		public Attachment()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Attachment" /> by reading in the contents of a
		///    specified file.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string"/> object containing the path of the
		///    file to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public Attachment(string path) : base (path)
		{
			SetTypeFromFilename();
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Attachment" /> by reading in the contents of a
		///    specified file abstraction.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction"/> object containing
		///    abstraction of the file to read.
		/// </param>
		/// <param name="offset">
		///    The position in bytes where the picture is located in the
		///    <see cref="T:File.IFileAbstraction"/>.
		/// </param>
		/// <param name="size">
		///    The size in bytes of the picture in the
		///    <see cref="T:File.IFileAbstraction"/> (default: read all).
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public Attachment(File.IFileAbstraction abstraction, long offset = 0, long size = -1) : base(abstraction, offset, size)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Attachment" /> by using the contents of a <see
		///    cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> object containing picture data
		///    to use.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public Attachment(ByteVector data) : base(data)
		{
		}


		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Attachment" /> by doing a shallow copy of <see 
		///    cref="IPicture" />.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture"/> object containing picture data
		///    to convert to an Attachment.
		/// </param>
		public Attachment(IPicture picture) : base(picture)
		{
		}


		#endregion


		#region Methods

		/// <summary>
		/// Derive the Picture-type from the the file-name. 
		/// It change the <see cref="P:Type"/> from the <see cref="P:Filename"/>.
		/// </summary>
		public void SetTypeFromFilename()
		{
			if (MimeType != null && !MimeType.StartsWith("image/"))
			{
				Type = PictureType.NotAPicture;
				return;
			}

			if (Filename == null)
			{
				Type = PictureType.Other;
				return;
			}

			PictureType type = PictureType.Other;
			string fname = Filename.ToLower();

			foreach (var ptype in Enum.GetNames(typeof(PictureType)))
			{
				if (fname.Contains(ptype.ToLower()))
				{
					type = (PictureType)Enum.Parse(typeof(PictureType), ptype);
					break;
				}
			}

			if (type == PictureType.Other && ((fname.Contains("cover") || fname.Contains("poster"))))
			{
				type = PictureType.FrontCover;
			}

			Type = type;
		}

		/// <summary>
		/// Derive thefile-name from the the Piture type. 
		/// It change the <see cref="P:Filename"/> from the <see cref="P:Type"/> if required, 
		/// but not if the filename already matches the type.
		/// </summary>
		/// <returns>true if <see cref="P:Filename"/> changed</returns>
		public bool SetFilenameFromType()
		{
			PictureType type = Type;

			if (! string.IsNullOrEmpty(Filename))
			{
				SetTypeFromFilename();

				// Filename already matches the type, so do not change it
				if (type == Type) return false;

				// restore the type
				Type = type;
			}

			// Derive extension from file or MimeType
			string ext = null;
			if (Filename != null) ext = Path.GetExtension(Filename);
			if (ext == null && MimeType != null && MimeType.StartsWith("image/") ) ext = "." + MimeType.Substring(6);
			if (ext == null || ext.Length<2) ext = "";

			// Change the filename
			Filename = type.ToString() + ext;
			return true;
		}

		#endregion


		#region IUIDElement Boilerplate

		/// <summary>
		/// Unique ID representing the element, as random as possible (setting zero will generate automatically a new one).
		/// </summary>
		public ulong UID
		{
			get { return _UID; }
			set { _UID = UIDElement.GenUID(value); }
		}
		private ulong _UID = UIDElement.GenUID();


		/// <summary>
		/// Get the Tag type the UID should be represented by, or 0 if undefined
		/// </summary>
		public MatroskaID UIDType { get { return MatroskaID.TagAttachmentUID; } }

		#endregion

	}
}
