//
// UrlLinkFrame.cs: Provides support ID3v2 Url Link Frames
// (Section 4.3.1), covering "W000" to "WZZZ", excluding "WXXX".
//
// Author:
//   Helmut Wahrmann
//
// Original Source:
//   textidentificationframe.cpp from TagLib
//
// Copyright (C) 2008 Helmut Wahrmann
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
using System.Text;

namespace TagLib.Id3v2
{
		/// <summary>
		///    This class extends <see cref="Frame" /> to provide support ID3v2
		///    Url Link Frames (Section 4.3.1), covering "<c>W000</c>" to
		///    "<c>WZZZ</c>", excluding "<c>WXXX</c>".
		/// </summary>
		/// <remarks>
		///    <para>With these frames dynamic data such as webpages with touring
		///    information, price information or plain ordinary news can be added to
		///    the tag. There may only be one URL [URL] link frame of its kind in an
		///    tag, except when stated otherwise in the frame description. If the
		///    text string is followed by a string termination, all the following
		///    information should be ignored and not be displayed.</para>
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
		///          <term>WCOM</term>
		///          <description>The 'Commercial information' frame is a URL pointing at a webpage
		///          with information such as where the album can be bought. There may be
		///          more than one "WCOM" frame in a tag, but not with the same content.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WCOP</term>
		///          <description>The 'Copyright/Legal information' frame is a URL pointing at a
		///          webpage where the terms of use and ownership of the file is described.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WOAF</term>
		///          <description>The 'Official audio file webpage' frame is a URL pointing at a file
		///          specific webpage.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WOAR</term>
		///          <description>The 'Official artist/performer webpage' frame is a URL pointing at
		///          the artists official webpage. There may be more than one "WOAR" frame
		///          in a tag if the audio contains more than one performer, but not with
		///          the same content.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WOAS</term>
		///          <description>The 'Official audio source webpage' frame is a URL pointing at the
		///          official webpage for the source of the audio file, e.g. a movie.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WORS</term>
		///          <description>The 'Official Internet radio station homepage' contains a URL
		///          pointing at the homepage of the internet radio station.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WPAY</term>
		///          <description>The 'Payment' frame is a URL pointing at a webpage that will handle
		///          the process of paying for this file.
		///          </description>
		///       </item>
		///       <item>
		///          <term>WPUB</term>
		///          <description>The 'Publishers official webpage' frame is a URL pointing at the
		///          official webpage for the publisher.
		///          </description>
		///       </item>
		///    </list>
		/// </remarks>
		public class UrlLinkFrame : Frame
		{
				#region Private Fields

				/// <summary>
				///    Contains the encoding to use for the text.
				/// </summary>
				private StringType encoding = StringType.Latin1;

				/// <summary>
				///    Contains the text fields.
				/// </summary>
				private string [] text_fields = new string [0];

				/// <summary>
				///    Contains the raw data from the frame, or
				///    <see langword="null" /> if it has been processed.
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

				#endregion

