//
// EBMLElement.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
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


namespace TagLib.Matroska
{
	/// <summary>
	/// Read a Matroska EBML element from a file, but also provides basic modifications to an 
	/// EBML element directly on the file (write). This can also represent an abstract EBML 
	/// on the file (placeholder).
	/// </summary>
	/// <remarks>
	///  This was intitialy called <see cref="EBMLelement"/>, but this was in fact a file-reader.
	///  The name <see cref="EBMLelement"/> correspond more to the class which has been created to
	///  represent an EBML structure (regardless of file-issues) to support the EBML writing to file.
	/// </remarks>
	public class EBMLreader
	{
		#region Private Fields

		private Matroska.File file;
		private EBMLreader parent;
		private ulong offset;
		private ulong data_offset;
		private ulong ebml_size;
		private uint ebml_id;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a root <see cref="EBMLreader" /> instance, by reading from
		/// the provided file position.
		/// </summary>
		/// <param name="_file"><see cref="File" /> File instance to read from.</param>
		/// <param name="position">Position in the file to start reading from.</param>
		public EBMLreader(Matroska.File _file, ulong position)
		{
			// Keep a reference to the file
			file = _file;
			parent = null;

			// Initialize attributes
			offset = position;
			data_offset = position;
			ebml_id = 0;
			ebml_size = 0;

			// Actually read the EBML on the file
			Read(true);
		}


		/// <summary>
		/// Constructs a child <see cref="EBMLreader" /> reading the data from the
		/// EBML parent at the provided file position.
		/// </summary>
		/// <param name="parent">The <see cref="EBMLreader" /> that contains the instance to be created.</param>
		/// <param name="position">Position in the file to start reading from.</param>
		public EBMLreader(EBMLreader parent, ulong position)
		{
			if (parent == null)
				throw new ArgumentNullException("file");

			// Keep a reference to the file
			file = parent.file;
			this.parent = parent;

			// Initialize attributes
			offset = position;
			data_offset = position;
			ebml_id =  0;
			ebml_size = 0;


			// Actually read the EBML on the file
			Read(true);
		}


