//
// CanonFileInfoEntryTag.cs:
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
	///    Label tags for Canon File Info.
	///    Based on http://www.exiv2.org/tags-canon.html
	/// </summary>
	public enum CanonFileInfoEntryTag : ushort
	{
		/// <summary>
		///    File Number. (Hex: 0X0001)
		/// </summary>
		FileNumber                                          = 1,

		/// <summary>
		///    Bracket Mode. (Hex: 0X0003)
		/// </summary>
		BracketMode                                         = 3,

		/// <summary>
		///    Bracket Value. (Hex: 0X0004)
		/// </summary>
		BracketValue                                        = 4,

		/// <summary>
		///    Bracket Shot Number. (Hex: 0X0005)
		/// </summary>
		BracketShotNumber                                   = 5,

		/// <summary>
		///    Raw Jpg Quality. (Hex: 0X0006)
		/// </summary>
		RawJpgQuality                                       = 6,

		/// <summary>
		///    Raw Jpg Size. (Hex: 0X0007)
		/// </summary>
		RawJpgSize                                          = 7,

		/// <summary>
		///    Noise Reduction. (Hex: 0X0008)
		/// </summary>
		NoiseReduction                                      = 8,

		/// <summary>
		///    WB Bracket Mode. (Hex: 0X0009)
		/// </summary>
		WBBracketMode                                       = 9,

		/// <summary>
		///    WB Bracket Value AB. (Hex: 0X000C)
		/// </summary>
		WBBracketValueAB                                    = 12,

		/// <summary>
		///    WB Bracket Value GM. (Hex: 0X000D)
		/// </summary>
		WBBracketValueGM                                    = 13,

		/// <summary>
		///    Filter Effect. (Hex: 0X000E)
		/// </summary>
		FilterEffect                                        = 14,

		/// <summary>
		///    Toning Effect. (Hex: 0X000F)
		/// </summary>
		ToningEffect                                        = 15,
	}
}
