//
// EBMLElement.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
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

namespace TagLib.Matroska
{
	/// <summary>
	/// Represent a generic EBML Element and its content.
	/// </summary>
	public class EBMLelement
	{

		#region Constructors

		/// <summary>
		/// Constructs an empty <see cref="EBMLelement" />.
		/// </summary>
		public EBMLelement()
		{
		}


		/// <summary>
		/// Construct a <see cref="EBMLelement" /> to contain children elements.
		/// </summary>
		/// <param name="ebmlid">EBML ID of the element to be created.</param>
		public EBMLelement(MatroskaID ebmlid)
		{
			ID = ebmlid;
			Children = new List<EBMLelement>();
		}

		/// <summary>
		/// Construct a <see cref="EBMLelement" /> to contain data.
		/// </summary>
		/// <param name="ebmlid">EBML ID of the element to be created.</param>
		/// <param name="data">EBML data of the element to be created.</param>
		public EBMLelement(MatroskaID ebmlid, ByteVector data)
		{
			ID = ebmlid;
			this.Data = data;
		}


		/// <summary>
		/// Construct <see cref="EBMLelement" /> to contain data.
		/// </summary>
		/// <param name="ebmlid">EBML ID of the element to be created.</param>
		/// <param name="value">EBML data as an <see cref="ulong"/> value.</param>
		public EBMLelement(MatroskaID ebmlid, ulong value)
		{
			ID = ebmlid;
			SetData(value);
		}


		#endregion

		#region Public Properties

		/// <summary>
		/// EBML Element Identifier.
		/// </summary>
		public MatroskaID ID = 0;


		/// <summary>
		/// Get or set the data represented by the EBML
		/// </summary>
		public ByteVector Data = null;


		/// <summary>
		/// Get or set the element embedded in the EBML
		/// </summary>
		public List<EBMLelement> Children = null;


		/// <summary>
		/// Get or set whether the EBML should have a size of one byte more 
		/// than the optimal size.
		/// </summary>
		public bool IncSize = false;


		#endregion


		#region Public Methods

		/// <summary>
		/// EBML Element size in bytes.
		/// </summary>
		public long Size
		{
			get
			{
				long size_length = DataSize;
				return IDSize + EBMLByteSize((ulong)size_length) + (IncSize ? 1 : 0) + size_length;
			}
		}


		/// <summary>
		/// Get the size of the EBML ID, in bytes
		/// </summary>
		public long IDSize
		{
			get
			{
				uint ebml_id = (uint)ID;

				// Figure out the ID size in bytes
				long mask = 0xFF000000, id_length = 4;
				while (id_length > 0 && (ebml_id & mask) == 0)
				{
					id_length--;
					mask >>= 8;
				}
				if (id_length == 0)
					throw new CorruptFileException("invalid EBML ID (zero)");

				return id_length;
			}
		}

		/// <summary>
		/// Get the size of the EBML data-size, in bytes
		/// </summary>
		public long DataSizeSize
		{
			get { return EBMLByteSize((ulong)DataSize) + (IncSize ? 1 : 0);  }
		}


		/// <summary>
		/// EBML Element data/content size in bytes.
		/// </summary>
		public long DataSize
		{
			get
			{
				long ret = 0;

				if (Data != null)
				{
					// Get Data size
					ret = Data.Count;

					if (Children != null)
						throw new UnsupportedFormatException("EBML element cannot contain both Data and Children");
				}
				else
				{
					// Get the content size
					foreach (var child in Children)
					{
						ret += child.Size; 
					}
				}

				return ret;
			}
		}


		/// <summary>
		/// Try to increase the size of the EBML by 1 byte.
		/// </summary>
		/// <returns>True if successfully increased size, false if failed.</returns>
		public bool IncrementSize()
		{
			// Try to extend current DataSizeSize
			if ( !IncSize && DataSizeSize < 8)
			{
				return IncSize = true;
			}
			
			// Try to extend one of the children
			if (Children!=null)
			{
				foreach (var child in Children)
				{
					if (child.IncrementSize()) return true;
				}
			}

			// Failed
			return false;
		}



		/// <summary>
		/// Get the EBML ID and data-size as a vector of bytes.
		/// </summary>
		public ByteVector Header
		{
			get
			{
				// Retrieve sizes
				var id_length = IDSize;
				var size_length = DataSizeSize;

				// Create vector
				ByteVector vector = new ByteVector((int)(id_length + size_length));

				// Construct the ID field
				uint ebml_id = (uint)ID;
				uint mask = (uint)ebml_id;
				for (int i = (int)id_length - 1; i >= 0; i--)
				{
					vector[i] = (byte)(mask & 0xFF);
					mask >>= 8;
				}

				// Construct the data-size field
				ulong lmask = (ulong)DataSize;
				for (int i = (int)(id_length + size_length - 1); i >= id_length; i--)
				{
					vector[i] = (byte)(lmask & 0xFF);
					lmask >>= 8;
				}

				// Set the marker bit of the Data-size
				vector[(int)id_length] |= (byte)(0x100 >> (int)size_length);


				return vector;
			}
		}


