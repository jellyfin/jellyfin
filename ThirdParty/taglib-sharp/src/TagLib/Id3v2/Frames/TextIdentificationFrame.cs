//
// TextInformationFrame.cs: Provides support ID3v2 Text Information Frames
// (Section 4.2), covering "T000" to "TZZZ", excluding "TXXX".
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   textidentificationframe.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002,2003 Scott Wheeler (Original Implementation)
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
using System.Globalization;
using System.Text;

namespace TagLib.Id3v2 {
	/// <summary>
	///    This class extends <see cref="Frame" /> to provide support ID3v2
	///    Text Information Frames (Section 4.2), covering "<c>T000</c>" to
	///    "<c>TZZZ</c>", excluding "<c>TXXX</c>".
	/// </summary>
	/// <remarks>
	///    <para>Text Information Frames contain the most commonly used
	///    values in tagging, including the artist, the track name, and just
	///    about any value that can be expressed as text.</para>
	///    <para>The following table contains types and descriptions as
	///    found in the ID3 2.4.0 native frames specification. (Copyright
	///    (C) Martin Nilsson 2000.)</para>
	///    
	///    <list type="table">
	///       <listheader>
	///          <term>ID</term>
	///          <description>Description</description>
	///       </listheader>
	///       <item>
	///          <term>TIT1</term>
	///          <description>The 'Content group description' frame is used
	///          if the sound belongs to a larger category of sounds/music.
	///          For example, classical music is often sorted in different
	///          musical sections (e.g. "Piano Concerto", "Weather -
	///          Hurricane").</description>
	///       </item>
	///       <item>
	///          <term>TIT2</term>
	///          <description>The 'Title/Songname/Content description' frame
	///          is the actual name of the piece (e.g. "Adagio", "Hurricane
	///          Donna").</description>
	///       </item>
	///       <item>
	///          <term>TIT3</term>
	///          <description>The 'Subtitle/Description refinement' frame is
	///          used for information directly related to the contents title
	///          (e.g. "Op. 16" or "Performed live at
	///          Wembley").</description>
	///       </item>
	///       <item>
	///          <term>TALB</term>
	///          <description>The 'Album/Movie/Show title' frame is intended
	///          for the title of the recording (or source of sound) from
	///          which the audio in the file is taken.</description>
	///       </item>
	///       <item>
	///          <term>TOAL</term>
	///          <description>The 'Original album/movie/show title' frame is
	///          intended for the title of the original recording (or source
	///          of sound), if for example the music in the file should be a
	///          cover of a previously released song.</description>
	///       </item>
	///       <item>
	///          <term>TRCK</term>
	///          <description>The 'Track number/Position in set' frame is a
	///          numeric string containing the order number of the
	///          audio-file on its original recording. This MAY be extended
	///          with a "/" character and a numeric string containing the
	///          total number of tracks/elements on the original recording.
	///          E.g. "4/9".</description>
	///       </item>
	///       <item>
	///          <term>TPOS</term>
	///          <description>The 'Part of a set' frame is a numeric string
	///          that describes which part of a set the audio came from.
	///          This frame is used if the source described in the "TALB"
	///          frame is divided into several mediums, e.g. a double CD.
	///          The value MAY be extended with a "/" character and a
	///          numeric string containing the total number of parts in the
	///          set. E.g. "1/2".</description>
	///       </item>
	///       <item>
	///          <term>TSST</term>
	///          <description>The 'Set subtitle' frame is intended for the
	///          subtitle of the part of a set this track belongs
	///          to.</description>
	///       </item>
	///       <item>
	///          <term>TSRC</term>
	///          <description>The 'ISRC' frame should contain the
	///          International Standard Recording Code [ISRC] (12
	///          characters).</description>
	///       </item>
	///       <item>
	///          <term>TPE1</term>
	///          <description>The
	///          'Lead artist/Lead performer/Soloist/Performing group' is
	///          used for the main artist.</description>
	///       </item>
	///       <item>
	///          <term>TPE2</term>
	///          <description>The 'Band/Orchestra/Accompaniment' frame is
	///          used for additional information about the performers in the
	///          recording.</description>
	///       </item>
	///       <item>
	///          <term>TPE3</term>
	///          <description>The 'Conductor' frame is used for the name of
	///          the conductor.</description>
	///       </item>
	///       <item>
	///          <term>TPE4</term>
	///          <description>The 'Interpreted, remixed, or otherwise
	///          modified by' frame contains more information about the
	///          people behind a remix and similar interpretations of
	///          another existing piece.</description>
	///       </item>
	///       <item>
	///          <term>TOPE</term>
	///          <description>The 'Original artist/performer' frame is
	///          intended for the performer of the original recording, if
	///          for example the music in the file should be a cover of a
	///          previously released song.</description>
	///       </item>
	///       <item>
	///          <term>TEXT</term>
	///          <description>The 'Lyricist/Text writer' frame is intended
	///          for the writer of the text or lyrics in the
	///          recording.</description>
	///       </item>
	///       <item>
	///          <term>TOLY</term>
	///          <description>The 'Original lyricist/text writer' frame is
	///          intended for the text writer of the original recording, if
	///          for example the music in the file should be a cover of a
	///          previously released song.</description>
	///       </item>
	///       <item>
	///          <term>TCOM</term>
	///          <description>The 'Composer' frame is intended for the name
	///          of the composer.</description>
	///       </item>
	///       <item>
	///          <term>TMCL</term>
	///          <description>The 'Musician credits list' is intended as a
	///          mapping between instruments and the musician that played
	///          it. Every odd field is an instrument and every even is an
	///          artist or a comma delimited list of artists.</description>
	///       </item>
	///       <item>
	///          <term>TIPL</term>
	///          <description>The 'Involved people list' is very similar to
	///          the musician credits list, but maps between functions, like
	///          producer, and names.</description>
	///       </item>
	///       <item>
	///          <term>TENC</term>
	///          <description>The 'Encoded by' frame contains the name of
	///          the person or organisation that encoded the audio file.
	///          This field may contain a copyright message, if the audio
	///          file also is copyrighted by the encoder.</description>
	///       </item>
	///       <item>
	///          <term>TBPM</term>
	///          <description>The 'BPM' frame contains the number of beats
	///          per minute in the main part of the audio. The BPM is an
	///          integer and represented as a numerical
	///          string.</description>
	///       </item>
	///       <item>
	///          <term>TLEN</term>
	///          <description>The 'Length' frame contains the length of the
	///          audio file in milliseconds, represented as a numeric
	///          string.</description>
	///       </item>
	///       <item>
	///          <term>TKEY</term>
	///          <description>The 'Initial key' frame contains the musical
	///          key in which the sound starts. It is represented as a
	///          string with a maximum length of three characters. The
	///          ground keys are represented with "A","B","C","D","E", "F"
	///          and "G" and halfkeys represented with "b" and "#". Minor is
	///          represented as "m", e.g. "Dbm". Off key is represented with
	///          an "o" only.</description>
	///       </item>
	///       <item>
	///          <term>TLAN</term>
	///          <description>The 'Language' frame should contain the
	///          languages of the text or lyrics spoken or sung in the
	///          audio. The language is represented with three characters
	///          according to ISO-639-2. If more than one language is used
	///          in the text their language codes should follow according to
	///          the amount of their usage.</description>
	///       </item>
	///       <item>
	///          <term>TCON</term>
	///          <description>The 'Content type', which ID3v1 was stored as
	///          a one byte numeric value only, is now a string. You may use
	///          one or several of the ID3v1 types as numerical strings, or,
	///          since the category list would be impossible to maintain
	///          with accurate and up to date categories, define your
	///          own.</description>
	///       </item>
	///       <item>
	///          <term>TFLT</term>
	///          <description>The 'File type' frame indicates which type of
	///          audio this tag defines. (See the specification for more
	///          details.)</description>
	///       </item>
	///       <item>
	///          <term>TMED</term>
	///          <description>The 'Media type' frame describes from which
	///          media the sound originated. (See the specification for more
	///          details.)</description>
	///       </item>
	///       <item>
	///          <term>TMOO</term>
	///          <description>The 'Mood' frame is intended to reflect the
	///          mood of the audio with a few keywords, e.g. "Romantic" or
	///          "Sad".</description>
	///       </item>
	///       <item>
	///          <term>TCOP</term>
	///          <description>The 'Copyright message' frame, in which the
	///          string must begin with a year and a space character (making
	///          five characters), is intended for the copyright holder of
	///          the original sound, not the audio file itself. The absence
	///          of this frame means only that the copyright information is
	///          unavailable or has been removed, and must not be
	///          interpreted to mean that the audio is public domain. Every
	///          time this field is displayed the field must be preceded
	///          with "Copyright " (C) " ", where (C) is one character
	///          showing a C in a circle.</description>
	///       </item>
	///       <item>
	///          <term>TPRO</term>
	///          <description>The 'Produced notice' frame, in which the
	///          string must begin with a year and a space character (making
	///          five characters), is intended for the production copyright
	///          holder of the original sound, not the audio file itself.
	///          The absence of this frame means only that the production
	///          copyright information is unavailable or has been removed,
	///          and must not be interpreted to mean that the audio is
	///          public domain. Every time this field is displayed the field
	///          must be preceded with "Produced " (P) " ", where (P) is one
	///          character showing a P in a circle.</description>
	///       </item>
	///       <item>
	///          <term>TPUB</term>
	///          <description>The 'Publisher' frame simply contains the name
	///          of the label or publisher.</description>
	///       </item>
	///       <item>
	///          <term>TOWN</term>
	///          <description>The 'File owner/licensee' frame contains the
	///          name of the owner or licensee of the file and it's
	///          contents.</description>
	///       </item>
	///       <item>
	///          <term>TRSN</term>
	///          <description>The 'Internet radio station name' frame
	///          contains the name of the internet radio station from which
	///          the audio is streamed.</description>
	///       </item>
	///       <item>
	///          <term>TRSO</term>
	///          <description>The 'Internet radio station owner' frame
	///          contains the name of the owner of the internet radio
	///          station from which the audio is streamed.</description>
	///       </item>
	///       <item>
	///          <term>TOFN</term>
	///          <description>The 'Original filename' frame contains the
	///          preferred filename for the file, since some media doesn't
	///          allow the desired length of the filename. The filename is
	///          case sensitive and includes its suffix.</description>
	///       </item>
	///       <item>
	///          <term>TDLY</term>
	///          <description>The 'Playlist delay' defines the numbers of
	///          milliseconds of silence that should be inserted before this
	///          audio. The value zero indicates that this is a part of a
	///          multifile audio track that should be played
	///          continuously.</description>
	///       </item>
	///       <item>
	///          <term>TDEN</term>
	///          <description>The 'Encoding time' frame contains a timestamp
	///          describing when the audio was encoded. Timestamp format is
	///          described in the ID3v2 structure document.</description>
	///       </item>
	///       <item>
	///          <term>TDOR</term>
	///          <description>The 'Original release time' frame contains a
	///          timestamp describing when the original recording of the
	///          audio was released. Timestamp format is described in the
	///          ID3v2 structure document.</description>
	///       </item>
	///       <item>
	///          <term>TDRC</term>
	///          <description>The 'Recording time' frame contains a
	///          timestamp describing when the audio was recorded. Timestamp
	///          format is described in the ID3v2 structure
	///          document.</description>
	///       </item>
	///       <item>
	///          <term>TDRL</term>
	///          <description>The 'Release time' frame contains a timestamp
	///          describing when the audio was first released. Timestamp
	///          format is described in the ID3v2 structure
	///          document.</description>
	///       </item>
	///       <item>
	///          <term>TDTG</term>
	///          <description>The 'Tagging time' frame contains a timestamp
	///          describing then the audio was tagged. Timestamp format is
	///          described in the ID3v2 structure document.</description>
	///       </item>
	///       <item>
	///          <term>TSSE</term>
	///          <description>The 'Software/Hardware and settings used for
	///          encoding' frame includes the used audio encoder and its
	///          settings when the file was encoded. Hardware refers to
	///          hardware encoders, not the computer on which a program was
	///          run.</description>
	///       </item>
	///       <item>
	///          <term>TSOA</term>
	///          <description>The 'Album sort order' frame defines a string
	///          which should be used instead of the album name (TALB) for
	///          sorting purposes. E.g. an album named "A Soundtrack" might
	///          preferably be sorted as "Soundtrack".</description>
	///       </item>
	///       <item>
	///          <term>TSOP</term>
	///          <description>The 'Performer sort order' frame defines a
	///          string which should be used instead of the performer (TPE2)
	///          for sorting purposes.</description>
	///       </item>
	///       <item>
	///          <term>TSOT</term>
	///          <description>The 'Title sort order' frame defines a string
	///          which should be used instead of the title (TIT2) for
	///          sorting purposes.</description>
	///       </item>
	///    </list>
	/// </remarks>
	public class TextInformationFrame : Frame
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the encoding to use for the text.
		/// </summary>
		private StringType encoding = Id3v2.Tag.DefaultEncoding;
		
