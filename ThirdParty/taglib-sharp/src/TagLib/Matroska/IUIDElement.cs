//
// SimpleTag.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2017 Starwer
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

namespace TagLib.Matroska
{

	/// <summary>
	/// Represent a Matroska element that has an Unique Identifier, and can be tagged.
	/// </summary>
	public interface IUIDElement
	{
		/// <summary>
		/// Unique ID representing the file, as random as possible (setting zero will generate automatically a new one).
		/// </summary>
		ulong UID { get; set; }

		/// <summary>
		/// Get the Tag type the UID should be represented by, or 0 if undefined
		/// </summary>
		MatroskaID UIDType { get; }

	}


	/// <summary>
	/// Represent a basic Matroska UID element
	/// </summary>
	public class UIDElement : IUIDElement
	{

		private MatroskaID _Type = 0;


		#region Constructors

		/// <summary>
		/// Create a UIDElement Stub
		/// </summary>
		/// <param name="type">Tag-type the UID represents</param>
		/// <param name="uid">UID of the element</param>
		public UIDElement (MatroskaID type, ulong uid)
		{
			UID = uid;
			if (  type == MatroskaID.TagTrackUID
   			|| type == MatroskaID.TagEditionUID
   			|| type == MatroskaID.TagChapterUID
   			|| type == MatroskaID.TagAttachmentUID
   			)
				_Type = type;
			else _Type = 0;
		}


		#endregion


		#region Statics

		private static Random random = new Random();

		/// <summary>
		/// Generate a new random UID
		/// </summary>
		/// <param name="ret">Value of the UID to be generated. A zero value will randomize it.</param>
		/// <returns>Generated UID.</returns>
		public static ulong GenUID(ulong ret = 0)
		{
			while (ret == 0)
			{
				ret = ((ulong)random.Next()) << 32;
				ret |= (uint)random.Next();
			}

			return ret;
		}

		#endregion


		#region IUIDElement Boilerplate

		/// <summary>
		/// Unique ID representing the element, as random as possible (setting zero will generate automatically a new one).
		/// </summary>
		public ulong UID
		{
			get { return _UID; }
			set { _UID = UIDElement.GenUID(value); }
		}
		private ulong _UID = UIDElement.GenUID();


		/// <summary>
		/// Get the Tag type the UID should be represented by, or 0 if undefined
		/// </summary>
		public MatroskaID UIDType { get { return _Type; } }

		#endregion

	}


}
