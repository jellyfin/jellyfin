//
// AviStream.cs:
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

using System;
using System.Text;

namespace TagLib.Riff
{
	/// <summary>
	///    This abstract class provides basic support for parsing a raw AVI
	///    stream list.
	/// </summary>
	public abstract class AviStream
	{
		/// <summary>
		///    Contains the stream header.
		/// </summary>
		private AviStreamHeader header;
		
		/// <summary>
		///    Contains the stream codec information.
		/// </summary>
		private ICodec codec;
		
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="AviStream" /> with a specified stream header.
		/// </summary>
		/// <param name="header">
		///   A <see cref="AviStreamHeader"/> object containing the
		///   stream's header.
		/// </param>
		protected AviStream (AviStreamHeader header)
		{
			this.header = header;
		}
		
		/// <summary>
		///    Parses a stream list item.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the item's
		///    ID.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the item's
		///    data.
		/// </param>
		/// <param name="start">
		///    A <see cref="uint" /> value specifying the index in
		///    <paramref name="data" /> at which the item data begins.
		/// </param>
		/// <param name="length">
		///    A <see cref="uint" /> value specifying the length of the
		///    item.
		/// </param>
		public virtual void ParseItem (ByteVector id, ByteVector data,
		                               int start, int length)
		{
		}
		
		/// <summary>
		///    Gets the stream header.
		/// </summary>
		/// <value>
		///    A <see cref="AviStreamHeader" /> object containing the
		///    header information for the stream.
		/// </value>
		public AviStreamHeader Header {
			get {return header;}
		}
		
		/// <summary>
		///    Gets the codec information.
		/// </summary>
		/// <value>
		///    A <see cref="ICodec" /> object containing the codec
		///    information for the stream.
		/// </value>
		public ICodec Codec {
			get {return codec;}
			protected set {this.codec = value;}
		}
		
		/// <summary>
		///    Parses a raw AVI stream list and returns the stream
		///    information.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing raw stream
		///    list.
		/// </param>
		/// <returns>
		///    A <see cref="AviStream" /> object containing stream
		///    information.
		/// </returns>
		public static AviStream ParseStreamList (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
		
			
			if (!data.StartsWith ("strl"))
				return null;
			
			AviStream stream = null;
			int pos = 4;
			
			while (pos + 8 < data.Count) {
				ByteVector id = data.Mid (pos, 4);
				int block_length = (int) data.Mid (pos + 4, 4)
					.ToUInt (false);
				
				if (id == "strh" && stream == null) {
					AviStreamHeader stream_header =
						new AviStreamHeader (data, pos + 8);
					if (stream_header.Type == "vids")
						stream = new AviVideoStream (
							stream_header);
					else if (stream_header.Type == "auds")
						stream = new AviAudioStream (
							stream_header);
				} else if (stream != null) {
					stream.ParseItem (id, data, pos + 8, block_length);
				}
				
				pos += block_length + 8;
			}
			
			return stream;
		}
	}
	
	/// <summary>
	///    This class extends <see cref="AviStream" /> to provide support
	///    for reading audio stream data.
	/// </summary>
	public class AviAudioStream : AviStream
	{
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="AviAudioStream" /> with a specified stream header.
		/// </summary>
		/// <param name="header">
		///   A <see cref="AviStreamHeader"/> object containing the
		///   stream's header.
		/// </param>
		public AviAudioStream (AviStreamHeader header)
			: base (header)
		{
		}
		
		/// <summary>
		///    Parses a stream list item.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the item's
		///    ID.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the item's
		///    data.
		/// </param>
		/// <param name="start">
		///    A <see cref="uint" /> value specifying the index in
		///    <paramref name="data" /> at which the item data begins.
		/// </param>
		/// <param name="length">
		///    A <see cref="uint" /> value specifying the length of the
		///    item.
		/// </param>
		public override void ParseItem (ByteVector id, ByteVector data,
		                                int start, int length)
		{
			if (id == "strf")
				Codec = new WaveFormatEx (data, start);
		}
	}
	
