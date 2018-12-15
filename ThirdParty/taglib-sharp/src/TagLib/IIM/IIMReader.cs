//
//  IIMReader.cs
//
//  Author:
//       Eberhard Beilharz <eb1@sil.org>
//
//  Copyright (c) 2012 Eberhard Beilharz
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;

namespace TagLib.IIM
{
	/// <summary>
	/// Processes all IPTC-IIM segments
	/// </summary>
	public class IIMReader
	{
		/// <summary>
		/// The magic bytes that start a new IPTC-IIM segment
		/// </summary>
		private static readonly byte[] IPTC_IIM_SEGMENT = new byte[] { 0x1C, 0x02};

		private IIMTag Tag { get; set; }
		private ByteVector Data { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Bytes contained in the reader</param>
		public IIMReader (ByteVector data)
		{
			Data = data;
			Tag = new IIMTag ();
		}

		/// <summary>
		/// Proceed with the reading of the IIM
		/// </summary>
		/// <returns></returns>
		public IIMTag Process ()
		{
			// now process the IIM segments which all start with 0x1C 0x02 followed by the type
			// of the IIM segment
			int findOffset = 0;
			int count = 0;
			for (int i = Data.Find (IPTC_IIM_SEGMENT, findOffset); i >= findOffset; i = Data.Find (IPTC_IIM_SEGMENT, findOffset)) {
				count++;
				// skip over segment marker
				i += IPTC_IIM_SEGMENT.Length;

				int len = Data.Mid (i + 1).ToUShort ();

				// ENHANCE: enhance encoding used for string conversion. Unfortunately this is
				// not detectable from IIM data.
				switch (Data [i]) {
					case 5: // Object Name
						Tag.Title = Data.ToString (StringType.Latin1, i + 3, len);
						break;
					case 25: // Keywords
						Tag.AddKeyword (Data.ToString (StringType.Latin1, i + 3, len));
						break;
					case 80: // By-line
						Tag.Creator = Data.ToString (StringType.Latin1, i + 3, len);
						break;
					case 116: // Copyright notice
						Tag.Copyright = Data.ToString (StringType.Latin1, i + 3, len);
						break;
					case 120: // Caption/Abstract
						Tag.Comment = Data.ToString (StringType.Latin1, i + 3, len);
						break;
				}
				findOffset = i + 3 + len;
			}
			if (count == 0)
				return null;
			return Tag;
		}
	}
}
