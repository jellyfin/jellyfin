//
// Codec.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
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

namespace TagLib.Image
{
	/// <summary>
	///    A photo codec. Contains basic photo details.
	/// </summary>
	public abstract class Codec : IPhotoCodec
	{
#region Properties

		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		public TimeSpan Duration { get { return TimeSpan.Zero; } }

		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="MediaTypes" /> containing
		///    the types of media represented by the current instance.
		/// </value>
		public MediaTypes MediaTypes { get { return MediaTypes.Photo; } }

		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public abstract string Description { get; }

		/// <summary>
		///    Gets the width of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    photo represented by the current instance.
		/// </value>
		public int PhotoWidth  { get; protected set; }

		/// <summary>
		///    Gets the height of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    photo represented by the current instance.
		/// </value>
		public int PhotoHeight { get; protected set; }

		/// <summary>
		///    Gets the (format specific) quality indicator of the photo
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value indicating the quality. A value
		///    0 means that there was no quality indicator for the format
		///    or the file.
		/// </value>
		public int PhotoQuality { get; protected set; }

#endregion

#region Constructors

		/// <summary>
		///    Constructs a new <see cref="Codec" /> with the given width
		///    and height.
		/// </summary>
		/// <param name="width">
		///    The width of the photo.
		/// </param>
		/// <param name="height">
		///    The height of the photo.
		/// </param>
		/// <returns>
		///    A new <see cref="Codec" /> instance.
		/// </returns>
		public Codec (int width, int height) : this (width, height, 0)
		{
		}

		/// <summary>
		///    Constructs a new <see cref="Codec" /> with the given width
		///    and height.
		/// </summary>
		/// <param name="width">
		///    The width of the photo.
		/// </param>
		/// <param name="height">
		///    The height of the photo.
		/// </param>
		/// <param name="quality">
		///    The quality indicator for the photo, if the format supports it.
		/// </param>
		/// <returns>
		///    A new <see cref="Codec" /> instance.
		/// </returns>
		public Codec (int width, int height, int quality)
		{
			PhotoWidth = width;
			PhotoHeight = height;
			PhotoQuality = quality;
		}

#endregion

	}
}