	/// <summary>
	///    This class extends <see cref="AviStream" /> to provide support
	///    for reading video stream data.
	/// </summary>
	public class AviVideoStream : AviStream
	{
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="AviVideoStream" /> with a specified stream header.
		/// </summary>
		/// <param name="header">
		///   A <see cref="AviStreamHeader"/> object containing the
		///   stream's header.
		/// </param>
		public AviVideoStream (AviStreamHeader header)
			: base (header)
		{
		}
		
		/// <summary>
		///    Parses a stream list item.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing the item's
		///    ID.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the item's
		///    data.
		/// </param>
		/// <param name="start">
		///    A <see cref="uint" /> value specifying the index in
		///    <paramref name="data" /> at which the item data begins.
		/// </param>
		/// <param name="length">
		///    A <see cref="uint" /> value specifying the length of the
		///    item.
		/// </param>
		public override void ParseItem (ByteVector id, ByteVector data,
		                                int start, int length)
		{
			if (id == "strf")
				Codec = new BitmapInfoHeader (data, start);
		}
	}
	
	/// <summary>
	///    This structure provides a representation of a Microsoft
	///    AviStreamHeader structure, minus the first 8 bytes.
	/// </summary>
	public struct AviStreamHeader
	{
		/// <summary>
		///    Contains the stream type.
		/// </summary>
		private ByteVector type;
		
		/// <summary>
		///    Contains the stream handler.
		/// </summary>
		private ByteVector handler;
		
		/// <summary>
		///    Contains the flags.
		/// </summary>
		private uint flags;
		
		/// <summary>
		///    Contains the priority.
		/// </summary>
		private uint priority;
		
		/// <summary>
		///    Contains the initial frame count.
		/// </summary>
		private uint initial_frames;
		
		/// <summary>
		///    Contains the scale.
		/// </summary>
		private uint scale;
		
		/// <summary>
		///    Contains the rate.
		/// </summary>
		private uint rate;
		
		/// <summary>
		///    Contains the start delay.
		/// </summary>
		private uint start;
		
		/// <summary>
		///    Contains the stream length.
		/// </summary>
		private uint length;
		
		/// <summary>
		///    Contains the suggested buffer size.
		/// </summary>
		private uint suggested_buffer_size;
		
		/// <summary>
		///    Contains the quality (between 0 and 10,000).
		/// </summary>
		private uint quality;
		
		/// <summary>
		///    Contains the sample size.
		/// </summary>
		private uint sample_size;
		
		/// <summary>
		///    Contains the position for the left side of the video.
		/// </summary>
		private ushort left;
		
		/// <summary>
		///    Contains the position for the top side of the video.
		/// </summary>
		private ushort top;
		
		/// <summary>
		///    Contains the position for the right side of the video.
		/// </summary>
		private ushort right;
		
		/// <summary>
		///    Contains the position for the bottom side of the video.
		/// </summary>
		private ushort bottom;
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AviStreamHeader" /> by reading the raw structure
		///    from the beginning of a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 56 bytes.
		/// </exception>
		[Obsolete("Use WaveFormatEx(ByteVector,int)")]
		public AviStreamHeader (ByteVector data) : this (data, 0)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AviStreamHeader" /> by reading the raw structure
		///    from a specified position in a <see cref="ByteVector" />
		///    object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the index in
		///    <paramref name="data"/> at which the structure begins.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="offset" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 56 bytes at
		///    <paramref name="offset" />.
		/// </exception>
		public AviStreamHeader (ByteVector data, int offset)
		{
			if (data == null)
				throw new System.ArgumentNullException ("data");
			
			if (offset < 0)
				throw new ArgumentOutOfRangeException (
					"offset");
			
			if (offset + 56 > data.Count)
				throw new CorruptFileException (
					"Expected 56 bytes.");
			
			type                  = data.Mid (offset,      4);
			handler               = data.Mid (offset +  4, 4);
			flags                 = data.Mid (offset +  8, 4).ToUInt (false);
			priority              = data.Mid (offset + 12, 4).ToUInt (false);
			initial_frames        = data.Mid (offset + 16, 4).ToUInt (false);
			scale                 = data.Mid (offset + 20, 4).ToUInt (false);
			rate                  = data.Mid (offset + 24, 4).ToUInt (false);
			start                 = data.Mid (offset + 28, 4).ToUInt (false);
			length                = data.Mid (offset + 32, 4).ToUInt (false);
			suggested_buffer_size = data.Mid (offset + 36, 4).ToUInt (false);
			quality               = data.Mid (offset + 40, 4).ToUInt (false);
			sample_size           = data.Mid (offset + 44, 4).ToUInt (false);
			left                  = data.Mid (offset + 48, 2).ToUShort (false);
			top                   = data.Mid (offset + 50, 2).ToUShort (false);
			right                 = data.Mid (offset + 52, 2).ToUShort (false);
			bottom                = data.Mid (offset + 54, 2).ToUShort (false);
		}
		
