//
// AttachedPictureFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//   Sebastien Mouy (starwer@laposte.net)
//
// Original Sources:
//   attachedpictureframe.cpp from TagLib
//   generalencapsulatedobjectframe.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
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
using System.IO;

namespace TagLib.Id3v2 {

	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Attached Picture (APIC), ID3v2 General Encapsulated 
	///    Object (GEOB) and Frames.
	/// </summary>
	/// <remarks>
	///    <para>A <see cref="AttachmentFrame" /> is used for storing
	///    any file (picture or other types) that complement. 
	///    This is typically (but not only limited to) the album cover,
	///    the physical medium, leaflets, file icons or other files and
	///    object data.</para>
	///    <para>Additionally, <see cref="TagLib.Tag.Pictures" /> provides a
	///    generic way or getting and setting pictures/files which is 
	///    preferable to format specific code.</para>
	/// </remarks>
	public class AttachmentFrame : Frame, IPicture, ILazy
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the text encoding to use when rendering.
		/// </summary>
		private StringType encoding = Tag.DefaultEncoding;
		
		/// <summary>
		///    Contains the mime type of <see cref="data" />.
		/// </summary>
		private string mime_type = null;
		
		/// <summary>
		///    Contains the type of picture.
		/// </summary>
		private PictureType type = PictureType.Other;

		/// <summary>
		///    Contains the filename.
		/// </summary>
		private string filename = null;

		/// <summary>
		///    Contains the description.
		/// </summary>
		private string description = null;
		
		/// <summary>
		///    Contains the picture data.
		/// </summary>
		private ByteVector data = null;
		
		/// <summary>
		///    Contains the raw field data of the current instance as
		///    sent to <see cref="ParseFields" /> or <see
		///    langword="null" /> if <see cref="ParseFields" /> has not
		///    been called or <see cref="ParseRawData" /> has been
		///    called.
		/// </summary>
		/// <remarks>
		///    As this frame takes a while to parse and isn't read in
		///    all cases, the raw data is stored here until it is
		///    needed. This speeds up the file read time significantly.
		/// </remarks>
		private ByteVector raw_data = null;
		
		/// <summary>
		///    Contains the ID3v2 version <see cref="raw_data" /> is
		///    stored in.
		/// </summary>
		private byte raw_version = 0;

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
		///    cref="AttachmentFrame" /> with no contents and the
		///    default values.
		/// </summary>
		/// <remarks>
		///    <para>When a frame is created, it is not automatically
		///    added to the tag. Consider using <see
		///    cref="Get(Tag,string,PictureType,bool)" /> for more
		///    integrated frame creation.</para>
		///    <para>Additionally, <see cref="TagLib.Tag.Pictures" />
		///    provides a generic way or getting and setting
		///    attachments which is preferable to format specific
		///    code.</para>
		/// </remarks>
		public AttachmentFrame () : base (FrameType.APIC, 4)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by populating it with
		///    the contents of another <see cref="IPicture" /> object.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture" /> object containing values to use
		///    in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="picture" /> is <see langword="null" />.
		/// </exception>
		/// <remarks>
		///    <para>When a frame is created, it is not automatically
		///    added to the tag. Consider using <see
		///    cref="Get(Tag,string,PictureType,bool)" /> for more
		///    integrated frame creation.</para>
		///    <para>Additionally, <see cref="TagLib.Tag.Pictures" />
		///    provides a generic way or getting and setting
		///    attachments which is preferable to format specific
		///    code.</para>
		/// </remarks>
		/// <example>
		///    <para>Add a picture to a file.</para>
		///    <code lang="C#">
		/// using TagLib;
		/// using TagLib.Id3v2;
		///
		/// public static class AddId3v2Picture
		/// {
		/// 	public static void Main (string [] args)
		/// 	{
		/// 		if (args.Length != 2)
		/// 			throw new ApplicationException (
		/// 				"USAGE: AddId3v2Picture.exe AUDIO_FILE PICTURE_FILE");
		///
		/// 		// Create the file. Can throw file to TagLib# exceptions.
		/// 		File file = File.Create (args [0]);
		///
		/// 		// Get or create the ID3v2 tag.
		/// 		TagLib.Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
		/// 		if (tag == null)
		/// 			throw new ApplicationException ("File does not support ID3v2 tags.");
		///
		/// 		// Create a picture. Can throw file related exceptions.
		///			TagLib.Picture picture = TagLib.Picture.CreateFromPath (path);
		///
		/// 		// Add a new picture frame to the tag.
		/// 		tag.AddFrame (new AttachedPictureFrame (picture));
		///
		/// 		// Save the file.
		/// 		file.Save ();
		/// 	}
		/// }
		///    </code>
		/// </example>
		public AttachmentFrame (IPicture picture)
			: base(FrameType.APIC, 4)
		{
			if (picture == null)
				throw new ArgumentNullException("picture");

			Type = picture.Type;
			mime_type = picture.MimeType;
			filename = picture.Filename;
			description = picture.Description;
			data = picture.Data;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by reading its raw data in
		///    a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public AttachmentFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		protected internal AttachmentFrame (ByteVector data,
		                                    int offset,
		                                    FrameHeader header,
		                                    byte version)
			: base(header)
		{
			SetData (data, offset, version, false);
		}