		/// <summary>
		///    Contains the text fields.
		/// </summary>
		private string [] text_fields = new string [0];
		
		/// <summary>
		///    Contains the raw data from the frame, or <see
		///    langword="null" /> if it has been processed.
		/// </summary>
		/// <remarks>
		///    Rather than processing the data when the frame is loaded,
		///    it is parsed on demand, reducing the ammount of
		///    unnecessary conversion.
		/// </remarks>
		private ByteVector raw_data = null;
		
		/// <summary>
		///    Contains the ID3v2 version of <see cref="raw_data" />.
		/// </summary>
		private byte raw_version = 0;
		
		/// <summary>
		///    Contains the Encoding of the raw_data
		/// </summary>
		private StringType raw_encoding = StringType.Latin1;

		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TextInformationFrame" /> with a specified
		///    identifier and text encoding.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing an ID3v2.4
		///    frame identifier.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="StringType" /> value specifying the encoding
		///    to use for the new instance.
		/// </param>
		public TextInformationFrame (ByteVector ident,
		                             StringType encoding)
			: base (ident, 4)
		{
			this.encoding = encoding;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TextInformationFrame" /> with a specified
		///    identifer.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing an ID3v2.4
		///    frame identifier.
		/// </param>
		public TextInformationFrame (ByteVector ident)
			: this (ident, Id3v2.Tag.DefaultEncoding)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TextInformationFrame" /> by reading its raw
		///    contents in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the
		///    frame to read.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    in which <paramref name="data" /> is encoded.
		/// </param>
		public TextInformationFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TextInformationFrame" /> by reading its raw
		///    contents from a specifed position in a <see
		///    cref="ByteVector" /> object in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the frame
		///    to read.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the offset in
		///    <paramref name="data" /> at which the frame begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> value containing the header
		///    that would be read in the frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    in which <paramref name="data" /> is encoded.
		/// </param>
		protected internal TextInformationFrame (ByteVector data,
		                                         int offset,
		                                         FrameHeader header,
		                                         byte version)
			: base (header)
		{
			SetData (data, offset, version, false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the text contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="StringCollection" /> object containing the
		///    text contained in the current instance.
		/// </value>
		/// <remarks>
		///    Modifying the contents of the returned value will not
		///    modify the contents of the current instance.
		/// </remarks>
		[Obsolete("Use TextInformationFrame.Text")]
		public StringCollection FieldList {
			get {
				ParseRawData ();
				return new StringCollection (Text);
			}
		}

		/// <summary>
		///    Gets and sets the text contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the text contained
		///    in the current instance.
		/// </value>
		/// <remarks>
		///    <para>Modifying the contents of the returned value will
		///    not modify the contents of the current instance. The
		///    value must be reassigned for the value to change.</para>
		/// </remarks>
		/// <example>
		///    <para>Modifying the values text values of a frame.</para>
		///    <code> TextInformationFrame frame = TextInformationFrame.Get (myTag, "TPE1", true);
		/// /* Upper casing all the text: */
		/// string[] text = frame.Text;
		/// for (int i = 0; i &lt; text.Length; i++)
		///	text [i] = text [i].ToUpper ();
		/// frame.Text = text;
		///
		/// /* Replacing the value completely: */
		/// frame.Text = new string [] {"DJ Jazzy Jeff"};</code>
		/// </example>
		public virtual string [] Text {
			get {
				ParseRawData ();
				return (string[]) text_fields.Clone ();
			}
			set {
				raw_data = null;
				text_fields = value != null ?
					(string[]) value.Clone () :
					new string [0];
			}
		}
		
		/// <summary>
		///    Gets and sets the text encoding to use when rendering
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="StringType" /> value specifying the encoding
		///    to use when rendering the current instance.
		/// </value>
		/// <remarks>
		///    This value will be overwritten if <see
		///    cref="TagLib.Id3v2.Tag.ForceDefaultEncoding" /> is <see
		///    langword="true" />.
		/// </remarks>
		public StringType TextEncoding {
			get {
				ParseRawData ();
				return encoding;
			}
			set {encoding = value;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Sets the text contained in the current instance.
		/// </summary>
		/// <param name="fields">
		///    A <see cref="StringCollection" /> object containing text
		///    to store in the current instance.
		/// </param>
		[Obsolete("Use TextInformationFrame.Text")]
		public void SetText (StringCollection fields)
		{
			raw_data = null;
			Text = fields != null ? fields.ToArray () : null;
		}
		
		/// <summary>
		///    Sets the text contained in the current instance.
		/// </summary>
		/// <param name="text">
		///    A <see cref="T:string[]" /> containing text to store in the
		///    current instance.
		/// </param>
		[Obsolete("Use TextInformationFrame.Text")]
		public void SetText (params string [] text)
		{
			raw_data = null;
			Text = text;
		}
		
		/// <summary>
		///    Gets a string representation of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the joined text.
		/// </returns>
		public override string ToString ()
		{
			ParseRawData ();
			return string.Join ("; ", Text);
		}
		
		/// <summary>
		///    Renders the current instance, encoded in a specified
		///    ID3v2 version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> value specifying the version of
		///    ID3v2 to use when encoding the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public override ByteVector Render (byte version)
		{
			if (version != 3 || FrameId != FrameType.TDRC)
				return base.Render (version);
			
			string text = ToString ();
			if (text.Length < 10 || text [4] != '-' ||
				text [7] != '-')
				return base.Render (version);
			
			ByteVector output = new ByteVector ();
			TextInformationFrame f;
			
			f = new TextInformationFrame (FrameType.TYER, encoding);
			f.Text = new string [] {text.Substring (0, 4)};
			output.Add (f.Render (version));
			
			f = new TextInformationFrame (FrameType.TDAT, encoding);
			f.Text = new string [] {
				text.Substring (5, 2) + text.Substring (8, 2)
			};
			output.Add (f.Render (version));
			
			if (text.Length < 16 || text [10] != 'T' ||
				text [13] != ':')
				return output;
			
			f = new TextInformationFrame (FrameType.TIME, encoding);
			f.Text = new string [] {
				text.Substring (11, 2) + text.Substring (14, 2)
			};
			output.Add (f.Render (version));
			
			return output;
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets a <see cref="TextInformationFrame" /> object of a
		///    specified type from a specified tag, optionally creating
		///    and adding one with a specified encoding if none is
		///    found.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search for the specified
		///    tag in.
		/// </param>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifer to search for.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="StringType" /> value specifying the encoding
		///    to use if a new frame is created.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    create a new frame if an existing frame was not found.
		/// </param>
		/// <returns>
		///    A <see cref="TextInformationFrame" /> object containing
		///    the frame found in or added to <paramref name="tag" /> or
		///    <see langword="null" /> if no value was found <paramref
		///    name="create" /> is <see langword="false" />.
		/// </returns>
		/// <remarks>
		///    To create a frame without having to specify the encoding,
		///    use <see cref="Get(Tag,ByteVector,bool)" />.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="tag" /> or <paramref name="ident" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public static TextInformationFrame Get (Tag tag,
		                                        ByteVector ident,
		                                        StringType encoding,
		                                        bool create)
		{
			if (tag == null)
				throw new ArgumentNullException ("tag");
			
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			foreach (TextInformationFrame frame in
				tag.GetFrames<TextInformationFrame> (ident))
				return frame;
			
			if (!create)
				return null;
			
			TextInformationFrame new_frame =
				new TextInformationFrame (ident, encoding);
			tag.AddFrame (new_frame);
			return new_frame;
		}
		
		/// <summary>
		///    Gets a <see cref="TextInformationFrame" /> object of a
		///    specified type from a specified tag, optionally creating
		///    and adding one if none is found.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search for the specified
		///    tag in.
		/// </param>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifer to search for.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    create a new frame if an existing frame was not found.
		/// </param>
		/// <returns>
		///    A <see cref="TextInformationFrame" /> object containing
		///    the frame found in or added to <paramref name="tag" /> or
		///    <see langword="null" /> if no value was found <paramref
		///    name="create" /> is <see langword="false" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="tag" /> or <paramref name="ident" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public static TextInformationFrame Get (Tag tag,
		                                        ByteVector ident,
		                                        bool create)
		{
			return Get (tag, ident, Tag.DefaultEncoding, create);
		}
		
		/// <summary>
		///    Gets a <see cref="TextInformationFrame" /> object of a
		///    specified type from a specified tag.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search for the specified
		///    tag in.
		/// </param>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifer to search for.
		/// </param>
		/// <returns>
		///    A <see cref="TextInformationFrame" /> object containing
		///    the frame found in <paramref name="tag" /> or <see
		///    langword="null" /> if no value was found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="tag" /> or <paramref name="ident" /> is
		///    <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		[Obsolete("Use TextInformationFrame.Get(Tag,ByteVector,bool)")]
		public static TextInformationFrame Get (Tag tag,
		                                        ByteVector ident)
		{
			return Get (tag, ident, false);
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
		protected override void ParseFields (ByteVector data,
		                                     byte version)
		{
			raw_data = data;
			raw_version = version;

			// read the string data type (the first byte of the
			// field data)
			raw_encoding = (StringType)data[0];
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
			if (raw_data == null)
				return;
			
			ByteVector data = raw_data;
			raw_data = null;
			
			// read the string data type (the first byte of the
			// field data)
			encoding = (StringType) data [0];
			List<string> field_list = new List<string> ();
			
			ByteVector delim = ByteVector.TextDelimiter (encoding);
			
			if (raw_version > 3 || FrameId == FrameType.TXXX) {
				field_list.AddRange (data.ToStrings (encoding, 1));
			} else if (data.Count > 1 && !data.Mid (1,
				delim.Count).Equals (delim)) {
				string value = data.ToString (encoding, 1,
					data.Count - 1);
				
				// Truncate values containing NULL bytes
				int null_index = value.IndexOf ('\x00');
				if (null_index >= 0) {
					value = value.Substring (0, null_index);
				}
				
				if (FrameId == FrameType.TCOM ||
					FrameId == FrameType.TEXT ||
					FrameId == FrameType.TMCL ||
					FrameId == FrameType.TOLY ||
					FrameId == FrameType.TOPE ||
					FrameId == FrameType.TSOC ||
					FrameId == FrameType.TSOP ||
					FrameId == FrameType.TSO2 ||
					FrameId == FrameType.TPE1 ||
					FrameId == FrameType.TPE2 ||
					FrameId == FrameType.TPE3 ||
					FrameId == FrameType.TPE4) {
					field_list.AddRange (value.Split ('/'));
				} else if (FrameId == FrameType.TCON) {
					while (value.Length > 1 && value [0] == '(') {
						int closing = value.IndexOf (')');
						if (closing < 0)
							break;
						
						string number = value.Substring (1,
								closing - 1);
						
						field_list.Add (number);
						
						value = value.Substring (
							closing + 1).TrimStart ('/', ' ');
						
						string text = Genres.IndexToAudio (number);
						if (text != null && value.StartsWith (text))
							value = value.Substring (text.Length)
								.TrimStart ('/', ' ');
					}
					
					if (value.Length > 0)
						field_list.AddRange (value.Split (new char [] {'/', ';'}));
				} else {
					field_list.Add (value);
				}
			}
			
			// Bad tags may have one or more nul characters at the
			// end of a string, resulting in empty strings at the
			// end of the FieldList. Strip them off.
			while (field_list.Count != 0 &&
				string.IsNullOrEmpty (field_list [
					field_list.Count - 1]))
				field_list.RemoveAt (field_list.Count - 1);
			
			text_fields = field_list.ToArray ();
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
		protected override ByteVector RenderFields (byte version) {
			if (raw_data != null && raw_version == version && raw_encoding == Tag.DefaultEncoding)
				return raw_data;
			
			StringType encoding = CorrectEncoding (TextEncoding,
				version);
			ByteVector v = new ByteVector ((byte) encoding);
			string [] text = text_fields;
			
			bool txxx = FrameId == FrameType.TXXX;
			
			if (version > 3 || txxx) {
				
				if (txxx) {
					if (text.Length == 0)
						text = new string [] {null, null};
					else if (text.Length == 1)
						text = new string [] {text [0],
							null};
				}
				
				for (int i = 0; i < text.Length; i++) {
					// Since the field list is null
					// delimited, if this is not the first
					// element in the list, append the
					// appropriate delimiter for this
					// encoding.
					
					if (i != 0)
						v.Add (ByteVector.TextDelimiter (
							encoding));
						
					if (text [i] != null)
						v.Add (ByteVector.FromString (
							text [i],
							encoding));
				}
			} else if (FrameId == FrameType.TCON) {
				byte id;
				bool prev_value_indexed = true;
				StringBuilder data = new StringBuilder ();
				foreach (string s in text) {
					if (!prev_value_indexed) {
						data.Append (";").Append (s);
						continue;
					}
					
					if (prev_value_indexed =
						byte.TryParse (s, out id))
						data.AppendFormat (
							CultureInfo.InvariantCulture,
								"({0})", id);
					else
						data.Append (s);
				}
				
				v.Add (ByteVector.FromString (data.ToString (),
					encoding));
			} else {
				v.Add (ByteVector.FromString (
					string.Join ("/", text), encoding));
			}
			
			return v;
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
			TextInformationFrame frame =
				(this is UserTextInformationFrame) ?
				new UserTextInformationFrame (null, encoding) :
				new TextInformationFrame (FrameId, encoding);
			frame.text_fields = (string[]) text_fields.Clone ();
			if (raw_data != null)
				frame.raw_data = new ByteVector (raw_data);
			frame.raw_version = raw_version;
			return frame;
		}
		
#endregion
	}
	
	
	
	/// <summary>
	///    This class extends <see cref="TextInformationFrame" /> to provide
	///    support for ID3v2 User Text Information (TXXX) Frames.
	/// </summary>
	public class UserTextInformationFrame : TextInformationFrame
	{
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UserTextInformationFrame" /> with a specified
		///    description and text encoding.
		/// </summary>
		/// <param name="description">
		///    A <see cref="string" /> containing the description of the
		///    new frame.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="StringType" /> containing the text encoding
		///    to use when rendering the new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see
		///    cref="Get(Tag,string,StringType,bool)" /> for more
		///    integrated frame creation.
		/// </remarks>
		public UserTextInformationFrame (string description,
		                                 StringType encoding)
			: base (FrameType.TXXX, encoding)
		{
			base.Text = new string [] {description};
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UserTextInformationFrame" /> with a specified
		///    description.
		/// </summary>
		/// <param name="description">
		///    A <see cref="string" /> containing the description of the
		///    new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see
		///    cref="Get(Tag,string,bool)" /> for more integrated frame
		///    creation.
		/// </remarks>
		public UserTextInformationFrame (string description)
			: base (FrameType.TXXX)
		{
			base.Text = new string [] {description};
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UserTextInformationFrame" /> by reading its raw
		///    data in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public UserTextInformationFrame (ByteVector data, byte version)
			: base (data, version)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UserTextInformationFrame" /> by reading its raw
		///    data in a specified ID3v2 version.
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
		protected internal UserTextInformationFrame (ByteVector data,
		                                             int offset,
		                                             FrameHeader header,
		                                             byte version)
			: base (data, offset, header, version)
		{
		}
		
#endregion
		
		
		
#region Public Properties
		
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
		///    description per tag.
		/// </remarks>
		public string Description {
			get {
				string [] text = base.Text;
				return text.Length > 0 ? text [0] : null;
			}
			
			set {
				string [] text = base.Text;
				if (text.Length > 0)
					text [0] = value;
				else
					text = new string [] {value};
				
				base.Text = text;
			}
		}
		
		/// <summary>
		///    Gets and sets the text contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the text contained
		///    in the current instance.
		/// </value>
		/// <remarks>
		///    <para>Modifying the contents of the returned value will
		///    not modify the contents of the current instance. The
		///    value must be reassigned for the value to change.</para>
		/// </remarks>
		public override string [] Text {
			get {
				string [] text = base.Text;
				if (text.Length < 2)
					return new string [0];
				
				string [] new_text = new string [text.Length - 1];
				for (int i = 0; i < new_text.Length; i ++)
					new_text [i] = text [i+1];
				
				return new_text;
			}
			set {
				string [] new_value = new string [
					value != null ? (value.Length + 1) : 1];
				
				new_value [0] = Description;
				
				for (int i = 1; i < new_value.Length; i ++)
					new_value [i] = value [i - 1];
				
				base.Text = new_value;
			}
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Gets a string representation of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the joined text.
		/// </returns>
		public override string ToString ()
		{
			return new StringBuilder ().Append ("[")
				.Append (Description)
				.Append ("] ")
				.Append (base.ToString ()).ToString ();
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Gets a specified user text frame from the specified tag,
		///    optionally creating it if it does not exist and optionally
		///    searching for the frame case-insensitive.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="description">
		///    A <see cref="string" /> specifying the description to
		///    match.
		/// </param>
		/// <param name="type">
		///    A <see cref="StringType" /> specifying the encoding to
		///    use if creating a new frame.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <param name="caseSensitive">
		///    A <see cref="bool" /> specifying whether or not to search
		///    for the frame case-sensitive.
		/// </param>
		/// <returns>
		///    A <see cref="UserTextInformationFrame" /> object
		///    containing the matching frame, or <see langword="null" />
		///    if a match wasn't found and <paramref name="create" /> is
		///    <see langword="false" />.
		/// </returns>
		public static UserTextInformationFrame Get (Tag tag,
		                                            string description,
		                                            StringType type,
		                                            bool create,
		                                            bool caseSensitive)
		{
			if (tag == null)
				throw new ArgumentNullException ("tag");
			
			if (description == null)
				throw new ArgumentNullException ("description");
			
			if (description.Length == 0)
				throw new ArgumentException (
					"Description must not be empty.",
					"description");
					
			StringComparison stringComparison =
				caseSensitive ? StringComparison.InvariantCulture :
					StringComparison.InvariantCultureIgnoreCase;
			
			foreach (UserTextInformationFrame frame in
				tag.GetFrames<UserTextInformationFrame> (
					FrameType.TXXX))
				if (description.Equals (frame.Description, stringComparison))
					return frame;
			
			if (!create)
				return null;
			
			UserTextInformationFrame new_frame =
				new UserTextInformationFrame (description,
					type);
			tag.AddFrame (new_frame);
			return new_frame;
		}
		
		/// <summary>
		///    Gets a specified user text frame from the specified tag,
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
		///    A <see cref="StringType" /> specifying the encoding to
		///    use if creating a new frame.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="UserTextInformationFrame" /> object
		///    containing the matching frame, or <see langword="null" />
		///    if a match wasn't found and <paramref name="create" /> is
		///    <see langword="false" />.
		/// </returns>
		public static UserTextInformationFrame Get (Tag tag,
		                                            string description,
		                                            StringType type,
		                                            bool create)
		{
			return Get (tag, description, type, create, true);
		}
		
		/// <summary>
		///    Gets a specified user text frame from the specified tag,
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
		///    A <see cref="UserTextInformationFrame" /> object
		///    containing the matching frame, or <see langword="null" />
		///    if a match wasn't found and <paramref name="create" /> is
		///    <see langword="false" />.
		/// </returns>
		public static UserTextInformationFrame Get (Tag tag,
		                                            string description,
		                                            bool create)
		{
			return Get (tag, description, Tag.DefaultEncoding,
				create);
		}
		
		/// <summary>
		///    Gets a specified user text frame from the specified tag.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="description">
		///    A <see cref="string" /> specifying the description to
		///    match.
		/// </param>
		/// <returns>
		///    A <see cref="UserTextInformationFrame" /> object
		///    containing the matching frame, or <see langword="null" />
		///    if a match wasn't found.
		/// </returns>
		[Obsolete("Use UserTextInformationFrame.Get(Tag,string,bool)")]
		public static UserTextInformationFrame Get (Tag tag,
		                                            string description)
		{
			return Get (tag, description, false);
		}
		
#endregion
	}
}
