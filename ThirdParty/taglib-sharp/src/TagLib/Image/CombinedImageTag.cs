//
// CombinedImageTag.cs: The class provides an abstraction to combine
// ImageTags.
//
// Author:
//   Mike Gemuende (mike@gemuende.de)
//   Paul Lange (palango@gmx.de)
//
// Copyright (C) 2009 Mike Gemuende
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

using TagLib.IFD;
using TagLib.Xmp;

namespace TagLib.Image
{

	/// <summary>
	///    Combines some <see cref="ImageTag"/> instance to behave as one.
	/// </summary>
	public class CombinedImageTag : ImageTag
	{

#region Private Fields

		/// <summary>
		///    Direct access to the Exif (IFD) tag (if any)
		/// </summary>
		public IFDTag Exif { get; private set; }

		/// <summary>
		///    Direct access to the Xmp tag (if any)
		/// </summary>
		public XmpTag Xmp { get; private set; }

		/// <summary>
		///    Other image tags available in this tag.
		/// </summary>
		public List<ImageTag> OtherTags { get; private set; }

		/// <summary>
		///    Stores the types of the tags, which are allowed for
		///    the current instance.
		/// </summary>
		internal TagTypes AllowedTypes { get; private set; }

		/// <summary>
		///    Returns all image tags in this tag, with XMP
		///    and Exif first.
		/// </summary>
		public List<ImageTag> AllTags {
			get {
				if (all_tags == null) {
					all_tags = new List<ImageTag> ();
					if (Xmp != null)
						all_tags.Add (Xmp);
					if (Exif != null)
						all_tags.Add (Exif);
					all_tags.AddRange (OtherTags);
				}

				return all_tags;
			}
		}

		private List<ImageTag> all_tags = null;

#endregion

#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="CombinedImageTag" /> with a restriction on the
		///    allowed tag types contained in this combined tag.
		/// </summary>
		/// <param name="allowed_types">
		///    A <see cref="TagTypes" /> value, which restricts the
		///    types of metadata that can be contained in this
		///    combined tag.
		/// </param>
		public CombinedImageTag (TagTypes allowed_types)
		{
			AllowedTypes = allowed_types;
			OtherTags = new List<ImageTag> ();
		}

#endregion

#region Protected Methods

		internal void AddTag (ImageTag tag)
		{
			if ((tag.TagTypes & AllowedTypes) != tag.TagTypes)
				throw new Exception (String.Format ("Attempted to add {0} to an image, but the only allowed types are {1}", tag.TagTypes, AllowedTypes));

			if (tag is IFDTag)
				Exif = tag as IFDTag;
			else if (tag is XmpTag)
			{
				// we treat a IPTC-IIM tag as a XMP tag. However, we prefer the real XMP tag.
				// See comments in Jpeg/File.cs for what we should do to deal with this properly.
				if (Xmp != null && (tag is IIM.IIMTag || Xmp is IIM.IIMTag)) {
					var iimTag = tag as IIM.IIMTag;
					if (iimTag == null) {
						iimTag = Xmp as IIM.IIMTag;
						Xmp = tag as XmpTag;
					}

					if (string.IsNullOrEmpty (Xmp.Title))
						Xmp.Title = iimTag.Title;
					if (string.IsNullOrEmpty (Xmp.Creator))
						Xmp.Creator = iimTag.Creator;
					if (string.IsNullOrEmpty (Xmp.Copyright))
						Xmp.Copyright = iimTag.Copyright;
					if (string.IsNullOrEmpty (Xmp.Comment))
						Xmp.Comment = iimTag.Comment;
					if (Xmp.Keywords == null)
						Xmp.Keywords = iimTag.Keywords;
				} else {
					Xmp = tag as XmpTag;
				}
			}
			else
				OtherTags.Add (tag);

			all_tags = null;
		}

		internal void RemoveTag (ImageTag tag)
		{
			if (tag is IFDTag)
				Exif = null;
			else if (tag is XmpTag)
				Xmp = null;
			else
				OtherTags.Remove (tag);

			all_tags = null;
		}

#endregion

#region Public Methods (Tag)

		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="TagLib.TagTypes" />
		///    containing the tag types contained in the current
		///    instance.
		/// </value>
		public override TagTypes TagTypes {
			get {
				TagTypes types = TagTypes.None;

				foreach (ImageTag tag in AllTags)
					types |= tag.TagTypes;

				return types;
			}
		}

		/// <summary>
		///    Clears all of the child tags.
		/// </summary>
		public override void Clear ()
		{
			foreach (ImageTag tag in AllTags)
				tag.Clear ();
		}

		#endregion

		#region Public Properties (ImageTag)

		/// <summary>
		///    Gets or sets the keywords for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the keywords of the
		///    current instace.
		/// </value>
		public override string[] Keywords {
			get {
				foreach (ImageTag tag in AllTags) {
					string[] value = tag.Keywords;
					if (value != null && value.Length > 0)
						return value;
				}

				return new string[] {};
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Keywords = value;
			}
		}

