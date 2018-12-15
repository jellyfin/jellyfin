//
// SynchData.cs: Provides support for encoding and decoding unsynchronized data
// and numbers.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v2synchdata.cpp from TagLib
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

using System;

namespace TagLib.Id3v2 {
	/// <summary>
	///    This static class provides support for encoding and decoding
	///    unsynchronized data and numbers.
	/// </summary>
	/// <remarks>
	///    Unsynchronization is designed so that portions of the tag won't
	///    be misinterpreted as MPEG audio stream headers by removing the
	///    possibility of the synch bytes occuring in the tag.
	/// </remarks>
	public static class SynchData
	{
		/// <summary>
		///    Decodes synchronized integer data into a <see
		///    cref="uint" /> value.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the number
		///    to decode. Only the first 4 bytes of this value will be
		///    used.
		/// </param>
		/// <returns>
		///    A <see cref="uint" /> value containing the decoded
		///    number.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public static uint ToUInt (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			uint sum = 0;
			int last = data.Count > 4 ? 3 : data.Count - 1;
			
			for(int i = 0; i <= last; i++)
				sum |= (uint) (data [i] & 0x7f)
					<< ((last - i) * 7);
			
			return sum;
		}
		
		/// <summary>
		///    Encodes a <see cref="uint" /> value as synchronized
		///    integer data.
		/// </summary>
		/// <param name="value">
		///    A <see cref="uint" /> value containing the number to
		///    encode.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the encoded
		///    number.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="value" /> is greater than 268435455.
		/// </exception>
		public static ByteVector FromUInt (uint value)
		{
			if ((value >> 28) != 0)
				throw new ArgumentOutOfRangeException ("value",
					"value must be less than 268435456.");
			
			ByteVector v = new ByteVector (4, 0);
			
			for (int i = 0; i < 4; i++)
				v [i] = (byte) (value >> ((3 - i) * 7) & 0x7f);
			
			return v;
		}
		
		/// <summary>
		///    Unsynchronizes a <see cref="ByteVector" /> object by
		///    inserting empty bytes where necessary.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object to unsynchronize.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public static void UnsynchByteVector (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			for (int i = data.Count - 2; i >= 0; i --)
				if (data [i] == 0xFF && (data [i+1] == 0 ||
					(data [i+1] & 0xE0) != 0))
					data.Insert (i+1, 0);
		}
		
		/// <summary>
		///    Resynchronizes a <see cref="ByteVector" /> object by
		///    removing the added bytes.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object to resynchronize.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		public static void ResynchByteVector (ByteVector data)
		{
			if (data == null) {
				throw new ArgumentNullException ("data");
			}

			int i = 0, j = 0;
			while (i < data.Count - 1) {
				if (i != j) {
					data[j] = data[i];
				}
				i += data[i] == 0xFF && data[i + 1] == 0 ? 2 : 1;
				j++;
			}
			if (i < data.Count) {
				data[j++] = data[i++];
			}
			data.Resize (j);
		}
	}
}
