//
// Tag.cs: Provide support for reading and writing ID3v2 tags.
//
// Authors:
//   Brian Nickel (brian.nickel@gmail.com)
//   Gabriel BUrt (gabriel.burt@gmail.com)
//
// Original Source:
//   id3v2tag.cpp from TagLib
//
// Copyright (C) 2010 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TagLib.Id3v2 {
	/// <summary>
	///    This class extends <see cref="TagLib.Tag" /> and implements <see
	///    cref="T:System.Collections.Generic.IEnumerable`1" /> to provide support for reading and
	///    writing ID3v2 tags.
	/// </summary>
	public class Tag : TagLib.Tag, IEnumerable<Frame>, ICloneable
	{
#region Private Static Fields
		
		/// <summary>
		///    Contains the language to use for language specific
		///    fields.
		/// </summary>
		private static string language = 
			CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
		
		/// <summary>
		///    Contains the field to use for new tags.
		/// </summary>
		private static byte default_version = 3;
		
		/// <summary>
		///    Indicates whether or not all tags should be saved in
		///    <see cref="default_version" />.
		/// </summary>
		private static bool force_default_version = false;
		
		/// <summary>
		///    Specifies the default string type to use for new frames.
		/// </summary>
		private static StringType default_string_type = StringType.UTF8;
		
		/// <summary>
		///    Specifies whether or not all frames shoudl be saved in
		///    <see cref="default_string_type" />.
		/// </summary>
		private static bool force_default_string_type = false;
		
		/// <summary>
		///    Specifies whether or not numeric genres should be used
		///    when available.
		/// </summary>
		private static bool use_numeric_genres = true;
		
#endregion
		
		
		
#region Private Fields
		
		/// <summary>
		///    Contains the tag's header.
		/// </summary>
		private Header header = new Header ();
		
		/// <summary>
		///    Contains the tag's extended header.
		/// </summary>
		private ExtendedHeader extended_header = null;
		
		/// <summary>
		///    Contains the tag's frames.
		/// </summary>
		private List<Frame> frame_list = new List<Frame> ();


		/// <summary>
		/// Store the PerformersRole property
		/// </summary>
		private string[] performers_role;

#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> with no contents.
		/// </summary>
		public Tag ()
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> by reading the contents from a specified
		///    position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File" /> object containing the file from
		///    which the contents of the new instance is to be read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the tag.
		/// </param>
		/// <param name="style">
		///    A <see cref="ReadStyle"/> value specifying how the media
		///    data is to be read into the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		public Tag (File file, long position, ReadStyle style)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Mode = TagLib.File.AccessMode.Read;
			
			if (position < 0 ||
				position > file.Length - Header.Size)
				throw new ArgumentOutOfRangeException (
					"position");
			
			Read (file, position, style);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> by reading the contents from a specified
		///    <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object to read the tag from.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> does not contain enough data.
		/// </exception>
		public Tag (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < Header.Size)
				throw new CorruptFileException (
					"Does not contain enough header data.");
			
			header = new Header (data);
			
			// If the tag size is 0, then this is an invalid tag.
			// Tags must contain at least one frame.
			
			if(header.TagSize == 0)
				return;
			
			if (data.Count - Header.Size < header.TagSize)
				throw new CorruptFileException (
					"Does not contain enough tag data.");
			
			Parse (data.Mid ((int) Header.Size,
				(int) header.TagSize), null, 0, ReadStyle.None);
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Gets the text value from a specified Text Information
		///    Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifier of the Text Information Frame to get the value
		///    from.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the text of the
		///    specified frame, or <see langword="null" /> if no value
		///    was found.
		/// </returns>
		public string GetTextAsString (ByteVector ident)
		{
			Frame frame;
			// Handle URL LInk frames differently
			if (ident[0] == 'W')
				frame = UrlLinkFrame.Get(
					this, ident, false);
			else
				frame = TextInformationFrame.Get (
					this, ident, false);
			
			string result = frame == null ? null : frame.ToString ();
			return string.IsNullOrEmpty (result) ? null : result;
		}
		
		/// <summary>
		///    Gets all frames contained in the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the frames.
		/// </returns>
		public IEnumerable<Frame> GetFrames ()
		{
			return frame_list;
		}
		
		/// <summary>
		///    Gets all frames with a specified identifier contained in
		///    the current instance.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frames to return.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the frames.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public IEnumerable<Frame> GetFrames (ByteVector ident)
		{
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			foreach (Frame f in frame_list)
				if (f.FrameId.Equals (ident))
					yield return f;
		}
		
		/// <summary>
		///    Gets all frames with of a specified type contained in
		///    the current instance.
		/// </summary>
		/// <typeparam name="T">
		///    The type of object, derived from <see cref="Frame" />,
		///    to return from in the current instance.
		/// </typeparam>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the frames.
		/// </returns>
		public IEnumerable<T> GetFrames <T> () where T : Frame
		{
			foreach (Frame f in frame_list) {
				T tf = f as T;
				if (tf != null)
					yield return tf;
			}
		}
		
		/// <summary>
		///    Gets all frames with a of type <typeparamref name="T" />
		///    with a specified identifier contained in the current
		///    instance.
		/// </summary>
		/// <typeparam name="T">
		///    The type of object, derived from <see cref="Frame" />,
		///    to return from in the current instance.
		/// </typeparam>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frames to return.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating
		///    through the frames.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public IEnumerable<T> GetFrames <T> (ByteVector ident)
			where T : Frame
		{
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			foreach (Frame f in frame_list) {
				T tf = f as T;
				if (tf != null && f.FrameId.Equals (ident))
					yield return tf;
			}
		}
		
		/// <summary>
		///    Adds a frame to the current instance.
		/// </summary>
		/// <param name="frame">
		///    A <see cref="Frame" /> object to add to the current
		///    instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="frame" /> is <see langword="null" />.
		/// </exception>
		public void AddFrame (Frame frame)
		{
			if (frame == null)
				throw new ArgumentNullException ("frame");
			
			frame_list.Add (frame);
		}
		
		/// <summary>
		///    Replaces an existing frame with a new one in the list
		///    contained in the current instance, or adds a new one if
		///    the existing one is not contained.
		/// </summary>
		/// <param name="oldFrame">
		///    A <see cref="Frame" /> object to be replaced.
		/// </param>
		/// <param name="newFrame">
		///    A <see cref="Frame" /> object to add to the current
		///    instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="oldFrame" /> or <paramref name="newFrame"
		///    /> is <see langword="null" />.
		/// </exception>
		public void ReplaceFrame (Frame oldFrame, Frame newFrame)
		{
			if (oldFrame == null)
				throw new ArgumentNullException ("oldFrame");
			
			if (newFrame == null)
				throw new ArgumentNullException ("newFrame");
			
			if (oldFrame == newFrame)
				return;
			
			int i = frame_list.IndexOf (oldFrame);
			if (i >= 0)
				frame_list [i] = newFrame;
			else
				frame_list.Add (newFrame);
		}
		
		/// <summary>
		///    Removes a specified frame from the current instance.
		/// </summary>
		/// <param name="frame">
		///    A <see cref="Frame" /> object to remove from the current
		///    instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="frame" /> is <see langword="null" />.
		/// </exception>
		public void RemoveFrame (Frame frame)
		{
			if (frame == null)
				throw new ArgumentNullException ("frame");
			
			if (frame_list.Contains (frame))
				frame_list.Remove (frame);
		}
		
		/// <summary>
		///    Removes all frames with a specified identifier from the
		///    current instance.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frames to remove.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public void RemoveFrames (ByteVector ident)
		{
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			for (int i = frame_list.Count - 1; i >= 0; i --)
				if (frame_list [i].FrameId.Equals (ident))
					frame_list.RemoveAt (i);
		}
		
		/// <summary>
		///    Sets the text for a specified Text Information Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frame to set the data for.
		/// </param>
		/// <param name="text">
		///    A <see cref="T:string[]" /> containing the text to set for
		///    the specified frame, or <see langword="null" /> to unset
		///    the value.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public void SetTextFrame (ByteVector ident,
		                          params string [] text)
		{
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			bool empty = true;
			
			if (text != null)
				for (int i = 0; empty && i < text.Length; i ++)
					if (!string.IsNullOrEmpty (text [i]))
						empty = false;
			
			if (empty) {
				RemoveFrames (ident);
				return;
			}
			
			// Handle URL Link frames differently
			if (ident[0] == 'W')
			{
				UrlLinkFrame urlFrame = 
					UrlLinkFrame.Get(this, ident, true);

				urlFrame.Text = text;
				urlFrame.TextEncoding = DefaultEncoding;
				return;
			}

			TextInformationFrame frame =
				TextInformationFrame.Get (this, ident, true);
			
			frame.Text = text;
			frame.TextEncoding = DefaultEncoding;
		}
		
		/// <summary>
		///    Sets the text for a specified Text Information Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frame to set the data for.
		/// </param>
		/// <param name="text">
		///    A <see cref="StringCollection" /> object containing the
		///    text to set for the specified frame, or <see
		///    langword="null" /> to unset the value.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		[Obsolete("Use SetTextFrame(ByteVector,String[])")]
		public void SetTextFrame (ByteVector ident,
		                          StringCollection text)
		{
			if (text == null || text.Count == 0)
				RemoveFrames (ident);
			else
				SetTextFrame (ident, text.ToArray ());
		}

		/// <summary>
		///    Sets the numeric values for a specified Text Information
		///    Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier of the frame to set the data for.
		/// </param>
		/// <param name="number">
		///    A <see cref="uint" /> value containing the number to
		///    store.
		/// </param>
		/// <param name="count">
		///    A <see cref="uint" /> value representing a total which
		///    <paramref name="number" /> is a part of, or zero if
		///    <paramref name="number" /> is not part of a set.
		/// </param>
		/// <param name="format">
		///    A <see cref="string" /> value representing the format
		///    to be used to repreesent the <paramref name="number"/>.
		///    Default: simple decimal number ("0").
		/// </param>
		/// <remarks>
		///    If both <paramref name="number" /> and <paramref
		///    name="count" /> are equal to zero, the value will be
		///    cleared. If <paramref name="count" /> is zero, <paramref
		///    name="number" /> by itself will be stored. Otherwise, the
		///    values will be stored as "<paramref name="number"
		///    />/<paramref name="count" />".
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="ident" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="ident" /> is not exactly four bytes long.
		/// </exception>
		public void SetNumberFrame (ByteVector ident, uint number,
		                            uint count, string format = "0")
		{
			if (ident == null)
				throw new ArgumentNullException ("ident");
			
			if (ident.Count != 4)
				throw new ArgumentException (
					"Identifier must be four bytes long.",
					"ident");
			
			if (number == 0 && count == 0) {
				RemoveFrames (ident);
			} else if (count != 0) {
				SetTextFrame (ident, string.Format (
					CultureInfo.InvariantCulture, "{0:" + format + "}/{1}",
					number, count));
			} else {
				SetTextFrame (ident, number.ToString ( format,
					CultureInfo.InvariantCulture));
			}
		}
		
		/// <summary>
		///    Renders the current instance as a raw ID3v2 tag.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered tag.
		/// </returns>
		/// <remarks>
		///    By default, tags will be rendered in the version they
		///    were loaded in, and new tags using the version specified
		///    by <see cref="DefaultVersion" />. If <see
		///    cref="ForceDefaultVersion" /> is <see langword="true" />,
		///    all tags will be rendered in using the version specified
		///    by <see cref="DefaultVersion" />, except for tags with
		///    footers, which must be in version 4.
		/// </remarks>
		public ByteVector Render ()
		{
			// Convert the PerformersRole to the TMCL Tag

			string[] ret = null;
			if (performers_role != null)
			{
				var map = new Dictionary<string, string>();
				for (int i = 0; i < performers_role.Length; i++)
				{
					var insts = performers_role[i];
					if (string.IsNullOrEmpty(insts))
						continue;

					var instlist = insts.Split(';');
					foreach (var iinst in instlist)
					{
						var inst = iinst.Trim();

						if (i < Performers.Length)
						{
							var perf = Performers[i];
							if (map.ContainsKey(inst))
							{
								map[inst] += ", " + perf;
							}
							else
							{
								map.Add(inst, perf);
							}
						}
					}
				}

				// Convert dictionary to array
				ret = new string[map.Count * 2];
				int j = 0;
				foreach (var dict in map)
				{
					ret[j++] = dict.Key;
					ret[j++] = dict.Value;
				}
			}

			SetTextFrame(FrameType.TMCL, ret);


			// We need to render the "tag data" first so that we
			// have to correct size to render in the tag's header.
			// The "tag data" (everything that is included in
			// Header.TagSize) includes the extended header, frames
			// and padding, but does not include the tag's header or
			// footer.

			bool has_footer = (header.Flags &
				HeaderFlags.FooterPresent) != 0;
			bool unsynchAtFrameLevel = (header.Flags & HeaderFlags.Unsynchronisation) != 0 && Version >= 4;
			bool unsynchAtTagLevel = (header.Flags & HeaderFlags.Unsynchronisation) != 0 && Version < 4;

			header.MajorVersion = has_footer ? (byte) 4 : Version;
			
			ByteVector tag_data = new ByteVector ();
			
			// TODO: Render the extended header.
			header.Flags &= ~HeaderFlags.ExtendedHeader;
			
			// Loop through the frames rendering them and adding
			// them to the tag_data.
			foreach (Frame frame in frame_list) {
				if (unsynchAtFrameLevel)
					frame.Flags |= FrameFlags.Unsynchronisation;

				if ((frame.Flags &
					FrameFlags.TagAlterPreservation) != 0)
					continue;
				
				try {
					tag_data.Add (frame.Render (
						header.MajorVersion));
				} catch (NotImplementedException) {
				}
			}
			
			// Add unsyncronization bytes if necessary.
			if (unsynchAtTagLevel)
				SynchData.UnsynchByteVector (tag_data);
			
			// Compute the amount of padding, and append that to
			// tag_data.
			
			
			if (!has_footer)
				tag_data.Add (new ByteVector ((int)
					((tag_data.Count < header.TagSize) ? 
					(header.TagSize - tag_data.Count) :
					1024)));
			
			// Set the tag size.
			header.TagSize = (uint) tag_data.Count;
			
			tag_data.Insert (0, header.Render ());
			if (has_footer)
				tag_data.Add (new Footer (header).Render ());
			
			return tag_data;
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets and sets the header flags applied to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="HeaderFlags" /> value
		///    containing flags applied to the current instance.
		/// </value>
		public HeaderFlags Flags {
			get {return header.Flags;}
			set {header.Flags = value;}
		}
		
		/// <summary>
		///    Gets and sets the ID3v2 version of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value specifying the ID3v2 version
		///    of the current instance.
		/// </value>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="value" /> is less than 2 or more than 4.
		/// </exception>
		public byte Version {
			get {
				return ForceDefaultVersion ?
					DefaultVersion : header.MajorVersion;
			}
			set {
				if (value < 2 || value > 4)
					throw new ArgumentOutOfRangeException (
						"value",
						"Version must be 2, 3, or 4");
				
				header.MajorVersion = value;
			}
		}
		
#endregion
		
		
		
#region Public Static Properties
		
		/// <summary>
		///    Gets and sets the ISO-639-2 language code to use when
		///    searching for and storing language specific values.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing an ISO-639-2
		///    language code fto use when searching for and storing
		///    language specific values.
		/// </value>
		/// <remarks>
		///    If the language is unknown, "   " is the appropriate
		///    filler.
		/// </remarks>
		public static string Language {
			get {return language;}
			set {
				language = (value == null || value.Length < 3) ?
					"   " : value.Substring (0,3);
			}
		}
		
		/// <summary>
		///    Gets and sets the the default version to use when
		///    creating new tags.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value specifying the default ID3v2
		///    version. The default version for this library is 3.
		/// </value>
		/// <remarks>
		///    If <see cref="ForceDefaultVersion" /> is <see
		///    langword="true" />, all tags will be rendered with this
		///    version.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="value" /> is less than 2 or more than 4.
		/// </exception>
		public static byte DefaultVersion {
			get {return default_version;}
			set {
				if (value < 2 || value > 4)
					throw new ArgumentOutOfRangeException (
						"value",
						"Version must be 2, 3, or 4");
				
				default_version = value;
			}
		}
		
		/// <summary>
		///    Gets and sets whether or not to save all tags in the
		///    default version rather than their original version.
		/// </summary>
		/// <value>
		///    If <see langword="true"/>, tags will be saved in
		///    <see cref="DefaultVersion" /> rather than their original
		///    format, with the exception of tags with footers, which
		///    will be saved in version 4.
		/// </value>
		public static bool ForceDefaultVersion {
			get {return force_default_version;}
			set {force_default_version = value;}
		}
		
		/// <summary>
		///    Gets and sets the encoding to use when creating new
		///    frames.
		/// </summary>
		/// <value>
		///    A <see cref="StringType" /> value specifying the encoding
		///    to use when creating new frames.
		/// </value>
		public static StringType DefaultEncoding {
			get {return default_string_type;}
			set {default_string_type = value;}
		}
		
		/// <summary>
		///    Gets and sets whether or not to render all frames with
		///    the default encoding rather than their original encoding.
		/// </summary>
		/// <value>
		///    If <see langword="true"/>, fames will be rendered in
		///    <see cref="DefaultEncoding" /> rather than their original
		///    encoding.
		/// </value>
		public static bool ForceDefaultEncoding {
			get {return force_default_string_type;}
			set {force_default_string_type = value;}
		}
		
		/// <summary>
		///    Gets and sets whether or not to use ID3v1 style numeric
		///    genres when possible.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value specifying whether or not to
		///    use genres with numeric values when possible.
		/// </value>
		/// <remarks>
		///    If <see langword="true" />, TagLib# will try looking up
		///    the numeric genre code when storing the value. For
		///    ID3v2.2 and ID3v2.3, "Rock" would be stored as "(17)" and
		///    for ID3v2.4 it would be stored as "17".
		/// </remarks>
		public static bool UseNumericGenres {
			get {return use_numeric_genres;}
			set {use_numeric_genres = value;}
		}

		#endregion



		#region Protected Methods

		/// <summary>
		///    Populates the current instance be reading in a tag from
		///    a specified position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File" /> object to read the tag from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to read the tag.
		/// </param>
		/// <param name="style">
		///    A <see cref="ReadStyle"/> value specifying how the media
		///    data is to be read into the current instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than 0 or greater
		///    than the size of the file.
		/// </exception>
		protected void Read (File file, long position, ReadStyle style)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Mode = File.AccessMode.Read;
			
			if (position < 0 || position > file.Length - Header.Size)
				throw new ArgumentOutOfRangeException (
					"position");
			
			file.Seek (position);
			
			header = new Header (file.ReadBlock ((int) Header.Size));
			
			// If the tag size is 0, then this is an invalid tag.
			// Tags must contain at least one frame.
			
			if(header.TagSize == 0)
				return;

			position += Header.Size;
			Parse (null, file, position, style);
		}

		/// <summary>
		///    Populates the current instance by parsing the contents of
		///    a raw ID3v2 tag, minus the header.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the content
		///    of an ID3v2 tag, minus the header.
		/// </param>
		/// <param name="file">
		///    A <see cref="File"/> object containing
		///    abstraction of the file to read. 
		///    Ignored if <paramref name="data"/> is not null.
		/// </param>
		/// <param name="position">
		///    A <see cref="int" /> value reference specifying at what
		///    index in <paramref name="file" />
		///    at which the frame begins. 
		/// </param>
		/// <param name="style">
		///    A <see cref="ReadStyle"/> value specifying how the media
		///    data is to be read into the current instance.
		/// </param>
		/// <remarks>
		///    This method must only be called after the internal
		///    header has been read from the file, otherwise the data
		///    cannot be parsed correctly.
		/// </remarks>
		protected void Parse (ByteVector data, File file, long position, ReadStyle style)
		{
			// If the entire tag is marked as unsynchronized, and this tag
			// is version id3v2.3 or lower, resynchronize it.
			bool fullTagUnsynch =  (header.MajorVersion < 4) && 
				(header.Flags & HeaderFlags.Unsynchronisation) != 0;

			// Avoid to load all the ID3 tag if PictureLazy enabled and size is 
			// significant enough (ID3v4 and later only)
			if (data == null && 
				(fullTagUnsynch || 
				header.TagSize<1024 || 
				(style & ReadStyle.PictureLazy) == 0 || 
				(header.Flags & HeaderFlags.ExtendedHeader) != 0))
			{
				file.Seek(position);
				data = file.ReadBlock((int)header.TagSize);
			}

			if (fullTagUnsynch)
				SynchData.ResynchByteVector (data);
			
			int frame_data_position = data != null ? 0 : (int)position;
			int frame_data_endposition = (data != null ? data.Count  : (int)header.TagSize) 
				+ frame_data_position - (int)FrameHeader.Size(header.MajorVersion);
			

			// Check for the extended header (ID3v2 only)
			
			if ((header.Flags & HeaderFlags.ExtendedHeader) != 0) {
				extended_header = new ExtendedHeader (data,
					header.MajorVersion);
				
				if (extended_header.Size <= data.Count) {
					frame_data_position += (int)
						extended_header.Size;
					frame_data_endposition -= (int)
						extended_header.Size;
				}
			}
			
			// Parse the frames. TDRC, TDAT, and TIME will be needed
			// for post-processing, so check for them as they are
			// loaded.
			TextInformationFrame tdrc = null;
			TextInformationFrame tyer = null;
			TextInformationFrame tdat = null;
			TextInformationFrame time = null;
			
			while (frame_data_position < frame_data_endposition) {
				
				Frame frame = null;

				try {
					frame = FrameFactory.CreateFrame(
						data, file,
						ref frame_data_position,
						header.MajorVersion,
						fullTagUnsynch);
				} catch (NotImplementedException) {
					continue;
				} catch (CorruptFileException) {
					continue;
				}

				if(frame == null)
					break;

				// Only add frames that contain data.
				if (frame.Size == 0)
					continue;
				
				AddFrame (frame);
				
				// If the tag is version 4, no post-processing
				// is needed.
				if (header.MajorVersion == 4)
					continue;
					
				// Load up the first instance of each, for
				// post-processing.
				
				if (tdrc == null &&
					frame.FrameId.Equals (FrameType.TDRC)) {
					tdrc = frame as TextInformationFrame;
				} else if (tyer == null &&
					frame.FrameId.Equals (FrameType.TYER)) {
					tyer = frame as TextInformationFrame;
				} else if (tdat == null &&
					frame.FrameId.Equals (FrameType.TDAT)) {
					tdat = frame as TextInformationFrame;
				} else if (time == null &&
					frame.FrameId.Equals (FrameType.TIME)) {
					time = frame as TextInformationFrame;
				}
			}

			// Try to fill out the date/time of the TDRC frame.  Can't do that if no TDRC
			// frame exists, or if there is no TDAT frame, or if TDRC already has the date.
			if (tdrc == null || tdat == null || tdrc.ToString ().Length > 4) {
				return;
			}

			string year = tdrc.ToString ();
			if (year.Length != 4)
				return;

			// Start with the year already in TDRC, then add the TDAT and TIME if available
			StringBuilder tdrc_text = new StringBuilder ();
			tdrc_text.Append (year);

			// Add the date
			if (tdat != null) {
				string tdat_text = tdat.ToString ();
				if (tdat_text.Length == 4) {
					tdrc_text.Append ("-").Append (tdat_text, 0, 2)
						.Append ("-").Append (tdat_text, 2, 2);

					// Add the time
					if (time != null) {
						string time_text = time.ToString ();
							
						if (time_text.Length == 4)
							tdrc_text.Append ("T").Append (time_text, 0, 2)
								.Append (":").Append (time_text, 2, 2);

						RemoveFrames (FrameType.TIME);
					}
				}

				RemoveFrames (FrameType.TDAT);
			}

			tdrc.Text = new string [] { tdrc_text.ToString () };
		}

		#endregion



		#region Private Methods

		/// <summary>
		///    Gets the text values from a specified Text Information
		///    Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifier of the Text Information Frame to get the value
		///    from.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]" /> containing the text of the
		///    specified frame, or an empty array if no values were
		///    found.
		/// </returns>
		private string [] GetTextAsArray (ByteVector ident)
		{
			TextInformationFrame frame = TextInformationFrame.Get (
				this, ident, false);
			return frame == null ? new string [0] : frame.Text;
		}
		
		/// <summary>
		///    Gets an integer value from a "/" delimited list in a
		///    specified Text Information Frame.
		/// </summary>
		/// <param name="ident">
		///    A <see cref="ByteVector" /> object containing the frame
		///    identifier of the Text Information Frame to read from.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value specifying the index in the
		///    integer list of the value to return.
		/// </param>
		/// <returns>
		///    A <see cref="uint" /> value read from the list in the
		///    frame, or 0 if the value wasn't found.
		/// </returns>
		private uint GetTextAsUInt32 (ByteVector ident, int index)
		{
			string text = GetTextAsString (ident);
			
			if (text == null)
				return 0;
			
			string [] values = text.Split (new char [] {'/'},
				index + 2);
			
			if (values.Length < index + 1)
				return 0;
			
			uint result;
			if (uint.TryParse (values [index], out result))
				return result;
			
			return 0;
		}

		/// <summary>
		/// Gets a TXXX frame via reference of the description field, optionally searching for the
		/// frame in a case-sensitive manner.
		/// </summary>
		/// <param name="description">String containing the description field</param>
		/// <param name="caseSensitive">case-sensitive search if true.</param>
		/// <returns>UserTextInformationFrame (TXXX) that corresponds to the description</returns>
		private string GetUserTextAsString (string description, bool caseSensitive) {

			//Gets the TXXX frame, frame will be null if nonexistant
			UserTextInformationFrame frame = UserTextInformationFrame.Get (
				this, description, Tag.DefaultEncoding, false, caseSensitive);

			//TXXX frames support multivalue strings, join them up and return
			//only the text from the frame.
			string result = frame == null ? null : string.Join (";",frame.Text);
			return string.IsNullOrEmpty (result) ? null : result;

		}

		/// <summary>
		/// Gets a TXXX frame via reference of the description field.
		/// </summary>
		/// <param name="description">String containing the description field</param>
		/// <returns>UserTextInformationFrame (TXXX) that corresponds to the description</returns>
		private string GetUserTextAsString (string description) {
			return GetUserTextAsString (description, true);
		}

		/// <summary>
		/// Creates and/or sets a UserTextInformationFrame (TXXX)  with the given
		/// description and text, optionally searching for the frame in a case-sensitive manner.
		/// </summary>
		/// <param name="description">String containing the Description field for the
		/// TXXX frame</param>
		/// <param name="text">String containing the Text field for the TXXX frame</param>
		/// <param name="caseSensitive">case-sensitive search if true.</param>
		private void SetUserTextAsString(string description, string text, bool caseSensitive) {
			//Get the TXXX frame, create a new one if needed
			UserTextInformationFrame frame = UserTextInformationFrame.Get(
				this, description, Tag.DefaultEncoding, true, caseSensitive);

			if (!string.IsNullOrEmpty(text)) {
				frame.Text = text.Split(';');
			} else {
				//Text string is null or empty, delete the frame, prevent empties
				RemoveFrame(frame);
			}
		}

		/// <summary>
		/// Creates and/or sets a UserTextInformationFrame (TXXX)  with the given
		/// description and text.
		/// </summary>
		/// <param name="description">String containing the Description field for the
		/// TXXX frame</param>
		/// <param name="text">String containing the Text field for the TXXX frame</param>
		private void SetUserTextAsString(string description, string text) {
			SetUserTextAsString (description, text, true);
		}

		/// <summary>
		/// Gets the text from a particular UFID frame, referenced by the owner field
		/// </summary>
		/// <param name="owner">String containing the "Owner" data</param>
		/// <returns>String containing the text from the UFID frame, or null</returns>
		private string GetUfidText(string owner) {

			//Get the UFID frame, frame will be null if nonexistant
			UniqueFileIdentifierFrame frame = UniqueFileIdentifierFrame.Get(
				this, owner, false);
			
			//If the frame existed: frame.Identifier is a bytevector, get a string
			string result = frame == null ? null : frame.Identifier.ToString();
			return string.IsNullOrEmpty (result) ? null : result;
		}

		/// <summary>
		/// Creates and/or sets the text for a UFID frame, referenced by owner
		/// </summary>
		/// <param name="owner">String containing the Owner field</param>
		/// <param name="text">String containing the text to set for the frame</param>
		private void SetUfidText(string owner, string text) {

			//Get a UFID frame, create if necessary
			UniqueFileIdentifierFrame frame = UniqueFileIdentifierFrame.Get(
				this, owner, true);

			//If we have a real string, convert to ByteVector and apply to frame
			if (!string.IsNullOrEmpty(text)) {
				ByteVector identifier = ByteVector.FromString(text, StringType.UTF8);
				frame.Identifier = identifier;
			}
			else {
				//String was null or empty, remove the frame to prevent empties
				RemoveFrame(frame);
			}
		}

		/// <summary>
		///    Moves a specified frame so it is the first of its type in
		///    the tag.
		/// </summary>
		/// <param name="frame">
		///    A <see cref="Frame" /> object to make the first of its
		///    type.
		/// </param>
		private void MakeFirstOfType (Frame frame)
		{
			ByteVector type = frame.FrameId;
			Frame swapping = null;
			for (int i = 0; i < frame_list.Count; i ++) {
				if (swapping == null) {
					if (frame_list [i].FrameId.Equals (type))
						swapping = frame;
					else
						continue;
				}
				
				Frame tmp = frame_list [i];
				frame_list [i] = swapping;
				swapping = tmp;
				
				if (swapping == frame)
					return;
			}
			
			if (swapping != null)
				frame_list.Add (swapping);
		}
		
		#endregion
		
		
		
