//
// PictureLazy.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2018 Starwer
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

namespace TagLib {

	/// <summary>
	///    This class implements <see cref="IPicture" /> and provides
	///    mechanisms for loading pictures from files.
	///    Contrary to <see cref="Picture" />, a reference to a file
	///    where the picture is located can be given and the picture 
	///    is lazily loaded from the file, meaning that it will be 
	///    read from the file only when needed. This saves time and 
	///    memory if the picture loading is not required.
	/// </summary>
	public class PictureLazy : IPicture, ILazy
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the mime-type.
		/// </summary>
		private string mime_type;
		
		/// <summary>
		///    Contains the content type.
		/// </summary>
		private PictureType type;

		/// <summary>
		///    Contains the filename.
		/// </summary>
		private string filename;

		/// <summary>
		///    Contains the description.
		/// </summary>
		private string description;
		
		/// <summary>
		///    Contains the picture data.
		/// </summary>
		private ByteVector data;


		/// <summary>
		/// Stream where the picture is located
		/// </summary>
		private File.IFileAbstraction file;

		/// <summary>
		/// Offset from where the picture start in the <see cref="file"/>
		/// </summary>
		private long stream_offset;

		/// <summary>
		/// Size of the picture in the <see cref="file"/> (-1 = until end of Stream)
		/// </summary>
		private long stream_size = -1;

		#endregion


		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PictureLazy" /> with no data or values.
		/// </summary>
		public PictureLazy ()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PictureLazy" /> from a file.
		///    The content will be lazily loaded.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string"/> object containing the path of the
		///    file to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public PictureLazy(string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			file = new File.LocalFileAbstraction(path);

			filename = Path.GetFileName(path);
			description = filename;
			mime_type = Picture.GetMimeFromExtension(filename);
			type = mime_type.StartsWith("image/") ? PictureType.FrontCover : PictureType.NotAPicture;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PictureLazy" /> from a file abstraction.
		///    The content will be lazily loaded.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="File.IFileAbstraction"/> object containing
		///    abstraction of the file to read.
		/// </param>
		/// <param name="offset">
		///    The position in bytes where the picture is located in the
		///    <see cref="File.IFileAbstraction"/>.
		/// </param>
		/// <param name="size">
		///    The size in bytes of the picture in the
		///    <see cref="File.IFileAbstraction"/> (default: read all).
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public PictureLazy(File.IFileAbstraction abstraction, long offset = 0, long size = -1)
		{
			if (abstraction == null)
				throw new ArgumentNullException ("abstraction");


			file = abstraction;
			stream_offset = offset;
			stream_size = size;

			filename = abstraction.Name;
			description = abstraction.Name;

			if (!string.IsNullOrEmpty(filename) && filename.Contains("."))
			{
				mime_type = Picture.GetMimeFromExtension(filename);
				type = mime_type.StartsWith("image/") ? PictureType.FrontCover : PictureType.NotAPicture;
			}
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PictureLazy" /> by using the contents of a <see
		///    cref="ByteVector" /> object.
		///    The content will not be lazily loaded.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> object containing picture data
		///    to use.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public PictureLazy(ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			Data = new ByteVector (data);
			string ext = Picture.GetExtensionFromData(data);
			MimeType = Picture.GetMimeFromExtension(ext);
			if (ext != null)
			{
				type = PictureType.FrontCover;
				filename = description = "cover" + ext;
			}
			else
			{
				type = PictureType.NotAPicture;
				filename = "UnknownType";
			}
		}


		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PictureLazy" /> by doing a shallow copy of <see 
		///    cref="IPicture" />.
		///    The content will not be lazily loaded.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture"/> object containing picture data
		///    to convert to an Picture.
		/// </param>
		public PictureLazy (IPicture picture)
		{
			mime_type = picture.MimeType;
			type = picture.Type;
			filename = picture.Filename;
			description = picture.Description;
			data = picture.Data;
		}



		#endregion


		#region Public Methods

		/// <summary>
		/// Load the picture data from the file,
		/// if not done yet.
		/// </summary>
		public void Load()
		{
			// Already loaded ?
			if (data != null) return;


			// Load the picture from the stream

			Stream stream = null;

			try
			{
				if (stream_size == 0)
				{
					data = new ByteVector();
				}
				else if (stream_size > 0)
				{
					stream = file.ReadStream;
					stream.Seek(stream_offset, SeekOrigin.Begin);

					int count = 0, read = 0, needed = (int)stream_size;
					byte[] buffer = new byte[needed];

					do
					{
						count = stream.Read(buffer, read, needed);

						read += count;
						needed -= count;
					} while (needed > 0 && count != 0);

					data = new ByteVector(buffer, read);
				}
				else
				{
					stream = file.ReadStream;
					stream.Seek(stream_offset, SeekOrigin.Begin);

					data = ByteVector.FromStream(stream);
				}

			}
			finally
			{
				// Free the resources
				if (stream != null && file != null)
				{
					file.CloseStream(stream);
				}

				file = null;
			}

			// Retrieve remaining properties from data (if required)

			if (mime_type == null) 
			{
				string ext = Picture.GetExtensionFromData(data);
				MimeType = Picture.GetMimeFromExtension(ext);
				if (ext != null)
				{
					type = PictureType.FrontCover;
					if (filename == null)
						filename = description = "cover" + ext;
				}
				else
				{
					type = PictureType.NotAPicture;
					if (filename == null)
						filename = "UnknownType";
				}
			}
		}

		#endregion


		#region Public Properties

		/// <summary>
		///    Gets and sets the mime-type of the picture data
		///    stored in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the mime-type
		///    of the picture data stored in the current instance.
		/// </value>
		public string MimeType {
			get {
				if (mime_type == null)
					Load();
				return mime_type;
			}
			set { mime_type = value; }
		}
		
		/// <summary>
		///    Gets and sets the type of content visible in the picture
		///    stored in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="PictureType" /> containing the type of
		///    content visible in the picture stored in the current
		///    instance.
		/// </value>
		public PictureType Type {
			get {
				if (type == PictureType.Other && mime_type == null)
					Load();
				return type;
			}
			set { type = value; }
		}

		/// <summary>
		///    Gets and sets a filename of the picture stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a fielname, with
		///    extension, of the picture stored in the current instance.
		/// </value>
		public string Filename
		{
			get {
				if (filename == null)
					Load();
				return filename;
			}
			set { filename = value; }
		}

		/// <summary>
		///    Gets and sets a description of the picture stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the picture stored in the current instance.
		/// </value>
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		/// <summary>
		///    Gets and sets the picture data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the picture
		///    data stored in the current instance.
		/// </value>
		public ByteVector Data {
			get {
				if (data == null)
					Load();
				return data;
			}
			set { data = value; }
		}
		

		/// <summary>
		///    Gets an indication whether the picture is loaded.
		/// </summary>
		public bool IsLoaded {
			get {
				return data != null;
			}
		}

		#endregion
		
	}
}
