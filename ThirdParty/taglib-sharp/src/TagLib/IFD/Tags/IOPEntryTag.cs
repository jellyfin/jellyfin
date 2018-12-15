//
// IOPEntryTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009-2010 Ruben Vermeersch
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

namespace TagLib.IFD.Tags
{
	/// <summary>
	///    Entry tags occuring in the Interoperability IFD
	///    The complete overview can be obtained at:
	///    http://www.awaresystems.be/imaging/tiff.html
	/// </summary>
	public enum IOPEntryTag : ushort
	{
		/// <summary>
		///     Indicates the identification of the Interoperability rule. (Hex: 0x0001)
		///     http://www.awaresystems.be/imaging/tiff/tifftags/privateifd/interoperability/interoperabilityindex.html
		/// </summary>
		InteroperabilityIndex                              = 1,

		/// <summary>
		///     Interoperability version. (Hex: 0x0002)
		/// </summary>
		InteroperabilityVersion                            = 2,

		/// <summary>
		///     File format of image file. (Hex: 0x1000)
		/// </summary>
		RelatedImageFileFormat                             = 4096,

		/// <summary>
		///     Image Width. (Hex: 0x1001)
		/// </summary>
		RelatedImageWidth                                  = 4097,

		/// <summary>
		///     Image Height. (Hex: 0x1002)
		/// </summary>
		RelatedImageLength                                 = 4098,
	}
}
