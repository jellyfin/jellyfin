//
// Opus.cs:
//
// Author:
//   Les De Ridder (les@lesderid.net)
//
// Copyright (C) 2007 Brian Nickel
// Copyright (C) 2015 Les De Ridder
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
	///    Opus bitstreams.
	/// </summary>
	public class Opus : Codec, IAudioCodec
	{
#region Private Static Fields

		/// <summary>
		///    Contains the file identifier.
		/// </summary>
		private static ByteVector magic_signature_base = "Opus";
		private static ByteVector magic_signature_header = "OpusHead";
		private static ByteVector magic_signature_comment = "OpusTags";
		private static int magic_signature_length = 8;

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
		///    cref="Opus" />.
		/// </summary>
		private Opus ()
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

			ByteVector signature = MagicSignature (packet);
			if (signature != magic_signature_header && index == 0)
				throw new CorruptFileException (
					"Stream does not begin with opus header.");

			if (comment_data == null) {
				if (signature == magic_signature_header)
					header = new HeaderPacket (packet);
				else if (signature == magic_signature_comment)
					comment_data =
						packet.Mid (magic_signature_length);
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
			return TimeSpan.FromSeconds ((double)
					(lastGranularPosition -
						firstGranularPosition
						- 2 * header.pre_skip) /
					(double) 48000);
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

			ByteVector data = new ByteVector ();
			data.Add (magic_signature_comment);
			data.Add (comment.Render (true));
			if (packets.Count > 1 && MagicSignature (packets [1])
						  == magic_signature_comment)
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
		/// <remarks>
		///    Always returns zero, since bitrate is variable and no
		///    information is stored in the Ogg header (unlike e.g. Vorbis).
		/// </remarks>
		public int AudioBitrate {
			get {return 0;}
		}

		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the original
		///    sample rate of the audio represented by the current instance.
		/// </value>
		public int AudioSampleRate {
			get {return (int) header.input_sample_rate;}
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
			get {return (int) header.channel_count;}
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
					"Opus Version {0} Audio",
					header.opus_version);
			}
		}

#endregion



#region Public Static Methods

		/// <summary>
		///    Implements the <see cref="T:CodecProvider" /> delegate to
		///    provide support for recognizing a Opus stream from the
		///    header packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing the stream
		///    header packet.
		/// </param>
		/// <returns>
		///    A <see cref="Codec"/> object containing a codec capable
		///    of parsing the stream of <see langref="null" /> if the
		///    stream is not a Opus stream.
		/// </returns>
		public static Codec FromPacket (ByteVector packet)
		{
			return (MagicSignature (packet) == magic_signature_header) ?
				new Opus () : null;
		}

#endregion



#region Private Static Methods

		/// <summary>
		///    Gets the magic signature for a specified Opus packet.
		/// </summary>
		/// <param name="packet">
		///    A <see cref="ByteVector" /> object containing a Opus
		///    packet.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> value containing the magic
		///    signature or null if the packet is invalid.
		/// </returns>
		private static ByteVector MagicSignature (ByteVector packet)
		{
			if (packet.Count < magic_signature_length)
				return null;

			for (int i = 0; i < magic_signature_base.Count; i++)
				if (packet[i] != magic_signature_base[i])
					return null;

			return packet.Mid(0, magic_signature_length);
		}

#endregion

		/// <summary>
		///    This structure represents a Opus header packet.
		/// </summary>
		private struct HeaderPacket
		{
			public uint opus_version;
			public uint channel_count;
			public uint pre_skip;
			public uint input_sample_rate;
			public uint output_gain;
			public uint channel_map;
			public uint stream_count;
			public uint two_channel_stream_count;
			public uint[] channel_mappings;

			public HeaderPacket (ByteVector data)
			{
				opus_version  	  = data [8];
				channel_count     = data [9];
				pre_skip	  = data.Mid(10, 2).ToUInt (false);
				input_sample_rate = data.Mid(12, 4).ToUInt (false);
				output_gain	  = data.Mid(16, 2).ToUInt (false);
				channel_map       = data[18];

				if(channel_map == 0) {
					stream_count = 1;
					two_channel_stream_count = channel_count - 1;

					channel_mappings = new uint[channel_count];
					channel_mappings[0] = 0;
					if(channel_count == 2) {
						channel_mappings[1] = 1;
					}
				} else {
					stream_count = data[19];
					two_channel_stream_count = data[20];

					channel_mappings = new uint[channel_count];
					for (int i = 0; i < channel_count; i++) {
						channel_mappings[i] = data[21 + i];
					}
				}
			}
		}
	}
}
