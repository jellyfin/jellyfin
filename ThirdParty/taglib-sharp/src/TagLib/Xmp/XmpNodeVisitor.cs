//
// XmpNodeVisitor.cs:
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

namespace TagLib.Xmp
{
	/// <summary>
	///    A visitor that walks the XMP node tree. This can be used to
	///    perform cleanups of XMP data. See the Visitor pattern for
	///    more info if you don't know how to use this.
	/// </summary>
	public interface XmpNodeVisitor
	{
		/// <summary>
		///    Visit an <see cref="XmpNode" />.
		/// </summary>
		/// <param name="node">
		///    The <see cref="XmpNode" /> that is being visited.
		/// </param>
		void Visit (XmpNode node);
	}
}
