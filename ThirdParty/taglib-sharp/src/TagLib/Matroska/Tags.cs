//
// Tags.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2017 Starwer
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
using System.Collections.ObjectModel;

namespace TagLib.Matroska
{
	/// <summary>
	/// Describes all the Matroska Tags in a file as a list, ordered from higher TargetTypeValue to lower. 
	/// A <see cref="Tags"/> object contains several <see cref="Tag"/>
	/// </summary>
	public class Tags : Collection<Tag>
	{
		#region Private fields/Properties

		// Store the Attachments
		private Attachment[] attachments = new Attachment[0];

		#endregion


		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="tracks">List of Matroska tracks</param>
		public Tags(List<Track> tracks)
		{
			_Tracks = tracks;
		}


		#endregion


		#region Override Collection, to keep the items ordered

		/// <summary>
		/// Try to Insert an element to the Tag list at a given index, but can insert it at another index if the 
		/// index doesn't keep this list sorted by descending TargetTypeValue
		/// </summary>
		/// <param name="index">index at which the Tag element should be preferably inserted</param>
		/// <param name="tag">Tag element to be inserted in the Tag list</param>
		protected override void InsertItem(int index, Tag tag)
		{
			if (tag == null) throw new ArgumentNullException("Can't add a null Matroska.Tag to a Matroska.Tags object");

			// Remove duplicate
			for (int j = 0; j < this.Count; j++)
			{
				if (this[j] == tag)
				{
					RemoveAt(j);
					break;
				}
			}

			if (index < 0 || index >= this.Count || this[index].TargetTypeValue < tag.TargetTypeValue || (index + 1 < this.Count && this[index + 1].TargetTypeValue > tag.TargetTypeValue))
			{
				for (index = this.Count - 1; index >= 0; index--)
				{
					if (this[index].TargetTypeValue > tag.TargetTypeValue)
						break;
					if (this[index].TargetTypeValue == tag.TargetTypeValue && (this[index].Elements == null || tag.Elements != null))
						break;
				}

				index++;
			}

			base.InsertItem(index, tag);

		}

		/// <summary>
		/// Replace a tag in the list.
		/// </summary>
		/// <param name="index">Index of the lement to be replaced</param>
		/// <param name="tag">tag to replace the older one</param>
		protected override void SetItem(int index, Tag tag)
		{
			RemoveItem(index);
			InsertItem(index, tag);
		}

		/// <summary>
		/// Remove a Tag from the Tags list
		/// </summary>
		/// <param name="index"></param>
		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
		}

		/// <summary>
		/// Clears the values stored in the current Tags and Children.
		/// </summary>
		protected override void ClearItems()
		{
			Title = null;
			var medium = Medium;

			foreach (var tag in this)
			{
				tag.Clear();
			}

			base.ClearItems();

			// Keep Medium Tag reference unchanged (if any)
			if (medium != null) Add(medium);
		}


		#endregion


		#region Methods

		/// <summary>
		/// Find the first Tag of a given TargetTypeValue
		/// </summary>
		/// <param name="targetType">TargetTypeValue to find</param>
		/// <param name="medium">null: any kind, true: represent the current medium, false: represent a sub-element</param>
		/// <returns>the Tag if match found, null otherwise</returns>
		public Tag Get(TargetType targetType, bool? medium = true)
		{
			Tag ret = null;
			int i;


			// Coerce: Valid values are: 10 20 30 40 50 60 70
			ushort targetTypeValue = (ushort)targetType;
			targetTypeValue = (ushort)
 				(targetTypeValue > 70 ? 70
				: targetTypeValue < 10 ? 10
				: (targetTypeValue / 10) * 10
				);

			// Find first match of the given targetValue
			// List is sorted in descending TargetTypeValue
			for (i = this.Count - 1; i >= 0; i--)
			{
				if (targetTypeValue == this[i].TargetTypeValue)
				{
					ret = this[i];
					if (medium != null)
					{
						bool isMedium = (ret.Elements == null);
						if (medium == isMedium) break;
					}
					else
					{
						break;
					}
				}
			}

			return i >= 0 ? ret : null;
		}

		/// <summary>
		///  Find the first Tag applying to an object (Matroska UID), matching a TargetTypeValue
		/// </summary>
		/// <param name="UIDelement">Matroska Track, Edition, Chapter or Attachment (element having an UID)</param>
		/// <param name="targetTypeValue">TargetTypeValue to match (default: match any)</param>
		/// <returns>the first matching Tag representing the UID, or null if not found.</returns>
		public Tag Get(IUIDElement UIDelement, ushort targetTypeValue = 0)
		{
			Tag ret = null;
			int i;

			ulong UID = UIDelement.UID;

			for (i = this.Count - 1; i >= 0; i--)
			{
				if (targetTypeValue == 0 || targetTypeValue == this[i].TargetTypeValue)
				{
					ret = this[i];
					if (ret.Elements != null)
					{
						foreach (var uid in ret.Elements)
						{
							if (uid.UID == UID) return ret; // found
						}
					}
				}
			}

			return null;
		}



		#endregion


		#region Properties


		/// <summary>
		/// Define if this represent a video content (true), or an audio content (false)
		/// </summary>
		public bool IsVideo = false;


		/// <summary>
		/// Title of the medium, from the Segment
		/// </summary>
		public string Title { get; set; }


		
		/// <summary>
		/// Get/set the Tag that represents the current medium (file)
		/// </summary>
		public Tag Medium
		{
			get
			{
				Tag ret = null;
				bool vid = IsVideo;

				// Try to find a default TargetType
				for (int i = this.Count - 1; i >= 0; i--)
				{
					ret = this[i];
					if (ret.TargetType == TargetType.DEFAULT) // Avoid CD/DVD
					{
						if (ret.Elements == null) return ret;
					}
				}

				// Lower level without UID is the Tag representing the file
				// List is sorted in descending TargetTypeValue
				for (int i = this.Count - 1; i >= 0; i--)
				{
					ret = this[i];
					if (ret.TargetTypeValue != 40 || !vid) // Avoid CD/DVD
					{
						if (ret.Elements == null) break;
					}
				}
				

				return ret;
			}
		}

		/// <summary>
		/// Get/set the Tag that represents the Collection the current medium (file) belongs to.
		/// For Audio, this should be an Album, type 50 (itself if the mka file represents an album).
		/// For Video, this should be a Collection, type 70.
		/// </summary>
		public Tag Album
		{
			get
			{
				TargetType targetValue = IsVideo ? TargetType.COLLECTION : TargetType.ALBUM;
				return Get(targetValue, true);
			}
		}


		/// <summary>
		///    Gets and sets a collection of Attachments associated with
		///    the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:Attachment[]" /> containing a collection of
		///    attachments associated with the media represented by the
		///    current instance or an empty array if none are present.
		/// </value>
		public Attachment [] Attachments
		{
			get
			{
				return attachments;
			}
			set
			{
				if (value==null)
				{
					if(attachments.Length > 0)  attachments = new Attachment[0];
				}
				else
				{
					attachments = value;
				}
			}
		}

		/// <summary>
		/// Get direct access to the Matroska Tracks. 
		/// </summary>
		public ReadOnlyCollection<Track> Tracks
		{
			get { return _Tracks.AsReadOnly(); }
		}
		private List<Track> _Tracks = null;


		#endregion
	}
}
