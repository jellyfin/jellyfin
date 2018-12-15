//
// IFDTag.cs: Handles Panasonics weird metadata structure.
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2010 Ruben Vermeersch
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

using TagLib.IFD.Tags;

namespace TagLib.Tiff.Rw2
{
	/// <summary>
	///    Handles the weird structure of Panasonic metadata.
	/// </summary>
	public class IFDTag : TagLib.IFD.IFDTag
	{
		private File file;

		internal IFDTag (File file) : base ()
		{
			this.file = file;
		}

		/// <summary>
		///    Gets the ISO speed the image, the current instance belongs
		///    to, was taken with.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the ISO speed as defined in ISO 12232.
		/// </value>
		/// <remarks>
		///    <para>Panasonic stores these in a somewhat unstandard location.</para>
		/// </remarks>
		public override uint? ISOSpeedRatings {
			// TODO: The value in JPGFromRAW should probably be used as well.
			get {
				return Structure.GetLongValue (0, (ushort) PanasonicMakerNoteEntryTag.ISO);
			}
			set {
				Structure.SetLongValue (0, (ushort) PanasonicMakerNoteEntryTag.ISO, value.HasValue ? (uint) value : 0);
			}
		}

		/// <summary>
		///    Gets the focal length the image, the current instance belongs
		///    to, was taken with, assuming a 35mm film camera.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the focal length in 35mm equivalent in millimeters.
		/// </value>
		/// <remarks>
		///    <para>Panasonic stores these in a somewhat unstandard location.</para>
		/// </remarks>
		public override uint? FocalLengthIn35mmFilm {
			get {
				var jpg = file.JpgFromRaw;
				if (jpg == null)
					return base.FocalLengthIn35mmFilm;
				var tag = jpg.GetTag (TagTypes.TiffIFD, true) as Image.ImageTag;
				if (tag == null)
					return base.FocalLengthIn35mmFilm;
				return tag.FocalLengthIn35mmFilm ?? base.FocalLengthIn35mmFilm;
			}
			set {
				(file.JpgFromRaw.GetTag (TagTypes.TiffIFD, true) as Image.ImageTag).FocalLengthIn35mmFilm = value;
			}
		}
	}
}
