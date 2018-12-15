//
// AudioHeader.cs: Provides information about an MPEG audio stream.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   mpegheader.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2003 by Scott Wheeler (Original Implementation)
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

namespace TagLib.Mpeg {
	
	#region Enums
	
	/// <summary>
	///    Indicates the MPEG version of a file or stream.
	/// </summary>
	public enum Version
	{
		/// <summary>
		///    Unknown version.
		/// </summary>
		Unknown = -1,
		
		/// <summary>
		///    MPEG-1
		/// </summary>
		Version1 = 0,
		
		/// <summary>
		///    MPEG-2
		/// </summary>
		Version2 = 1,
		
		/// <summary>
		///    MPEG-2.5
		/// </summary>
		Version25 = 2
	}
	
	/// <summary>
	///    Indicates the MPEG audio channel mode of a file or stream.
	/// </summary>
	public enum ChannelMode
	{
		/// <summary>
		///    Stereo
		/// </summary>
		Stereo = 0,
		
		/// <summary>
		///    Joint Stereo
		/// </summary>
		JointStereo = 1,
		
		/// <summary>
		///    Dual Channel Mono
		/// </summary>
		DualChannel = 2,
		
		/// <summary>
		///    Single Channel Mono
		/// </summary>
		SingleChannel = 3
	}
	
	#endregion
	
	/// <summary>
	///    This structure implements <see cref="IAudioCodec" /> and provides
	///    information about an MPEG audio stream.
	/// </summary>
	public struct AudioHeader : IAudioCodec
	{
		#region Private Static Value Arrays
		
		/// <summary>
		///    Contains a sample rate table for MPEG audio.
		/// </summary>
		private static readonly int [,] sample_rates = new int [3,4] {
			{44100, 48000, 32000, 0}, // Version 1
			{22050, 24000, 16000, 0}, // Version 2
			{11025, 12000,  8000, 0}  // Version 2.5
		};
		
		/// <summary>
		///    Contains a block size table for MPEG audio.
		/// </summary>
		private static readonly int [,] block_size = new int [3,4] {
			{0, 384, 1152, 1152}, // Version 1
			{0, 384, 1152,  576}, // Version 2
			{0, 384, 1152,  576}  // Version 2.5
		};
		
		/// <summary>
		///    Contains a bitrate table for MPEG audio.
		/// </summary>
		private static readonly int [,,] bitrates = new int [2,3,16] {
			{ // Version 1
				{0, 32, 64, 96, 128, 160, 192, 224, 256, 288,
					320, 352, 384, 416, 448, -1}, // layer 1
				{0, 32, 48, 56,  64,  80,  96, 112, 128, 160,
					192, 224, 256, 320, 384, -1}, // layer 2
				{0, 32, 40, 48,  56,  64,  80,  96, 112, 128,
					160, 192, 224, 256, 320, -1}  // layer 3
			},
			{ // Version 2 or 2.5
				{0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160,
					176, 192, 224, 256, -1}, // layer 1
				{0,  8, 16, 24, 32, 40, 48,  56,  64,  80,  96,
					112, 128, 144, 160, -1}, // layer 2
				{0,  8, 16, 24, 32, 40, 48,  56,  64,  80,  96,
					112, 128, 144, 160, -1}  // layer 3
			}
		};
		
		#endregion
		
		
		
		#region Private Properties
		
		/// <summary>
		///    Contains the header flags.
		/// </summary>
		private uint flags;
		
		/// <summary>
		///    Contains the audio stream length.
		/// </summary>
		private long stream_length;
		
		/// <summary>
		///    Contains the associated Xing header.
		/// </summary>
		private XingHeader xing_header;
		
		/// <summary>
		///    Contains the associated VBRI header.
		/// </summary>
		private VBRIHeader vbri_header;
		
		/// <summary>
		///    Contains the audio stream duration.
		/// </summary>
		private TimeSpan duration;
		
		#endregion
		
		
		
		#region Public Fields
		
