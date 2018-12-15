//
// StreamHeader.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   flagproperties.cpp from TagLib
//
// Copyright (C) 2006-2007 Brian Nickel
// Copyright (C) 2003 Allan Sandfeld Jensen (Original Implementation)
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

using System.Collections;
using System;

namespace TagLib.Flac
{
	/// <summary>
	///    This structure implements <see cref="IAudioCodec" /> and provides
	///    information about a Flac audio stream.
	/// </summary>
	public struct StreamHeader : IAudioCodec, ILosslessAudioCodec
	{
#region Private Properties
		
		/// <summary>
		///    Contains the flags.
		/// </summary>
		private uint flags;
		
		/// <summary>
		///    Contains the low portion of the length.
		/// </summary>
		private uint low_length;
		
		/// <summary>
		///    Contains the stream length.
		/// </summary>
		private long stream_length;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StreamHeader" /> by reading a raw stream header
		///    structure and using the stream length.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    stream header.
		/// </param>
		/// <param name="streamLength">
		///    A <see cref="long" /> value containing the length of the
		///    stream.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 18 bytes.
		/// </exception>
		public StreamHeader (ByteVector data, long streamLength)
		{
			if (data == null)
			throw new ArgumentNullException ("data");
			
			if (data.Count < 18)
				throw new CorruptFileException (
				"Not enough data in FLAC header.");
			
			this.stream_length = streamLength;
			this.flags = data.Mid (10, 4).ToUInt (true);
			low_length = data.Mid (14, 4).ToUInt (true);
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
				return (AudioSampleRate > 0 && stream_length > 0)
					? TimeSpan.FromSeconds (
						(double) low_length /
						(double) AudioSampleRate +
						(double) HighLength) :
					TimeSpan.Zero;
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
				return  (int) (Duration > TimeSpan.Zero ?
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
			get {return (int) (flags >> 12);}
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
			get {return (int) (((flags >> 9) & 7) + 1);}
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
		///    Gets the sample width of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample width of
		///    the audio represented by the current instance.
		/// </value>
		[Obsolete ("This property is depreciated, use BitsPerSample instead")]
		public int AudioSampleWidth {
			get {return BitsPerSample;}
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
			get {return (int) (((flags >> 4) & 31) + 1);}
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
			get {return "Flac Audio";}
		}
		
#endregion
		
		
		
#region Private Properties
		
		/// <summary>
		///    Gets a high portion of the length of the audio
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the high portion
		///    of the length.
		/// </value>
		private uint HighLength {
			get {
				// The last 4 bits are the most significant 4
				// bits for the 36 bit stream length in samples.
				// (Audio files measured in days)
				return (uint) (AudioSampleRate > 0 ?
					(((flags & 0xf) << 28) /
					AudioSampleRate) << 4 : 0);
			}
		}
		
#endregion
	}
}
