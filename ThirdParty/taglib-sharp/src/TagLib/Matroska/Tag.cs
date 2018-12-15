//
// Tag.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
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

namespace TagLib.Matroska
{
	/// <summary>
	/// Describes a Matroska Tag.
	/// A <see cref="Tag"/> object may contain several <see cref="SimpleTag"/>.
	/// </summary>
	public class Tag : TagLib.Tag
	{
		#region Private fields/Properties

		/// <summary>
		/// Define if this represent a video content (true), or an audio content (false)
		/// </summary>
		private bool IsVideo
		{
			get
			{
				if (Elements != null)
				{
					foreach (var uid in Elements)
					{
						if (uid is VideoTrack || uid is SubtitleTrack) return true;
					}
					return false;
				}
				else
				{
					return Tags == null || Tags.IsVideo;
				}
			}
		}


		#endregion


		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tags">The Tags object this Tag should be added to.</param>
		/// <param name="targetTypeValue">the Target Type ValueTags this Tag represents.</param>
		/// <param name="element">The UID element that should be represented by this tag.</param>
		public Tag(Tags tags = null, TargetType targetTypeValue = 0, IUIDElement element = null)
		{
			if (targetTypeValue != 0) TargetType = targetTypeValue;
			if (element != null) Elements = new List<IUIDElement>() { element };
			Tags = tags;
			if(tags != null) tags.Add(this);
		}


		#endregion


		#region Methods


		/// <summary>
		/// Create a TargetType from a given TargetTypeValue, depending on the media-type
		/// </summary>
		/// <param name="targetTypeValue">TargetTypeValue to be converted to TargetType (text)</param>
		/// <returns>Representation of the TargetTypeValue</returns>
		public TargetType MakeTargetType(ushort targetTypeValue)
		{
			TargetType ret = 0;

			switch (targetTypeValue)
			{
				case 70: ret = TargetType.COLLECTION; break;
				case 60: ret = IsVideo ? TargetType.SEASON : TargetType.VOLUME; break;
				case 50: ret = IsVideo ? TargetType.MOVIE : TargetType.ALBUM; break;
				case 40: ret = TargetType.PART; break;
				case 30: ret = IsVideo ? TargetType.CHAPTER : TargetType.TRACK; break;
				case 20: ret = IsVideo ? TargetType.SCENE : TargetType.MOVEMENT; break;
				case 10: ret = TargetType.SHOT; break;
			}
			return ret;
		}

		/// <summary>
		/// Return a Tag of a certain Target type.  
		/// </summary>
		/// <param name="create">Create one if it doesn't exist yet.</param>
		/// <param name="targetType">Target Type Value.</param>
		/// <returns>the Tag representing the collection</returns>
		private Tag TagsGet(bool create, TargetType targetType)
		{
			Tag ret = Tags != null ? Tags.Get(targetType, true) : null;
			if (ret == null && create)
			{
				ret = new Tag(Tags, targetType);
			}
			return ret;
		}


		/// <summary>
		/// Return the Tag representing the Album the medium belongs to.  
		/// </summary>
		/// <param name="create">Create one if it doesn't exist yet.</param>
		/// <returns>the Tag representing the collection</returns>
		private Tag TagsAlbum(bool create)
		{
			Tag ret = null;
			if (Tags != null)
			{
				ret = Tags.Album;
				if (ret == null && create)
				{
					var targetType = Tags.IsVideo ? TargetType.COLLECTION  : TargetType.ALBUM;
					ret = new Tag(Tags, targetType);
				}
			}
			return ret;
		}


		/// <summary>
		/// Remove a Tag
		/// </summary>
		/// <param name="key">Tag Name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		public void Remove(string key, string subkey = null)
		{
			List<SimpleTag> list = null;
			if (SimpleTags.TryGetValue(key, out list))
			{
				if (list != null)
				{
					if (subkey != null)
					{
						foreach (var stag in list)
						{
							if (stag.SimpleTags != null)
							{
								List<SimpleTag> slist = null;
								stag.SimpleTags.TryGetValue(subkey, out slist);
								if (slist != null)
								{
									if (list.Count > 1)
									{
										if(slist.Count>0) slist.RemoveAt(0);
									}
									else
									{
										slist.Clear();
									}
								}
							}
						}
					}
					else
					{
						list.Clear();
					}
				}

				if (subkey == null) SimpleTags.Remove(key);
			}
		}


