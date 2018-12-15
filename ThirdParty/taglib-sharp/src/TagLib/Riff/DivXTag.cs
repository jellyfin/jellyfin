//
// DivXTag.cs: Provide support for reading and writing DivX tags.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   TagLib.Id3v1.Tag
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

using System.Collections;
using System;
using System.Text;
using System.Globalization;

namespace TagLib.Riff {
	/// <summary>
	///    This class extends <see cref="Tag" /> to provide support for
	///    reading and writing tags stored in the DivX format.
	/// </summary>
	public class DivXTag : TagLib.Tag
	{
#region Private Fields
		
		/// <summary>
		///    Contains the title.
		/// </summary>
		private string title;
		
		/// <summary>
		///    Contains the semicolon separated performers.
		/// </summary>
		private string artist;
		
		/// <summary>
		///    Contains the 4 digit year.
		/// </summary>
		private string year;
		
		/// <summary>
		///    Contains a comment on track.
		/// </summary>
		private string comment;
		
		/// <summary>
		///    Contains the genre index.
		/// </summary>
		private string genre;
		
		/// <summary>
		///    Contains the extra 6 bytes at the end of the tag.
		/// </summary>
		private ByteVector extra_data;
		
#endregion




#region Public Static Fields
		
		/// <summary>
		///    The size of a DivX tag.
		/// </summary>
		public const uint Size = 128;
		
		/// <summary>
		///    The identifier used to recognize a DivX tags.
		/// </summary>
		/// <value>
		///    "DIVXTAG"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "DIVXTAG";

#endregion



#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="DivXTag" /> with no contents.
		/// </summary>
		public DivXTag ()
		{
			Clear ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="DivXTag" /> by reading the contents from a
		///    specified position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File" /> object containing the file from
		///    which the contents of the new instance is to be read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the tag.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The file does not contain the file identifier at the
		///    correct offset from the given position.
		/// </exception>
		public DivXTag (File file, long position)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Mode = TagLib.File.AccessMode.Read;
			
			if (position < 0 ||
				position > file.Length - Size)
				throw new ArgumentOutOfRangeException (
					"position");
			
			file.Seek (position);
			
			// read the tag -- always 128 bytes
			
			ByteVector data = file.ReadBlock ((int) Size);
			
			// some initial sanity checking
			
			if (!data.EndsWith (FileIdentifier))
				throw new CorruptFileException (
					"DivX tag data does not end with identifier.");
			
			Parse (data);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="DivXTag" /> by reading the contents raw tag data
		///    stored in a specified <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw DivX tag to
		///    read into the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The file does not contain the file identifier at the
		///    correct offset from the given position.
		/// </exception>
		public DivXTag (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < Size)
				throw new CorruptFileException (
					"DivX tag data is less than 128 bytes long.");
			
			if (!data.EndsWith (FileIdentifier))
				throw new CorruptFileException (
					"DivX tag data does not end with identifier.");
			
			Parse (data);
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw DivX tag.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered tag.
		/// </returns>
		public ByteVector Render ()
		{
			ByteVector data = new ByteVector ();
			data.Add (ByteVector.FromString (title,   StringType.Latin1).Resize (32, 0x20));
			data.Add (ByteVector.FromString (artist,  StringType.Latin1).Resize (28, 0x20));
			data.Add (ByteVector.FromString (year,    StringType.Latin1).Resize ( 4, 0x20));
			data.Add (ByteVector.FromString (comment, StringType.Latin1).Resize (48, 0x20));
			data.Add (ByteVector.FromString (genre,   StringType.Latin1).Resize ( 3, 0x20));
			data.Add (extra_data);
			data.Add (FileIdentifier);
			return data;
		}
		
#endregion
		
		
		
#region Private Methods
		
		/// <summary>
		///    Populates the current instance by parsing the contents of
		///    a raw DivX tag.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    starting with an DivX tag.
		/// </param>
		private void Parse (ByteVector data)
		{
			title      = data.ToString (StringType.Latin1,  0, 32).Trim ();
			artist     = data.ToString (StringType.Latin1, 32, 28).Trim ();
			year       = data.ToString (StringType.Latin1, 60,  4).Trim ();
			comment    = data.ToString (StringType.Latin1, 64, 48).Trim ();
			genre      = data.ToString (StringType.Latin1,112,  3).Trim ();
			extra_data = data.Mid (115,  6);
		}
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Id3v1" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.DivX;}
		}
		
		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    When stored on disk, only the first 32 bytes of the
		///    Latin-1 encoded value will be stored. This may result in
		///    lost data.
		/// </remarks>
		public override string Title {
			get {
				return string.IsNullOrEmpty (title) ?
					null : title;
			}
			set {
				title = value != null ?
					value.Trim () : String.Empty;
			}
		}
		
		/// <summary>
		///    Gets and sets the performers or artists who performed in
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the performers or
		///    artists who performed in the media described by the
		///    current instance or an empty array if no value is
		///    present.
		/// </value>
		/// <remarks>
		///    When stored on disk, only the first 28 bytes of the
		///    Latin-1 encoded value will be stored, minus a byte for
		///    each additionial performer (i.e. two performers will only
		///    have 27 bytes and three performers will only have 26
		///    bytes).This may result in lost data.
		/// </remarks>
		public override string [] Performers {
			get {
				return string.IsNullOrEmpty (artist) ?
					new string [0] : artist.Split (';');
			}
			set {
				artist = value != null ?
					string.Join (";", value) : string.Empty;
			}
		}
		
		/// <summary>
		///    Gets and sets a user comment on the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing user comments
		///    on the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    When stored on disk, only the first 48 bytes of the
		///    Latin-1 encoded value will be stored. This may result in
		///    lost data.
		/// </remarks>
		public override string Comment {
			get {
				return string.IsNullOrEmpty (comment) ?
					null : comment;
			}
			set {
				comment = value != null ?
					value.Trim () : String.Empty;
			}
		}
		
		/// <summary>
		///    Gets and sets the genres of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the genres of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    Only first genre will be stored and only if it is an
		///    exact match for a value appearing in <see
		///    cref="TagLib.Genres.Video" />. All other values will
		///    result in the property being cleared.
		/// </remarks>
		public override string [] Genres {
			get {
				string genre_name =
					TagLib.Genres.IndexToVideo (genre);
				
				return (genre_name != null) ?
					new string [] {genre_name} :
					new string [0];
			}
			set {
				genre = (value != null && value.Length > 0) ?
					TagLib.Genres.VideoToIndex (
						value [0].Trim ()).ToString (
							CultureInfo.InvariantCulture)
					: string.Empty;
			}
		}
		
		/// <summary>
		///    Gets and sets the year that the media represented by the
		///    current instance was recorded.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the year that the media
		///    represented by the current instance was created or zero
		///    if no value is present.
		/// </value>
		/// <remarks>
		///    Only values between 1 and 9999 will be stored, all other
		///    values will result in the property being zeroed.
		/// </remarks>
		public override uint Year {
			get {
				uint value;
				return uint.TryParse (year,
					NumberStyles.Integer,
					CultureInfo.InvariantCulture,
					out value) ? value : 0;
			}
			
			set {
				year = (value > 0 && value < 10000) ?
					value.ToString (
						CultureInfo.InvariantCulture) :
					String.Empty;
			}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			title = artist = genre = year = comment = String.Empty;
			extra_data = new ByteVector (6);
		}
		
#endregion
	}
}
