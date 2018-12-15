//
// EBMLIDs.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//
// Copyright (C) 2011 FLUENDO S.A.
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

namespace TagLib.Matroska
{
	/// <summary>
	/// Public enumeration listing the possible EBML element identifiers.
	/// </summary>
	public enum EBMLID
	{
		/// <summary>
		/// Indicates an EBML Header element.
		/// </summary>
		EBMLHeader = 0x1A45DFA3,

		/// <summary>
		/// Indicates an EBML Version element.
		/// </summary>
		EBMLVersion = 0x4286,

		/// <summary>
		/// Indicates an EBML Read Version element.
		/// </summary>
		EBMLReadVersion = 0x42F7,

		/// <summary>
		/// Indicates an EBML Max ID Length element.
		/// </summary>
		EBMLMaxIDLength = 0x42F2,

		/// <summary>
		/// Indicates an EBML Max Size Length element.
		/// </summary>
		EBMLMaxSizeLength = 0x42F3,

		/// <summary>
		/// Indicates an EBML Doc Type element.
		/// </summary>
		EBMLDocType = 0x4282,

		/// <summary>
		/// Indicates an EBML Doc Type Version element.
		/// </summary>
		EBMLDocTypeVersion = 0x4287,

		/// <summary>
		/// Indicates an EBML Doc Type Read Version element.
		/// </summary>
		EBMLDocTypeReadVersion = 0x4285,

		/// <summary>
		/// Indicates an EBML Void element.
		/// </summary>
		EBMLVoid = 0xEC
	}
}
