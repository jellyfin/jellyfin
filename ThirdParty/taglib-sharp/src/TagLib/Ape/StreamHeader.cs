//
// StreamHeader.cs: Provides support for reading Monkey's Audio APE stream
// properties.
//
// Author:
//   Helmut Wahrmann
//
// Copyright (C) 2007 Helmut Wahrmann
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
using System.Globalization;

namespace TagLib.Ape {
	/// <summary>
	///    Indicates the compression level used when encoding a Monkey's
	///    Audio APE file.
	/// </summary>
	public enum CompressionLevel {
		/// <summary>
		///    The audio is not compressed.
		/// </summary>
		None = 0,
		
		/// <summary>
		///    The audio is mildly compressed.
		/// </summary>
		Fast = 1000,
		
		/// <summary>
		///    The audio is compressed at a normal level.
		/// </summary>
		Normal = 2000,
		
		/// <summary>
		///    The audio is highly compressed.
		/// </summary>
		High = 3000,
		
		/// <summary>
		///    The audio is extremely highly compressed.
		/// </summary>
		ExtraHigh = 4000,
		
		/// <summary>
		///    The audio is compressed to an insane level.
		/// </summary>
		Insane
	}
	
	/// <summary>
	///    This struct implements <see cref="IAudioCodec" /> to provide
	///    support for reading Monkey's Audio APE stream properties.
	/// </summary>
	public struct StreamHeader : IAudioCodec, ILosslessAudioCodec
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the APE version.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (4,5) of the file and is
		///    1000 times the actual version number, so 3810 indicates
		///    version 3.81.
		/// </remarks>
		private ushort version;
		
		// Ape Header (24 bytes) starting at Offest 52 into the file
		/// <summary>
		///    Contains the compression level.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (51,52).
		/// </remarks>
		private CompressionLevel compression_level;
		
		/*
		/// <summary>
		///    Contains the format flags.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (53,54).
		/// </remarks>
		private ushort format_flags;
		*/
		
		/// <summary>
		///    Contains the number of audio blocks in one frame.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (55-58).
		/// </remarks>
		private uint blocks_per_frame;
		
		/// <summary>
		///    Contains the number of audio blocks in the final frame.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (59-62).
		/// </remarks>
		private uint final_frame_blocks;
		
		/// <summary>
		///    Contains the total number of frames.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (63-66).
		/// </remarks>
		private uint total_frames;
		
		/// <summary>
		///    Contains the number of bits per sample.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (67,68) and is typically
		///    16.
		/// </remarks>
		private ushort bits_per_sample;
		
		/// <summary>
		///    Contains the number of channels.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (69,70) and is typically
		///    1 or 2.
		/// </remarks>
		private ushort channels;
		
		/// <summary>
		///    Contains the sample rate.
		/// </summary>
		/// <remarks>
		///    This value is stored in bytes (71-74) and is typically
		///    44100.
		/// </remarks>
		private uint sample_rate;
		
		/// <summary>
		///    Contains the length of the audio stream.
		/// </summary>
		/// <remarks>
		///    This value is provided by the constructor.
		/// </remarks>
		private long stream_length;
		
		#endregion
		
		
		
		#region Public Static Fields
		
		/// <summary>
		///    The size of a Monkey Audio header.
		/// </summary>
		public const uint Size = 76;
		
		/// <summary>
		///    The identifier used to recognize a WavPack file.
		/// </summary>
		/// <value>
		///    "MAC "
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier =
			"MAC ";
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StreamHeader" /> for a specified header block and
		///    stream length.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the stream
		///    header data.
		/// </param>
		/// <param name="streamLength">
		///    A <see cref="long" /> value containing the length of the
		///    Monkey Audio stream in bytes.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> does not begin with <see
		///    cref="FileIdentifier" /> or is less than <see cref="Size"
		///    /> bytes long.
		/// </exception>
		public StreamHeader(ByteVector data, long streamLength)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			
			if (!data.StartsWith(FileIdentifier))
				throw new CorruptFileException(
					"Data does not begin with identifier.");
			
			if (data.Count < Size)
				throw new CorruptFileException(
					"Insufficient data in stream header");
			
			stream_length = streamLength;
			version = data.Mid (4, 2).ToUShort(false);
			compression_level = (CompressionLevel) data.Mid(52, 2)
				.ToUShort(false);
			// format_flags = data.Mid(54, 2).ToUShort(false);
			blocks_per_frame = data.Mid(56, 4).ToUInt(false);
			final_frame_blocks = data.Mid(60, 4).ToUInt(false);
			total_frames = data.Mid(64, 4).ToUInt(false);
			bits_per_sample = data.Mid(68, 2).ToUShort(false);
			channels = data.Mid(70, 2).ToUShort(false);
			sample_rate = data.Mid(72, 4).ToUInt(false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		public TimeSpan Duration {
			get {
				if (sample_rate <= 0 || total_frames <= 0)
					return TimeSpan.Zero;
				
				return TimeSpan.FromSeconds (
					(double) ((total_frames - 1) *
					blocks_per_frame + final_frame_blocks) /
					(double) sample_rate);
			}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Audio" />.
		/// </value>
		public MediaTypes MediaTypes {
			get {return MediaTypes.Audio;}
		}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public string Description {
			get {
				return string.Format(
					CultureInfo.InvariantCulture,
					"Monkey's Audio APE Version {0:0.000}",
					Version);
			}
		}
		
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
				TimeSpan d = Duration;
				if (d <= TimeSpan.Zero)
					return 0;
				
				return (int) ((stream_length * 8L) /
					d.TotalSeconds) / 1000;
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
			get {return (int)sample_rate;}
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
			get {return channels;}
		}
		
		/// <summary>
		///    Gets the APE version of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="double" /> value containing the APE version
		///    of the audio represented by the current instance.
		/// </value>
		public double Version {
			get {return (double) version / (double) 1000;}
		}
		
		/// <summary>
		///    Gets the number of bits per sample in the audio
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of bits
		///    per sample in the audio represented by the current
		///    instance.
		/// </value>
		public int BitsPerSample {
			get {return bits_per_sample;}
		}
		
		/// <summary>
		///    Gets the level of compression used when encoding the
		///    audio represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="CompressionLevel" /> value indicating the
		///    level of compression used when encoding the audio
		///    represented by the current instance.
		/// </value>
		public CompressionLevel Compression {
			get {return compression_level;}
		}
		
		#endregion
	}}