				#region Constructors

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UrlLinkFrame" /> with a specified
				///    identifier and text encoding.
				/// </summary>
				/// <param name="ident">
				///    A <see cref="ByteVector" /> object containing an ID3v2.4
				///    frame identifier.
				/// </param>
				public UrlLinkFrame (ByteVector ident)
				  : base (ident, 4)
				{
				}

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UrlLinkFrame" /> by reading its raw
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
				public UrlLinkFrame (ByteVector data, byte version)
				  : base (data, version)
				{
						SetData (data, 0, version, true);
				}

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UrlLinkFrame" /> by reading its raw
				///    contents from a specifed position in a
				///    <see cref="ByteVector" /> object in a specified ID3v2 version.
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
				protected internal UrlLinkFrame (ByteVector data,
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
		///    <code> UrlLinkFrame frame = UrlLinkFrame.Get (myTag, "WCOP", true);
		/// /* Upper casing all the text: */
		/// string[] text = frame.Text;
		/// for (int i = 0; i &lt; text.Length; i++)
		///	text [i] = text [i].ToUpper ();
		/// frame.Text = text;
		///
		/// /* Replacing the value completely: */
		/// frame.Text = new string [] {"http://www.somewhere.com"};</code>
		/// </example>
		public virtual string [] Text {
						get {
								ParseRawData ();
								return (string [])text_fields.Clone ();
						}
						set {
								raw_data = null;
								text_fields = value != null ?
								  (string [])value.Clone () :
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
				///    This value will be overwritten if
				///    <see cref="TagLib.Id3v2.Tag.ForceDefaultEncoding" /> is
				///    <see langword="true" />.
				/// </remarks>
				public StringType TextEncoding {
						get {
								ParseRawData ();
								return encoding;
						}
						set { encoding = value; }
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
						return base.Render (version);
				}

		#endregion

		#region Public Static Methods

		/// <summary>
		///    Gets a <see cref="UrlLinkFrame" /> object of a
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
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    create a new frame if an existing frame was not found.
		/// </param>
		/// <returns>
		///    A <see cref="UrlLinkFrame" /> object containing
		///    the frame found in or added to <paramref name="tag" /> or
		///    <see langword="null" /> if no value was found
		///    <paramref name="create" /> is <see langword="false" />.
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
		public static UrlLinkFrame Get (Tag tag,
				                                ByteVector ident,
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

						foreach (UrlLinkFrame frame in
						  tag.GetFrames<UrlLinkFrame> (ident))
								return frame;

						if (!create)
								return null;

						UrlLinkFrame new_frame =
						  new UrlLinkFrame (ident);
						tag.AddFrame (new_frame);
						return new_frame;
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

						List<string> field_list = new List<string> ();

						ByteVector delim = ByteVector.TextDelimiter (encoding);

						if (FrameId != FrameType.WXXX) {
								field_list.AddRange (data.ToStrings (StringType.Latin1, 0));
						} else if (data.Count > 1 && !data.Mid (0,
							delim.Count).Equals (delim)) {
								string value = data.ToString (StringType.Latin1, 1,
								  data.Count - 1);

								// Do a fast removal of end bytes.
								if (value.Length > 1 &&
								  value [value.Length - 1] == 0)
										for (int i = value.Length - 1; i >= 0; i--)
												if (value [i] != 0) {
														value = value.Substring (0, i + 1);
														break;
												}

								field_list.Add (value);
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
				protected override ByteVector RenderFields (byte version)
				{
						if (raw_data != null && raw_version == version)
								return raw_data;

						StringType encoding = CorrectEncoding (TextEncoding,
						  version);

						bool wxxx = FrameId == FrameType.WXXX;

						ByteVector v;

						if (wxxx)
								v = new ByteVector ((byte)encoding);
						else
								v = new ByteVector ();
						string [] text = text_fields;

						if (version > 3 || wxxx) {
								if (wxxx) {
										if (text.Length == 0)
												text = new string [] { null, null };
										else if (text.Length == 1)
												text = new string [] {text [0],
							null};
								}

								v.Add (ByteVector.FromString (
								string.Join ("/", text), StringType.Latin1));
						} else {
								v.Add (ByteVector.FromString (
								  string.Join ("/", text), StringType.Latin1));
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
						UrlLinkFrame frame =
						  (this is UserUrlLinkFrame) ?
						  new UserUrlLinkFrame (null, encoding) :
						  new UrlLinkFrame (FrameId);
						frame.text_fields = (string [])text_fields.Clone ();
						if (raw_data != null)
								frame.raw_data = new ByteVector (raw_data);
						frame.raw_version = raw_version;
						return frame;
				}

				#endregion
		}



		/// <summary>
		///    This class extends <see cref="UrlLinkFrame" /> to provide
		///    support for ID3v2 User Url Link (WXXX) Frames.
		/// </summary>
		public class UserUrlLinkFrame : UrlLinkFrame
		{
				#region Constructors

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UserUrlLinkFrame" /> with a specified
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
				///    the tag. Consider using
				///    <see cref="Get(Tag,string,StringType,bool)" /> for more
				///    integrated frame creation.
				/// </remarks>
				public UserUrlLinkFrame (string description,
				                         StringType encoding)
				  : base (FrameType.WXXX)
				{
						base.Text = new string [] { description };
				}

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UserUrlLinkFrame" /> with a specified
				///    description.
				/// </summary>
				/// <param name="description">
				///    A <see cref="string" /> containing the description of the
				///    new frame.
				/// </param>
				/// <remarks>
				///    When a frame is created, it is not automatically added to
				///    the tag. Consider using
				///    <see cref="Get(Tag,string,bool)" /> for more integrated frame
				///    creation.
				/// </remarks>
				public UserUrlLinkFrame (string description)
				  : base (FrameType.WXXX)
				{
						base.Text = new string [] { description };
				}

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UserUrlLinkFrame" /> by reading its raw
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
				public UserUrlLinkFrame (ByteVector data, byte version)
				  : base (data, version)
				{
				}

				/// <summary>
				///    Constructs and initializes a new instance of
				///    <see cref="UserUrlLinkFrame" /> by reading its raw
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
				protected internal UserUrlLinkFrame (ByteVector data,
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
										text = new string [] { value };

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
								for (int i = 0; i < new_text.Length; i++)
										new_text [i] = text [i + 1];

								return new_text;
						}
						set {
								string [] new_value = new string [
								  value != null ? (value.Length + 1) : 1];

								new_value [0] = Description;

								for (int i = 1; i < new_value.Length; i++)
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
				///    A <see cref="UserUrlLinkFrame" /> object
				///    containing the matching frame, or <see langword="null" />
				///    if a match wasn't found and <paramref name="create" /> is
				///    <see langword="false" />.
				/// </returns>
				public static UserUrlLinkFrame Get (Tag tag,
				                                    string description,
				                                    StringType type,
				                                    bool create)
				{
						if (tag == null)
								throw new ArgumentNullException ("tag");

						if (description == null)
								throw new ArgumentNullException ("description");

						if (description.Length == 0)
								throw new ArgumentException (
								  "Description must not be empty.",
								  "description");

						foreach (UserUrlLinkFrame frame in
						  tag.GetFrames<UserUrlLinkFrame> (
							FrameType.WXXX))
								if (description.Equals (frame.Description))
										return frame;

						if (!create)
								return null;

						UserUrlLinkFrame new_frame =
						  new UserUrlLinkFrame (description,
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
				/// <param name="create">
				///    A <see cref="bool" /> specifying whether or not to create
				///    and add a new frame to the tag if a match is not found.
				/// </param>
				/// <returns>
				///    A <see cref="UserUrlLinkFrame" /> object
				///    containing the matching frame, or <see langword="null" />
				///    if a match wasn't found and <paramref name="create" /> is
				///    <see langword="false" />.
				/// </returns>
				public static UserUrlLinkFrame Get (Tag tag,
				                                    string description,
				                                    bool create)
				{
						return Get (tag, description, Tag.DefaultEncoding, create);
				}
				#endregion
		}
}
