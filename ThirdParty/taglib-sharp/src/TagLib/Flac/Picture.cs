//
// Picture.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace TagLib.Flac
{
	/// <summary>
	///    This class implements <see cref="IPicture" /> to provide support
	///    for reading and writing Flac picture metadata.
	/// </summary>
	public class Picture : IPicture
	{
#region Private Fields
		
		/// <summary>
		///    Contains the picture type.
		/// </summary>
		private PictureType type;
		
		/// <summary>
		///    Contains the mime-type.
		/// </summary>
		private string mime_type;

		/// <summary>
		///    Contains the filename.
		/// </summary>
		private string filename;
		
		/// <summary>
		///    Contains the description.
		/// </summary>
		private string description;
		
		/// <summary>
		///    Contains the width.
		/// </summary>
		private int width = 0;
		
		/// <summary>
		///    Contains the height.
		/// </summary>
		private int height = 0;
		
		/// <summary>
		///    Contains the color depth.
		/// </summary>
		private int color_depth = 0;
		
		/// <summary>
		///    Contains the number of indexed colors.
		/// </summary>
		private int indexed_colors = 0;
		
		/// <summary>
		///    Contains the picture data.
		/// </summary>
		private ByteVector picture_data;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> by reading the contents of a raw Flac
		///    image structure.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    Flac image.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 32 bytes.
		/// </exception>
		public Picture (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < 32)
				throw new CorruptFileException (
					"Data must be at least 32 bytes long");
			
			int pos = 0;
			type = (PictureType) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			int mimetype_length = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			mime_type = data.ToString (StringType.Latin1, pos,
				mimetype_length);
			pos += mimetype_length;
			
			int description_length = (int) data.Mid (pos, 4)
				.ToUInt ();
			pos += 4;
			
			description = data.ToString (StringType.UTF8, pos,
				description_length);
			pos += description_length;
			
			width = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			height = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			color_depth = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			indexed_colors = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			int data_length = (int) data.Mid (pos, 4).ToUInt ();
			pos += 4;
			
			picture_data = data.Mid (pos, data_length);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> by copying the properties of a <see
		///    cref="IPicture" /> object.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture" /> object to use for the new
		///    instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="picture" /> is <see langword="null" />.
		/// </exception>
		public Picture (IPicture picture)
		{
			if (picture == null)
				throw new ArgumentNullException ("picture");
			
			type = picture.Type;
			mime_type = picture.MimeType;
			filename = picture.Filename;
			description = picture.Description;
			picture_data = picture.Data;
			
			TagLib.Flac.Picture flac_picture =
				picture as TagLib.Flac.Picture;
			
			if (flac_picture == null)
				return;
			
			width = flac_picture.Width;
			height = flac_picture.Height;
			color_depth = flac_picture.ColorDepth;
			indexed_colors = flac_picture.IndexedColors;
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw Flac picture.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector data = new ByteVector ();
			
			data.Add (ByteVector.FromUInt ((uint) Type));
			
			ByteVector mime_data = ByteVector.FromString (MimeType,
				StringType.Latin1);
			data.Add (ByteVector.FromUInt ((uint) mime_data.Count));
			data.Add (mime_data);
			
			ByteVector decription_data = ByteVector.FromString (
				Description, StringType.UTF8);
			data.Add (ByteVector.FromUInt ((uint)
				decription_data.Count));
			data.Add (decription_data);
			
			data.Add (ByteVector.FromUInt ((uint) Width));
			data.Add (ByteVector.FromUInt ((uint) Height));
			data.Add (ByteVector.FromUInt ((uint) ColorDepth));
			data.Add (ByteVector.FromUInt ((uint) IndexedColors));
			
			data.Add (ByteVector.FromUInt ((uint) Data.Count));
			data.Add (Data);
			
			return data;
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
			get {return mime_type;}
			set {mime_type = value;}
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
			get {return type;}
			set {type = value;}
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
			get { return filename; }
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
			get {return description;}
			set {description = value;}
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
			get {return picture_data;}
			set {picture_data = value;}
		}
		
		/// <summary>
		///    Gets and sets the width of the picture in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing width of the
		///    picture stored in the current instance.
		/// </value>
		public int Width {
			get {return width;}
			set {width = value;}
		}
		
		/// <summary>
		///    Gets and sets the height of the picture in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing height of the
		///    picture stored in the current instance.
		/// </value>
		public int Height {
			get {return height;}
			set {height = value;}
		}
		
		/// <summary>
		///    Gets and sets the color depth of the picture in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing color depth of the
		///    picture stored in the current instance.
		/// </value>
		public int ColorDepth {
			get {return color_depth;}
			set {color_depth = value;}
		}
		
		/// <summary>
		///    Gets and sets the number of indexed colors in the picture
		///    in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing number of indexed
		///    colors in the picture, or zero if the picture is not
		///    stored in an indexed format.
		/// </value>
		public int IndexedColors {
			get {return indexed_colors;}
			set {indexed_colors = value;}
		}
		
#endregion
	}
}