		/// <summary>
		/// Create a new abstract <see cref="EBMLreader" /> with arbitrary attributes, 
		/// without reading its information on the file.
		/// </summary>
		/// <param name="parent">The <see cref="EBMLreader" /> that contains the instance to be described.</param>
		/// <param name="position">Position in the file.</param>
		/// <param name="ebmlid">EBML ID of the element</param>
		/// <param name="size">Total size of the EBML, in bytes</param>
		public EBMLreader(EBMLreader parent, ulong position, MatroskaID ebmlid, ulong size = 0)
		{
			// Keep a reference to the file
			if (parent != null) file = parent.file;
			this.parent = parent;

			// Initialize attributes
			offset = position;
			data_offset = offset;
			ebml_id = (uint)ebmlid;
			ebml_size = size;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// EBML Element Identifier.
		/// </summary>
		public MatroskaID ID
		{
			get { return (MatroskaID) ebml_id; }
		}

		/// <summary>
		/// EBML Parent instance.
		/// </summary>
		public EBMLreader Parent
		{
			get { return parent; }
		}

		/// <summary>
		/// EBML Element size in bytes.
		/// </summary>
		public ulong Size
		{
			set { ebml_size = value - (data_offset - offset); }
			get { return (data_offset - offset) + ebml_size; }
		}

		/// <summary>
		/// EBML Element data size in bytes.
		/// </summary>
		public ulong DataSize
		{
   		set { ebml_size = value; }
   		get { return ebml_size; }
		}

		/// <summary>
		/// EBML Element data offset position in file in bytes.
		/// </summary>
		public ulong DataOffset
		{
			get { return data_offset; }
		}

		/// <summary>
		/// EBML Element offset position in file in bytes.
		/// </summary>
		public ulong Offset
		{
			set
			{
				data_offset = (ulong)((long)data_offset + ((long)value - (long)offset));
				offset = value;
			}
			get { return offset; }
		}

		/// <summary>
		/// Defines that the EBML element is not read-out from file,
		/// but is an abstract representation of an element on the disk.
		/// </summary>
		public bool Abstract
		{
			get { return offset == data_offset; }
		}


		#endregion

		#region Public Methods for Reading

		/// <summary>
		/// Read EBML header and data-size if it is an abstract one. 
		/// It then becomes a non abstract EBML.
		/// </summary>
		/// <param name="throwException">Throw exception on invalid EBML read if true (Default: false).</param>
		/// <returns>True if successful.</returns>
		public bool Read(bool throwException = false)
		{
			if (!Abstract) return true;

			if (file == null)
				throw new ArgumentNullException("file");

			try
			{
				var ex = new InvalidOperationException("Invalid EBML format Read");

				if (offset >= (ulong)(file.Length) - 1) throw ex;

				// Prepare for Consitency check
				uint ebml_id_check = ebml_id;
				ulong ebml_size_check = Size;


				file.Seek((long)offset);

				// Get the header byte
				ByteVector vector = file.ReadBlock(1);
				byte header_byte = vector[0];
				// Define a mask
				byte mask = 0x80, id_length = 1;
				// Figure out the size in bytes
				while (id_length <= 4 && (header_byte & mask) == 0)
				{
					id_length++;
					mask >>= 1;
				}
				if (id_length > 4) throw ex;

				// Now read the rest of the EBML ID
				if (id_length > 1)
				{
					vector.Add(file.ReadBlock(id_length - 1));
				}

				ebml_id = vector.ToUInt();

				vector.Clear();

				// Get the size length
				vector = file.ReadBlock(1);
				header_byte = vector[0];
				mask = 0x80;
				Byte size_length = 1;

				// Iterate through various possibilities
				while (size_length <= 8 && (header_byte & mask) == 0)
				{
					size_length++;
					mask >>= 1;
				}


				if (size_length > 8)
					size_length = 1; // Special: Empty element (all zero state)
				else
					vector[0] &= (Byte)(mask - 1);  // Clear the marker bit


				// Now read the rest of the EBML element size
				if (size_length > 1)
				{
					vector.Add(file.ReadBlock(size_length - 1));
				}

				ebml_size = vector.ToULong();

				// Special: Auto-size (0xFF byte)
				if (size_length == 1 && ebml_size == 0x7F)
				{
					// Resolve auto-size to fill in to its containing element
					ulong bound = parent == null ? (ulong)file.Length : parent.Offset + parent.Size;
					ebml_size = bound - offset - (ulong)(id_length + size_length);
				}

				data_offset = offset + id_length + size_length;

				// Consistency check: Detect descrepencies between read data and abstract data 
				if (ebml_id_check != 0 && ebml_id_check != ebml_id) throw ex;
				if (ebml_size_check != 0 && ebml_size_check != Size) throw ex;

				return true;

			}
			catch (Exception ex)
			{
				if (throwException) throw ex;
				return false;
			}

		}



		/// <summary>
		/// Reads a vector of bytes (raw data) from EBML Element's data section.
		/// </summary>
		/// <returns>a <see cref="ByteVector" /> containing the parsed value.</returns>
		public ByteVector ReadBytes()
		{
			if (file == null)
			{
				return null;
			}

			file.Seek((long)data_offset);

			ByteVector vector = file.ReadBlock((int)ebml_size);

			return vector;
		}

		/// <summary>
		/// Reads a string from EBML Element's data section (UTF-8).
		/// </summary>
		/// <returns>a string object containing the parsed value.</returns>
		public string ReadString()
		{
			if (file == null ) return null;
			ByteVector vector = ReadBytes();
			var ebml = new EBMLelement((MatroskaID)ebml_id, vector);
			return ebml.GetString();

		}

		/// <summary>
		/// Reads a boolean from EBML Element's data section.
		/// </summary>
		/// <returns>a bool containing the parsed value.</returns>
		public bool ReadBool()
		{
			if (file == null || ebml_size == 0) return false;
			ByteVector vector = ReadBytes();
			var ebml = new EBMLelement((MatroskaID)ebml_id, vector);
			return ebml.GetBool();

		}

		/// <summary>
		/// Reads a double from EBML Element's data section.
		/// </summary>
		/// <returns>a double containing the parsed value.</returns>
		public double ReadDouble()
		{
			if (file == null || ebml_size == 0) return 0;
			ByteVector vector = ReadBytes();
			var ebml = new EBMLelement((MatroskaID) ebml_id, vector);
			return ebml.GetDouble();
		}

		/// <summary>
		/// Reads an unsigned integer (any size from 1 to 8 bytes) from EBML Element's data section.
		/// </summary>
		/// <returns>a ulong containing the parsed value.</returns>
		public ulong ReadULong()
		{
			if (file == null || ebml_size == 0) return 0;
			ByteVector vector = ReadBytes();
			var ebml = new EBMLelement((MatroskaID)ebml_id, vector);
			return ebml.GetULong();
		}


		#endregion

		#region Public Methods for Writing

		/// <summary>
		/// Write the <see cref="DataSize"/> to the EBML file.
		/// Resize the data-size length to 8 bytes.
		/// This will *not* insert extra bytes, but overwrite next contiguous bytes.
		/// It will claim the size added on the value of the data-size.
		/// </summary>
		/// <returns>Offset created in Writing the new data-size</returns>
		public long WriteDataSize()
		{
			ulong value = ebml_size;
			const ulong newsize_length = 8;

			// Figure out the ID size in bytes
			ulong mask = 0xFF000000, id_length = 4;
			while (id_length > 0 && (ebml_id & mask) == 0)
			{
				id_length--;
				mask >>= 8;
			}
			if (id_length == 0)
				throw new CorruptFileException("invalid EBML ID (zero)");

			// Figure out the Data size length in bytes
			ulong size_length = data_offset - offset - id_length;
			if (size_length > 8)
				throw new CorruptFileException("invalid EBML element size");

			// Construct the data-size field
			ByteVector vector = new ByteVector((int)newsize_length);
			mask = value;
			for (int i = (int)newsize_length - 1; i >= 0; i--)
			{
				vector[i] = (byte)(mask & 0xFF);
				mask >>= 8;
			}
			// Set the marker bit
			vector[0] |= (byte)(0x100 >> (int)newsize_length);

			// Write data-size field to file
			file.Insert(vector, (long)(offset + id_length), (long)newsize_length);

			// Update fields
			ulong woffset = newsize_length - size_length;
			data_offset = data_offset + woffset;
			ebml_size = value - woffset;

			return (long)woffset;
		}

		
		/// <summary>
		/// Change an EBML element to a Abstract Void element, but do not write to the file.
		/// </summary>
		/// <remarks>
		/// To do a real conversion to Void EBML element on the file, use <see cref="WriteVoid()"/>.
		/// </remarks>
		public void SetVoid()
		{
			ulong size = Size;

			// Update this object
			ebml_id = (uint) MatroskaID.Void;
			data_offset = offset; // This will make it abstract
			ebml_size = size; // Keep the size unchanged
		}


		/// <summary>
		/// Change an EBML element to a Void element directly on the file.
		/// </summary>
		public void WriteVoid()
		{
			if (Size < 2) throw new ArgumentOutOfRangeException("WriteVoid Size < 2");

			if (file == null)
				throw new ArgumentNullException("WriteVoid file");

			if (offset + Size > (ulong)(file.Length))
				throw new ArgumentOutOfRangeException("WriteVoid tries to write out of the file");


			ByteVector vector;
			int datasize;

			if (Size < 100)
			{
				vector = new ByteVector(2);
				datasize = (int)Size - 2;
				vector[0] = (byte)MatroskaID.Void; // size = 1
				vector[1] = (byte)(0x80 | datasize); // Marker + data-size
			}
			else
			{
				vector = new ByteVector(9);
				datasize = (int)Size - 9;
				vector[0] = (byte)MatroskaID.Void; // size = 1
				vector[1] = 0x01; // set marker

				// Set data size
				int mask = datasize;
				for (int i = 8; i > 1; i--)
				{
					vector[i] = (byte)(mask & 0xFF);
					mask >>= 8;
				}
			}

			file.Insert(vector, (long)Offset, vector.Count);

			// Update this object
			ebml_id = (uint)MatroskaID.Void;
			data_offset = Offset + (ulong)vector.Count;
			ebml_size = (ulong)datasize;
		}


		/// <summary>
		/// Remove the EBML element from the file
		/// </summary>
		/// <returns>Size difference compare to previous EBML size</returns>
		public long Remove()
		{
			long ret = -(long)Size;

			file.RemoveBlock((long)Offset, (long)Size);

			// Invalidate this object
			ebml_id = 0;
			data_offset = offset;
			ebml_size = 0;
			file = null;

			return ret;
		}


		#endregion

	}
}
