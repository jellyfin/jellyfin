//
// GroupedComment.cs:
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
using System.Collections.Generic;

namespace TagLib.Ogg
{
	/// <summary>
	///    This class combines a collection of <see cref="XiphComment"/>
	///    objects so that properties can be read from each but are only set
	///    to the first comment of the file.
	/// </summary>
	public class GroupedComment : Tag
	{
		#region Private Fields

		/// <summary>
		///    Contains a mapping between stream serial numbers and
		///    comments.
		/// </summary>
		private Dictionary<uint, XiphComment> comment_hash;

		/// <summary>
		///    Contains comments in the order they are added.
		/// </summary>
		private List<XiphComment> tags;

		#endregion



		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="GroupedComment" /> with now contents.
		/// </summary>
		public GroupedComment() : base()
		{
			comment_hash = new Dictionary<uint, XiphComment>();
			tags = new List<XiphComment>();
		}

		/// <summary>
		///    Gets an enumeration of the comments in the current
		///    instance, in the order they were added.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1"
		///    /> object enumerating through the <see cref="XiphComment"
		///    /> objects contained in the current instance.
		/// </value>
		public IEnumerable<XiphComment> Comments {
			get { return tags; }
		}

		/// <summary>
		///    Gets a comment in the current instance for a specified
		///    stream.
		/// </summary>
		/// <param name="streamSerialNumber">
		///    A <see cref="uint" /> value containing the serial number
		///    of the stream of the comment to get.
		/// </param>
		/// <returns>
		///    A <see cref="XiphComment"/> with the matching serial
		///    number.
		/// </returns>
		public XiphComment GetComment(uint streamSerialNumber)
		{
			return comment_hash[streamSerialNumber];
		}

		/// <summary>
		///    Adds a Xiph comment to the current instance.
		/// </summary>
		/// <param name="streamSerialNumber">
		///    A <see cref="uint" /> value containing the serial number
		///    of the stream containing the comment.
		/// </param>
		/// <param name="comment">
		///    A <see cref="XiphComment" /> object to add to the current
		///    instance.
		/// </param>
		public void AddComment(uint streamSerialNumber,
								XiphComment comment)
		{
			comment_hash.Add(streamSerialNumber, comment);
			tags.Add(comment);
		}

		/// <summary>
		///    Adds a Xiph comment to the current instance.
		/// </summary>
		/// <param name="streamSerialNumber">
		///    A <see cref="uint" /> value containing the serial number
		///    of the stream containing the comment.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector"/> object containing the raw Xiph
		///    comment to add to the current instance.
		/// </param>
		public void AddComment(uint streamSerialNumber,
								ByteVector data)
		{
			AddComment(streamSerialNumber, new XiphComment(data));
		}

		#endregion



		#region TagLib.Tag

		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="TagLib.TagTypes" />
		///    containing the tag types contained in the current
		///    instance.
		/// </value>
		/// <remarks>
		///    This value contains a bitwise combined value from all the
		///    child tags.
		/// </remarks>
		/// <seealso cref="Tag.TagTypes" />
		public override TagTypes TagTypes {
			get {
				TagTypes types = TagTypes.None;
				foreach (XiphComment tag in tags)
					if (tag != null)
						types |= tag.TagTypes;

				return types;
			}
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Title" />
		public override string Title {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Title;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Title = value; }
		}

		/// <summary>
		///    Gets and sets the sort names for the individual track title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort name
		///    for the track title of the media described by the current 
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.TitleSort" />
		public override string TitleSort {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.TitleSort;

					if (!string.IsNullOrEmpty(value))
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].TitleSort = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Subtitle" />
		public override string Subtitle {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Subtitle;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Subtitle = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Description" />
		public override string Description {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Description;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Description = value; }
		}

