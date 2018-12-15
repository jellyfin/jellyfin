//
// Theora.cs:
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

namespace TagLib.Ogg.Codecs
{
	/// <summary>
	///    This class extends <see cref="Codec" /> and implements <see
	///    cref="IVideoCodec" /> to provide support for processing Ogg
	///    Theora bitstreams.
	/// </summary>
	public class Theora : Codec, IVideoCodec
	{
#region Private Static Fields
		
		/// <summary>
		///    Contains the file identifier.
		/// </summary>
		private static ByteVector id = "theora";
		
#endregion
		
		
		
#region Private Fields
		
		/// <summary>
		///    Contains the header packet.
		/// </summary>
		private HeaderPacket header;
		
		/// <summary>
		///    Contains the comment data.
		/// </summary>
		private ByteVector comment_data;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Theora" />.
		/// </summary>
		private Theora ()
		{
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Reads a Ogg packet that has been encountered in the
		///    stream.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing a packet to
		///    be read by the current instance.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value containing the index of the
		///    packet in the stream.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if the codec has read all the
		///    necessary packets for the stream and does not need to be
		///    called again, typically once the Xiph comment has been
		///    found. Otherwise <see langword="false" />.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="packet" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="index" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The data does not conform to the specificiation for the
		///    codec represented by the current instance.
		/// </exception>
		public override bool ReadPacket (ByteVector packet, int index)
		{
			if (packet == null)
				throw new ArgumentNullException ("packet");
			
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index",
					"index must be at least zero.");
			
			int type = PacketType (packet);
			if (type != 0x80 && index == 0)
				throw new CorruptFileException (
					"Stream does not begin with theora header.");
			
			if (comment_data == null) {
				if (type == 0x80)
					header = new HeaderPacket (packet);
				else if (type == 0x81)
					comment_data = packet.Mid (7);
				else
					return true;
			}
			
			return comment_data != null;
		}
		
		/// <summary>
		///    Computes the duration of the stream using the first and
		///    last granular positions of the stream.
		/// </summary>
		/// <param name="firstGranularPosition">
		///    A <see cref="long" /> value containing the first granular
		///    position of the stream.
		/// </param>
		/// <param name="lastGranularPosition">
		///    A <see cref="long" /> value containing the last granular
		///    position of the stream.
		/// </param>
		/// <returns>
		///    A <see cref="TimeSpan" /> value containing the duration
		///    of the stream.
		/// </returns>
		public override TimeSpan GetDuration (long firstGranularPosition,
		                                      long lastGranularPosition)
		{
			return TimeSpan.FromSeconds (
				header.GranuleTime (lastGranularPosition) -
				header.GranuleTime (firstGranularPosition));
		}
		
		/// <summary>
		///    Replaces the comment packet in a collection of packets
		///    with the rendered version of a Xiph comment or inserts a
		///    comment packet if the stream lacks one.
		/// </summary>
		/// <param name="packets">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    a collection of packets.
		/// </param>
		/// <param name="comment">
		///    A <see cref="XiphComment" /> object to store the rendered
		///    version of in <paramref name="packets" />.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="packets" /> or <paramref name="comment"
		///    /> is <see langword="null" />.
		/// </exception>
		public override void SetCommentPacket (ByteVectorCollection packets,
		                                       XiphComment comment)
		{
			if (packets == null)
				throw new ArgumentNullException ("packets");
			
			if (comment == null)
				throw new ArgumentNullException ("comment");
			
			ByteVector data = new ByteVector ((byte) 0x81);
			data.Add (id);
			data.Add (comment.Render (true));
			
			if (packets.Count > 1 && PacketType (packets [1]) == 0x81)
				packets [1] = data;
			else
				packets.Insert (1, data);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the width of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    video represented by the current instance.
		/// </value>
		public int VideoWidth {
			get {return header.width;}
		}
		
		/// <summary>
		///    Gets the height of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    video represented by the current instance.
		/// </value>
		public int VideoHeight {
			get {return header.height;}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Video" />.
		/// </value>
		public override MediaTypes MediaTypes {
			get {return MediaTypes.Video;}
		}
		
		/// <summary>
		///    Gets the raw Xiph comment data contained in the codec.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing a raw Xiph
		///    comment or <see langword="null"/> if none was found.
		/// </value>
		public override ByteVector CommentData {
			get {return comment_data;}
		}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public override string Description {
			get {
				return string.Format (
					"Theora Version {0}.{1} Video",
					header.major_version,
					header.minor_version);
			}
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Implements the <see cref="T:CodecProvider" /> delegate to
		///    provide support for recognizing a Theora stream from the
		///    header packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing the stream
		///    header packet.
		/// </param>
		/// <returns>
		///    A <see cref="Codec"/> object containing a codec capable
		///    of parsing the stream of <see langref="null" /> if the
		///    stream is not a Theora stream.
		/// </returns>
		public static Codec FromPacket (ByteVector packet)
		{
			return (PacketType (packet) == 0x80) ? new Theora () : null;
		}
		
#endregion
		
		
		
#region Private Static Methods
		
		/// <summary>
		///    Gets the packet type for a specified Theora packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing a Theora
		///    packet.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> value containing the packet type or
		///    -1 if the packet is invalid.
		/// </returns>
		private static int PacketType (ByteVector packet)
		{
			if (packet.Count <= id.Count || packet [0] < 0x80)
				return -1;
			
			for (int i = 0; i < id.Count; i ++)
				if (packet [i + 1] != id [i])
				return -1;
			
			return packet [0];
		}
		
#endregion
		
		/// <summary>
		///    This structure represents a Theora header packet.
		/// </summary>
		private struct HeaderPacket
		{
			public byte major_version;
			public byte minor_version;
			public byte revision_version;
			public int width;
			public int height;
			public int fps_numerator;
			public int fps_denominator;
			public int keyframe_granule_shift;
			
			public HeaderPacket (ByteVector data)
			{
				major_version = data [7];
				minor_version = data [8];
				revision_version = data [9];
				// width = data.Mid (10, 2).ToShort () << 4;
				// height = data.Mid (12, 2).ToShort () << 4;
				width = (int) data.Mid (14, 3).ToUInt (); // Frame Width.
				height = (int) data.Mid (17, 3).ToUInt (); // Frame Height.
				// Offset X.
				// Offset Y.
				fps_numerator = (int) data.Mid (22, 4).ToUInt ();
				fps_denominator = (int) data.Mid (26, 4).ToUInt ();
				// Aspect Numerator.
				// Aspect Denominator.
				// Colorspace.
				// Target bitrate.
				ushort last_bits = data.Mid (40, 2).ToUShort ();
				keyframe_granule_shift = (last_bits >> 5) & 0x1F;
			}
			
			/// <summary>
			///    Converts an absolute granular position into a
			///    seconds.
			/// </summary>
			/// <param name="granularPosition">
			///    A <see cref="long" /> value containing the
			///   absolute granular position.
			/// </param>
			/// <returns>
			///    A <see cref="double" /> value containing the time
			///    at <paramref name="granularPosition" /> in
			///    seconds.</returns>
			/// <remarks>
			///    Many thanks to the good people at
			///    irc://irc.freenode.net#theora for making this
			///    code a reality.
			/// </remarks>
			public double GranuleTime (long granularPosition)
			{
				long iframe = granularPosition >>
					keyframe_granule_shift;
				long pframe = granularPosition -
					(iframe << keyframe_granule_shift);
				return (iframe + pframe) *
					((double) fps_denominator /
					(double) fps_numerator);
			}
		}
	}
}