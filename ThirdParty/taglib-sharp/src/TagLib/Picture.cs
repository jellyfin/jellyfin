//
// Picture.cs: Provides IPicture and Picture.
//
// Author:
//   Aaron Bockover (abockover@novell.com)
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   attachedpictureframe.cpp from TagLib
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2007 Brian Nickel
// Copyright (C) 2004 Scott Wheeler (Original Implementation)
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

namespace TagLib {
	/// <summary>
	///    Specifies the type of content appearing in the picture.
	/// </summary>
	public enum PictureType
	{
		/// <summary>
		///    The picture is of a type other than those specified.
		/// </summary>
		Other = 0x00,
		
		/// <summary>
		///    The picture is a 32x32 PNG image that should be used when
		///    displaying the file in a browser.
		/// </summary>
		FileIcon = 0x01,
		
		/// <summary>
		///    The picture is of an icon different from <see
		///    cref="FileIcon" />.
		/// </summary>
		OtherFileIcon = 0x02,
		
		/// <summary>
		///    The picture is of the front cover of the album.
		/// </summary>
		FrontCover = 0x03,
		
		/// <summary>
		///    The picture is of the back cover of the album.
		/// </summary>
		BackCover = 0x04,
		
		/// <summary>
		///    The picture is of a leaflet page including with the
		///    album.
		/// </summary>
		LeafletPage = 0x05,
		
		/// <summary>
		///    The picture is of the album or disc itself.
		/// </summary>
		Media = 0x06,
		// Image from the album itself
		
		/// <summary>
		///    The picture is of the lead artist or soloist.
		/// </summary>
		LeadArtist = 0x07,
		
		/// <summary>
		///    The picture is of the artist or performer.
		/// </summary>
		Artist = 0x08,
		
		/// <summary>
		///    The picture is of the conductor.
		/// </summary>
		Conductor = 0x09,
		
		/// <summary>
		///    The picture is of the band or orchestra.
		/// </summary>
		Band = 0x0A,
		
		/// <summary>
		///    The picture is of the composer.
		/// </summary>
		Composer = 0x0B,
		
		/// <summary>
		///    The picture is of the lyricist or text writer.
		/// </summary>
		Lyricist = 0x0C,
		
		/// <summary>
		///    The picture is of the recording location or studio.
		/// </summary>
		RecordingLocation = 0x0D,
		
		/// <summary>
		///    The picture is one taken during the track's recording.
		/// </summary>
		DuringRecording = 0x0E,
		
		/// <summary>
		///    The picture is one taken during the track's performance.
		/// </summary>
		DuringPerformance = 0x0F,
		
		/// <summary>
		///    The picture is a capture from a movie screen.
		/// </summary>
		MovieScreenCapture = 0x10,
		
		/// <summary>
		///    The picture is of a large, colored fish.
		/// </summary>
		ColoredFish = 0x11,
		
		/// <summary>
		///    The picture is an illustration related to the track.
		/// </summary>
		Illustration = 0x12,
		
		/// <summary>
		///    The picture contains the logo of the band or performer.
		/// </summary>
		BandLogo = 0x13,
		
		/// <summary>
		///    The picture is the logo of the publisher or record
		///    company.
		/// </summary>
		PublisherLogo = 0x14,


		/// <summary>
		///    In fact, this is not a Picture, but another file-type.
		/// </summary>
		NotAPicture = 0xff

	}

	/// <summary>
	///    This interface provides generic information about a picture,
	///    including its contents, as used by various formats.
	/// </summary>
	public interface IPicture
	{
		/// <summary>
		///    Gets and sets the mime-type of the picture data
		///    stored in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the mime-type
		///    of the picture data stored in the current instance.
		/// </value>
		string MimeType {get; set;}
		
		/// <summary>
		///    Gets and sets the type of content visible in the picture
		///    stored in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="PictureType" /> containing the type of
		///    content visible in the picture stored in the current
		///    instance.
		/// </value>
		PictureType Type {get; set;}



