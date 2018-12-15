//
// StripOffsetsIFDEntry.cs:
//
// Author:
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Mike Gemuende
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
using System.IO;


namespace TagLib.IFD.Entries
{
	/// <summary>
	///    Contains the offsets to the image data strips.
	/// </summary>
	public class StripOffsetsIFDEntry : ArrayIFDEntry<uint>
	{

#region Private Fields

		/// <value>
		///    Store the strip length to read them before writing.
		/// </value>
		private uint[] byte_counts;

		/// <value>
		///    The file the offsets belong to
		/// </value>
		private File file;

		#endregion

		#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="values">
		///    A <see cref="T:System.UInt32[]"/> with the strip offsets.
		/// </param>
		/// <param name="byte_counts">
		///    The length of the strips.
		/// </param>
		/// <param name="file">
		///    The file from which the strips will be read.
		/// </param>
		public StripOffsetsIFDEntry (ushort tag, uint[] values, uint[] byte_counts, File file) : base (tag)
		{
			Values = values;
			this.byte_counts = byte_counts;
			this.file = file;

			if (values.Length != byte_counts.Length)
				throw new Exception ("strip offsets and strip byte counts do not have the same length");
		}

#endregion

#region Public Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>
		/// </summary>
		/// <param name="is_bigendian">
		///    A <see cref="System.Boolean"/> indicating the endianess for rendering.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset, the data is stored.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> the ID of the type, which is rendered
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the values which are
		///    rendered.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered data.
		/// </returns>
		public override ByteVector Render (bool is_bigendian, uint offset, out ushort type, out uint count)
		{
			// The StripOffsets are an array of offsets, where the image data can be found.
			// We store the offsets and behind the offsets the image data is stored. Therfore,
			// the ByteVector data first collects the image data and the offsets itself are
			// collected by offset_data. Then both are concatenated.
			ByteVector data = new ByteVector ();
			ByteVector offset_data = new ByteVector ();

			// every offset needs 4 byte, we need to reserve the bytes.
			uint data_offset = offset + (uint) (4 * Values.Length);

			for (int i = 0; i < Values.Length; i++) {
				uint new_offset = (uint) (data_offset + data.Count);

				file.Seek (Values[i], SeekOrigin.Begin);
				data.Add (file.ReadBlock ((int) byte_counts[i]));

				// update strip offset data to new offset
				Values[i] = new_offset;

				offset_data.Add (ByteVector.FromUInt (new_offset, is_bigendian));
			}

			// If the StripOffsets only consists of one offset, this doesn't work, because this offset
			// should be stored inside the IFD as a value. But, because of the additional image data,
			// it is not stored there. We need to fix this, that the offset is adjusted correctly.
			// Therefore, the offset_data is only added if it contains more than one value.
			// Then, the offset is set correctly. (However, we need to ensure, that the image data
			// consists at least of 4 bytes, which is probably the case every time, but to be sure ...)
			// However, the strip offset in the array must also be adjusted, if the offset_data is ignored.
			if (Values.Length > 1)
				data.Insert (0, offset_data);
			else
				Values[0] = offset;

			while (data.Count < 4)
				data.Add (0x00);

			// the entry is a single long entry where the value is an offset to the data
			// the offset is automatically updated by the renderer.
			type = (ushort) IFDEntryType.Long;
			count = (uint) Values.Length;

			return data;
		}

#endregion

	}
}