		/// <summary>
		///    Constructs a new instance of <see
		///    cref="AttachmentFrame" /> from a file.
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
		///    <see cref="File.IFileAbstraction"/> (-1 : read all).
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public AttachmentFrame(File.IFileAbstraction abstraction,
									long offset,
									long size,
									FrameHeader header,
									byte version)
			: base(header)
		{
			if (abstraction == null)
				throw new ArgumentNullException("abstraction");

			file = abstraction;
			stream_offset = offset;
			stream_size = size;
			raw_version = version;
		}


		#endregion



		#region Public Properties

		/// <summary>
		///    Gets and sets the text encoding to use when storing the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the text encoding to
		///    use when storing the current instance.
		/// </value>
		/// <remarks>
		///    This encoding is overridden when rendering if <see
		///    cref="Tag.ForceDefaultEncoding" /> is <see
		///    langword="true" /> or the render version does not support
		///    it.
		/// </remarks>
		public StringType TextEncoding {
			get {
				if (file != null)
					Load();
				ParseRawData (); return encoding;
			}
			set	{
				if (file != null)
					Load();

				encoding = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the mime-type of the picture stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the mime-type of the
		///    picture stored in the current instance.
		/// </value>
		public string MimeType {
			get {
				if (file != null)
					Load();

				ParseRawData();
				if (mime_type != null)
					return mime_type;
				
				return string.Empty;
			}
			set	{
				if (file != null)
					Load();

				mime_type = value;
			}
		}

		/// <summary>
		///    Gets and sets the object type stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="PictureType" /> containing the object type
		///    stored in the current instance.
		/// </value>
		/// <remarks>
		///    For a General Object Frame, use: 
		///    <see cref="PictureType.NotAPicture" />.
		///    Other types will make it a Picture Frame
		/// </remarks>
		public PictureType Type {
			get {
				if (file != null)
					Load();
				ParseRawData(); return type;
			}
			set	{
				if (file != null)
					Load();

				// Change the Frame type depending if this is 
				// a picture or a general object

				var frameid = value == PictureType.NotAPicture ?
					FrameType.GEOB : FrameType.APIC;

				if (header.FrameId != frameid)
					header = new FrameHeader(frameid, 4);

				type = value;
			}
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
				if (file != null)
					Load();
				return filename;
			}
			set	{
				if (file != null)
					Load();

				filename = value;
			}
		}

		/// <summary>
		///    Gets and sets the description stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the description
		///    stored in the current instance.
		/// </value>
		/// <remarks>
		///    There should only be one frame with a matching
		///    description and type per tag.
		/// </remarks>
		public string Description {
			get {
				if (file != null)
					Load();

				ParseRawData();
				if (description != null)
					return description;
				
				return string.Empty;
			}
			set	{
				if (file != null)
					Load();

				description = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the image data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> containing the image data
		///    stored in the current instance.
		/// </value>
		public ByteVector Data {
			get {
				if (file != null)
					Load();

				ParseRawData();
				return data != null ? data : new ByteVector ();
			}
			set	{
				if (file != null)
					Load();

				data = value;}
		}


		/// <summary>
		///    Gets an indication whether the object is loaded.
		/// </summary>
		public bool IsLoaded
		{
			get	{
				return data != null || raw_data != null;
			}
		}

		#endregion



		#region Public Methods

		/// <summary>
		///    Gets a string representation of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> representing the current
		///    instance.
		/// </returns>
		public override string ToString ()
		{
			if (file != null)
				Load();

			System.Text.StringBuilder builder
				= new System.Text.StringBuilder ();
			
			if (string.IsNullOrEmpty (Description)) {
				builder.Append (Description);
				builder.Append (" ");
			}
			
			builder.AppendFormat (
				System.Globalization.CultureInfo.InvariantCulture,
				"[{0}] {1} bytes", MimeType, Data.Count);
			
			return builder.ToString ();
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets a specified picture frame from the specified tag,
		///    optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="description">
		///    A <see cref="string" /> specifying the description to
		///    match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="AttachmentFrame" /> object containing
		///    the matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static AttachmentFrame Get (Tag tag,
		                                    string description,
		                                    bool create)
		{
			return Get (tag, description, PictureType.Other, create);
		}
		
		/// <summary>
		///    Gets a specified picture frame from the specified tag,
		///    optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="type">
		///    A <see cref="PictureType" /> specifying the picture type
		///    to match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="AttachmentFrame" /> object containing
		///    the matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static AttachmentFrame Get (Tag tag,
		                                    PictureType type,
		                                    bool create)
		{
			return Get (tag, null, type, create);
		}
		
		/// <summary>
		///    Gets a specified attachment frame from the specified tag,
		///    optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="description">
		///    A <see cref="string" /> specifying the description to
		///    match.
		/// </param>
		/// <param name="type">
		///    A <see cref="PictureType" /> specifying the picture type
		///    to match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="AttachmentFrame" /> object containing
		///    the matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		/// <example>
		///    <para>Sets a cover image with a description. Because <see
		///    cref="Get(Tag,string,PictureType,bool)" /> is used, if
		///    the program is called again with the same audio file and
		///    desciption, the picture will be overwritten with the new
		///    one.</para>
		///    <code lang="C#">
		/// using TagLib;
		/// using TagLib.Id3v2;
		///
		/// public static class SetId3v2Cover
		/// {
		/// 	public static void Main (string [] args)
		/// 	{
		/// 		if (args.Length != 3)
		/// 			throw new ApplicationException (
		/// 				"USAGE: SetId3v2Cover.exe AUDIO_FILE PICTURE_FILE DESCRIPTION");
		///
		/// 		// Create the file. Can throw file to TagLib# exceptions.
		/// 		File file = File.Create (args [0]);
		///
		/// 		// Get or create the ID3v2 tag.
		/// 		TagLib.Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
		/// 		if (tag == null)
		/// 			throw new ApplicationException ("File does not support ID3v2 tags.");
		///
		/// 		// Create a picture. Can throw file related exceptions.
		///		TagLib.Picture picture = TagLib.Picture.CreateFromPath (args [1]);
		///
		/// 		// Get or create the picture frame.
		/// 		AttachedPictureFrame frame = AttachedPictureFrame.Get (
		/// 			tag, args [2], PictureType.FrontCover, true);
		///
		/// 		// Set the data from the picture.
		/// 		frame.MimeType = picture.MimeType;
		/// 		frame.Data     = picture.data;
		/// 		
		/// 		// Save the file.
		/// 		file.Save ();
		/// 	}
		/// }
		///    </code>
		/// </example>
		public static AttachmentFrame Get (Tag tag,
		                                    string description,
		                                    PictureType type,
		                                    bool create)
		{
			AttachmentFrame att;
			foreach (Frame frame in tag.GetFrames<AttachmentFrame>()) {
				att = frame as AttachmentFrame;
				
				if (att == null)
					continue;
				
				if (description != null && att.Description != description)
					continue;
				
				if (type != PictureType.Other && att.Type != type)
					continue;
				
				return att;
			}
			
			if (!create)
				return null;
			
			att = new AttachmentFrame ();
			att.Description = description;
			att.Type = type;
			
			tag.AddFrame (att);
			
			return att;
		}



		/// <summary>
		/// Load the picture data from the file,
		/// if not done yet.
		/// </summary>
		public void Load()
		{
			// Already loaded ?
			if (file == null) return;

			// Load the picture from the stream

			Stream stream = null;
			ByteVector data = null;

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

			// Decode the raw data if required, by using FieldData
			raw_data = FieldData(data, - (int)FrameHeader.Size(raw_version), raw_version);

			// Get the actual data
			ParseRawData();
		}

		#endregion



		#region Protected Methods

		/// <summary>
		///    Populates the values in the current instance by parsing
		///    its field data in a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is encoded in.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 5 bytes.
		/// </exception>
		protected override void ParseFields (ByteVector data, byte version)
		{
			if (file != null)
				Load();

			if (data.Count < 5)
				throw new CorruptFileException (
					"A picture frame must contain at least 5 bytes.");

			raw_data = data;
			raw_version = version;
		}

		
		/// <summary>
		///    Performs the actual parsing of the raw data.
		/// </summary>
		/// <remarks>
		///    Because of the high parsing cost and relatively low usage
		///    of the class, <see cref="ParseFields" /> only stores the
		///    field data so it can be parsed on demand. Whenever a
		///    property or method is called which requires the data,
		///    this method is called, and only on the first call does it
		///    actually parse the data.
		/// </remarks>
		protected void ParseRawData ()
		{
			if (file != null)
				Load();

			if (raw_data == null)
				return;

			data = raw_data;
			raw_data = null;

			int pos = 0;
			int offset;
			
			encoding = (StringType) data [pos++];

			ByteVector delim = ByteVector.TextDelimiter(encoding);

			if (header.FrameId == FrameType.APIC)
			{
				// Retrieve an ID3v2 Attached Picture (APIC)

				if (raw_version > 2)
				{
					offset = data.Find(ByteVector.TextDelimiter(
						StringType.Latin1), pos);

					if (offset < pos)
						return;

					mime_type = data.ToString(
						StringType.Latin1, pos, offset - pos);
					pos = offset + 1;
				}
				else
				{
					ByteVector ext = data.Mid(pos, 3);
					mime_type = Picture.GetMimeFromExtension(ext.ToString());
					pos += 3;
				}

				Type = (PictureType)data[pos++];

				offset = data.Find(delim, pos, delim.Count);

			}
			else if (header.FrameId == FrameType.GEOB)
			{
				// Retrieve an ID3v2 General Encapsulated Object (GEOB)

				offset = data.Find(
					ByteVector.TextDelimiter(StringType.Latin1),
					pos);

				if (offset < pos)
					return;

				mime_type = data.ToString(StringType.Latin1, pos,
					offset - pos);

				pos = offset + 1;
				offset = data.Find(delim, pos, delim.Count);

				if (offset < pos)
					return;

				filename = data.ToString(encoding, pos,
					offset - pos);
				pos = offset + delim.Count;
				offset = data.Find(delim, pos, delim.Count);

				Type = PictureType.NotAPicture;
			}
			else
			{
				throw new InvalidOperationException("Bad Frame type");
			}

			if (offset < pos)
				return;

			description = data.ToString(encoding, pos,
				offset - pos);
			pos = offset + delim.Count;

			data.RemoveRange(0, pos);
		}

		/// <summary>
		///    Renders the values in the current instance into field
		///    data for a specified version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is to be encoded in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered field data.
		/// </returns>
		protected override ByteVector RenderFields (byte version)
		{
			if (file != null)
				Load();

			if (raw_data != null && raw_version == version)
				return raw_data;
			
			StringType encoding = CorrectEncoding (TextEncoding,
				version);
			ByteVector data = new ByteVector ();

			if (header.FrameId == FrameType.APIC)
			{
				// Make an ID3v2 Attached Picture (APIC)

				data.Add((byte)encoding);

				if (version == 2)
				{
					string ext = Picture.GetExtensionFromMime(MimeType);
					data.Add(ext != null && ext.Length == 3 ? 
						ext.ToUpper() : "XXX");
				}
				else
				{
					data.Add(ByteVector.FromString(MimeType,
						StringType.Latin1));
					data.Add(ByteVector.TextDelimiter(
						StringType.Latin1));
				}

				data.Add((byte)type);
				data.Add(ByteVector.FromString(Description, encoding));
				data.Add(ByteVector.TextDelimiter(encoding));
			}
			else if (header.FrameId == FrameType.GEOB)
			{
				// Make an ID3v2 General Encapsulated Object (GEOB)

				data.Add((byte)encoding);

				if (MimeType != null)
					data.Add(ByteVector.FromString(MimeType,
						StringType.Latin1));
				data.Add(ByteVector.TextDelimiter(StringType.Latin1));

				if (filename != null)
					data.Add(ByteVector.FromString(filename,
						encoding));
				data.Add(ByteVector.TextDelimiter(encoding));

				if (Description != null)
					data.Add(ByteVector.FromString(Description,
						encoding));
				data.Add(ByteVector.TextDelimiter(encoding));

			}
			else
			{
				throw new InvalidOperationException("Bad Frame type");
			}

			data.Add(this.data);
			return data;
		}

		#endregion



		#region ICloneable

		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Frame" /> object identical to the
		///    current instance.
		/// </returns>
		public override Frame Clone ()
		{
			if (file != null)
				Load();

			AttachmentFrame frame = new AttachmentFrame ();
			frame.encoding = encoding;
			frame.mime_type = mime_type;
			frame.Type = type;
			frame.filename = filename;
			frame.description = description;
			if (data != null)
				frame.data = new ByteVector (data);
			if (raw_data != null)
				frame.data = new ByteVector (raw_data);
			frame.raw_version = raw_version;
			return frame;
		}
		
#endregion
	}

#region Legacy Class

	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Attached Picture (APIC) Frames.
	/// </summary>
	/// <remarks>
	///    <para>A <see cref="AttachmentFrame" /> is used for storing
	///    pictures that complement the media, including the album cover,
	///    the physical medium, leaflets, file icons, etc. Other file and
	///    object data can be encapulsated via <see
	///    cref="GeneralEncapsulatedObjectFrame" />.</para>
	///    <para>Additionally, <see cref="TagLib.Tag.Pictures" /> provides a
	///    generic way or getting and setting pictures which is preferable
	///    to format specific code.</para>
	/// </remarks>
	[Obsolete("Use AttachementFrame instead")]
	public class AttachedPictureFrame : AttachmentFrame
	{
		#region Constructors
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> with no contents and the
		///    default values.
		/// </summary>
		[Obsolete("Use AttachementFrame instead")]
		public AttachedPictureFrame() : base()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by populating it with
		///    the contents of another <see cref="IPicture" /> object.
		/// </summary>
		/// <param name="picture">
		///    A <see cref="IPicture" /> object containing values to use
		///    in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="picture" /> is <see langword="null" />.
		/// </exception>
		/// <example>
		///    <para>Add a picture to a file.</para>
		///    <code lang="C#">
		/// using TagLib;
		/// using TagLib.Id3v2;
		///
		/// public static class AddId3v2Picture
		/// {
		/// 	public static void Main (string [] args)
		/// 	{
		/// 		if (args.Length != 2)
		/// 			throw new ApplicationException (
		/// 				"USAGE: AddId3v2Picture.exe AUDIO_FILE PICTURE_FILE");
		///
		/// 		// Create the file. Can throw file to TagLib# exceptions.
		/// 		File file = File.Create (args [0]);
		///
		/// 		// Get or create the ID3v2 tag.
		/// 		TagLib.Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
		/// 		if (tag == null)
		/// 			throw new ApplicationException ("File does not support ID3v2 tags.");
		///
		/// 		// Create a picture. Can throw file related exceptions.
		///			TagLib.Picture picture = TagLib.Picture.CreateFromPath (path);
		///
		/// 		// Add a new picture frame to the tag.
		/// 		tag.AddFrame (new AttachedPictureFrame (picture));
		///
		/// 		// Save the file.
		/// 		file.Save ();
		/// 	}
		/// }
		///    </code>
		/// </example>
		[Obsolete("Use AttachementFrame instead")]
		public AttachedPictureFrame(IPicture picture)
			: base(picture)
		{
			if (picture.Type == PictureType.NotAPicture)
				throw new InvalidCastException("Creating an AttachedPictureFrame from a non-picture object");
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by reading its raw data in
		///    a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		[Obsolete("Use AttachementFrame instead")]
		public AttachedPictureFrame(ByteVector data, byte version)
			: base(data, version)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AttachmentFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		[Obsolete("Use AttachementFrame instead")]
		protected internal AttachedPictureFrame(ByteVector data,
												 int offset,
												 FrameHeader header,
												 byte version)
			: base(data, offset, header, version)
		{
		}

		#endregion

	}





	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 General Encapsulated Object (GEOB) Frames.
	/// </summary>
	/// <remarks>
	///    <para>A <see cref="GeneralEncapsulatedObjectFrame" /> should be
	///    used for storing files and other objects relevant to the file but
	///    not supported by other frames.</para>
	/// </remarks>
	[Obsolete("Use AttachementFrame instead")]
	public class GeneralEncapsulatedObjectFrame : AttachmentFrame
	{

		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="GeneralEncapsulatedObjectFrame" /> with no
		///    contents.
		/// </summary>
		[Obsolete("Use AttachementFrame instead")]
		public GeneralEncapsulatedObjectFrame()
			: base()
		{
			Type = PictureType.NotAPicture;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="GeneralEncapsulatedObjectFrame" /> by reading its
		///    raw data in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		[Obsolete("Use AttachementFrame instead")]
		public GeneralEncapsulatedObjectFrame(ByteVector data,
											   byte version)
			: base(data, version)
		{
			Type = PictureType.NotAPicture;
		}


		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="GeneralEncapsulatedObjectFrame" /> by reading its
		///    raw data in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		[Obsolete("Use AttachementFrame instead")]
		protected internal GeneralEncapsulatedObjectFrame(ByteVector data,
														   int offset,
														   FrameHeader header,
														   byte version)
			: base(data, offset, header, version)
		{
			Type = PictureType.NotAPicture;
		}

		#endregion



		#region Public Properties

		/// <summary>
		///    Gets and sets the file name of the object stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the file name of the
		///    object stored in the current instance.
		/// </value>
		[Obsolete("Use AttachementFrame instead")]
		public string FileName
		{
			get
			{
				if (Filename != null)
					return Filename;

				return string.Empty;
			}
			set { Filename = value; }
		}

		/// <summary>
		///    Gets and sets the object data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> containing the object data
		///    stored in the current instance.
		/// </value>
		[Obsolete("Use AttachementFrame instead")]
		public ByteVector Object
		{
			get { return Data != null ? Data : new ByteVector(); }
			set { Data = value; }
		}

		#endregion

	}

	#endregion
}
