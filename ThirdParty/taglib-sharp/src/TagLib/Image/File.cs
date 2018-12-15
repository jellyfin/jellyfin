//
// File.cs: Base class for Image types.
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2009 Ruben Vermeersch
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

using TagLib.Jpeg;
using TagLib.Gif;
using TagLib.IFD;
using TagLib.Xmp;
using TagLib.Png;

namespace TagLib.Image
{
	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide basic
	///    functionality common to all image types.
	/// </summary>
	public abstract class File : TagLib.File
	{
		private CombinedImageTag image_tag;

#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		protected File (string path) : base (path)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		protected File (IFileAbstraction abstraction) : base (abstraction)
		{
		}

#endregion

#region Public Properties

		/// <summary>
		///    Gets a abstract representation of all tags stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Tag" /> object representing all tags
		///    stored in the current instance.
		/// </value>
		public override Tag Tag { get { return ImageTag; } }

		/// <summary>
		///    Gets a abstract representation of all tags stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Image.CombinedImageTag" /> object
		///    representing all image tags stored in the current instance.
		/// </value>
		public CombinedImageTag ImageTag {
			get { return image_tag; }
			protected set { image_tag = value; }
		}

#endregion

#region Public Methods

		/// <summary>
		///    The method creates all tags which are allowed for the current
		///    instance of the image file. This method can be used to ensure,
		///    that all tags are in place and properties can be safely used
		///    to set values.
		/// </summary>
		public void EnsureAvailableTags ()
		{
			foreach (TagTypes type in Enum.GetValues (typeof (TagTypes))) {
				if ((type & ImageTag.AllowedTypes) != 0x00 && type != TagTypes.AllTags)
					GetTag (type, true);
			}
		}

		/// <summary>
		///    Removes a set of tag types from the current instance.
		/// </summary>
		/// <param name="types">
		///    A bitwise combined <see cref="TagLib.TagTypes" /> value
		///    containing tag types to be removed from the file.
		/// </param>
		/// <remarks>
		///    In order to remove all tags from a file, pass <see
		///    cref="TagTypes.AllTags" /> as <paramref name="types" />.
		/// </remarks>
		public override void RemoveTags (TagLib.TagTypes types)
		{
			List<ImageTag> to_delete = new List<ImageTag> ();

			foreach (ImageTag tag in ImageTag.AllTags) {
				if ((tag.TagTypes & types) == tag.TagTypes)
					to_delete.Add (tag);
			}

			foreach (ImageTag tag in to_delete)
				ImageTag.RemoveTag (tag);
		}

		/// <summary>
		///    Gets a tag of a specified type from the current instance,
		///    optionally creating a new tag if possible.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagLib.TagTypes" /> value indicating the
		///    type of tag to read.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    try and create the tag if one is not found.
		/// </param>
		/// <returns>
		///    A <see cref="Tag" /> object containing the tag that was
		///    found in or added to the current instance. If no
		///    matching tag was found and none was created, <see
		///    langword="null" /> is returned.
		/// </returns>
		public override TagLib.Tag GetTag (TagLib.TagTypes type,
		                                   bool create)
		{
			foreach (Tag tag in ImageTag.AllTags) {
				if ((tag.TagTypes & type) == type)
					return tag;
			}

			if (!create || (type & ImageTag.AllowedTypes) == 0)
				return null;

			ImageTag new_tag = null;
			switch (type) {
			case TagTypes.JpegComment:
				new_tag = new JpegCommentTag ();
				break;

			case TagTypes.GifComment:
				new_tag = new GifCommentTag ();
				break;

			case TagTypes.Png:
				new_tag = new PngTag ();
				break;

			case TagTypes.TiffIFD:
				new_tag = new IFDTag ();
				break;

			case TagTypes.XMP:
				new_tag = new XmpTag ();
				break;
			}

			if (new_tag != null) {
				ImageTag.AddTag (new_tag);
				return new_tag;
			}

			throw new NotImplementedException (String.Format ("Adding tag of type {0} not supported!", type));
		}

		/// <summary>
		/// 	Copies metadata from the given file..
		/// </summary>
		/// <param name='file'>
		/// 	File to copy metadata from.
		/// </param>
		public void CopyFrom (TagLib.Image.File file)
		{
			EnsureAvailableTags ();
			var from_tag = file.ImageTag;
			var to_tag = ImageTag;
			foreach (var prop in typeof (TagLib.Image.ImageTag).GetProperties ()) {
				if (!prop.CanWrite || prop.Name == "TagTypes")
					continue;

				var value = prop.GetValue (from_tag, null);
				prop.SetValue (to_tag, value, null);
			}
		}

#endregion

	}
}
