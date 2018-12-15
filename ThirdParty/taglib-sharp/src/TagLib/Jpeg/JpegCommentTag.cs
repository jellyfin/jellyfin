//
// JpegCommentTag.cs:
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

using TagLib.Image;

namespace TagLib.Jpeg
{
	/// <summary>
	///    Contains the JPEG comment.
	/// </summary>
	public class JpegCommentTag : ImageTag
	{
#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="value">
		///    The value of the comment.
		/// </param>
		public JpegCommentTag (string value)
		{
			Value = value;
		}

		/// <summary>
		///    Constructor. Creates a new empty comment.
		/// </summary>
		public JpegCommentTag () {
			Value = null;
		}

#endregion

#region Public Properties

		/// <summary>
		///    The value of the comment represented by the current instance.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		///    Gets or sets the comment for the image described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the comment of the
		///    current instace.
		/// </value>
		public override string Comment {
			get { return Value; }
			set { Value = value; }
		}

#endregion

#region Public Methods

		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.JpegComment" />.
		/// </value>
		public override TagTypes TagTypes {
			get { return TagTypes.JpegComment; }
		}

		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			Value = null;
		}

#endregion
	}
}
