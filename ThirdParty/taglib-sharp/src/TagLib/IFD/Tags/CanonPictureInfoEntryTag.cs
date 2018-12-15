//
// CanonPictureInfoEntryTag.cs:
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

namespace TagLib.IFD.Tags
{
	/// <summary>
	///    Label tags for Canon Picture Info.
	///    Based on http://www.exiv2.org/tags-canon.html
	/// </summary>
	public enum CanonPictureInfoEntryTag : ushort
	{
		/// <summary>
		///    Image width. (Hex: 0X0002)
		/// </summary>
		ImageWidth                                          = 2,

		/// <summary>
		///    Image height. (Hex: 0X0003)
		/// </summary>
		ImageHeight                                         = 3,

		/// <summary>
		///    Image width (as shot). (Hex: 0X0004)
		/// </summary>
		ImageWidthAsShot                                    = 4,

		/// <summary>
		///    Image height (as shot). (Hex: 0X0005)
		/// </summary>
		ImageHeightAsShot                                   = 5,

		/// <summary>
		///    AF points used. (Hex: 0X0016)
		/// </summary>
		AFPointsUsed                                        = 22,

		/// <summary>
		///    AF points used (20D). (Hex: 0X001A)
		/// </summary>
		AFPointsUsed20D                                     = 26,
	}
}
