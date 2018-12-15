//
// XmpNodeType.cs:
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
using System.Collections.Generic;

namespace TagLib.Xmp
{
	/// <summary>
	///    Denotes the type of a node.
	/// </summary>
	public enum XmpNodeType
	{
		/// <summary>
		///    Unstructured (simple) value node.
		/// </summary>
		Simple,

		/// <summary>
		///    Structured value node.
		/// </summary>
		Struct,

		/// <summary>
		///    Ordered array.
		/// </summary>
		Seq,

		/// <summary>
		///    Language alternative.
		/// </summary>
		Alt,

		/// <summary>
		///    Unordered structured value.
		/// </summary>
		Bag
	}
}
