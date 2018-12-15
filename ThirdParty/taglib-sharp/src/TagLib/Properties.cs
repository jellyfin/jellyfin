//
// Properties.cs: This class implements IAudioCodec and IVideoCodec
// and combines codecs to create generic media properties for a file.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   audioproperties.cpp from TagLib
//
// Copyright (C) 2006,2007 Brian Nickel
// Copyright (C) 2003 Scott Wheeler (Original Implementation)
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
using System.Collections.Generic;

namespace TagLib {
	/// <summary>
	///    This class implements <see cref="IAudioCodec" />, <see
	///    cref="IVideoCodec" /> and <see cref="IPhotoCodec" />
	///    and combines codecs to create generic media properties
	///    for a file.
	/// </summary>
	public class Properties : IAudioCodec, IVideoCodec, IPhotoCodec
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the codecs.
		/// </summary>
		private ICodec[] codecs = new ICodec [0];
		
		/// <summary>
		///    Contains the duration.
		/// </summary>
		private TimeSpan duration = TimeSpan.Zero;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Properties" /> with no codecs or duration.
		/// </summary>
		/// <remarks>
		///    <para>This constructor is used when media properties are
		///    not read.</para>
		/// </remarks>
		public Properties ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Properties" /> with a specified duration and array
		///    of codecs.
		/// </summary>
		/// <param name="duration">
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media, or <see cref="TimeSpan.Zero" /> if the duration is
		///    to be read from the codecs.
		/// </param>
		/// <param name="codecs">
		///    A <see cref="T:T:ICodec[]" /> containing the codecs to be
		///    used in the new instance.
		/// </param>
		public Properties (TimeSpan duration, params ICodec[] codecs)
		{
			this.duration = duration;
			if (codecs != null)
				this.codecs = codecs;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Properties" /> with a specified duration and
		///    enumaration of codecs.
		/// </summary>
		/// <param name="duration">
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media, or <see cref="TimeSpan.Zero" /> if the duration is
		///    to be read from the codecs.
		/// </param>
		/// <param name="codecs">
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object containing the
		///    codec to be used in the new instance.
		/// </param>
		public Properties (TimeSpan duration, IEnumerable<ICodec> codecs)
		{
			this.duration = duration;
			if (codecs != null)
				this.codecs = new List<ICodec> (codecs)
					.ToArray ();
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the codecs contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object containing the
		///    <see cref="ICodec" /> objects contained in the current
		///    instance.
		/// </value>
		public IEnumerable<ICodec> Codecs {
			get {return codecs;}
		}
		
		#endregion
		
		
		
		#region ICodec
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		/// <remarks>
		///    If the duration was set in the constructor, that value is
		///    returned. Otherwise, the longest codec duration is used.
		/// </remarks>
		public TimeSpan Duration {
			get {
				TimeSpan duration = this.duration;
				
				if (duration != TimeSpan.Zero)
					return duration;
				
				foreach (ICodec codec in codecs)
					if (codec != null &&
						codec.Duration > duration)
						duration = codec.Duration;
				
				return duration;
			}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="MediaTypes" /> containing
		///    the types of media represented by the current instance.
		/// </value>
		public MediaTypes MediaTypes {
			get {
				MediaTypes types = MediaTypes.None;
				
				foreach (ICodec codec in codecs)
					if (codec != null)
						types |= codec.MediaTypes;
				
				return types;
			}
		}
		
		/// <summary>
		///    Gets a string description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		/// <remarks>
		///    The value contains the descriptions of the codecs joined
		///    by colons.
		/// </remarks>
		public string Description {
			get {
				StringBuilder builder = new StringBuilder ();
				foreach (ICodec codec in codecs) {
					if (codec == null)
						continue;
					
					if (builder.Length != 0)
						builder.Append ("; ");
					
					builder.Append (codec.Description);
				}
				return builder.ToString ();
			}
		}
		
		#endregion
		
		
		
		#region IAudioCodec
		
		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the bitrate of the audio
		///    represented by the current instance.
		/// </value>
		/// <remarks>
		///    This value is equal to the first non-zero audio bitrate.
		/// </remarks>
		public int AudioBitrate {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Audio) == 0)
						continue;
					
					IAudioCodec audio = codec as IAudioCodec;
					
					if (audio != null && audio.AudioBitrate != 0)
						return audio.AudioBitrate;
				}
				
				return 0;
			}
		}
		
		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the sample rate of the
		///    audio represented by the current instance.
		/// </value>
		/// <remarks>
		///    This value is equal to the first non-zero audio sample
		///    rate.
		/// </remarks>
		public int AudioSampleRate {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Audio) == 0)
						continue;
					
					IAudioCodec audio = codec as IAudioCodec;
					
					if (audio != null && audio.AudioSampleRate != 0)
						return audio.AudioSampleRate;
				}
				
				return 0;
			}
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
		/// <remarks>
		///    This value is equal to the first non-zero quantization.
		/// </remarks>
		public int BitsPerSample {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Audio) == 0)
						continue;

					ILosslessAudioCodec lossless = codec as ILosslessAudioCodec;

					if (lossless != null && lossless.BitsPerSample != 0)
						return lossless.BitsPerSample;
				}

				return 0;
			}
		}

		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> object containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		/// <remarks>
		///    This value is equal to the first non-zero audio channel
		///    count.
		/// </remarks>
		public int AudioChannels {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Audio) == 0)
						continue;
					
					IAudioCodec audio = codec as IAudioCodec;
					
					if (audio != null && audio.AudioChannels != 0)
						return audio.AudioChannels;
				}
				
				return 0;
			}
		}
		
		#endregion
		
		
		
		#region IVideoCodec
		
		/// <summary>
		///    Gets the width of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the width of the video
		///    represented by the current instance.
		/// </value>
		/// <remarks>
		///    This value is equal to the first non-zero video width.
		/// </remarks>
		public int VideoWidth {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Video) == 0)
						continue;
					
					IVideoCodec video = codec as IVideoCodec;
					
					if (video != null && video.VideoWidth != 0)
						return video.VideoWidth;
				}
				
				return 0;
			}
		}
		
		/// <summary>
		///    Gets the height of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> containing the height of the video
		///    represented by the current instance.
		/// </value>
		/// <remarks>
		///    This value is equal to the first non-zero video height.
		/// </remarks>
		public int VideoHeight {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Video) == 0)
						continue;
					
					IVideoCodec video = codec as IVideoCodec;
					
					if (video != null && video.VideoHeight != 0)
						return video.VideoHeight;
				}
				
				return 0;
			}
		}
		
		#endregion



		#region IPhotoCodec

		/// <summary>
		///    Gets the width of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    photo represented by the current instance.
		/// </value>
		public int PhotoWidth {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Photo) == 0)
						continue;

					IPhotoCodec photo = codec as IPhotoCodec;

					if (photo != null && photo.PhotoWidth != 0)
						return photo.PhotoWidth;
				}

				return 0;
			}
		}

		/// <summary>
		///    Gets the height of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    photo represented by the current instance.
		/// </value>
		public int PhotoHeight {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Photo) == 0)
						continue;

					IPhotoCodec photo = codec as IPhotoCodec;

					if (photo != null && photo.PhotoHeight != 0)
						return photo.PhotoHeight;
				}

				return 0;
			}
		}

		/// <summary>
		///    Gets the (format specific) quality indicator of the photo
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value indicating the quality. A value
		///    0 means that there was no quality indicator for the format
		///    or the file.
		/// </value>
		public int PhotoQuality {
			get {
				foreach (ICodec codec in codecs) {
					if (codec == null ||
						(codec.MediaTypes & MediaTypes.Photo) == 0)
						continue;

					IPhotoCodec photo = codec as IPhotoCodec;

					if (photo != null && photo.PhotoQuality != 0)
						return photo.PhotoQuality;
				}

				return 0;
			}
		}


		#endregion
	}
}
