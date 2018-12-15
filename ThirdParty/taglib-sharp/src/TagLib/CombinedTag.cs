//
// CombinedTag.cs: Combines a collection of tags so that they behave as one.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2005-2007 Brian Nickel
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

namespace TagLib {
	/// <summary>
	///    This class combines a collection of tags so that they behave as
	///    one.
	/// </summary>
	public class CombinedTag : Tag
	{
		#region Private Fields
		
		/// <summary>
		///    Contains tags to be combined.
		/// </summary>
		private List<Tag> tags;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="CombinedTag" /> with no internal tags.
		/// </summary>
		/// <remarks>
		///    You can set the tags in the new instance later using
		///    <see cref="SetTags" />.
		/// </remarks>
		public CombinedTag ()
		{
			this.tags = new List<Tag> ();
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="CombinedTag" /> with a specified collection of
		///    tags.
		/// </summary>
		/// <param name="tags">
		///    A <see cref="T:Tag[]" /> containing a collection of tags to
		///    combine in the new instance.
		/// </param>
		public CombinedTag (params Tag [] tags)
		{
			this.tags = new List<Tag> (tags);
		}

		#endregion



		#region Public Properties

		/// <summary>
		///    Gets the tags combined in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:Tag[]" /> containing the tags combined in
		///    the current instance.
		/// </value>
		public virtual Tag [] Tags {
			get {return tags.ToArray ();}
		}

		#endregion



		#region Public Methods

		/// <summary>
		///    Sets the child tags to combine in the current instance.
		/// </summary>
		/// <param name="tags">
		///    A <see cref="T:Tag[]" /> containing the tags to combine.
		/// </param>
		public void SetTags (params Tag [] tags)
		{
			this.tags.Clear ();
			this.tags.AddRange (tags);
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Inserts a tag into the collection of tags in the current
		///    instance.
		/// </summary>
		/// <param name="index">
		///    A <see cref="int" /> value specifying the index at which
		///    to insert the tag.
		/// </param>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to insert into the collection
		///    of tags.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///    <paramref name="index" /> is less than zero or greater
		///    than the count.
		/// </exception>
		protected void InsertTag (int index, Tag tag)
		{
			this.tags.Insert (index, tag);
		}
		
		/// <summary>
		///    Adds a tag at the end of the collection of tags in the
		///    current instance.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to add to the collection of
		///    tags.
		/// </param>
		protected void AddTag (Tag tag)
		{
			this.tags.Add (tag);
		}
		
		/// <summary>
		///    Removes a specified tag from the collection in the
		///    current instance.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to remove from the
		///    collection.
		/// </param>
		protected void RemoveTag (Tag tag)
		{
			this.tags.Remove (tag);
		}
		
		/// <summary>
		///    Clears the tag collection in the current instance.
		/// </summary>
		protected void ClearTags ()
		{
			this.tags.Clear ();
		}
		
		#endregion
		
		
		
		#region Overrides
		
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
				foreach (Tag tag in tags)
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Title" />
		public override string Title {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Title;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Title = value;
			}
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
		public override string Subtitle
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.Subtitle;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Subtitle = value;
			}
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
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.Description;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Description = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Performers" />
		public override string [] Performers {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string [] value = tag.Performers;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string [] {};
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Performers = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.PerformersSort" />
		public override string[] PerformersSort {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string[] value = tag.PerformersSort;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string[] { };
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.PerformersSort = value;
			}
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
		public override string[] PerformersRole
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string[] value = tag.PerformersRole;

					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] { };
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.PerformersRole = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumArtistsSort" />
		public override string[] AlbumArtistsSort {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string[] value = tag.AlbumArtistsSort;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string[] { };
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.AlbumArtistsSort = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumArtists" />
		public override string [] AlbumArtists {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string [] value = tag.AlbumArtists;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string [] {};
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.AlbumArtists = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Composers" />
		public override string [] Composers {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string [] value = tag.Composers;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string [] {};
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Composers = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the sort names for the composer of the 
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the composers of the media described by the 
		///    current instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ComposersSort" />
		public override string[] ComposersSort {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string[] value = tag.ComposersSort;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string[] { };
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ComposersSort = value;
			}
		}
		
		/// <summary>
		///    Gets and sets the sort names for the Track Title of the 
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names
		///    for the Track Title of the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.TitleSort" />
		public override string TitleSort {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.TitleSort;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.TitleSort = value;
			}
		}

		/// <summary>
		///    Gets and sets the sort names for the Album Title of the 
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names
		///    for the Title of the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.AlbumSort" />
		public override string AlbumSort {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.AlbumSort;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.AlbumSort = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Album" />
		public override string Album {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Album;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Album = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Comment" />
		public override string Comment {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Comment;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Comment = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Genres" />
		public override string [] Genres {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string [] value = tag.Genres;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return new string [] {};
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Genres = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Year" />
		public override uint Year {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.Year;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Year = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Track" />
		public override uint Track {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.Track;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Track = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.TrackCount" />
		public override uint TrackCount {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.TrackCount;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.TrackCount = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Disc" />
		public override uint Disc {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.Disc;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Disc = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.DiscCount" />
		public override uint DiscCount {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.DiscCount;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.DiscCount = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Lyrics" />
		public override string Lyrics {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Lyrics;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Lyrics = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Grouping" />
		public override string Grouping {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Grouping;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Grouping = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.BeatsPerMinute" />
		public override uint BeatsPerMinute {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					uint value = tag.BeatsPerMinute;
					
					if (value != 0)
						return value;
				}
				
				return 0;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.BeatsPerMinute = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Conductor" />
		public override string Conductor {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Conductor;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Conductor = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Copyright" />
		public override string Copyright {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.Copyright;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Copyright = value;
			}
		}

		/// <summary>
		///    Gets and sets the date at which the tag has been written.
		/// </summary>
		/// <value>
		///    A nullable <see cref="DateTime" /> object containing the 
		///    date at which the tag has been written, or <see 
		///    langword="null" /> if no value present.
		/// </value>
		public override DateTime? DateTagged
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					DateTime? value = tag.DateTagged;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.DateTagged = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzArtistId" />
		public override string MusicBrainzArtistId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzArtistId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzArtistId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseGroupId" />
		public override string MusicBrainzReleaseGroupId {
			get {
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.MusicBrainzReleaseGroupId;

					if (value != null)
						return value;
				}

				return null;
			}

			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseGroupId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseId" />
		public override string MusicBrainzReleaseId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzReleaseId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseArtistId" />
		public override string MusicBrainzReleaseArtistId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzReleaseArtistId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseArtistId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzTrackId" />
		public override string MusicBrainzTrackId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzTrackId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzTrackId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzDiscId" />
		public override string MusicBrainzDiscId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzDiscId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzDiscId = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicIpId" />
		public override string MusicIpId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicIpId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicIpId = value;
			}
		}

		/// <summary>
		///    Gets and sets the Amazon ID.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the Amazon Id
		///    for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.AmazonId" />
		public override string AmazonId {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.AmazonId;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.AmazonId = value;
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseStatus for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseStatus" />
		public override string MusicBrainzReleaseStatus {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzReleaseStatus;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseStatus = value;
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseType for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseType" />
		public override string MusicBrainzReleaseType {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzReleaseType;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseType = value;
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseCountry for the media described by the 
		///    current instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.MusicBrainzReleaseCountry" />
		public override string MusicBrainzReleaseCountry {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					string value = tag.MusicBrainzReleaseCountry;
					
					if (value != null)
						return value;
				}
				
				return null;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.MusicBrainzReleaseCountry = value;
			}
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    and non-empty value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Pictures" />
		public override IPicture [] Pictures {
			get {
				foreach(Tag tag in tags) {
					if (tag == null)
						continue;
					
					IPicture [] value = tag.Pictures;
					
					if (value != null && value.Length > 0)
						return value;
				}
				
				return base.Pictures;
			}
			
			set {
				foreach(Tag tag in tags)
					if(tag != null)
						tag.Pictures = value;
			}
		}

		/// <summary>
		///    Gets and sets the ReplayGain track gain in dB.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value in dB for the track gain as
		///    per the ReplayGain specification.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainTrackGain" />
		public override double ReplayGainTrackGain {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					double value = tag.ReplayGainTrackGain;
					
					if (!double.IsNaN (value))
						return value;
				}
				
				return double.NaN;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ReplayGainTrackGain = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainTrackPeak" />
		public override double ReplayGainTrackPeak {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					double value = tag.ReplayGainTrackPeak;
					
					if (!double.IsNaN (value))
						return value;
				}
				
				return double.NaN;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ReplayGainTrackPeak = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainAlbumGain" />
		public override double ReplayGainAlbumGain {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					double value = tag.ReplayGainAlbumGain;
					
					if (!double.IsNaN (value))
						return value;
				}
				
				return double.NaN;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ReplayGainAlbumGain = value;
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
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-zero value is
		///    returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ReplayGainAlbumPeak" />
		public override double ReplayGainAlbumPeak {
			get {
				foreach (Tag tag in tags) {
					if (tag == null)
						continue;
					
					double value = tag.ReplayGainAlbumPeak;
					
					if (!double.IsNaN (value))
						return value;
				}
				
				return double.NaN;
			}
			
			set {
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ReplayGainAlbumPeak = value;
			}
		}

		/// <summary>
		///    Gets and sets the initial key of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the initial
		///    key of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.InitialKey" />
		public override string InitialKey
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.InitialKey;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.InitialKey = value;
			}
		}

		/// <summary>
		///    Gets and sets the remixer of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the remixer
		///    of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.RemixedBy" />
		public override string RemixedBy
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.RemixedBy;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.RemixedBy = value;
			}
		}

		/// <summary>
		///    Gets and sets the publisher of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the 
		///    publisher of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.Publisher" />
		public override string Publisher
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.Publisher;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.Publisher = value;
			}
		}

		/// <summary>
		///    Gets and sets the ISRC (International Standard Recording Code)
		///    of the song represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the ISRC
		///    of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    <para>When getting the value, the child tags are looped
		///    through in order and the first non-<see langword="null" />
		///    value is returned.</para>
		///    <para>When setting the value, it is stored in each child
		///    tag.</para>
		/// </remarks>
		/// <seealso cref="Tag.ISRC" />
		public override string ISRC
		{
			get
			{
				foreach (Tag tag in tags)
				{
					if (tag == null)
						continue;

					string value = tag.ISRC;

					if (value != null)
						return value;
				}

				return null;
			}

			set
			{
				foreach (Tag tag in tags)
					if (tag != null)
						tag.ISRC = value;
			}
		}

		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if all the child tags are empty.
		///    Otherwise <see langword="false" />.
		/// </value>
		/// <seealso cref="Tag.IsEmpty" />
		public override bool IsEmpty {
			get {
				foreach (Tag tag in tags)
					if (tag.IsEmpty)
						return true;
				
				return false;
			}
		}
		
		/// <summary>
		///    Clears all of the child tags.
		/// </summary>
		public override void Clear ()
		{
			foreach (Tag tag in tags)
				tag.Clear ();
		}
		
		#endregion
	}
}
