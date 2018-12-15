//
// PictureLazy.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2018 Starwer
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

namespace TagLib {


	/// <summary>
	///    This interface provides generic information about ab object,
	///    from which the content can be load only on demand.
	/// </summary>
	public interface ILazy
	{
		/// <summary>
		///    Gets an indication whether the object is loaded.
		/// </summary>
		bool IsLoaded { get; }

		/// <summary>
		/// Load the object data if not done yet.
		/// </summary>
		void Load();

	}

}
