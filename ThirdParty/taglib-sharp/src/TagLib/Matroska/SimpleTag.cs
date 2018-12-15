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

using System.Collections.Generic;
using System.Globalization;

namespace TagLib.Matroska
{
	/// <summary>
	/// Describes a SimpleTag content. The TagName property is not part of the SimpleTag. 
	/// A <see cref="Tag"/> object may contain several <see cref="SimpleTag"/>.
	/// A <see cref="SimpleTag"/> object may contains several <see cref="SimpleTag"/>.
	/// </summary>
	public class SimpleTag : ByteVector
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public SimpleTag()
		{
		}


		/// <summary>
		/// Construct from value
		/// </summary>
		public SimpleTag(ByteVector value)
		{
			Value = value;
		}

		#endregion



		#region Properties

		/// <summary>
		/// Indicate if the content of the SimpleTag is in binary (true) or as a string (false).
		/// </summary>
		public bool TagBinary = false;

		/// <summary>
		/// Indication to know if this is the default/original language to use for the given tag.
		/// </summary>
		public bool TagDefault = true;

		/// <summary>
		/// Specifies the language of the tag, as a string.
		/// </summary>
		public string TagLanguage
		{
			get
			{
				var ret = Language.ToString();
				return string.IsNullOrEmpty(ret) ? "und" : ret;
			}
			set
			{
				if (string.IsNullOrEmpty(value) || value == "und")
				{
					Language = CultureInfo.InvariantCulture;
				}
				else
				{
					try
					{
						Language = new CultureInfo(value);
					}
					catch
					{
						Language = CultureInfo.InvariantCulture;
					}
				}
			}
		}

		/// <summary>
		/// Specifies the language of the tag.
		/// </summary>
		public CultureInfo Language = CultureInfo.InvariantCulture;


		/// <summary>
		/// Get/Set the data contained in the SimpleTag
		/// </summary>
		public ByteVector Value
		{
			get { return this; }
			set { Clear(); Add(value); }
		}


		/// <summary>
		/// Children SimpleTag nested inside this SimpleTag
		/// </summary>
		public Dictionary<string, List<SimpleTag>> SimpleTags = null;

		#endregion

		#region Implicit Conversions

		/// <summary>
		/// Convert a SimpleTag to a String in the Default encoding
		/// </summary>
		/// <param name="v"></param>
		public static implicit operator string(SimpleTag v)
		{
			return v != null ? v.ToString(StringType.UTF8) : null;
		}

		#endregion

	}
}
