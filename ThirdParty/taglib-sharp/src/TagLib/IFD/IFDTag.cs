//
// IFDTag.cs: Basic Tag-class to handle an IFD (Image File Directory) with
// its image-tags.
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//   Paul Lange (palango@gmx.de)
//
// Copyright (C) 2009 Ruben Vermeersch
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
using System.IO;

using TagLib.Image;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace TagLib.IFD
{
	/// <summary>
	///    Contains the metadata for one IFD (Image File Directory).
	/// </summary>
	public class IFDTag : ImageTag
	{

#region Private Fields

		/// <summary>
		///    A reference to the Exif IFD (which can be found by following the
		///    pointer in IFD0, ExifIFD tag). This variable should not be used
		///    directly, use the <see cref="ExifIFD"/> property instead.
		/// </summary>
		private IFDStructure exif_ifd = null;

		/// <summary>
		///    A reference to the GPS IFD (which can be found by following the
		///    pointer in IFD0, GPSIFD tag). This variable should not be used
		///    directly, use the <see cref="GPSIFD"/> property instead.
		/// </summary>
		private IFDStructure gps_ifd = null;

#endregion

#region Public Properties

		/// <value>
		///    The IFD structure referenced by the current instance
		/// </value>
		public IFDStructure Structure { get; private set; }

		/// <summary>
		///    The Exif IFD. Will create one if the file doesn't alread have it.
		/// </summary>
		/// <remarks>
		///    <para>Note how this also creates an empty IFD for exif, even if
		///    you don't set a value. That's okay, empty nested IFDs get ignored
		///    when rendering.</para>
		/// </remarks>
		public IFDStructure ExifIFD {
			get {
				if (exif_ifd == null) {
					var entry = Structure.GetEntry (0, IFDEntryTag.ExifIFD) as SubIFDEntry;
					if (entry == null) {
						exif_ifd = new IFDStructure ();
						entry = new SubIFDEntry ((ushort) IFDEntryTag.ExifIFD, (ushort) IFDEntryType.Long, 1, exif_ifd);
						Structure.SetEntry (0, entry);
					}

					exif_ifd = entry.Structure;
				}

				return exif_ifd;
			}
		}

		/// <summary>
		///    The GPS IFD. Will create one if the file doesn't alread have it.
		/// </summary>
		/// <remarks>
		///    <para>Note how this also creates an empty IFD for GPS, even if
		///    you don't set a value. That's okay, empty nested IFDs get ignored
		///    when rendering.</para>
		/// </remarks>
		public IFDStructure GPSIFD {
			get {
				if (gps_ifd == null) {
					var entry = Structure.GetEntry (0, IFDEntryTag.GPSIFD) as SubIFDEntry;
					if (entry == null) {
						gps_ifd = new IFDStructure ();
						entry = new SubIFDEntry ((ushort) IFDEntryTag.GPSIFD, (ushort) IFDEntryType.Long, 1, gps_ifd);
						Structure.SetEntry (0, entry);
					}

					gps_ifd = entry.Structure;
				}

				return gps_ifd;
			}
		}

		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.TiffIFD" />.
		/// </value>
		public override TagTypes TagTypes {
			get { return TagTypes.TiffIFD; }
		}

#endregion

#region Constructors

		/// <summary>
		///    Constructor. Creates an empty IFD tag. Can be populated manually, or via
		///    <see cref="IFDReader"/>.
		/// </summary>
		public IFDTag ()
		{
			Structure = new IFDStructure ();
		}

#endregion

#region Public Methods

		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			throw new NotImplementedException ();
		}

#endregion

#region Metadata fields

		/// <summary>
		///    Gets or sets the comment for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the comment of the
		///    current instace.
		/// </value>
		public override string Comment {
			get {
				var comment_entry = ExifIFD.GetEntry (0, (ushort) ExifEntryTag.UserComment) as UserCommentIFDEntry;

				if (comment_entry == null) {
					var description = Structure.GetEntry (0, IFDEntryTag.ImageDescription) as StringIFDEntry;
					return description == null ? null : description.Value;
				}

				return comment_entry.Value;
			}
			set {
				if (value == null) {
					ExifIFD.RemoveTag (0, (ushort) ExifEntryTag.UserComment);
					Structure.RemoveTag (0, (ushort) IFDEntryTag.ImageDescription);
					return;
				}

				ExifIFD.SetEntry (0, new UserCommentIFDEntry ((ushort) ExifEntryTag.UserComment, value));
				Structure.SetEntry (0, new StringIFDEntry ((ushort) IFDEntryTag.ImageDescription, value));
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
				return Structure.GetStringValue (0, (ushort) IFDEntryTag.Copyright);
			}
			set {
				if (value == null) {
					Structure.RemoveTag (0, (ushort) IFDEntryTag.Copyright);
					return;
				}

				Structure.SetEntry (0, new StringIFDEntry ((ushort) IFDEntryTag.Copyright, value));
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
				return Structure.GetStringValue (0, (ushort) IFDEntryTag.Artist);
			}
			set {
				Structure.SetStringValue (0, (ushort) IFDEntryTag.Artist, value);
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
				return Structure.GetStringValue (0, (ushort) IFDEntryTag.Software);
			}
			set {
				Structure.SetStringValue (0, (ushort) IFDEntryTag.Software, value);
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
			get { return DateTimeOriginal; }
			set { DateTimeOriginal = value; }
		}

		/// <summary>
		///    The time of capturing.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the time of capturing.
		/// </value>
		public DateTime? DateTimeOriginal {
			get {
				return ExifIFD.GetDateTimeValue (0, (ushort) ExifEntryTag.DateTimeOriginal);
			}
			set {
				if (value == null) {
					ExifIFD.RemoveTag (0, (ushort) ExifEntryTag.DateTimeOriginal);
					return;
				}

				ExifIFD.SetDateTimeValue (0, (ushort) ExifEntryTag.DateTimeOriginal, value.Value);
			}
		}

		/// <summary>
		///    The time of digitization.
		/// </summary>
		/// <value>
		///    A <see cref="System.Nullable"/> with the time of digitization.
		/// </value>
		public DateTime? DateTimeDigitized {
			get {
				return ExifIFD.GetDateTimeValue (0, (ushort) ExifEntryTag.DateTimeDigitized);
			}
			set {
				if (value == null) {
					ExifIFD.RemoveTag (0, (ushort) ExifEntryTag.DateTimeDigitized);
					return;
				}

				ExifIFD.SetDateTimeValue (0, (ushort) ExifEntryTag.DateTimeDigitized, value.Value);
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
				var gps_ifd = GPSIFD;
				var degree_entry = gps_ifd.GetEntry (0, (ushort) GPSEntryTag.GPSLatitude) as RationalArrayIFDEntry;
				var degree_ref = gps_ifd.GetStringValue (0, (ushort) GPSEntryTag.GPSLatitudeRef);

				if (degree_entry == null || degree_ref == null)
					return null;

				Rational [] values  = degree_entry.Values;
				if (values.Length != 3)
					return null;

				double deg = values[0] + values[1] / 60.0d + values[2] / 3600.0d;

				if (degree_ref == "S")
					deg *= -1.0d;

				return Math.Max (Math.Min (deg, 90.0d), -90.0d);
			}
			set {
				var gps_ifd = GPSIFD;

				if (value == null) {
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSLatitudeRef);
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSLatitude);
					return;
				}

				double angle = value.Value;

				if (angle < -90.0d || angle > 90.0d)
					throw new ArgumentException ("value");

				InitGpsDirectory ();

				gps_ifd.SetStringValue (0, (ushort) GPSEntryTag.GPSLatitudeRef, angle < 0 ? "S" : "N");

				var entry =
					new RationalArrayIFDEntry ((ushort) GPSEntryTag.GPSLatitude,
					                           DegreeToRationals (Math.Abs (angle)));
				gps_ifd.SetEntry (0, entry);
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
				var gps_ifd = GPSIFD;
				var degree_entry = gps_ifd.GetEntry (0, (ushort) GPSEntryTag.GPSLongitude) as RationalArrayIFDEntry;
				var degree_ref = gps_ifd.GetStringValue (0, (ushort) GPSEntryTag.GPSLongitudeRef);

				if (degree_entry == null || degree_ref == null)
					return null;

				Rational [] values  = degree_entry.Values;
				if (values.Length != 3)
					return null;

				double deg = values[0] + values[1] / 60.0d + values[2] / 3600.0d;

				if (degree_ref == "W")
					deg *= -1.0d;

				return Math.Max (Math.Min (deg, 180.0d), -180.0d);
			}
			set {
				var gps_ifd = GPSIFD;

				if (value == null) {
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSLongitudeRef);
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSLongitude);
					return;
				}

				double angle = value.Value;

				if (angle < -180.0d || angle > 180.0d)
					throw new ArgumentException ("value");

				InitGpsDirectory ();

				gps_ifd.SetStringValue (0, (ushort) GPSEntryTag.GPSLongitudeRef, angle < 0 ? "W" : "E");

				var entry =
					new RationalArrayIFDEntry ((ushort) GPSEntryTag.GPSLongitude,
					                           DegreeToRationals (Math.Abs (angle)));
				gps_ifd.SetEntry (0, entry);
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
				var gps_ifd = GPSIFD;
				var altitude = gps_ifd.GetRationalValue (0, (ushort) GPSEntryTag.GPSAltitude);
				var ref_entry = gps_ifd.GetByteValue (0, (ushort) GPSEntryTag.GPSAltitudeRef);

				if (altitude == null)
					return null;

				if (ref_entry != null && ref_entry.Value == 1)
					altitude *= -1.0d;

				return altitude;
			}
			set {
				var gps_ifd = GPSIFD;

				if (value == null) {
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSAltitudeRef);
					gps_ifd.RemoveTag (0, (ushort) GPSEntryTag.GPSAltitude);
					return;
				}

				double altitude = value.Value;

				InitGpsDirectory ();

				gps_ifd.SetByteValue (0, (ushort) GPSEntryTag.GPSAltitudeRef, (byte)(altitude < 0 ? 1 : 0));
				gps_ifd.SetRationalValue (0, (ushort) GPSEntryTag.GPSAltitude, Math.Abs (altitude));
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
				return ExifIFD.GetRationalValue (0, (ushort) ExifEntryTag.ExposureTime);
			}
			set {
				ExifIFD.SetRationalValue (0, (ushort) ExifEntryTag.ExposureTime, value.HasValue ? (double) value : 0);
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
				return ExifIFD.GetRationalValue (0, (ushort) ExifEntryTag.FNumber);
			}
			set {
				ExifIFD.SetRationalValue (0, (ushort) ExifEntryTag.FNumber, value.HasValue ? (double) value : 0);
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
				return ExifIFD.GetLongValue (0, (ushort) ExifEntryTag.ISOSpeedRatings);
			}
			set {
				ExifIFD.SetLongValue (0, (ushort) ExifEntryTag.ISOSpeedRatings, value.HasValue ? (uint) value : 0);
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
				return ExifIFD.GetRationalValue (0, (ushort) ExifEntryTag.FocalLength);
			}
			set {
				ExifIFD.SetRationalValue (0, (ushort) ExifEntryTag.FocalLength, value.HasValue ? (double) value : 0);
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
				return ExifIFD.GetLongValue (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
			}
			set {
				if (value.HasValue) {
					ExifIFD.SetLongValue (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm, (uint) value);
				} else {
					ExifIFD.RemoveTag (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				}
			}
		}

		/// <summary>
		///    Gets or sets the orientation of the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Image.ImageOrientation" /> containing the orientation of the
		///    image
		/// </value>
		public override ImageOrientation Orientation {
			get {
				var orientation = Structure.GetLongValue (0, (ushort) IFDEntryTag.Orientation);

				if (orientation.HasValue)
					return (ImageOrientation) orientation;

				return ImageOrientation.None;
			}
			set {
				if ((uint) value < 1U || (uint) value > 8U) {
					Structure.RemoveTag (0, (ushort) IFDEntryTag.Orientation);
					return;
				}

				Structure.SetLongValue (0, (ushort) IFDEntryTag.Orientation, (uint) value);
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
				return Structure.GetStringValue (0, (ushort) IFDEntryTag.Make);
			}
			set {
				Structure.SetStringValue (0, (ushort) IFDEntryTag.Make, value);
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
				return Structure.GetStringValue (0, (ushort) IFDEntryTag.Model);
			}
			set {
				Structure.SetStringValue (0, (ushort) IFDEntryTag.Model, value);
			}
		}

#endregion

#region Private Methods

		/// <summary>
		///    Initilazies the GPS IFD with some basic entries.
		/// </summary>
		private void InitGpsDirectory ()
		{
			GPSIFD.SetStringValue (0, (ushort) GPSEntryTag.GPSVersionID, "2 0 0 0");
			GPSIFD.SetStringValue (0, (ushort) GPSEntryTag.GPSMapDatum, "WGS-84");
		}

		/// <summary>
		///    Converts a given (positive) angle value to three rationals like they
		///    are used to store an angle for GPS data.
		/// </summary>
		/// <param name="angle">
		///    A <see cref="System.Double"/> between 0.0d and 180.0d with the angle
		///    in degrees
		/// </param>
		/// <returns>
		///    A <see cref="Rational"/> representing the same angle by degree, minutes
		///    and seconds of the angle.
		/// </returns>
		private Rational[] DegreeToRationals (double angle)
		{
			if (angle < 0.0 || angle > 180.0)
				throw new ArgumentException ("angle");

			uint deg = (uint) Math.Floor (angle);
			uint min = (uint) ((angle - Math.Floor (angle)) * 60.0);
			uint sec = (uint) ((angle - Math.Floor (angle) - (min / 60.0))  * 360000000.0);

			Rational[] rationals = new Rational [] {
				new Rational (deg, 1),
				new Rational (min, 1),
				new Rational (sec, 100000)
			};

			return rationals;
		}

#endregion

	}
}
