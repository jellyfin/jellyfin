//
// BitStream.cs: Helper to read bits from a byte array.
//
// Copyright (C) 2009 Patrick Dehne
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
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace TagLib.Aac
{
	/// <summary>
	///    This class is used to help reading arbitary number of bits from
	///    a fixed array of bytes
	/// </summary>
	public class BitStream
	{
		#region Private Fields

		private BitArray bits;
		private int bitindex;

		#endregion



		#region Constructors

		/// <summary>
		///    Construct a new <see cref="BitStream"/>.
		/// </summary>
		/// <param name="buffer">
		///    A <see cref="T:System.Byte[]"/>, must be 7 bytes long.
		/// </param>
		public BitStream(byte[] buffer)
		{
			Debug.Assert(buffer.Length == 7, "buffer.Length == 7", "buffer size invalid");
			
			if (buffer.Length != 7)
				throw new ArgumentException("Buffer size must be 7 bytes");

			// Reverse bits            
			bits = new BitArray(buffer.Length * 8);
			for (int i = 0; i < buffer.Length; i++)
			{
				for (int y = 0; y < 8; y++)
				{
					bits[i * 8 + y] = ((buffer[i] & (1 << (7 - y))) > 0);
				}
			}
			
			bitindex = 0;
		}

		#endregion



		#region Public Methods
		
		/// <summary>
		///    Reads an Int32 from the bitstream        
		/// </summary>
		/// <param name="numberOfBits">
		///    A <see cref="int" /> value containing the number
		///    of bits to read from the bitstream
		/// </param>
		public int ReadInt32(int numberOfBits)
		{
			Debug.Assert(numberOfBits > 0, "numberOfBits < 1");
			Debug.Assert(numberOfBits <= 32, "numberOfBits <= 32");

			if (numberOfBits <= 0)
				throw new ArgumentException("Number of bits to read must be >= 1");

			if (numberOfBits > 32)
				throw new ArgumentException("Number of bits to read must be <= 32");

			int value = 0;
			int start = bitindex + numberOfBits - 1;
			for (int i = 0; i < numberOfBits; i++)
			{
				value += bits[start] ? (1 << i) : 0;
				bitindex++;
				start--;
			}

			return value;
		}

		#endregion
	}
}
