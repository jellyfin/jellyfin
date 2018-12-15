//
// TimestampFormat.cs:
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
	///    Specifies the timestamp format used by a <see
	///    cref="SynchronisedLyricsFrame" /> and <see cref="EventTimeCodesFrame"/>.
	/// </summary>
	public enum TimestampFormat
	{
		/// <summary>
		///    The timestamp is of unknown format.
		/// </summary>
		Unknown              = 0x00,
		
		/// <summary>
		///    The timestamp represents the number of MPEG frames since
		///    the beginning of the audio stream.
		/// </summary>
		AbsoluteMpegFrames   = 0x01,
		
		/// <summary>
		///    The timestamp represents the number of milliseconds since
		///    the beginning of the audio stream.
		/// </summary>
		AbsoluteMilliseconds = 0x02
	}
}