		#endregion

		#region Class functions

		/// <summary>
		/// Get the byte-size required to encode an EBML value with the leading 1. 
		/// </summary>
		/// <param name="value">Encoded value</param>
		/// <returns>size in bytes</returns>
		public static long EBMLByteSize(ulong value)
		{
			// Figure out the required data-size size in bytes
			long size_length;
			if (value == 0x7F)
			{
				// Special case: Avoid element-size reserved word of 0xFF (all ones)
				size_length = 2;
			}
			else
			{
				size_length = 8;
				ulong mask = (ulong)0x7F << (7*7);
				while (size_length > 1 && (value & mask) == 0)
				{
					size_length--;
					mask >>= 7;
				}
			}

			return size_length;
		}
		
		#endregion


		#region Public Methods Data access


		/// <summary>
		/// Get a string from EBML Element's data section (UTF-8).
		/// Handle null-termination.
		/// </summary>
		/// <returns>a string object containing the parsed value.</returns>
		public string GetString ()
		{
			if (Data == null)  return null;
			var idx = Data.IndexOf(0x00); // Detected Null termination
			if (idx>=0)  return Data.ToString(StringType.UTF8, 0, idx);
			return Data.ToString(StringType.UTF8);
		}

		/// <summary>
		/// Get a boolean from EBML Element's data section.
		/// </summary>
		/// <returns>a bool containing the parsed value.</returns>
		public bool GetBool ()
		{
			if (Data == null) return false;
			return (Data.ToUInt() > 0);
		}

		/// <summary>
		/// Get a double from EBML Element's data section.
		/// </summary>
		/// <returns>a double containing the parsed value.</returns>
		public double GetDouble ()
		{
			if (Data == null) return 0;

			double result = 0.0;

			if (Data.Count == 4) {
				result = (double)Data.ToFloat();
			}
			else if (Data.Count == 8) {
				result = Data.ToDouble();
			}
			else
			{
				throw new UnsupportedFormatException("Can not read a Double with sizes differing from 4 or 8");
			}

			return result;
		}

		/// <summary>
		/// Get an unsigned integer (any size from 1 to 8 bytes) from EBML Element's data section.
		/// </summary>
		/// <returns>a ulong containing the parsed value.</returns>
		public ulong GetULong ()
		{
			if (Data == null) return 0;
			return Data.ToULong();
		}


		/// <summary>
		/// Get a bytes vector from EBML Element's data section.
		/// </summary>
		/// <returns>a <see cref="ByteVector" /> containing the parsed value.</returns>
		public ByteVector GetBytes()
		{
			return Data;
		}




		/// <summary>
		/// Set data content as <see cref="string"/> to the EBML file
		/// </summary>
		/// <param name="data">data as <see cref="string"/></param>
		public void SetData(string data)
		{
			Data = data;
		}


		/// <summary>
		///  Set data content as <see cref="ulong"/> to the EBML file
		/// </summary>
		/// <param name="data">unsigned long number to write</param>
		public void SetData(ulong data)
		{
			const ulong mask = 0xffffffff00000000;
			bool isLong = (data & mask) != 0;

			ByteVector vector = new ByteVector(isLong ? 8 : 4);
			for (int i = vector.Count - 1; i >= 0; i--)
			{
				vector[i] = (byte)(data & 0xff);
				data >>= 8;
			}

			Data = vector;
		}


		#endregion


		#region Public Methods Write to file

		/// <summary>
		/// Write the EMBL (and all its data/content) to a file.
		/// </summary>
		/// <param name="file">A <see cref="File"/> representing the file to write to.</param>
		/// <param name="position">The byte-position in the file to write the EBML to.</param>
		/// <param name="reserved">The reserved size in bytes that the EBML may overwrite from the given position. (Default: 0, insert)</param>
		public void Write(Matroska.File file, long position, long reserved = 0)
		{
			if (file == null)
				throw new ArgumentNullException("file");

			if (position > file.Length || position < 0)
				throw new ArgumentOutOfRangeException("position");

			if (Data != null && Children != null)
				throw new UnsupportedFormatException("EBML element cannot contain both Data and Children");


			// Reserve required size upfront to speed up writing
			var size = Size;
			if (size > reserved)
			{
				// Extend reserved size
				file.Insert(size - reserved, position + reserved);
				reserved = size;
			}

			// Write the Header
			var header = Header;
			file.Insert(header, position, header.Count);
			position += header.Count;
			reserved -= header.Count;

			// Write the data/content
			if (Data != null)
			{
				file.Insert(Data, position, Data.Count);
			}
			else if(Children != null)
			{
				foreach (var child in Children)
				{
					child.Write(file, position, reserved);
					var csize = child.Size;
					position += csize;
					reserved -= csize;
				}
			}
		}


		#endregion


	}
}