		/// <summary>
		///    Gets or sets the rating for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> containing the rating of the
		///    current instace.
		/// </value>
		public override uint? Rating {
			get {
				foreach (ImageTag tag in AllTags) {
					uint? value = tag.Rating;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Rating = value;
			}
		}

		/// <summary>
		///    Gets or sets the time when the image, the current instance
		///    belongs to, was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the time the image was taken.
		/// </value>
		public override DateTime? DateTime {
			get {
				foreach (ImageTag tag in AllTags) {
					DateTime? value = tag.DateTime;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.DateTime = value;
			}
		}

		/// <summary>
		///    Gets or sets the orientation of the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Image.ImageOrientation" /> containing the orienatation of the
		///    image
		/// </value>
		public override ImageOrientation Orientation {
			get {
				foreach (ImageTag tag in AllTags) {
					ImageOrientation value = tag.Orientation;

					if ((uint) value >= 1U && (uint) value <= 8U)
						return value;
				}

				return ImageOrientation.None;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Orientation = value;
			}
		}

		/// <summary>
		///    Gets or sets the software the image, the current instance
		///    belongs to, was created with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the name of the
		///    software the current instace was created with.
		/// </value>
		public override string Software {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Software;

					if (!string.IsNullOrEmpty(value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Software = value;
			}
		}

		/// <summary>
		///    Gets or sets the latitude of the GPS coordinate the current
		///    image was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the latitude ranging from -90.0
		///    to +90.0 degrees.
		/// </value>
		public override double? Latitude {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.Latitude;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Latitude = value;
			}
		}

		/// <summary>
		///    Gets or sets the longitude of the GPS coordinate the current
		///    image was taken.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the longitude ranging from -180.0
		///    to +180.0 degrees.
		/// </value>
		public override double? Longitude {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.Longitude;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Longitude = value;
			}
		}

		/// <summary>
		///    Gets or sets the altitude of the GPS coordinate the current
		///    image was taken. The unit is meter.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the altitude. A positive value
		///    is above sea level, a negative one below sea level. The unit is meter.
		/// </value>
		public override double? Altitude {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.Altitude;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Altitude = value;
			}
		}

		/// <summary>
		///    Gets the exposure time the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the exposure time in seconds.
		/// </value>
		public override double? ExposureTime {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.ExposureTime;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.ExposureTime = value;
			}
		}

		/// <summary>
		///    Gets the FNumber the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the FNumber.
		/// </value>
		public override double? FNumber {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.FNumber;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.FNumber = value;
			}
		}

		/// <summary>
		///    Gets the ISO speed the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the ISO speed as defined in ISO 12232.
		/// </value>
		public override uint? ISOSpeedRatings {
			get {
				foreach (ImageTag tag in AllTags) {
					uint? value = tag.ISOSpeedRatings;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.ISOSpeedRatings = value;
			}
		}

		/// <summary>
		///    Gets the focal length the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the focal length in millimeters.
		/// </value>
		public override double? FocalLength {
			get {
				foreach (ImageTag tag in AllTags) {
					double? value = tag.FocalLength;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.FocalLength = value;
			}
		}

		/// <summary>
		///    Gets the focal length the image, the current instance belongs
		///    to, was taken with, assuming a 35mm film camera.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the focal length in 35mm equivalent in millimeters.
		/// </value>
		public override uint? FocalLengthIn35mmFilm {
			get {
				foreach (ImageTag tag in AllTags) {
					uint? value = tag.FocalLengthIn35mmFilm;

					if (value != null)
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.FocalLengthIn35mmFilm = value;
			}
		}

		/// <summary>
		///    Gets the manufacture of the recording equipment the image, the
		///    current instance belongs to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the manufacture name.
		/// </value>
		public override string Make {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Make;

					if (!string.IsNullOrEmpty(value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Make = value;
			}
		}

		/// <summary>
		///    Gets the model name of the recording equipment the image, the
		///    current instance belongs to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the model name.
		/// </value>
		public override string Model {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Model;

					if (!string.IsNullOrEmpty(value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Model = value;
			}
		}

		/// <summary>
		///    Gets or sets the creator of the image.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> with the name of the creator.
		/// </value>
		public override string Creator {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Creator;

					if (! string.IsNullOrEmpty (value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Creator = value;
			}
		}

#endregion

#region Public Properties (Tag)


		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		public override string Title {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Title;

					if (! string.IsNullOrEmpty (value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Title = value;
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
		public override string Comment {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Comment;

					if (! string.IsNullOrEmpty (value))
						return value;
				}

				return String.Empty;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Comment = value;
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
		public override string Copyright {
			get {
				foreach (ImageTag tag in AllTags) {
					string value = tag.Copyright;

					if (! string.IsNullOrEmpty (value))
						return value;
				}

				return null;
			}
			set {
				foreach (ImageTag tag in AllTags)
					tag.Copyright = value;
			}
		}

#endregion

	}
}