#region IEnumerable
		
		/// <summary>
		///    Gets an enumerator for enumerating through the frames.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the frames.
		/// </returns>
		public IEnumerator<Frame> GetEnumerator ()
		{
			return frame_list.GetEnumerator ();
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return frame_list.GetEnumerator ();
		}
		
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Id3v2" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.Id3v2;}
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
		///    This property is implemented using the "TIT2" Text
		///    Information Frame.
		/// </remarks>
		public override string Title {
			get {return GetTextAsString (FrameType.TIT2);}
			set {SetTextFrame (FrameType.TIT2, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the Title of the
		///    media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names for
		///    the Title of the media described by the current instance,
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSOT" Text
		///    Information Frame.
		/// </remarks>
		public override string TitleSort {
			get {return GetTextAsString (FrameType.TSOT);}
			set {SetTextFrame (FrameType.TSOT, value);}
		}


		/// <summary>
		///    Gets and sets a short description, one-liner. 
		///    It represents the tagline of the Video/music.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>This field gives a nice/short precision to 
		///    the title, which is typically below the title on the
		///    front cover of a media.
		///    For example, for "Back to the future", this would be 
		///    "It's About Time". 
		///    </para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "TIT3" Text
		///    Information Frame.
		/// </remarks>
		public override string Subtitle
		{
			get { return GetTextAsString(FrameType.TIT3); }
			set { SetTextFrame(FrameType.TIT3, value); }
		}

		/// <summary>
		///    Gets and sets a short description of the media.
		///    For a music, this could be the comment that the artist
		///    made of its artwork. For a video, this should be a 
		///    short summary of the story/plot, but a spoiler. This
		///    should give the impression of what to expect in the
		///    media.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>This is especially relevant for a movie.
		///    For example, for "Back to the Future 2", this could be
		///    "After visiting 2015, Marty McFly must repeat his visit 
		///    to 1955 to prevent disastrous changes to 1985...without
		///    interfering with his first trip".
		///    </para>
		/// </remarks>
		public override string Description
		{
			get { return GetUserTextAsString("Description"); }
			set { SetUserTextAsString("Description", value); }
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
		///    This property is implemented using the "TPE1" Text
		///    Information Frame.
		/// </remarks>
		public override string [] Performers {
			get {return GetTextAsArray (FrameType.TPE1);}
			set {SetTextFrame (FrameType.TPE1, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the performers or artists
		///    who performed in the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSOP" Text
		///    Information Frame. http://www.id3.org/id3v2.4.0-frames
		/// </remarks>
		public override string [] PerformersSort {
			get {return GetTextAsArray (FrameType.TSOP);}
			set {SetTextFrame (FrameType.TSOP, value);}
		}

		/// <summary>
		///    Gets and sets the Charaters for a video media, or
		///    instruments played for music media. 
		///    This should match the <see cref="Performers"/> array (for
		///    each person correspond one/more role). Several roles for
		///    the same artist/actor can be made up with semicolons. 
		///    For example, "Marty McFly; Marty McFly Jr.; Marlene McFly".
		/// </summary>
		/// <remarks>
		///    <para> This is typically usefull for movies, although the
		///    instrument played by each artist in a music may be of
		///    relevance.
		///    </para>
		///    <para>It is highly important to match each role to the 
		///    performers. This means that a role may be <see 
		///    langword="null"/> to keep the match between a
		///    Performers[i] and PerformersRole[i].
		///    </para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the "TMCL" Text
		///    Information Frame: The 'Musician credits list' is 
		///    intended as a mapping between instruments and the 
		///    musician that played it.Every odd field is an 
		///    instrument and every even is an artist or a comma 
		///    delimited list of artists.
		/// </remarks>
		public override string[] PerformersRole
		{
			get
			{
				if (performers_role != null)
				{
					return performers_role;
				}

				var perfref = Performers;
				if (Performers == null)
					return performers_role = new string[0];

				// Map the instruments to the performers

				string[] map = GetTextAsArray(FrameType.TMCL);
				performers_role = new string[Performers.Length];
				for (int i = 0; i + 1 < map.Length; i += 2)
				{
					string inst = map[i];
					string perfs = map[i + 1];
					if ( string.IsNullOrEmpty(inst) 
						|| string.IsNullOrEmpty(perfs))
						continue;

					var perflist = perfs.Split(',');
					foreach (string iperf in perflist)
					{
						if (iperf == null) continue;
						var perf = iperf.Trim();
						if (string.IsNullOrEmpty(perf)) continue;
						for (int j = 0; j < perfref.Length; j++)
						{
							if (perfref[j] == perf)
							{
								performers_role[j] = performers_role[j] == null ? inst :
									performers_role[j] + "; " + inst;
							}
						}
					}
				}

				return performers_role;
			}

			set
			{
				performers_role = value == null ? 
					new string[0] : value;
			}
		}


		/// <summary>
		///    Gets and sets the sort names of the band or artist who is 
		///    credited in the creation of the entire album or collection
		///    containing the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSO2" Text
		///    Information Frame. http://www.id3.org/iTunes
		/// </remarks>
		public override string [] AlbumArtistsSort {
			get {return GetTextAsArray (FrameType.TSO2);}
			set {SetTextFrame (FrameType.TSO2, value);}
		}

		/// <summary>
		///    Gets and sets the band or artist who is credited in the
		///    creation of the entire album or collection containing the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the band or artist
		///    who is credited in the creation of the entire album or
		///    collection containing the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPE2" Text
		///    Information Frame.
		/// </remarks>
		public override string [] AlbumArtists {
			get {return GetTextAsArray (FrameType.TPE2);}
			set {SetTextFrame (FrameType.TPE2, value);}
		}
		
		/// <summary>
		///    Gets and sets the composers of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the composers of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TCOM" Text
		///    Information Frame.
		/// </remarks>
		public override string [] Composers {
			get {return GetTextAsArray (FrameType.TCOM);}
			set {SetTextFrame (FrameType.TCOM, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the composers of the
		///    media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSOC" Text
		///    Information Frame. http://www.id3.org/id3v2.4.0-frames
		/// </remarks>
		public override string [] ComposersSort {
			get {return GetTextAsArray (FrameType.TSOC);}
			set {SetTextFrame (FrameType.TSOC, value);}
		}

		/// <summary>
		///    Gets and sets the album of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the album of
		///    the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TALB" Text
		///    Information Frame.
		/// </remarks>
		public override string Album {
			get {return GetTextAsString (FrameType.TALB);}
			set {SetTextFrame (FrameType.TALB, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the Album title of the
		///    media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names for
		///    the Title in the media described by the current instance,
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSOA" Text
		///    Information Frame. http://www.id3.org/id3v2.4.0-frames
		/// </remarks>
		public override string AlbumSort {
			get {return GetTextAsString (FrameType.TSOA);}
			set {SetTextFrame (FrameType.TSOA, value);}
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
		///    This property is implemented using the "COMM" Comments
		///    Frame with an empty description and the language
		///    specified by <see cref="Language" />.
		/// </remarks>
		public override string Comment {
			get {
				CommentsFrame f =
					CommentsFrame.GetPreferred (this,
						String.Empty, Language);
				return f != null ? f.ToString () : null;
			}
			set {
				CommentsFrame frame;
				
				if (string.IsNullOrEmpty (value)) {
					while ((frame = CommentsFrame
						.GetPreferred (this,
							string.Empty,
							Language)) != null)
						RemoveFrame (frame);
					
					return;
				}
				
				frame = CommentsFrame.Get (this, String.Empty,
					Language, true);
				
				frame.Text = value;
				frame.TextEncoding = DefaultEncoding;
				MakeFirstOfType (frame);
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
		///    This property is implemented using the "TCON" Text
		///    Information Frame.
		/// </remarks>
		public override string [] Genres {
			get {
				string [] text = GetTextAsArray (FrameType.TCON);
				
				if (text.Length == 0)
					return text;
				
				List<string> list = new List<string> ();
				
				foreach (string genre in text) {
					if (string.IsNullOrEmpty (genre))
						continue;
					
					// The string may just be a genre
					// number.
					
					string genre_from_index =
						TagLib.Genres.IndexToAudio (
							genre);
					
					if (genre_from_index != null)
						list.Add (genre_from_index);
					else
						list.Add (genre);
				}
				
				return list.ToArray ();
			}
			set {
				if (value == null || !use_numeric_genres) {
					SetTextFrame (FrameType.TCON, value);
					return;
				}
				
				// Clone the array so changes made won't effect
				// the passed array.
				value = (string []) value.Clone ();
				
				for (int i = 0; i < value.Length; i ++) {
					int index = TagLib.Genres.AudioToIndex (
						value [i]);
					
					if (index != 255)
						value [i] = index.ToString (
							CultureInfo.InvariantCulture);
				}
				
				SetTextFrame (FrameType.TCON, value);
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
		///    This property is implemented using the "TDRC" Text
		///    Information Frame. If a value greater than 9999 is set,
		///    this property will be cleared.
		/// </remarks>
		public override uint Year {
			get {
				string text = GetTextAsString (FrameType.TDRC);
				
				if (text == null || text.Length < 4)
					return 0;
				
				uint value;
				if (uint.TryParse (text.Substring (0, 4),
					out value))
					return value;
				
				return 0;
			}
			set {
				if (value > 9999)
					value = 0;
				
				SetNumberFrame (FrameType.TDRC, value, 0);
			}
		}
		
		/// <summary>
		///    Gets and sets the position of the media represented by
		///    the current instance in its containing album.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the position of the
		///    media represented by the current instance in its
		///    containing album or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TRCK" Text
		///    Information Frame.
		/// </remarks>
		public override uint Track {
			get {return GetTextAsUInt32 (FrameType.TRCK, 0);}
			set {SetNumberFrame (FrameType.TRCK, value, TrackCount, "00");}
		}
		
		/// <summary>
		///    Gets and sets the number of tracks in the album
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of tracks in
		///    the album containing the media represented by the current
		///    instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TRCK" Text
		///    Information Frame.
		/// </remarks>
		public override uint TrackCount {
			get {return GetTextAsUInt32 (FrameType.TRCK, 1);}
			set {SetNumberFrame (FrameType.TRCK, Track, value);}
		}
		
		/// <summary>
		///    Gets and sets the number of the disc containing the media
		///    represented by the current instance in the boxed set.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of the disc
		///    containing the media represented by the current instance
		///    in the boxed set.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPOS" Text
		///    Information Frame.
		/// </remarks>
		public override uint Disc {
			get {return GetTextAsUInt32 (FrameType.TPOS, 0);}
			set {SetNumberFrame (FrameType.TPOS, value, DiscCount);}
		}
		
		/// <summary>
		///    Gets and sets the number of discs in the boxed set
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of discs in
		///    the boxed set containing the media represented by the
		///    current instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPOS" Text
		///    Information Frame.
		/// </remarks>
		public override uint DiscCount {
			get {return GetTextAsUInt32 (FrameType.TPOS, 1);}
			set {SetNumberFrame (FrameType.TPOS, Disc, value);}
		}
		
		/// <summary>
		///    Gets and sets the lyrics or script of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the lyrics or
		///    script of the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "USLT"
		///    Unsynchronized Lyrics Frame with an empty description and
		///    the language specified by <see cref="Language" />.
		/// </remarks>
		public override string Lyrics {
			get {
				UnsynchronisedLyricsFrame f =
					UnsynchronisedLyricsFrame.GetPreferred (
						this, string.Empty, Language);
				
				return f != null ? f.ToString () : null;
			}
			set {
				UnsynchronisedLyricsFrame frame;
				
				if (string.IsNullOrEmpty (value)) {
					while ((frame = UnsynchronisedLyricsFrame
						.GetPreferred (this,
							string.Empty,
							Language)) != null)
						RemoveFrame (frame);
					
					return;
				}
				
				frame = UnsynchronisedLyricsFrame.Get (this,
						String.Empty, Language, true);
				
				frame.Text = value;
				frame.TextEncoding = DefaultEncoding;
			}
		}
		
		/// <summary>
		///    Gets and sets the grouping on the album which the media
		///    in the current instance belongs to.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the grouping on
		///    the album which the media in the current instance belongs
		///    to or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TIT1" Text
		///    Information Frame.
		/// </remarks>
		public override string Grouping {
			get {return GetTextAsString (FrameType.TIT1);}
			set {SetTextFrame (FrameType.TIT1, value);}
		}
		
		/// <summary>
		///    Gets and sets the number of beats per minute in the audio
		///    of the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of beats per
		///    minute in the audio of the media represented by the
		///    current instance, or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TBPM" Text
		///    Information Frame.
		/// </remarks>
		public override uint BeatsPerMinute {
			get {
				string text = GetTextAsString (FrameType.TBPM);
				
				if (text == null)
					return 0;
				
				double result;
				if (double.TryParse (text, out result) &&
					result >= 0.0)
					return (uint) Math.Round (result);
				
				return 0;
			}
			set {SetNumberFrame (FrameType.TBPM, value, 0);}
		}
		
		/// <summary>
		///    Gets and sets the conductor or director of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the conductor
		///    or director of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPE3" Text
		///    Information Frame.
		/// </remarks>
		public override string Conductor {
			get {return GetTextAsString (FrameType.TPE3);}
			set {SetTextFrame (FrameType.TPE3, value);}
		}
		
		/// <summary>
		///    Gets and sets the copyright information for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TCOP" Text
		///    Information Frame.
		/// </remarks>
		public override string Copyright {
			get {return GetTextAsString (FrameType.TCOP);}
			set {SetTextFrame (FrameType.TCOP, value);}
		}

		/// <summary>
		///    Gets and sets the date at which the tag has been written.
		/// </summary>
		/// <value>
		///    A nullable <see cref="DateTime" /> object containing the 
		///    date at which the tag has been written, or <see 
		///    langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TDTG" Timestamp
		///    Information Frame.
		/// </remarks>
		public override DateTime? DateTagged
		{
			get
			{
				string value = GetTextAsString(FrameType.TDTG);
				if (value != null)
				{
					value = value.Replace('T', ' ');
					DateTime date;
					if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out date))
					{
						return date;
					}
				}
				return null;
			}
			set
			{
				string date = null;
				if (value != null)
				{
					date = string.Format("{0:yyyy-MM-dd HH:mm:ss}", value);
					date = date.Replace(' ', 'T');
				}
				SetTextFrame(FrameType.TDTG, date);
			}
		}
		
		/// <summary>
		///    Gets and sets the MusicBrainz ArtistID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ArtistID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Artist Id" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzArtistId {
			get {return GetUserTextAsString ("MusicBrainz Artist Id");}
			set {SetUserTextAsString ("MusicBrainz Artist Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseGroupID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseGroupID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Release Group Id" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseGroupId {
			get { return GetUserTextAsString("MusicBrainz Release Group Id"); }
			set { SetUserTextAsString("MusicBrainz Release Group Id", value); }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Album Id" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseId {
			get {return GetUserTextAsString ("MusicBrainz Album Id");}
			set {SetUserTextAsString ("MusicBrainz Album Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseArtistID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseArtistID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Album Artist Id" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseArtistId {
			get {return GetUserTextAsString ("MusicBrainz Album Artist Id");}
			set {SetUserTextAsString ("MusicBrainz Album Artist Id",value);}
		}
		
		/// <summary>
		///    Gets and sets the MusicBrainz TrackID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    TrackID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "UFID:http://musicbrainz.org" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzTrackId {
			get { return GetUfidText ("http://musicbrainz.org");}
			set {SetUfidText ("http://musicbrainz.org", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz DiscID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    DiscID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Disc Id" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzDiscId {
			get {return GetUserTextAsString ("MusicBrainz Disc Id");}
			set {SetUserTextAsString ("MusicBrainz Disc Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicIP PUID
		///    for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicIP PUID" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicIpId {
			get {return GetUserTextAsString ("MusicIP PUID");}
			set {SetUserTextAsString ("MusicIP PUID",value);}
		}

		/// <summary>
		///    Gets and sets the Amazon ID (ASIN)
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the Amazon Id
		///    for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:ASIN" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string AmazonId {
			get {return GetUserTextAsString ("ASIN");}
			set {SetUserTextAsString ("ASIN",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseStatus
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseStatus for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Album Status" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseStatus {
			get {return GetUserTextAsString ("MusicBrainz Album Status");}
			set {SetUserTextAsString ("MusicBrainz Album Status",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseType
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseType for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Album Type" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseType {
			get {return GetUserTextAsString ("MusicBrainz Album Type");}
			set {SetUserTextAsString ("MusicBrainz Album Type",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseCountry
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseCountry for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:MusicBrainz Album Release Country" frame.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseCountry {
			get {return GetUserTextAsString ("MusicBrainz Album Release Country");}
			set {SetUserTextAsString ("MusicBrainz Album Release Country",value);}
		}

		/// <summary>
		///    Gets and sets the ReplayGain track gain in dB.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value in dB for the track gain as
		///    per the ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:REPLAYGAIN_TRACK_GAIN" frame.
		///    http://wiki.hydrogenaudio.org/index.php?title=ReplayGain_specification#ID3v2
		/// </remarks>
		public override double ReplayGainTrackGain {
			get {
				string text = GetUserTextAsString ("REPLAYGAIN_TRACK_GAIN", false);
				double value;

				if (text == null) {
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db")) {
					text = text.Substring (0, text.Length - 2).Trim();
				}
				
				if (double.TryParse (text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value)) {
					return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					SetUserTextAsString ("REPLAYGAIN_TRACK_GAIN", null, false);
				} else {
					string text = value.ToString("0.00 dB",
						CultureInfo.InvariantCulture);
					SetUserTextAsString ("REPLAYGAIN_TRACK_GAIN", text, false);
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain track peak sample.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value for the track peak as per the
		///    ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:REPLAYGAIN_TRACK_PEAK" frame.
		///    http://wiki.hydrogenaudio.org/index.php?title=ReplayGain_specification#ID3v2
		/// </remarks>
		public override double ReplayGainTrackPeak {
			get {
				string text;
				double value;

				if ((text = GetUserTextAsString ("REPLAYGAIN_TRACK_PEAK", false)) !=
					null && double.TryParse (text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value)) {
						return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					SetUserTextAsString ("REPLAYGAIN_TRACK_PEAK", null, false);
				} else {
					string text = value.ToString ("0.000000", CultureInfo.InvariantCulture);
					SetUserTextAsString ("REPLAYGAIN_TRACK_PEAK", text, false);
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain album gain in dB.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value in dB for the album gain as
		///    per the ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:REPLAYGAIN_ALBUM_GAIN" frame.
		///    http://wiki.hydrogenaudio.org/index.php?title=ReplayGain_specification#ID3v2
		/// </remarks>
		public override double ReplayGainAlbumGain {
			get {
				string text = GetUserTextAsString ("REPLAYGAIN_ALBUM_GAIN", false);
				double value;

				if (text == null) {
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db")) {
					text = text.Substring (0, text.Length - 2).Trim();
				}
				
				if (double.TryParse (text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value)) {
					return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					SetUserTextAsString ("REPLAYGAIN_ALBUM_GAIN", null, false);
				} else {
					string text = value.ToString ("0.00 dB",
						CultureInfo.InvariantCulture);
					SetUserTextAsString ("REPLAYGAIN_ALBUM_GAIN", text, false);
				}
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain album peak sample.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value for the album peak as per the
		///    ReplayGain specification.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TXXX:REPLAYGAIN_ALBUM_PEAK" frame.
		///    http://wiki.hydrogenaudio.org/index.php?title=ReplayGain_specification#ID3v2
		/// </remarks>
		public override double ReplayGainAlbumPeak {
			get {
				string text;
				double value;

				if ((text = GetUserTextAsString ("REPLAYGAIN_ALBUM_PEAK", false)) !=
					null && double.TryParse (text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value)) {
						return value;
				}
				return double.NaN;
			}
			set {
				if (double.IsNaN (value)) {
					SetUserTextAsString ("REPLAYGAIN_ALBUM_PEAK", null, false);
				} else {
					string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
					SetUserTextAsString ("REPLAYGAIN_ALBUM_PEAK", text, false);
				}
			}
		}

		/// <summary>
		///    Gets and sets the initial key of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the initial key of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TKEY" field.
		/// </remarks>
		public override string InitialKey
		{
			get { return GetTextAsString(FrameType.TKEY); }
			set { SetTextFrame(FrameType.TKEY, value); }
		}

		/// <summary>
		///    Gets and sets the remixer of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the remixer of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPE4" field.
		/// </remarks>
		public override string RemixedBy
		{
			get { return GetTextAsString(FrameType.TPE4); }
			set { SetTextFrame(FrameType.TPE4, value); }
		}

		/// <summary>
		///    Gets and sets the publisher of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the publisher of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TPUB" field.
		/// </remarks>
		public override string Publisher
		{
			get { return GetTextAsString(FrameType.TPUB); }
			set { SetTextFrame(FrameType.TPUB, value); }
		}

		/// <summary>
		///    Gets and sets the ISRC (International Standard Recording Code) of the song.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the ISRC of the song.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TSRC" field.
		/// </remarks>
		public override string ISRC
		{
			get { return GetTextAsString(FrameType.TSRC); }
			set { SetTextFrame(FrameType.TSRC, value); }
		}

		/// <summary>
		///    Gets and sets a collection of pictures associated with
		///    the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:IPicture[]" /> containing a collection of
		///    pictures associated with the media represented by the
		///    current instance or an empty array if none are present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "APIC" Attached
		///    Picture Frame.
		/// </remarks>
		public override IPicture [] Pictures {
			get {
				return new List<AttachmentFrame> (
					GetFrames <AttachmentFrame> ()).ToArray ();
			}
			set {
				RemoveFrames(FrameType.APIC);
				RemoveFrames(FrameType.GEOB);

				if (value == null || value.Length == 0)
					return;
				
				foreach(IPicture picture in value) {
					AttachmentFrame frame =
						picture as AttachmentFrame;
					
					if (frame == null)
						frame = new AttachmentFrame (
							picture);
					
					AddFrame (frame);
				}
			}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance does not
		///    any values. Otherwise <see langword="false" />.
		/// </value>
		public override bool IsEmpty {
			get {return frame_list.Count == 0;}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			frame_list.Clear ();
		}
		
		/// <summary>
		///    Gets and sets whether or not the album described by the
		///    current instance is a compilation.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    album described by the current instance is a compilation.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TCMP" Text
		///    Information Frame to provide support for a feature of the
		///    Apple iPod and iTunes products.
		/// </remarks>
		public bool IsCompilation {
			get {
				string val = GetTextAsString (FrameType.TCMP);
				return !string.IsNullOrEmpty (val) && val != "0";
			}
			set {SetTextFrame (FrameType.TCMP, value ? "1" : null);}
		}
		
		/// <summary>
		///    Copies the values from the current instance to another
		///    <see cref="TagLib.Tag" />, optionally overwriting
		///    existing values.
		/// </summary>
		/// <param name="target">
		///    A <see cref="TagLib.Tag" /> object containing the target
		///    tag to copy values to.
		/// </param>
		/// <param name="overwrite">
		///    A <see cref="bool" /> specifying whether or not to copy
		///    values over existing one.
		/// </param>
		/// <remarks>
		///    <para>If <paramref name="target" /> is of type <see
		///    cref="TagLib.Ape.Tag" /> a complete copy of all values
		///    will be performed. Otherwise, only standard values will
		///    be copied.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="target" /> is <see langword="null" />.
		/// </exception>
		public override void CopyTo (TagLib.Tag target, bool overwrite)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			TagLib.Id3v2.Tag match = target as TagLib.Id3v2.Tag;
			
			if (match == null) {
				base.CopyTo (target, overwrite);
				return;
			}
			
			List<Frame> frames = new List<Frame> (frame_list);
			while (frames.Count > 0) {
				ByteVector ident = frames [0].FrameId;
				bool copy = true;
				if (overwrite) {
					match.RemoveFrames (ident);
				} else {
					foreach (Frame f in match.frame_list)
						if (f.FrameId.Equals (ident)) {
							copy = false;
							break;
						}
				}
				
				for (int i = 0; i < frames.Count;) {
					if (frames [i].FrameId.Equals (ident)) {
						if (copy)
							match.frame_list.Add (
								frames [i].Clone ());
						
						frames.RemoveAt (i);
					} else {
						i ++;
					}
				}
			}
		}
		
#endregion
		
		
		
#region ICloneable
		
		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Tag" /> object identical to the current
		///    instance.
		/// </returns>
		public Tag Clone ()
		{
			Tag tag = new Tag ();
			tag.header = header;
			if (tag.extended_header != null)
				tag.extended_header = extended_header.Clone ();
			
			foreach (Frame frame in frame_list)
				tag.frame_list.Add (frame.Clone ());
			
			return tag;
		}
		
		object ICloneable.Clone ()
		{
			return Clone ();
		}
		
#endregion
	}
}