		/// <summary>
		/// Set a Tag value. A null value removes the Tag.
		/// </summary>
		/// <param name="key">Tag Name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="value">value to be set. A list can be passed for a subtag by separating the values by ';'</param>
		public void Set(string key, string subkey, string value)
		{
			if (value == null)
			{
				Remove(key, subkey);
				return;
			}

			List<SimpleTag> list = null;

			SimpleTags.TryGetValue(key, out list);

			if (list == null)
				SimpleTags[key] = list = new List<SimpleTag>(1); 

			if (list.Count == 0)
				list.Add(new SimpleTag());

			if (subkey == null)
			{
				list[0].Value = value;
			}
			else
			{
				if (list[0].SimpleTags == null)
					list[0].SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);

				List<SimpleTag> slist = null;
				list[0].SimpleTags.TryGetValue(subkey, out slist);

				if (slist == null)
					slist = new List<SimpleTag>(1);

				list[0].SimpleTags[subkey] = slist;

				if (slist.Count == 0)
					slist.Add(new SimpleTag());

				// Sub-values
				var svalues = value.Split(';');
				int j;
				for (j = 0; j < svalues.Length; j++)
				{
					SimpleTag subtag;
					if (j >= slist.Count)
						slist.Add(subtag = new SimpleTag());
					else
						subtag = slist[j];

					subtag.Value = svalues[j];
				}

				if (j < slist.Count)
					slist.RemoveRange(j, slist.Count - j);
			}

		}