		/// <summary>
		///    Gets the stream type.
		/// </summary>
		/// <value>
		///    A four-byte <see cref="ByteVector" /> object specifying
		///    stream type.
		/// </value>
		public ByteVector Type {
			get {return type;}
		}
		
		/// <summary>
		///    Gets the stream handler (codec) ID.
		/// </summary>
		/// <value>
		///    A four-byte <see cref="ByteVector" /> object specifying
		///    stream handler ID.
		/// </value>
		public ByteVector Handler {
			get {return handler;}
		}
		
		/// <summary>
		///    Gets the stream flags.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying stream flags.
		/// </value>
		public uint Flags {
			get {return flags;}
		}
		
		/// <summary>
		///    Gets the stream priority.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying stream priority.
		/// </value>
		public uint Priority {
			get {return priority;}
		}
		
		/// <summary>
		///    Gets how far ahead audio is from video.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying how far ahead
		///    audio is from video.
		/// </value>
		public uint InitialFrames {
			get {return initial_frames;}
		}
		
		/// <summary>
		///    Gets the scale of the stream.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the scale of the
		///    stream.
		/// </value>
		/// <remarks>
		///    Dividing <see cref="Rate"/> by <see cref="Scale" /> gives
		///    the number of samples per second.
		/// </remarks>
		public uint Scale {
			get {return scale;}
		}
		
		/// <summary>
		///    Gets the rate of the stream.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the rate of the
		///    stream.
		/// </value>
		/// <remarks>
		///    Dividing <see cref="Rate"/> by <see cref="Scale" /> gives
		///    the number of samples per second.
		/// </remarks>
		public uint Rate {
			get {return rate;}
		}
		
		/// <summary>
		///    Gets the start delay of the stream.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the start delay of
		///    the stream.
		/// </value>
		public uint Start {
			get {return start;}
		}
		
		/// <summary>
		///    Gets the length of the stream.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the length of the
		///    stream.
		/// </value>
		public uint Length {
			get {return length;}
		}
		
		/// <summary>
		///    Gets the suggested buffer size for the stream.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the buffer size.
		/// </value>
		public uint SuggestedBufferSize {
			get {return suggested_buffer_size;}
		}
		
		/// <summary>
		///    Gets the quality of the stream data.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the quality of the
		///    stream data between 0 and 10,000.
		/// </value>
		public uint Quality {
			get {return quality;}
		}
		
		/// <summary>
		///    Gets the sample size of the stream data.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value specifying the sample size.
		/// </value>
		public uint SampleSize {
			get {return sample_size;}
		}
		
		/// <summary>
		///    Gets the position at which the left of the video is to
		///    be displayed in the rectangle whose width is given in the
		///    the file's <see cref="AviHeader"/>.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value specifying the left
		///    position.
		/// </value>
		public ushort Left {
			get {return left;}
		}
		
		/// <summary>
		///    Gets the position at which the top of the video is to be
		///    displayed in the rectangle whose height is given in the
		///    the file's <see cref="AviHeader"/>.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value specifying the top
		///    position.
		/// </value>
		public ushort Top {
			get {return top;}
		}
		
		/// <summary>
		///    Gets the position at which the right of the video is to
		///    be displayed in the rectangle whose width is given in the
		///    the file's <see cref="AviHeader"/>.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value specifying the right
		///    position.
		/// </value>
		public ushort Right {
			get {return right;}
		}
		
		/// <summary>
		///    Gets the position at which the bottom of the video is
		///    to be displayed in the rectangle whose height is given in
		///    the file's <see cref="AviHeader"/>.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value specifying the bottom
		///    position.
		/// </value>
		public ushort Bottom {
			get {return bottom;}
		}
	}
}