		/// <summary>
		///    Gets and sets a filename of the picture stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the filename,
		///    with its extension, of the picture stored in the current 
		///    instance.
		/// </value>
		string Filename { get; set; }


		/// <summary>
		///    Gets and sets a description of the picture stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the picture stored in the current instance.
		/// </value>
		string Description {get; set;}
		
		/// <summary>
		///    Gets and sets the picture data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the picture
		///    data stored in the current instance.
		/// </value>
		ByteVector Data {get; set;}
	}
	
	/// <summary>
	///    This class implements <see cref="IPicture" /> and provides
	///    mechanisms for loading pictures from files.
	/// </summary>
	public class Picture : IPicture
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

		#endregion

		#region Constants

		/// <summary>
		///    Look-Up-Table associating a file-extension to 
		///    a Mime-Type 
		/// </summary>
		private static readonly string[] lutExtensionMime = new string[] {
			"aac", "audio/aac", // AAC audio file
			"abw", "application/x-abiword", // AbiWord document
			"arc", "application/octet-stream", // Archive document (multiple files embedded)
			"avi", "video/x-msvideo", // AVI: Audio Video Interleave
			"azw", "application/vnd.amazon.ebook", // Amazon Kindle eBook format
			"bin", "application/octet-stream", // Any kind of binary data
			"bmp", "image/bmp", // BMP image data
			"bmp", "image/x-windows-bmp", // BMP image data
			"bm", "image/bmp", // BMP image data
			"bz", "application/x-bzip", // BZip archive
			"bz2", "application/x-bzip2", // BZip2 archive
			"csh", "application/x-csh", // C-Shell script
			"css", "text/css", // Cascading Style Sheets (CSS)
			"csv", "text/csv", // Comma-separated values (CSV)
			"doc", "application/msword", // Microsoft Word
			"eot", "application/vnd.ms-fontobject", // MS Embedded OpenType fonts
			"epub", "application/epub+zip", // Electronic publication (EPUB)
			"gif", "image/gif", // Graphics Interchange Format (GIF)
			"htm", "text/html", // HyperText Markup Language (HTML)text / html
			"html", "text/html", // HyperText Markup Language (HTML)text / html
			"ico", "image/x-icon", // Icon format
			"ics", "text/calendar", // iCalendar format
			"jar", "application/java-archive", // Java Archive (JAR)
			"jpg", "image/jpeg", // JPEG images
			"jpeg", "image/jpeg", // JPEG images
			"js", "application/javascript", // JavaScript (ECMAScript)
			"json", "application/json", // JSON format
			"mid", "audio/midi", // Musical Instrument Digital Interface (MIDI)
			"midi", "audio/midi", // Musical Instrument Digital Interface (MIDI)
			"mp3", "audio/mpeg",
			"mp1", "audio/mpeg",
			"mp2", "audio/mpeg",
			"mpg", "video/mpeg",
			"mpeg", "video/mpeg", // MPEG Video
			"m4a", "audio/mp4",
			"mp4", "video/mp4",
			"m4v", "video/mp4",
			"mpkg", "application/vnd.apple.installer+xml", // Apple Installer Package
			"odp", "application/vnd.oasis.opendocument.presentation", // OpenDocuemnt presentation document
			"ods", "application/vnd.oasis.opendocument.spreadsheet", // OpenDocuemnt spreadsheet document
			"odt", "application/vnd.oasis.opendocument.text", // OpenDocument text document
			"oga", "audio/ogg", // OGG audio
			"ogg", "audio/ogg",
			"ogx", "application/ogg", // OGG
			"ogv", "video/ogg",
			"otf", "font/otf", // OpenType font
			"png", "image/png", // Portable Network Graphics
			"pdf", "application/pdf", // Adobe Portable Document Format (PDF)
			"ppt", "application/vnd.ms-powerpoint", // Microsoft PowerPoint
			"rar", "application/x-rar-compressed", // RAR archive
			"rtf", "application/rtf", // Rich Text Format (RTF)
			"sh", "application/x-sh", // Bourne shell script
			"svg", "image/svg+xml", // Scalable Vector Graphics (SVG)
			"swf", "application/x-shockwave-flash", // Small web format (SWF) or Adobe Flash document
			"tar", "application/x-tar", // Tape Archive (TAR)
			"tif", "image/tiff", //  Tagged Image File Format(TIFF)
			"tiff", "image/tiff", //  Tagged Image File Format(TIFF)
			"ts", "video/vnd.dlna.mpeg-tts", // Typescript file
			"ttf", "font/ttf", // TrueType Font
			"vsd", "application/vnd.visio", // Microsoft Visio
			"wav", "audio/x-wav", // Waveform Audio Format
			"weba", "audio/webm", // WEBM audio
			"webm", "video/webm", // WEBM video
			"webp", "image/webp", // WEBP image
			"woff", "font/woff", // Web Open Font Format (WOFF)
			"woff2", "font/woff2", // Web Open Font Format (WOFF)
			"xhtml", "application/xhtml+xml", // XHTML
			"xls", "application/vnd.ms", // excel application
			"xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // excel 2007 application
			"xml", "application/xml", // XML
			"xul", "application/vnd.mozilla.xul+xml", // XUL
			"zip", "application/zip", // ZIP archive
			"3gp", "video/3gpp", // 3GPP audio/video container
			"3g2", "video/3gpp2", // 3GPP2 audio/video container
			"7z", "application/x-7z-compressed", // 7-zip archive
		};