		/// <summary>
		///    An empty and unset header.
		/// </summary>
		public static readonly AudioHeader Unknown =
			new AudioHeader (0, 0, XingHeader.Unknown,
				VBRIHeader.Unknown);
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AudioHeader" /> by populating it with specified
		///    values.
		/// </summary>
		/// <param name="flags">
		///    A <see cref="uint" /> value specifying flags for the new
		///    instance.
		/// </param>
		/// <param name="streamLength">
		///    A <see cref="long" /> value specifying the stream length
		///    of the new instance.
		/// </param>
		/// <param name="xingHeader">
		///    A <see cref="XingHeader" /> object representing the Xing
		///    header associated with the new instance.
		/// </param>
		/// <param name="vbriHeader">
		///    A <see cref="VBRIHeader" /> object representing the VBRI
		///    header associated with the new instance.
		/// </param>
		private AudioHeader (uint flags, long streamLength,
		                     XingHeader xingHeader,
		                     VBRIHeader vbriHeader)
		{
			this.flags = flags;
			this.stream_length = streamLength;
			this.xing_header = xingHeader;
			this.vbri_header = vbriHeader;
			this.duration = TimeSpan.Zero;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AudioHeader" /> by reading its contents from a
		///    <see cref="ByteVector" /> object and its Xing Header from
		///    the appropriate location in the specified file.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the header
		///    to read.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the Xing
		///    header from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value indicating the position in
		///    <paramref name="file" /> at which the header begins.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is less than 4 bytes long,
		///    does not begin with a MPEG audio synch, has a negative
		///    bitrate, or has a sample rate of zero.
		/// </exception>
		private AudioHeader (ByteVector data, TagLib.File file,
		                     long position)
		{
			this.duration = TimeSpan.Zero;
			stream_length = 0;

			string error = GetHeaderError(data);
			if (error != null) {
				throw new CorruptFileException (error);
			}
			
			flags = data.ToUInt ();

			xing_header = XingHeader.Unknown;
			
			vbri_header = VBRIHeader.Unknown;
			
			// Check for a Xing header that will help us in
			// gathering information about a VBR stream.
			file.Seek (position + XingHeader.XingHeaderOffset (
				Version, ChannelMode));
				
			ByteVector xing_data = file.ReadBlock (16);
			if (xing_data.Count == 16 && xing_data.StartsWith (
				XingHeader.FileIdentifier))
				xing_header = new XingHeader (xing_data);

			if (xing_header.Present)
				return;
			
			// A Xing header could not be found, next chec for a
			// Fraunhofer VBRI header.
			file.Seek (position + VBRIHeader.VBRIHeaderOffset ());

			// Only get the first 24 bytes of the Header.
			// We're not interested in the TOC entries.
			ByteVector vbri_data = file.ReadBlock (24);
			if (vbri_data.Count == 24 &&
				vbri_data.StartsWith(VBRIHeader.FileIdentifier))
			vbri_header = new VBRIHeader (vbri_data);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the MPEG version used to encode the audio
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="Version" /> value indicating the MPEG
		///    version used to encode the audio represented by the
		///    current instance.
		/// </value>
		public Version Version {
			get {
				switch ((flags >> 19) & 0x03)
				{
				case 0:
					return Version.Version25;
				case 2:
					return Version.Version2;
				default:
					return Version.Version1;
				}
			}
		}
		
		/// <summary>
		///    Gets the MPEG audio layer used to encode the audio
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value indicating the MPEG audio
		///    layer used to encode the audio represented by the current
		///    instance.
		/// </value>
		public int AudioLayer {
			get {
				switch ((flags >> 17) & 0x03)
				{
				case 1:
					return 3;
				case 2:
					return 2;
				default:
					return 1;
				}
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
				if (xing_header.TotalSize > 0 &&
					Duration > TimeSpan.Zero)
					return (int) Math.Round (((
						(XingHeader.TotalSize * 8L) /
						Duration.TotalSeconds) / 1000.0));

				if (vbri_header.TotalSize > 0 && 
					Duration > TimeSpan.Zero)
					return (int)Math.Round(((
						(VBRIHeader.TotalSize * 8L) /
						Duration.TotalSeconds) / 1000.0));
				
				return bitrates [
					Version == Version.Version1 ? 0 : 1,
					AudioLayer > 0 ? AudioLayer - 1 : 0,
					(int) (flags >> 12) & 0x0F];
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
				return sample_rates [(int) Version,
					(int) (flags >> 10) & 0x03];
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
			get {return ChannelMode == ChannelMode.SingleChannel ? 1 : 2;}
		}
		
		/// <summary>
		///    Gets the length of the frames in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the length of the
		///    frames in the audio represented by the current instance.
		/// </value>
		public int AudioFrameLength {
			get {
				switch (AudioLayer)
				{
					case 1:
						return 48000  * AudioBitrate /
							AudioSampleRate +
							(IsPadded ? 4 : 0);
					case 2:
						return 144000 * AudioBitrate /
							AudioSampleRate +
							(IsPadded ? 1 : 0);
					case 3:
						if (Version == Version.Version1)
							goto case 2;
						
						return 72000 * AudioBitrate /
							AudioSampleRate +
							(IsPadded ? 1 : 0);
					default: return 0;
				}
			}
		}
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		/// <remarks>
		///    If <see cref="XingHeader" /> is equal to <see
		///    cref="XingHeader.Unknown" /> and <see
		///    cref="SetStreamLength" /> has not been called, this value
		///    will not be correct.
		///    If <see cref="VBRIHeader" /> is equal to <see
		///    cref="VBRIHeader.Unknown" /> and <see
		///    cref="SetStreamLength" /> has not been called, this value
		///    will not be correct.
		/// </remarks>
		public TimeSpan Duration {
			get {
				if (duration > TimeSpan.Zero)
					return duration;
					
				if (xing_header.TotalFrames > 0) {
					// Read the length and the bitrate from
					// the Xing header.
					
					double time_per_frame = (double)
						block_size [(int) Version,
						AudioLayer] / (double)
						AudioSampleRate;
					
					duration = TimeSpan.FromSeconds (
						time_per_frame *
						XingHeader.TotalFrames);
				} else if (vbri_header.TotalFrames > 0) {
					// Read the length and the bitrate from
					// the VBRI header.

					double time_per_frame =
						(double) block_size [
							(int) Version, AudioLayer]
						/ (double) AudioSampleRate;

					duration = TimeSpan.FromSeconds (
						Math.Round (time_per_frame *
							VBRIHeader.TotalFrames));
				} else if (AudioFrameLength > 0 &&
					AudioBitrate > 0) {
					// Since there was no valid Xing or VBRI
					// header found, we hope that we're in a
					// constant bitrate file.

					// Round off to upper integer value
					int frames = (int) ((stream_length + 
						AudioFrameLength - 1) /
						AudioFrameLength);
					
					duration = TimeSpan.FromSeconds (
						(double) (AudioFrameLength *
						frames) / (AudioBitrate * 125));
				}
				
				return duration;
			}
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
				System.Text.StringBuilder builder =
					new System.Text.StringBuilder ();
				
				builder.Append ("MPEG Version ");
				switch (Version)
				{
				case Version.Version1:
					builder.Append ("1");
					break;
				case Version.Version2:
					builder.Append ("2");
					break;
				case Version.Version25:
					builder.Append ("2.5");
					break;
				}
				builder.Append (" Audio, Layer ");
				builder.Append (AudioLayer);
				
				if (xing_header.Present || vbri_header.Present)
					builder.Append (" VBR");
				
				return builder.ToString ();
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
		///    Gets whether or not the audio represented by the current
		///    instance is protected.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    audio represented by the current instance is protected.
		/// </value>
		public bool IsProtected {
			get {return ((flags >>16) & 1) == 0;}
		}
		
		/// <summary>
		///    Gets whether or not the audio represented by the current
		///    instance is padded.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    audio represented by the current instance is padded.
		/// </value>
		public bool IsPadded {
			get {return ((flags >> 9) & 1) == 1;}
		}
		
		/// <summary>
		///    Gets whether or not the audio represented by the current
		///    instance is copyrighted.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    audio represented by the current instance is copyrighted.
		/// </value>
		public bool IsCopyrighted {
			get {return ((flags >> 3) & 1) == 1;}
		}
		
		/// <summary>
		///    Gets whether or not the audio represented by the current
		///    instance is original.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    audio represented by the current instance is original.
		/// </value>
		public bool IsOriginal {
			get {return ((flags >> 2) & 1) == 1;}
		}
		
		/// <summary>
		///    Gets the MPEG audio channel mode of the audio represented
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ChannelMode" /> value indicating the MPEG
		///    audio channel mode of the audio represented by the
		///    current instance.
		/// </value>
		public ChannelMode ChannelMode {
			get {return (ChannelMode) ((flags >> 6) & 0x03);}
		}
		
		/// <summary>
		///    Gets the Xing header found in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="XingHeader" /> object containing the Xing
		///    header found in the audio represented by the current
		///    instance, or <see cref="XingHeader.Unknown" /> if no
		///    header was found.
		/// </value>
		public XingHeader XingHeader {
			get {return xing_header;}
		}
		
		/// <summary>
		///    Gets the VBRI header found in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="VBRIHeader" /> object containing the VBRI
		///    header found in the audio represented by the current
		///    instance, or <see cref="VBRIHeader.Unknown" /> if no
		///    header was found.
		/// </value>
		public VBRIHeader VBRIHeader {
			get {return vbri_header;}
		}
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Sets the length of the audio stream represented by the
		///    current instance.
		/// </summary>
		/// <param name="streamLength">
		///    A <see cref="long" /> value specifying the length in
		///    bytes of the audio stream represented by the current
		///    instance.
		/// </param>
		/// <remarks>
		///    The this value has been set, <see cref="Duration" /> will
		///    return an incorrect value.
		/// </remarks>
		public void SetStreamLength (long streamLength)
		{
			this.stream_length = streamLength;
			
			// Force the recalculation of duration if it depends on
			// the stream length.
			if (xing_header.TotalFrames == 0 ||
				vbri_header.TotalFrames == 0)
				duration = TimeSpan.Zero;
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Searches for an audio header in a <see cref="TagLib.File"
		///    /> starting at a specified position and searching through
		///    a specified number of bytes.
		/// </summary>
		/// <param name="header">
		///    A <see cref="AudioHeader" /> object in which the found
		///    header will be stored.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to search.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    in <paramref name="file" /> at which to start searching.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> value specifying the maximum number
		///    of bytes to search before aborting.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not a
		///    header was found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		public static bool Find (out AudioHeader header,
		                         TagLib.File file, long position,
		                         int length)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			long end = position + length;
			header = AudioHeader.Unknown;
			
			file.Seek (position);
			
			ByteVector buffer = file.ReadBlock (3);
			
			if (buffer.Count < 3)
				return false;
			
			do {
				file.Seek (position + 3);
				buffer = buffer.Mid (buffer.Count - 3);
				buffer.Add (file.ReadBlock (
					(int) File.BufferSize));
				
				for (int i = 0; i < buffer.Count - 3 &&
					(length < 0 || position + i < end); i++)
					if (buffer [i] == 0xFF &&
						buffer [i + 1] > 0xE0) {
						ByteVector data = buffer.Mid(i, 4);
						if (GetHeaderError(data) == null) {
							try {
								header = new AudioHeader(
									data,
									file, position + i);
								return true;
							} catch (CorruptFileException) {
							}
						}
					}
				
				position += File.BufferSize;
			} while (buffer.Count > 3 && (length < 0 || position < end));
			
			return false;
		}
		
		/// <summary>
		///    Searches for an audio header in a <see cref="TagLib.File"
		///    /> starting at a specified position and searching to the
		///    end of the file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="AudioHeader" /> object in which the found
		///    header will be stored.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to search.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the seek position
		///    in <paramref name="file" /> at which to start searching.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not a
		///    header was found.
		/// </returns>
		/// <remarks>
		///    Searching to the end of the file can be very, very slow
		///    especially for corrupt or non-MPEG files. It is
		///    recommended to use <see
		///    cref="M:AudioHeader.Find(AudioHeader,TagLib.File,long,int)" />
		///    instead.
		/// </remarks>
		public static bool Find (out AudioHeader header,
		                         TagLib.File file, long position)
		{
			return Find (out header, file, position, -1);
		}

		private static string GetHeaderError(ByteVector data)
		{
			if (data.Count < 4)
				return "Insufficient header length.";

			if (data [0] != 0xFF)
				return "First byte did not match MPEG synch.";

			// Checking bits from high to low:
			//
			// First 3 bits MUST be set. Bits 4 and 5 can
			// be 00, 10, or 11 but not 01. One or more of
			// bits 6 and 7 must be set. Bit 8 can be
			// anything.
			if ((data [1] & 0xE6) <= 0xE0 || (data [1] & 0x18) == 0x08)
				return "Second byte did not match MPEG synch.";

			uint flags = data.ToUInt ();

			if (((flags >> 12) & 0x0F) == 0x0F)
				return "Header uses invalid bitrate index.";

			if (((flags >> 10) & 0x03) == 0x03)
				return "Invalid sample rate.";

			return null;
		}
		
		#endregion
	}
}