		/// <summary>
		/// Set a Tag value as unsigned integer. Please note that a value zero removes the Tag.
		/// </summary>
		/// <param name="key">Tag Name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="value">unsigned integer value to be set</param>
		/// <param name="format">Format for string convertion to be used (default: null)</param>
		public void Set(string key, string subkey, uint value, string format = null)
		{
			if (value == 0)
			{
				Remove(key, subkey);
				return;
			}

			Set(key, subkey, value.ToString(format, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Create or overwrite the actual tags of a given name/sub-name by new values. 
		/// </summary>
		/// <param name="key">Tag Name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="values">Array of values. for each subtag value, a list can be passed by separating the values by ';'</param>
		public void Set(string key, string subkey, string[] values)
		{
			if (values == null)
			{
				Remove(key, subkey);
				return;
			}

			List<SimpleTag> list = null;

			SimpleTags.TryGetValue(key, out list);

			if (list == null)
				SimpleTags[key] = list = new List<SimpleTag>(1);

			int i;
			for (i = 0; i < values.Length; i++)
			{
				SimpleTag stag;
				if (i >= list.Count)
					list.Add(stag = new SimpleTag());
				else
					stag = list[i];

				if (subkey == null)
				{
					stag.Value = values[i];
				}
				else
				{
					if (stag.SimpleTags == null)
						stag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);

					List<SimpleTag> slist = null;
					stag.SimpleTags.TryGetValue(subkey, out slist);

					if (slist == null)
						slist = new List<SimpleTag>(1);

					stag.SimpleTags[subkey] = slist;

					// Sub-values
					var svalues = values[i].Split(';');
					int j;
					for (j = 0; j < svalues.Length; j++)
					{
						SimpleTag subtag;
						if (j >= slist.Count)
							slist.Add(subtag = new SimpleTag());
						else
							subtag = slist[j];

						subtag.Value = svalues[j].Trim();
					}

					if (j < slist.Count)
						slist.RemoveRange(j, slist.Count - j);
				}
			}


			if(subkey == null && i < list.Count)
				list.RemoveRange(i, list.Count - i);
		}


		/// <summary>
		/// Retrieve a Tag list. If there are multiple tag inside a SimpleTag (when
		/// accessing a sub-key), these sub-list are represented as semicolon-separated
		/// values.
		/// </summary>
		/// <param name="key">Tag name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="recu">Also search in parent Tag if true (default: true)</param>
		/// <returns>Array of values. Nested sub-list are represented by a semicolon-
		/// separated string 
		/// </returns>
		public string[] Get(string key, string subkey = null, bool recu = true)
		{
			string[] ret = null;

			List<SimpleTag> mtags;
			if ((!SimpleTags.TryGetValue(key, out mtags) || mtags == null) && recu)
			{
				Tag tag = this;
				while ((tag = tag.Parent) != null && !tag.SimpleTags.TryGetValue(key, out mtags)) ;
			}

			if (subkey != null && mtags != null)
			{
				ret = new string[mtags.Count];

				// Handle Nested SimpleTags
				for (int i = 0; i < mtags.Count; i++)
				{
					string str = null;

					var stag = mtags[i];
					if (stag.SimpleTags != null)
					{
						List<SimpleTag> list = null;
						stag.SimpleTags.TryGetValue(subkey, out list);
						if (list == null || list.Count==0)
						{
							str = null;
						}
						else if (mtags.Count == 1)
						{
							str = list[0];
						}
						else
						{
							str = string.Join("; ", list);
						}
					}

					ret[i] = str;
				}

			}
			else if (mtags != null)
			{
				ret = new string[mtags.Count];
				for (int i = 0; i < mtags.Count; i++)
				{
					ret[i] = mtags[i];
				}
			}

			return ret;
		}

		/// <summary>
		/// Retrieve a Tag value as string
		/// </summary>
		/// <param name="key">Tag name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="recu">Also search in parent Tag if true (default: true)</param>
		/// <returns>Tag value</returns>
		private string GetString(string key, string subkey = null, bool recu = true)
		{
			string ret = null;

			string[] list = Get(key, subkey, recu);
			if (list != null && list.Length>0) ret = list[0];

			return ret;
		}


		/// <summary>
		/// Retrieve a Tag value as unsigned integer
		/// </summary>
		/// <param name="key">Tag name</param>
		/// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
		/// <param name="recu">Also search in parent Tag if true (default: false)</param>
		/// <returns>Tag value as unsigned integer</returns>
		private uint GetUint(string key, string subkey = null, bool recu = false)
		{
			uint ret = 0;
			string val = GetString(key, subkey, recu);

			if (val != null)
			{
				uint.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);
			}

			return ret;
		}


		#endregion


		#region Properties

		/// <summary>
		/// Retrieve a list of Matroska Tags 
		/// </summary>
		public Tags Tags { private set; get; }


		/// <summary>
		/// Retrieve the parent Tag, of higher TargetTypeValue (if any, null if none).
		/// This will only match the tag applying to the  same target as the current tag, or to more elements.
		/// </summary>
		public Tag Parent
		{
			get
			{
				Tag ret = null;

				if (Tags != null)
				{
					int i = Tags.IndexOf(this);
					while (i > 0)
					{
						i--;
						ret = Tags[i];

						bool match = true;

						if (ret.Elements != null)
						{
							if (Elements == null)
							{
								match = false;
								break;
							}
							else
							{ 
								// All UID in the reference should be found also in the parent
								foreach (var refUid in Elements)
								{
									bool submatch = false;
									foreach (var uid in ret.Elements)
									{
										if (uid == refUid)
										{
											submatch = true;
											break;
										}
									}

									match = match && submatch;
								}
							}
						}

						if (match)
						{
							return ret;
						}
					}
				}

				return null;
			}
		}


		/// <summary>
		///    Gets the Matroska Target Type Value of this Tag.
		///    This value can be change with the <see cref="TargetType"/> property. 
		/// </summary>
		public ushort TargetTypeValue
		{
			get
			{
				ushort ret = (ushort)TargetType;

				// Coerce: Valid values are: 0 10 20 30 40 50 60 70
				ret = (ushort) (ret > 70 ? 70 : (ret / 10) * 10);

				return ret;
			}
		}

		/// <summary>
		/// Get or set the Matroska Target Type of this Tag.
		/// </summary>
		public TargetType TargetType
		{
			get
			{
				return _TargetType;
			}
			set
			{
				_TargetType = value;

				// Make sure the List keeps ordered
				if (Tags != null)
				{
					Tags.Add(this);
				}
			}
		}
		private TargetType _TargetType = TargetType.DEFAULT;

		/// <summary>
		/// Array of UID elements the tag applies to. If null, the tag apply to all elements.
		/// </summary>
		public List<IUIDElement> Elements = null;


		/// <summary>
		/// List SimpleTag contained in the current Tag (must never be null)
		/// </summary>
		public Dictionary<string, List<SimpleTag>> SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);



		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Matroska" />.
		/// </value>
		public override TagTypes TagTypes
		{
			get { return TagTypes.Matroska; }
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
		///    This property is implemented using the TITLE tag and the Segment Title.
		/// </remarks>
		public override string Title
		{
			get
			{
				var ret = GetString("TITLE");
				if (ret == null && Tags != null && Tags.Medium == this) ret = Tags.Title;
				return ret;
			}
			set
			{
				Set("TITLE", null, value);
				if (Tags != null && Tags.Medium == this) Tags.Title = value;
			}
		}

		/// <summary>
		///    Gets and sets the sort names for the Track Title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort name of 
		///    the Track Title of the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "SORT_WITH" inside the "TITLE" SimpleTag.
		/// </remarks>
		public override string TitleSort
		{
			get { return GetString("TITLE", "SORT_WITH"); }
			set { Set("TITLE", "SORT_WITH", value); }
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
		///    This property is implemented using the Matroska 
		///    SimpleTag "SUBTITLE".
		/// </remarks>
		public override string Subtitle
		{
			get { return GetString("SUBTITLE"); }
			set { Set("SUBTITLE", null, value); }
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
		///    This property is implemented using the Matroska 
		///    SimpleTag "SUMMARY" (note that this is not the
		///    "DESCRIPTION" tag).
		/// </remarks>
		public override string Description
		{
			get { return GetString("SUMMARY"); }
			set { Set("SUMMARY", null, value); }
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
		///    This property is implemented using the ACTOR/PERFORMER stored in
		///    the MKV Tag element.
		/// </remarks>
		public override string [] Performers
		{
			get { return Get(IsVideo ? "ACTOR" : "PERFORMER"); }
			set { Set(IsVideo ? "ACTOR" : "PERFORMER", null, value); }
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
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "SORT_WITH" inside the "ACTOR" or "PERFORMER" SimpleTag.
		/// </remarks>
		public override string [] PerformersSort
		{
			get { return Get(IsVideo ? "ACTOR" : "PERFORMER", "SORT_WITH"); }
			set { Set(IsVideo ? "ACTOR" : "PERFORMER", "SORT_WITH", value); }
		}


		/// <summary>
		///    Gets and sets the role of the performers or artists
		///    who performed in the media described by the current instance.
		///    For an movie, this represents a character of an actor.
		///    For a music, this may represent the instrument of the artist.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the roles for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "CHARACTER" or "INSTRUMENTS" inside the 
		///    "ACTOR" or "PERFORMER" SimpleTag.
		/// </remarks>
		public override string[] PerformersRole
		{
			get { return Get(IsVideo ? "ACTOR" : "PERFORMER", IsVideo ? "CHARACTER" : "INSTRUMENTS"); }
			set { Set(IsVideo ? "ACTOR" : "PERFORMER", IsVideo ? "CHARACTER" : "INSTRUMENTS", value); }
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
		///    This property is implemented using the "ARTIST" Tag.
		/// </remarks>
		public override string [] AlbumArtists
		{
			get
			{
				var tag = TagsAlbum(false);
				return tag != null ? tag.Get("ARTIST") : null;
			}
			set
			{
				var tag = TagsAlbum(true);
				if (tag != null ) tag.Set("ARTIST", null, value);
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
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "SORT_WITH" inside the "ARTIST" SimpleTag.
		/// </remarks>

		public override string [] AlbumArtistsSort
		{
			get
			{
				var tag = TagsAlbum(false);
				return tag != null ? tag.Get("ARTIST", "SORT_WITH") : null;
			}
			set
			{
				var tag = TagsAlbum(true);
				if (tag != null) tag.Set("ARTIST", "SORT_WITH", value);
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
		///    This property is implemented using the "COMPOSER" Tag.
		/// </remarks>
		public override string [] Composers
		{
			get { return Get("COMPOSER"); }
			set { Set("COMPOSER", null, value); }
		}


		/// <summary>
		///    Gets and sets the sort names for the composers of the 
		///    media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the composers of the media represented by the 
		///    current instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    <para>This field is typically optional but aids in the
		///    sorting of compilations or albums with multiple Composers.
		///    </para>
		///    <para>As this value is to be used as a sorting key, it
		///    should be used with less variation than <see
		///    cref="Composers" />. Where performers can be broken into
		///    muliple artist it is best to stick with a single composer.
		///    For example, "McCartney, Paul".</para>
		/// </remarks>
		/// <remarks>
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "SORT_WITH" inside the "COMPOSER" SimpleTag.
		/// </remarks>
		public override string[] ComposersSort
		{
			get { return Get("COMPOSER", "SORT_WITH"); }
			set { Set("COMPOSER", "SORT_WITH", value); }
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
		///    This property is implemented using the "TITLE" Tag in the Collection Tags.
		/// </remarks>
		public override string Album
		{
			get
			{
				var tag = TagsAlbum(false);
				return tag != null ? tag.GetString("TITLE") : null;
			}
			set
			{
				var tag = TagsAlbum(true);
				if (tag != null) tag.Set("TITLE", null, value);
			}
		}

		/// <summary>
		///    Gets and sets the sort names for the Album Title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort name of 
		///    the Album Title of the media described by the current
		///    instance or null if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the nested Matroska 
		///    SimpleTag "SORT_WITH" inside the "TITLE" SimpleTag.
		/// </remarks>
		public override string AlbumSort
		{
			get
			{
				var tag = TagsAlbum(false);
				return tag != null ? tag.GetString("TITLE", "SORT_WITH") : null;
			}
			set
			{
				var tag = TagsAlbum(true);
				if (tag != null) tag.Set("TITLE", "SORT_WITH", value);
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
		///    This property is implemented using the "COMMENT" Tag.
		/// </remarks>
		public override string Comment
		{
			get { return GetString("COMMENT"); }
			set { Set("COMMENT", null, value); }
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
		///    This property is implemented using the "GENRE" Tag.
		/// </remarks>
		public override string [] Genres
		{
			get
			{
				string value = GetString("GENRE");

				if (value == null || value.Trim ().Length == 0)
					return new string [] { };

				string [] result = value.Split (';');

				for (int i = 0; i < result.Length; i++) {
					string genre = result [i].Trim ();

					byte genre_id;
					int closing = genre.IndexOf (')');
					if (closing > 0 && genre [0] == '(' &&
						byte.TryParse (genre.Substring (
						1, closing - 1), out genre_id))
						genre = TagLib.Genres
							.IndexToAudio (genre_id);

					result [i] = genre;
				}

				return result;
			}

			set
			{
				Set("GENRE", null, value != null ? String.Join ("; ", value) : null);
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
		public override uint Year
		{
			get
			{
				string val = GetString("DATE_RECORDED");
				uint ret = 0;

				// Parse Date to retrieve year
				// Expected format: YYYY-MM-DD HH:MM:SS.MSS 
				//   with: YYYY = Year, -MM = Month, -DD = Days, 
				//         HH = Hours, :MM = Minutes, :SS = Seconds, :MSS = Milliseconds
				if (val != null)
				{
					int off = val.IndexOf('-');
					if (off > 0) val = val.Substring(0, off);
					uint.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);
				}

				return ret;
			}

			set { Set("DATE_RECORDED", null, value); }
		}

		/// <summary>
		///    Gets and sets the position of the media represented by
		///    the current instance in its containing item (album, disc, episode, collection...).
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the position of the
		///    media represented by the current instance in its
		///    containing album or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "PART_NUMBER" Tag.
		/// </remarks>
		public override uint Track
		{
			get { return GetUint("PART_NUMBER"); }
			set { Set("PART_NUMBER", null, value, "00"); }
		}

		/// <summary>
		///    Gets and sets the number of items contained in the parent Tag (album, disc, episode, collection...)
		///    the media represented by the current instance belongs to.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of tracks in
		///    the album containing the media represented by the current
		///    instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TOTAL_PARTS" Tag
		///    in the parent tag (one level up).
		/// </remarks>
		public override uint TrackCount
		{
			get
			{
				var tag = TagsGet(false, MakeTargetType((ushort)(TargetTypeValue + 10)));
				return tag != null ? tag.GetUint("TOTAL_PARTS") : 0;
			}
			set
			{
				var tag = TagsGet(true, MakeTargetType((ushort)(TargetTypeValue + 10)));
				if (tag != null) tag.Set("TOTAL_PARTS", null, value);
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
		///    This property is implemented using the "PART_NUMBER" Tag in
		///    a parent tag (VOLUME for video, PART for audio).
		/// </remarks>
		public override uint Disc
		{
			get
			{
				var tag = TagsGet(false, IsVideo ? TargetType.VOLUME : TargetType.PART);
				return tag != null ? tag.GetUint("PART_NUMBER") : 0;
			}
			set
			{
				var tag = TagsGet(true, IsVideo ? TargetType.VOLUME : TargetType.PART);
				if (tag != null) tag.Set("PART_NUMBER", null, value);
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
		///    This property is implemented using the "TOTAL_PARTS" Tag in
		///    a parent tag (COLLECTION for video, ALBUM for audio).
		/// </remarks>
		public override uint DiscCount
		{
			get
			{
				var tag = TagsGet(false, IsVideo ? TargetType.COLLECTION : TargetType.ALBUM);
				return tag != null ? tag.GetUint("TOTAL_PARTS") : 0;
			}
			set
			{
				var tag = TagsGet(true, IsVideo ? TargetType.COLLECTION : TargetType.ALBUM);
				if (tag != null) tag.Set("TOTAL_PARTS", null, value);
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
		public override string Lyrics
		{
			get { return GetString("LYRICS"); }
			set { Set("LYRICS", null, value); }
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
		public override string Grouping
		{
			get
			{
				var tag = TagsAlbum(false);
				return tag != null ? tag.GetString("GROUPING") : null;
			}
			set
			{
				var tag = TagsAlbum(true);
				if (tag != null) tag.Set("GROUPING", null, value);
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
		public override uint BeatsPerMinute
		{
			get { return GetUint("BPM", null, true); }
			set { Set("BPM", null, value); }
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
		public override string Conductor
		{
			get { return GetString(IsVideo ? "DIRECTOR" : "CONDUCTOR"); }
			set { Set(IsVideo ? "DIRECTOR" : "CONDUCTOR", null, value); }
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
		///    This property is implemented using the "COPYRIGHT" Tag.
		/// </remarks>
		public override string Copyright
		{
			get { return GetString("COPYRIGHT"); }
			set { Set("COPYRIGHT", null, value); }
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
		///    This property is implemented using the "DATE_TAGGED" Tag.
		/// </remarks>
		public override DateTime? DateTagged
		{
			get
			{
				string value =  GetString("DATE_TAGGED");
				if (value != null)
				{
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
				}
				Set("DATE_TAGGED", null, date);
			}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Artist ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ArtistID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzArtistId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Group ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseGroupID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseGroupId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Artist ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseArtistID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseArtistId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Track ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    TrackID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzTrackId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Disc ID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    DiscID for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzDiscId
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicIPPUID 
		///    for the media described by the current instance or
		///    null if no value is present.
		/// </value>
		public override string MusicIpId
		{
			get { return null; }
			set { }
		}

		// <summary>
		//    Gets and sets the AmazonID of
		//    the media described by the current instance.
		// </summary>
		// <value>
		//    A <see cref="string" /> containing the AmazonID 
		//    for the media described by the current instance or
		//    null if no value is present.  
		// </value>
		// <remarks>
		//    A definition on where to store the ASIN for
		//    Windows Media is not currently defined
		// </remarks>
		//public override string AmazonId {
		//    get { return null; }
		//    set {}
		//}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Status of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseStatus for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseStatus
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Type of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseType for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseType
		{
			get { return null; }
			set { }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz 
		///    ReleaseCountry for the media described by the current
		///    instance or null if no value is present.
		/// </value>
		public override string MusicBrainzReleaseCountry
		{
			get { return null; }
			set { }
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
		public override IPicture [] Pictures
		{
			get
			{
				return Tags != null ? Tags.Attachments : null;
			}

			set
			{
				if (value == null)
				{
					Tags.Attachments = null;
				}
				else if (value is Attachment[])
				{
					Tags.Attachments = (Attachment[])value;
				}
				else
				{
					var attach = new Attachment[value.Length]; 
					for (int i = 0; i < attach.Length; i++)
					{
						if (value[i] is Attachment)
						{
							attach[i] = value[i] as Attachment;
						}
						else
						{
							attach[i] = new Attachment(value[i]);
						}
					}

					Tags.Attachments = attach;
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
		public override bool IsEmpty
		{
			get
			{
				return SimpleTags.Count == 0;
			}
		}

		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			SimpleTags.Clear();
		}

		#endregion
	}
}
