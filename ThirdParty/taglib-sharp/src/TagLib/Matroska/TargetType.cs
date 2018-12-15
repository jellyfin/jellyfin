//
// TargetType.cs:
//
// Author:
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
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

namespace TagLib.Matroska
{
	/// <summary>
	/// Represents a Matroska TargetType.
	/// The TargetType element allows tagging of different parts that are inside or outside a given file. 
	/// For example in an audio file with one song you could have information about the album it comes 
	/// from and even the CD set even if it's not found in the file.
	/// </summary>
	public enum TargetType : ushort
	{
		/// <summary>
		/// Audio/Video: The high hierarchy consisting of many different lower items
		/// </summary>
		COLLECTION = 70,

		/// <summary>
		/// Audio/Video: a list of lower levels grouped together
		/// </summary>
		VOLUME = 60,

		/// <summary>
		/// Audio: a list of lower levels grouped together
		/// </summary>
		/// 
		EDITION = 61,

		/// <summary>
		/// Audio: a list of lower levels grouped together
		/// </summary>
		ISSUE = 62,

		/// <summary>
		/// Audio: a list of lower levels grouped together
		/// </summary>
		OPUS = 63,

		/// <summary>
		/// Video: a list of lower levels grouped together
		/// </summary>
		SEASON = 64,

		/// <summary>
		/// Video: a list of lower levels grouped together
		/// </summary>
		SEQUEL = 65,

		/// <summary>
		/// Audio: the most common grouping level of music and video (equals to an episode for TV series)
		/// </summary>
		ALBUM = 50,

		/// <summary>
		/// Audio: the most common grouping level of music and video (equals to an episode for TV series)
		/// </summary>
		OPERA = 51,

		/// <summary>
		/// Audio/Video: the most common grouping level of music and video (equals to an episode for TV series)
		/// </summary>
		CONCERT = 52,

		/// <summary>
		/// Video: the most common grouping level of music and video (equals to an episode for TV series)
		/// </summary>    
		MOVIE = 53,

		/// <summary>
		/// Video: the most common grouping level of music and video (equals to an episode for TV series)
		/// </summary>
		EPISODE = 54,

		/// <summary>
		/// Represent the default TargetType (an empty Targets), i.e. targets everything in the segment.
		/// </summary>
		DEFAULT = 55,

		/// <summary>
		/// Audio/Video: when an album or episode has different logical parts
		/// </summary>
		PART = 40,

		/// <summary>
		/// Audio/Video: when an album or episode has different logical parts
		/// </summary>
		SESSION = 41,

		/// <summary>
		/// Audio: the common parts of an album or a movie 
		/// </summary>
		TRACK = 30,

		/// <summary>
		/// Audio: the common parts of an album or a movie 
		/// </summary>
		SONG = 31,

		/// <summary>
		/// Video: the common parts of an album or a movie 
		/// </summary>
		CHAPTER = 32,

		/// <summary>
		/// Audio: corresponds to parts of a track for audio (like a movement)
		/// </summary>
		SUBTRACK = 20,

		/// <summary>
		/// Audio: corresponds to parts of a track for audio (like a movement)
		/// </summary>
		MOVEMENT = 21,

		/// <summary>
		/// Video: corresponds to parts of a track for audio (like a movement)
		/// </summary>
		SCENE = 22,

		/// <summary>
		/// Video: the lowest hierarchy found in music or movies
		/// </summary>
		SHOT = 10
	}

}