		#endregion

		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> with no data or values.
		/// </summary>
		public Picture ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> by reading in the contents of a
		///    specified file.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string"/> object containing the path of the
		///    file to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public Picture (string path)
		{
			if (path == null)
				throw new ArgumentNullException ("path");
			
			Data = ByteVector.FromPath (path);
			filename = System.IO.Path.GetFileName(path);
			description = filename;
			mime_type = GetMimeFromExtension(filename);
			type = mime_type.StartsWith("image/") ? PictureType.FrontCover : PictureType.NotAPicture;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> by reading in the contents of a
		///    specified file abstraction.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="File.IFileAbstraction"/> object containing
		///    abstraction of the file to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public Picture (File.IFileAbstraction abstraction)
		{
			if (abstraction == null)
				throw new ArgumentNullException ("abstraction");
			
			Data = ByteVector.FromFile (abstraction);
			filename = abstraction.Name;
			description = abstraction.Name;

			if (!string.IsNullOrEmpty(filename) && filename.Contains("."))
			{
				mime_type = GetMimeFromExtension(filename);
				type = mime_type.StartsWith("image/") ? PictureType.FrontCover : PictureType.NotAPicture;
			}
			else
			{
				string ext = GetExtensionFromData(data);
				MimeType = GetMimeFromExtension(ext);
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
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Picture" /> by using the contents of a <see
		///    cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> object containing picture data
		///    to use.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public Picture (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			Data = new ByteVector (data);
			string ext = GetExtensionFromData(data);
			MimeType = GetMimeFromExtension(ext);
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
		///    cref="Picture" /> by doing a shallow copy of <see 
		///    cref="IPicture" />.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture"/> object containing picture data
		///    to convert to an Picture.
		/// </param>
		public Picture(IPicture picture)
		{
			mime_type = picture.MimeType;
			type = picture.Type;
			filename = picture.Filename;
			description = picture.Description;
			data = picture.Data;
		}



		#endregion



		#region Legacy Factory methods

		/// <summary>
		///    Creates a new <see cref="Picture" />, populating it with
		///    the contents of a file.
		/// </summary>
		/// <param name="filename">
		///    A <see cref="string" /> object containing the path to a
		///    file to read the picture from.
		/// </param>
		/// <returns>
		///    A new <see cref="Picture" /> object containing the
		///    contents of the file and with a mime-type guessed from
		///    the file's contents.
		/// </returns>
		[Obsolete("Use Picture(string filename) constructor instead.")]
		public static Picture CreateFromPath (string filename)
		{
			return new Picture (filename);
		}
		
		/// <summary>
		///    Creates a new <see cref="Picture" />, populating it with
		///    the contents of a file.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="File.IFileAbstraction" /> object containing
		///    the file abstraction to read the picture from.
		/// </param>
		/// <returns>
		///    A new <see cref="Picture" /> object containing the
		///    contents of the file and with a mime-type guessed from
		///    the file's contents.
		/// </returns>
		[Obsolete("Use Picture(File.IFileAbstraction abstraction) constructor instead.")]
		public static Picture CreateFromFile (File.IFileAbstraction abstraction)
		{
			return new Picture (abstraction);
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
			get { return mime_type; }
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
			get { return type; }
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
			get { return data; }
			set { data = value; }
		}

		#endregion



		#region Public static Methods (class functions)

		/// <summary>
		///    Retrieve a mime type from raw file data by reading
		///    the first few bytes of the file. 
		///    Less accurate than <see cref="GetExtensionFromMime"/>.
		/// </summary>
		/// <param name="data">
		///    file name with extension, or just extension of a file
		/// </param>
		/// <returns>File-extension as <see cref="string"/>, or null if 
		///    not identified</returns>
		public static string GetExtensionFromData (ByteVector data)
		{
			string ext = null;

			// No picture, unless it is corrupted, can fit in a file of less than 4 bytes
			if (data.Count >= 4)
			{
				if (data[1] == 'P' && data[2] == 'N' && data[3] == 'G')
				{
					ext = ".png";
				}
				else if (data[0] == 'G' && data[1] == 'I' && data[2] == 'F')
				{
					ext = ".gif";
				}
				else if (data[0] == 'B' && data[1] == 'M')
				{
					ext = ".bmp";
				}
				else if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF && data[3] == 0xE0 )
				{
					ext = ".jpg";
				}

			}

			return ext;
		}

		/// <summary>
		///    Gets the file-extension that fits a mime-type. 
		///    More accurate than <see cref="GetExtensionFromData"/>.
		/// </summary>
		/// <param name="mime">
		///    Mime-type as <see cref="string"/>.
		/// </param>
		/// <returns>File-extension as <see cref="string"/>, or null if 
		///    not identified</returns>
		public static string GetExtensionFromMime(string mime)
		{
			// Default
			string ext = null;

			for (int i = 1; i< lutExtensionMime.Length; i += 2)
			{
				if (lutExtensionMime[i] == mime)
				{
					ext = lutExtensionMime[i - 1];
					break;
				}
			}

			return ext;
		}


		/// <summary>
		///    Gets the mime type of from a file-name (it's extensions). 
		///    If the format cannot be identified, it assumed to be a Binary file.
		/// </summary>
		/// <param name="name">
		///    file name with extension, or just extension of a file
		/// </param>
		/// <returns>Mime-type as <see cref="string"/></returns>
		public static string GetMimeFromExtension(string name)
		{
			// Default
			string mime_type = "application/octet-stream";

			// Get extension from Filename
			if (string.IsNullOrEmpty(name)) return mime_type;
			var ext = System.IO.Path.GetExtension(name);
			if (string.IsNullOrEmpty(ext))
				ext = name;
			else
				ext = ext.Substring(1);

			ext = ext.ToLower();

			for (int i = 0; i < lutExtensionMime.Length; i += 2)
			{
				if (lutExtensionMime[i] == ext)
				{
					mime_type = lutExtensionMime[i + 1];
					break;
				}
			}

			return mime_type;
		}

		#endregion
	}
}
