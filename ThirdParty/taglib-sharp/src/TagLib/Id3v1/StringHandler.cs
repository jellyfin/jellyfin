//
// StringHandler.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v1tag.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002,2003 Scott Wheeler (Original Implementation)
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

using System.Collections;
using System;

namespace TagLib.Id3v1
{
	/// <summary>
	///    This class provides a mechanism for customizing how Id3v1 text
	///    is read and written.
	/// </summary>
	public class StringHandler
	{
		/// <summary>
		///    Converts raw ID3v1 text data to a <see cref="string" />
		///    object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing raw Id3v1
		///    text data.
		/// </param>
		/// <returns>
		///    A <see cref="string"/> object containing the converted
		///    text.
		/// </returns>
		public virtual string Parse (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			string output = data.ToString (StringType.Latin1).Trim ();
			int i = output.IndexOf ('\0');
			return (i >= 0) ? output.Substring (0, i) : output;
		}
		
		/// <summary>
		///    Converts a <see cref="string" /> object to raw ID3v1 text
		///    data.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> object to convert.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> containing the raw ID3v1 text
		///    data.
		/// </returns>
		public virtual ByteVector Render (string text)
		{
			return ByteVector.FromString (text, StringType.Latin1);
		}
	}
}
