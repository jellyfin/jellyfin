//
// SynchedTextType.cs:
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

namespace TagLib.Id3v2
{
	/// <summary>
	///    Specifies the type of text contained in a <see
	///    cref="SynchronisedLyricsFrame" />.
	/// </summary>
	public enum SynchedTextType
	{
		/// <summary>
		///    The text is some other type of text.
		/// </summary>
		Other = 0x00,

		/// <summary>
		///    The text contains lyrical data.
		/// </summary>
		Lyrics = 0x01,

		/// <summary>
		///    The text contains a transcription.
		/// </summary>
		TextTranscription = 0x02,

		/// <summary>
		///    The text lists the movements in the piece.
		/// </summary>
		Movement = 0x03,

		/// <summary>
		///    The text describes events that occur.
		/// </summary>
		Events = 0x04,

		/// <summary>
		///    The text contains chord changes that occur in the music.
		/// </summary>
		Chord = 0x05,

		/// <summary>
		///    The text contains trivia or "pop up" information about
		///    the media.
		/// </summary>
		Trivia = 0x06,

		/// <summary>
		///    The text contains URL's for relevant webpages.
		/// </summary>
		WebpageUrls = 0x07,

		/// <summary>
		///     The text contains URL's for relevant images.
		/// </summary>
		ImageUrls = 0x08
	}
}
