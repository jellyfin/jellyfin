//
// AppleElementaryStreamDescriptor.cs: Provides an implementation of an Apple
// ItemListBox.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
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

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="FullBox" /> to provide an
	///    implementation of an Apple ElementaryStreamDescriptor.
	/// </summary>
	/// <remarks>
	///    This box may appear as a child of a <see
	///    cref="IsoAudioSampleEntry" /> and provided further information
	///    about an audio stream.
	/// </remarks>
	public class AppleElementaryStreamDescriptor : FullBox
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the stream ID.
		/// </summary>
		private ushort es_id;
		
		/// <summary>
		///    Contains the stream priority.
		/// </summary>
		private byte stream_priority;
		
		/// <summary>
		///    Contains the object type ID.
		/// </summary>
		private byte object_type_id;
		
		/// <summary>
		///    Contains the stream type.
		/// </summary>
		private byte stream_type;
		
		/// <summary>
		///    Contains the bugger size.
		/// </summary>
		private uint buffer_size_db;
		
		/// <summary>
		///    Contains the maximum bitrate.
		/// </summary>
		private uint max_bitrate;
		
		/// <summary>
		///    Contains the average bitrate.
		/// </summary>
		private uint average_bitrate;
		
		/// <summary>
		///    Contains the decoder config.
		/// </summary>
		private ByteVector decoder_config;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleElementaryStreamDescriptor" /> with a provided
		///    header and handler by reading the contents from a
		///    specified file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    Valid data could not be read.
		/// </exception>
		public AppleElementaryStreamDescriptor (BoxHeader header,
		                                        TagLib.File file,
		                                        IsoHandlerBox handler)
			: base (header, file, handler)
		{
			int offset = 0;
			ByteVector box_data = file.ReadBlock (DataSize);
			decoder_config = new ByteVector ();
			
			// Elementary Stream Descriptor Tag
			if (box_data [offset ++] == 3) {
				// We have a descriptor tag. Check that it's at
				// least 20 long.
				if (ReadLength (box_data, ref offset) < 20)
					throw new CorruptFileException (
						"Insufficient data present.");
				
				es_id = box_data.Mid (offset, 2).ToUShort ();
				offset += 2;
				stream_priority = box_data [offset ++];
			} else {
				// The tag wasn't found, so the next two byte
				// are the ID, and after that, business as
				// usual.
				es_id = box_data.Mid (offset, 2).ToUShort ();
				offset += 2;
			}
			
			// Verify that the next data is the Decoder
			// Configuration Descriptor Tag and escape if it won't
			// work out.
			if (box_data [offset ++] != 4)
				throw new CorruptFileException (
					"Could not identify decoder configuration descriptor.");
			
			// Check that it's at least 15 long.
			if (ReadLength (box_data, ref offset) < 15)
				throw new CorruptFileException (
					"Could not read data. Too small.");
			
			// Read a lot of good info.
			object_type_id = box_data [offset ++];
			stream_type = box_data [offset ++];
			buffer_size_db = box_data.Mid (offset, 3).ToUInt ();
			offset += 3;
			max_bitrate = box_data.Mid (offset, 4).ToUInt ();
			offset += 4;
			average_bitrate = box_data.Mid (offset, 4).ToUInt ();
			offset += 4;
			
			// Verify that the next data is the Decoder Specific
			// Descriptor Tag and escape if it won't work out.
			if (box_data [offset ++] != 5)
				throw new CorruptFileException (
					"Could not identify decoder specific descriptor.");
			
			// The rest of the info is decoder specific.
			uint length = ReadLength (box_data, ref offset);
			decoder_config = box_data.Mid (offset, (int) length);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the ID of the stream described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value containing the ID of the
		///    stream described by the current instance.
		/// </value>
		public ushort StreamId {
			get {return es_id;}
		}
		
		/// <summary>
		///    Gets the priority of the stream described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value containing the priority of
		///    the stream described by the current instance.
		/// </value>
		public byte StreamPriority {
			get {return stream_priority;}
		}
		
		/// <summary>
		///    Gets the object type ID of the stream described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value containing the object type ID
		///    of the stream described by the current instance.
		/// </value>
		public byte ObjectTypeId {
			get {return object_type_id;}
		}
		
		/// <summary>
		///    Gets the type the stream described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> value containing the type the
		///    stream described by the current instance.
		/// </value>
		public byte StreamType {
			get {return stream_type;}
		}
		
		/// <summary>
		///    Gets the buffer size DB value the stream described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the buffer size DB
		///    value the stream described by the current instance.
		/// </value>
		public uint BufferSizeDB {
			get {return buffer_size_db;}
		}
		
		/// <summary>
		///    Gets the maximum bitrate the stream described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the maximum
		///    bitrate the stream described by the current instance.
		/// </value>
		public uint MaximumBitrate {
			get {return max_bitrate / 1000;}
		}
		
		/// <summary>
		///    Gets the maximum average the stream described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the average
		///    bitrate the stream described by the current instance.
		/// </value>
		public uint AverageBitrate {
			get {return average_bitrate / 1000;}
		}
		
		/// <summary>
		///    Gets the decoder config data of stream described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the decoder
		///    config data of the stream described by the current
		///    instance.
		/// </value>
		public ByteVector DecoderConfig {
			get {return decoder_config;}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Reads a section length and updates the offset to the end
		///    of of the length block.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object to read from.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value reference specifying the
		///    offset at which to read. This value gets updated to the
		///    position following the size data.
		/// </param>
		/// <returns>
		///    A <see cref="uint" /> value containing the length that
		///    was read.
		/// </returns>
		private static uint ReadLength (ByteVector data, ref int offset)
		{
			byte b;
			int end = offset + 4;
			uint length = 0;
			
			do {
				b = data [offset ++];
				length = (uint) (length << 7) |
					(uint) (b & 0x7f);
			} while ((b & 0x80) != 0 && offset <= end);
			
			return length;
		}
		
		#endregion
	}
}
