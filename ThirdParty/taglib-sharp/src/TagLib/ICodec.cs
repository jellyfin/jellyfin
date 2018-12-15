//
// ICodec.cs: Provides ICodec, IAudioCodec, and IVideoCodec interfaces.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace TagLib {
	/// <summary>
	///    Indicates the types of media represented by a <see cref="ICodec"
	///    /> or <see cref="Properties" /> object.
	/// </summary>
	/// <remarks>
	///    These values can be bitwise combined to represent multiple media
	///    types.
	/// </remarks>
	[Flags]
	public enum MediaTypes
	{
		/// <summary>
		///    No media is present.
		/// </summary>
		None  = 0,
		
		/// <summary>
		///    Audio is present.
		/// </summary>
		Audio = 1,
		
		/// <summary>
		///    Video is present.
		/// </summary>
		Video = 2,

		/// <summary>
		///    A Photo is present.
		/// </summary>
		Photo = 4,

		/// <summary>
		///    Text is present.
		/// </summary>
		Text = 8
	}
	
	/// <summary>
	///    This interface provides basic information, common to all media
	///    codecs.
	/// </summary>
	public interface ICodec
	{
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> containing the duration of the
		///    media represented by the current instance.
		/// </value>
		TimeSpan Duration {get;}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="MediaTypes" /> containing
		///    the types of media represented by the current instance.
		/// </value>
		MediaTypes MediaTypes {get;}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		string Description {get;}
	}
	
	/// <summary>
	///    This interface inherits <see cref="ICodec" /> to provide
	///    information about an audio codec.
	/// </summary>
	/// <remarks>
	///    <para>When dealing with a <see cref="ICodec" />, if <see
	///    cref="ICodec.MediaTypes" /> contains <see cref="MediaTypes.Audio"
	///    />, it is safe to assume that the object also inherits <see
	///    cref="IAudioCodec" /> and can be recast without issue.</para>
	/// </remarks>
	public interface IAudioCodec : ICodec
	{
		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing a bitrate of the
		///    audio represented by the current instance.
		/// </value>
		int AudioBitrate {get;}
		
		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample rate of
		///    the audio represented by the current instance.
		/// </value>
		int AudioSampleRate {get;}
		
		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		int AudioChannels {get;}
	}

	/// <summary>
	///    This interface provides information specific
	///    to lossless audio codecs.
	/// </summary>
	public interface ILosslessAudioCodec
	{
		/// <summary>
		///    Gets the number of bits per sample in the audio
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of bits
		///    per sample in the audio represented by the current
		///    instance.
		/// </value>
		int BitsPerSample {get;}
	}

	/// <summary>
	///    This interface inherits <see cref="ICodec" /> to provide
	///    information about a video codec.
	/// </summary>
	/// <remarks>
	///    <para>When dealing with a <see cref="ICodec" />, if <see
	///    cref="ICodec.MediaTypes" /> contains <see cref="MediaTypes.Video"
	///    />, it is safe to assume that the object also inherits <see
	///    cref="IVideoCodec" /> and can be recast without issue.</para>
	/// </remarks>
	public interface IVideoCodec : ICodec
	{
		/// <summary>
		///    Gets the width of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    video represented by the current instance.
		/// </value>
		int VideoWidth  {get;}
		
		/// <summary>
		///    Gets the height of the video represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    video represented by the current instance.
		/// </value>
		int VideoHeight {get;}
	}

	/// <summary>
	///    This interface inherits <see cref="ICodec" /> to provide
	///    information about a photo.
	/// </summary>
	/// <remarks>
	///    <para>When dealing with a <see cref="ICodec" />, if <see
	///    cref="ICodec.MediaTypes" /> contains <see cref="MediaTypes.Photo"
	///    />, it is safe to assume that the object also inherits <see
	///    cref="IPhotoCodec" /> and can be recast without issue.</para>
	/// </remarks>
	public interface IPhotoCodec : ICodec
	{
		/// <summary>
		///    Gets the width of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the width of the
		///    photo represented by the current instance.
		/// </value>
		int PhotoWidth  {get;}

		/// <summary>
		///    Gets the height of the photo represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the height of the
		///    photo represented by the current instance.
		/// </value>
		int PhotoHeight {get;}

		/// <summary>
		///    Gets the (format specific) quality indicator of the photo
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value indicating the quality. A value
		///    0 means that there was no quality indicator for the format
		///    or the file.
		/// </value>
		int PhotoQuality {get;}
	}
}
