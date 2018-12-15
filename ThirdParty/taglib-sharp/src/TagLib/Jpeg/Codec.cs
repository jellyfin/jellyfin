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

namespace TagLib.Jpeg
{
	/// <summary>
	///    A Jpeg photo codec. Contains basic photo details.
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
		public override string Description { get { return "JFIF File"; } }


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
		///    The quality of the photo.
		/// </param>
		/// <returns>
		///    A new <see cref="Codec" /> instance.
		/// </returns>
		public Codec (int width, int height, int quality)
			: base (width, height, quality) {}
	}
}
