//
// Vorbis.cs:
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
	///    cref="IAudioCodec" /> to provide support for processing Ogg
	///    Vorbis bitstreams.
	/// </summary>
	public class Vorbis : Codec, IAudioCodec
	{
#region Private Static Fields
		
		/// <summary>
		///    Contains the file identifier.
		/// </summary>
		private static ByteVector id = "vorbis";
		
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
		///    cref="Vorbis" />.
		/// </summary>
		private Vorbis ()
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
			if (type != 1 && index == 0)
				throw new CorruptFileException (
					"Stream does not begin with vorbis header.");
			
			if (comment_data == null) {
				if (type == 1)
					header = new HeaderPacket (packet);
				else if (type == 3)
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
			return header.sample_rate == 0 ? TimeSpan.Zero : 
				TimeSpan.FromSeconds ((double)
					(lastGranularPosition -
						firstGranularPosition) /
					(double) header.sample_rate);
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
			
			ByteVector data = new ByteVector ((byte) 0x03);
			data.Add (id);
			data.Add (comment.Render (true));
			if (packets.Count > 1 && PacketType (packets [1]) == 0x03)
				packets [1] = data;
			else
				packets.Insert (1, data);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing a bitrate of the
		///    audio represented by the current instance.
		/// </value>
		public int AudioBitrate {
			get {
				return (int) ((float)header.bitrate_nominal /
					1000f + 0.5);
			}
		}
		
		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample rate of
		///    the audio represented by the current instance.
		/// </value>
		public int AudioSampleRate {
			get {return (int) header.sample_rate;}
		}
		
		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		public int AudioChannels {
			get {return (int) header.channels;}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Audio" />.
		/// </value>
		public override MediaTypes MediaTypes {
			get {return MediaTypes.Audio;}
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
					"Vorbis Version {0} Audio",
					header.vorbis_version);
			}
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Implements the <see cref="T:CodecProvider" /> delegate to
		///    provide support for recognizing a Vorbis stream from the
		///    header packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing the stream
		///    header packet.
		/// </param>
		/// <returns>
		///    A <see cref="Codec"/> object containing a codec capable
		///    of parsing the stream of <see langref="null" /> if the
		///    stream is not a Vorbis stream.
		/// </returns>
		public static Codec FromPacket (ByteVector packet)
		{
			return (PacketType (packet) == 1) ? new Vorbis () : null;
		}
		
#endregion
		
		
		
#region Private Static Methods
		
		/// <summary>
		///    Gets the packet type for a specified Vorbis packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing a Vorbis
		///    packet.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> value containing the packet type or
		///    -1 if the packet is invalid.
		/// </returns>
		private static int PacketType (ByteVector packet)
		{
			if (packet.Count <= id.Count)
				return -1;
			
			for (int i = 0; i < id.Count; i ++)
				if (packet [i + 1] != id [i])
					return -1;
			
			return packet [0];
		}
		
#endregion
		
		/// <summary>
		///    This structure represents a Vorbis header packet.
		/// </summary>
		private struct HeaderPacket
		{
			public uint sample_rate;
			public uint channels;
			public uint vorbis_version;
			public uint bitrate_maximum;
			public uint bitrate_nominal;
			public uint bitrate_minimum;
			
			public HeaderPacket (ByteVector data)
			{
				vorbis_version  = data.Mid(7, 4).ToUInt (false);
				channels        = data [11];
				sample_rate     = data.Mid(12, 4).ToUInt (false);
				bitrate_maximum = data.Mid(16, 4).ToUInt (false);
				bitrate_nominal = data.Mid(20, 4).ToUInt (false);
				bitrate_minimum = data.Mid(24, 4).ToUInt (false);
			}
		}
	}
}
