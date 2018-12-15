//
// BlockHeader.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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
using System;

namespace TagLib.Flac
{
	/// <summary>
	///    Specifies the contents of a Flac block in <see cref="BlockHeader"
	///    />.
	/// </summary>
	public enum BlockType
	{
		/// <summary>
		///    The block contains stream information.
		/// </summary>
		StreamInfo = 0,
		
		/// <summary>
		///    The block contains padding.
		/// </summary>
		Padding,
		
		/// <summary>
		///    The block contains application data.
		/// </summary>
		Application,
		
		/// <summary>
		///    The block contains seek table.
		/// </summary>
		SeekTable,
		
		/// <summary>
		///    The block contains a Xipp comment.
		/// </summary>
		XiphComment,
		
		/// <summary>
		///    The block contains a cue sheet.
		/// </summary>
		CueSheet,
		
		/// <summary>
		///    The block contains a picture.
		/// </summary>
		Picture
	}
	
	/// <summary>
	///    This structure provides a representation of a Flac metadata block
	///    header structure.
	/// </summary>
	public struct BlockHeader
	{
		/// <summary>
		///    Contains the block type.
		/// </summary>
		private BlockType block_type;
		
		/// <summary>
		///    Indicates whether or not this is the last metadata block.
		/// </summary>
		private bool is_last_block;
		
		/// <summary>
		///    Contains the block size.
		/// </summary>
		private uint block_size;
		
		/// <summary>
		///    The size of a block header.
		/// </summary>
		public const uint Size = 4;

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="BlockHeader" /> by reading a raw header from a <see
		///    cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing a raw
		///    block header.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 4 bytes.
		/// </exception>
		public BlockHeader (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			if (data.Count < Size)
				throw new CorruptFileException (
					"Not enough data in Flac header.");
			
			block_type = (BlockType) (data[0] & 0x7f);
			is_last_block = (data[0] & 0x80) != 0;
			block_size = data.Mid (1,3).ToUInt ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="BlockHeader" /> for a specified block type and size.
		/// </summary>
		/// <param name="type">
		///    A <see cref="BlockType" /> value describing the contents
		///    of the block.
		/// </param>
		/// <param name="blockSize">
		///    A <see cref="uint" /> value containing the block data
		///    size minus the size of the header.
		/// </param>
		public BlockHeader (BlockType type, uint blockSize)
		{
			block_type    = type;
			is_last_block = false;
			block_size    = blockSize;
		}
		
		/// <summary>
		///    Renderes the current instance as a raw Flac block header.
		/// </summary>
		/// <param name="isLastBlock">
		///    A <see cref="bool" /> value specifying whether or not the
		///    header is the last header of the file.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered header.
		/// </returns>
		public ByteVector Render (bool isLastBlock)
		{
			ByteVector data = ByteVector.FromUInt (block_size);
			data [0] = (byte)(block_type + (isLastBlock ? 0x80 : 0));
			return data;
		}
		
		/// <summary>
		///    Gets the type of block described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="BlockType" /> value describing the block
		///    type.
		/// </value>
		public BlockType BlockType {
			get {return block_type;}
		}
		
		/// <summary>
		///    Gets whether or not the block is the last in the file.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the block is the last in the
		///    file; otherwise <see langword="false" />.
		/// </value>
		public bool IsLastBlock {
			get {return is_last_block;}
		}
		
		/// <summary>
		///    Gets the size of the block described by the current
		///    instance, minus the block header.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    block, minus the header.
		/// </value>
		public uint BlockSize {
			get {return block_size;}
		}
	}
}