		/// <summary>
		///    Gets and sets the performers or artists who performed in
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the performers or
		///    artists who performed in the media described by the
		///    current instance or an empty array if no value is
		///    present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Performers" />
		public override string[] Performers {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.Performers;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].Performers = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.PerformersSort" />
		public override string[] PerformersSort {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.PerformersSort;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].PerformersSort = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.PerformersRole" />
		public override string[] PerformersRole {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.PerformersRole;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].PerformersRole = value; }
		}

		/// <summary>
		///    Gets and sets the band or artist who is credited in the
		///    creation of the entire album or collection containing the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the band or artist
		///    who is credited in the creation of the entire album or
		///    collection containing the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumArtists" />
		public override string[] AlbumArtists {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.AlbumArtists;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].AlbumArtists = value; }
		}

		/// <summary>
		///    Gets and sets the sort names for the band or artist who
		///    is credited in the creation of the entire album or
		///    collection containing the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the band or artist who is credited in the creation
		///    of the entire album or collection containing the media
		///    described by the current instance or an empty array if
		///    no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumArtistsSort" />
		public override string[] AlbumArtistsSort {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.AlbumArtistsSort;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}

			set { if (tags.Count > 0) tags[0].AlbumArtistsSort = value; }
		}

		/// <summary>
		///    Gets and sets the composers of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the composers of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Composers" />
		public override string[] Composers {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.Composers;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].Composers = value; }
		}

		/// <summary>
		///    Gets and sets the sort names for the composer of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the composer of the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.ComposersSort" />
		public override string[] ComposersSort {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.ComposersSort;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].ComposersSort = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Album" />
		public override string Album {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Album;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Album = value; }
		}

		/// <summary>
		///    Gets and sets the sort names for the album title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names
		///    for the album title of the media described by the
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumSort" />
		public override string AlbumSort {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.AlbumSort;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].AlbumSort = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Comment" />
		public override string Comment {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Comment;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Comment = value; }
		}

		/// <summary>
		///    Gets and sets the genres of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> array containing the genres of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Genres" />
		public override string[] Genres {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string[] value = tag.Genres;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}
			set { if (tags.Count > 0) tags[0].Genres = value; }
		}

		/// <summary>
		///    Gets and sets the year that the media represented by the
		///    current instance was recorded.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the year that the media
		///    represented by the current instance was created or zero
		///    if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Year" />
		public override uint Year {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.Year != 0)
						return tag.Year;

				return 0;
			}
			set { if (tags.Count > 0) tags[0].Year = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Track" />
		public override uint Track {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.Track != 0)
						return tag.Track;

				return 0;
			}
			set { if (tags.Count > 0) tags[0].Track = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.TrackCount" />
		public override uint TrackCount {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.TrackCount != 0)
						return tag.TrackCount;

				return 0;
			}
			set { if (tags.Count > 0) tags[0].TrackCount = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Disc" />
		public override uint Disc {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.Disc != 0)
						return tag.Disc;

				return 0;
			}
			set { if (tags.Count > 0) tags[0].Disc = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.DiscCount" />
		public override uint DiscCount {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.DiscCount != 0)
						return tag.DiscCount;

				return 0;
			}
			set { if (tags.Count > 0) tags[0].DiscCount = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Lyrics" />
		public override string Lyrics {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Lyrics;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Lyrics = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Grouping" />
		public override string Grouping {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Grouping;

					if (!string.IsNullOrEmpty(value))
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Grouping = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.BeatsPerMinute" />
		public override uint BeatsPerMinute {
			get {
				foreach (XiphComment tag in tags)
					if (tag != null && tag.BeatsPerMinute != 0)
						return tag.BeatsPerMinute;

				return 0;
			}

			set { if (tags.Count > 0) tags[0].BeatsPerMinute = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Conductor" />
		public override string Conductor {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Conductor;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Conductor = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Copyright" />
		public override string Copyright {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.Copyright;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].Copyright = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-<see
		///    langword="null" /> value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.DateTagged" />
		public override DateTime? DateTagged {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					DateTime? value = tag.DateTagged;

					if (value != null)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].DateTagged = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Artist ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ArtistID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzArtistId" />
		public override string MusicBrainzArtistId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzArtistId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzArtistId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Group ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseGroupID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseGroupId" />
		public override string MusicBrainzReleaseGroupId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseGroupId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseGroupId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseId" />
		public override string MusicBrainzReleaseId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Artist ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseArtistID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseArtistId" />
		public override string MusicBrainzReleaseArtistId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseArtistId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseArtistId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Track ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    TrackID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzTrackId" />
		public override string MusicBrainzTrackId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzTrackId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzTrackId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Disc ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    DiscID for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzDiscId" />
		public override string MusicBrainzDiscId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzDiscId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzDiscId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicIP PUID
		///    for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicIpId" />
		public override string MusicIpId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicIpId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicIpId = value; }
		}

		/// <summary>
		///    Gets and sets the Amazon ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the Amazon ID
		///    for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.AmazonId" />
		public override string AmazonId {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.AmazonId;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].AmazonId = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    Release Status for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseStatus" />
		public override string MusicBrainzReleaseStatus {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseStatus;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseStatus = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    Release Type for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseType" />
		public override string MusicBrainzReleaseType {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseType;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseType = value; }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    Release Country for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child comments are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseCountry" />
		public override string MusicBrainzReleaseCountry {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseCountry;

					if (value != null && value.Length > 0)
						return value;
				}

				return null;
			}
			set { if (tags.Count > 0) tags[0].MusicBrainzReleaseCountry = value; }
		}

		/// <summary>
		///    Gets and sets the ReplayGain Track Value of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="double" /> containing the ReplayGain Track Value of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainTrackGain" />
		public override double ReplayGainTrackGain {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					double value = tag.ReplayGainTrackGain;

					if (!double.IsNaN(value))
						return value;
				}

				return double.NaN;
			}
			set { if (tags.Count > 0) tags[0].ReplayGainTrackGain = value; }
		}

		/// <summary>
		///    Gets and sets the ReplayGain Peak Value of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="double" /> containing the ReplayGain Peak Value of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainTrackPeak" />
		public override double ReplayGainTrackPeak {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					double value = tag.ReplayGainTrackPeak;

					if (!double.IsNaN(value))
						return value;
				}

				return double.NaN;
			}
			set { if (tags.Count > 0) tags[0].ReplayGainTrackPeak = value; }
		}

		/// <summary>
		///    Gets and sets the ReplayGain Album Value of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="double" /> containing the ReplayGain Album Value of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainAlbumGain" />
		public override double ReplayGainAlbumGain {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					double value = tag.ReplayGainAlbumGain;

					if (!double.IsNaN(value))
						return value;
				}

				return double.NaN;
			}
			set { if (tags.Count > 0) tags[0].ReplayGainAlbumGain = value; }
		}

		/// <summary>
		///    Gets and sets the ReplayGain Album Peak Value of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="double" /> containing the ReplayGain Album Peak Value of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainAlbumPeak" />
		public override double ReplayGainAlbumPeak {
			get {
				foreach (XiphComment tag in tags) {
					if (tag == null)
						continue;

					double value = tag.ReplayGainAlbumPeak;

					if (!double.IsNaN(value))
						return value;
				}

				return double.NaN;
			}
			set { if (tags.Count > 0) tags[0].ReplayGainAlbumPeak = value; }
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
		///    <para>When getting the value, the child comments are
		///    looped through in order and the first non-empty value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in the first
		///    comment.</para>
		/// </remarks>
		/// <seealso cref="Tag.Pictures" />
		public override IPicture[] Pictures {
			get {
				IPicture[] output = new IPicture[0];
				foreach (XiphComment tag in tags)
					if (tag != null && output.Length == 0)
						output = tag.Pictures;

				return output;
			}
			set { if (tags.Count > 0) tags[0].Pictures = value; }
		}

		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if all the comments tags are
		///     empty; otherwise <see langword="false" />.
		/// </value>
		/// <seealso cref="Tag.IsEmpty" />
		public override bool IsEmpty {
			get {
				foreach (XiphComment tag in tags)
					if (!tag.IsEmpty)
						return false;

				return true;
			}
		}

		/// <summary>
		///    Clears all of the child tags.
		/// </summary>
		public override void Clear()
		{
			foreach (XiphComment tag in tags)
				tag.Clear();
		}

		#endregion
	}
}
