//
// Codec.cs:
//
// Author:
//   Mike Gemuende (mike@gemuende.be)
//
// Copyright (C) 2010 Mike Gemuende
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

namespace TagLib.Png
{

	/// <summary>
	///    A Png photo codec. Contains basic photo details.
	/// </summary>
	public class Codec : Image.Codec
	{

		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public override string Description { get { return "PNG File"; } }


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
		public Codec (int width, int height)
			: base (width, height) {}
	}
}
