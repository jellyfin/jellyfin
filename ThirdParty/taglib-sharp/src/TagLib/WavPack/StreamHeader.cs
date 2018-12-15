//
// StreamHeader.cs: Provides support for reading WavPack audio properties.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   wvproperties.cpp from libtunepimp
//
// Copyright (C) 2006-2007 Brian Nickel
// Copyright (C) 2006 by Lukáš Lalinský (Original Implementation)
// Copyright (C) 2004 by Allan Sandfeld Jensen (Original Implementation)
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

namespace TagLib.WavPack {
	/// <summary>
	///    This struct implements <see cref="IAudioCodec" /> to provide
	///    support for reading WavPack audio properties.
	/// </summary>
	public struct StreamHeader : IAudioCodec, ILosslessAudioCodec, IEquatable<StreamHeader>
	{
		#region Constants
		
		private static readonly uint [] sample_rates = new uint [] {
			6000, 8000, 9600, 11025, 12000, 16000, 22050, 24000,
			32000, 44100, 48000, 64000, 88200, 96000, 192000};
		
		private const int  BYTES_STORED = 3;
		private const int  MONO_FLAG    = 4;
		private const int  SHIFT_LSB   = 13;
		private const long SHIFT_MASK  = (0x1fL << SHIFT_LSB);
		private const int  SRATE_LSB   = 23;
		private const long SRATE_MASK  = (0xfL << SRATE_LSB);
		
		#endregion
		
		
		
		#region Private Fields
		
		/// <summary>
		///    Contains the number of bytes in the stream.
		/// </summary>
		private long stream_length;
		
		/// <summary>
		///    Contains the WavPack version.
		/// </summary>
		private ushort version;
		
		/// <summary>
		///    Contains the flags.
		/// </summary>
		private uint flags;
		
		/// <summary>
		///    Contains the sample count.
		/// </summary>
		private uint samples;
		
		#endregion
		
		
		#region Public Static Fields
		
		/// <summary>
		///    The size of a WavPack header.
		/// </summary>
		public const uint Size = 32;
		
		/// <summary>
		///    The identifier used to recognize a WavPack file.
		/// </summary>
		/// <value>
		///    "wvpk"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "wvpk";
		
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
		///    WavPack stream in bytes.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> does not begin with <see
		///    cref="FileIdentifier" /> or is less than <see cref="Size"
		///    /> bytes long.
		/// </exception>
		public StreamHeader (ByteVector data, long streamLength)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (!data.StartsWith (FileIdentifier))
				throw new CorruptFileException (
					"Data does not begin with identifier.");
			
			if (data.Count < Size)
				throw new CorruptFileException (
					"Insufficient data in stream header");
			
			stream_length = streamLength;
			version = data.Mid (8, 2).ToUShort (false);
			flags = data.Mid (24, 4).ToUInt (false);
			samples = data.Mid (12, 4).ToUInt (false);
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
				return AudioSampleRate > 0 ?
					TimeSpan.FromSeconds ((double) samples /
						(double) AudioSampleRate + 0.5) :
					TimeSpan.Zero;
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
			get {return string.Format (
				System.Globalization.CultureInfo.InvariantCulture,
				"WavPack Version {0} Audio", Version);}
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
				return (int) (Duration > TimeSpan.Zero ?
					((stream_length * 8L) /
					Duration.TotalSeconds) / 1000 : 0);
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
			get {
				return (int) (sample_rates [
					(flags & SRATE_MASK) >> SRATE_LSB]);
			}
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
			get {return ((flags & MONO_FLAG) != 0) ? 1 : 2;}
		}
		
		/// <summary>
		///    Gets the WavPack version of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the WavPack version
		///    of the audio represented by the current instance.
		/// </value>
		public int Version {
			get {return version;}
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
			get {
				return (int) (((flags & BYTES_STORED) + 1) * 8 -
					((flags & SHIFT_MASK) >> SHIFT_LSB));
			}
		}
		
		#endregion
		
		
		
		#region IEquatable
		
		/// <summary>
		///    Generates a hash code for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> value containing the hash code for
		///    the current instance.
		/// </returns>
		public override int GetHashCode ()
		{
			unchecked {
				return (int) (flags ^ samples ^ version);
			}
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another object.
		/// </summary>
		/// <param name="other">
		///    A <see cref="object" /> to compare to the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public override bool Equals (object other)
		{
			if (!(other is StreamHeader))
				return false;
			
			return Equals ((StreamHeader) other);
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another instance of <see cref="StreamHeader" />.
		/// </summary>
		/// <param name="other">
		///    A <see cref="StreamHeader" /> object to compare to the
		///    current instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public bool Equals (StreamHeader other)
		{
			return flags == other.flags &&
				samples == other.samples &&
				version == other.version;
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="StreamHeader" /> are equal to eachother.
		/// </summary>
		/// <param name="first">
		///    The first <see cref="StreamHeader" /> object to compare.
		/// </param>
		/// <param name="second">
		///    The second <see cref="StreamHeader" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    equal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator == (StreamHeader first,
		                                StreamHeader second)
		{
			return first.Equals (second);
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="StreamHeader" /> are unequal to eachother.
		/// </summary>
		/// <param name="first">
		///    The first <see cref="StreamHeader" /> object to compare.
		/// </param>
		/// <param name="second">
		///    The second <see cref="StreamHeader" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    unequal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator != (StreamHeader first,
		                                StreamHeader second)
		{
			return !first.Equals (second);
		}
		
		#endregion
	}
}
