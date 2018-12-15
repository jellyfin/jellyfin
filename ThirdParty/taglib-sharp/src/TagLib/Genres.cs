//
// Genres.cs: Provides convenience functions for converting between String
// genres and their respective audio and video indices as used by several
// formats.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v1genres.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002 Scott Wheeler (Original Implementation)
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

namespace TagLib {
	/// <summary>
	///    This static class provides convenience functions for converting
	///    between <see cref="string" /> genres and their respective audio
	///    and video indices as used by several formats.
	/// </summary>
	public static class Genres
	{
		/// <summary>
		///    Contains a list of ID3v1 audio generes.
		/// </summary>
		private static readonly string [] audio = {
			"Blues",
			"Classic Rock",
			"Country",
			"Dance",
			"Disco",
			"Funk",
			"Grunge",
			"Hip-Hop",
			"Jazz",
			"Metal",
			"New Age",
			"Oldies",
			"Other",
			"Pop",
			"R&B",
			"Rap",
			"Reggae",
			"Rock",
			"Techno",
			"Industrial",
			"Alternative",
			"Ska",
			"Death Metal",
			"Pranks",
			"Soundtrack",
			"Euro-Techno",
			"Ambient",
			"Trip-Hop",
			"Vocal",
			"Jazz+Funk",
			"Fusion",
			"Trance",
			"Classical",
			"Instrumental",
			"Acid",
			"House",
			"Game",
			"Sound Clip",
			"Gospel",
			"Noise",
			"Alternative Rock",
			"Bass",
			"Soul",
			"Punk",
			"Space",
			"Meditative",
			"Instrumental Pop",
			"Instrumental Rock",
			"Ethnic",
			"Gothic",
			"Darkwave",
			"Techno-Industrial",
			"Electronic",
			"Pop-Folk",
			"Eurodance",
			"Dream",
			"Southern Rock",
			"Comedy",
			"Cult",
			"Gangsta",
			"Top 40",
			"Christian Rap",
			"Pop/Funk",
			"Jungle",
			"Native American",
			"Cabaret",
			"New Wave",
			"Psychedelic",
			"Rave",
			"Showtunes",
			"Trailer",
			"Lo-Fi",
			"Tribal",
			"Acid Punk",
			"Acid Jazz",
			"Polka",
			"Retro",
			"Musical",
			"Rock & Roll",
			"Hard Rock",
			"Folk",
			"Folk/Rock",
			"National Folk",
			"Swing",
			"Fusion",
			"Bebob",
			"Latin",
			"Revival",
			"Celtic",
			"Bluegrass",
			"Avantgarde",
			"Gothic Rock",
			"Progressive Rock",
			"Psychedelic Rock",
			"Symphonic Rock",
			"Slow Rock",
			"Big Band",
			"Chorus",
			"Easy Listening",
			"Acoustic",
			"Humour",
			"Speech",
			"Chanson",
			"Opera",
			"Chamber Music",
			"Sonata",
			"Symphony",
			"Booty Bass",
			"Primus",
			"Porn Groove",
			"Satire",
			"Slow Jam",
			"Club",
			"Tango",
			"Samba",
			"Folklore",
			"Ballad",
			"Power Ballad",
			"Rhythmic Soul",
			"Freestyle",
			"Duet",
			"Punk Rock",
			"Drum Solo",
			"A Cappella",
			"Euro-House",
			"Dance Hall",
			"Goa",
			"Drum & Bass",
			"Club-House",
			"Hardcore",
			"Terror",
			"Indie",
			"BritPop",
			"Negerpunk",
			"Polsk Punk",
			"Beat",
			"Christian Gangsta Rap",
			"Heavy Metal",
			"Black Metal",
			"Crossover",
			"Contemporary Christian",
			"Christian Rock",
			"Merengue",
			"Salsa",
			"Thrash Metal",
			"Anime",
			"Jpop",
			"Synthpop"
		};
		
		/// <summary>
		///    Contains a list of DivX audio generes.
		/// </summary>
		private static readonly string [] video = new string [] {
			"Action",
			"Action/Adventure",
			"Adult",
			"Adventure",
			"Catastrophe",
			"Child's",
			"Claymation",
			"Comedy",
			"Concert",
			"Documentary",
			"Drama",
			"Eastern",
			"Entertaining",
			"Erotic",
			"Extremal Sport",
			"Fantasy",
			"Fashion",
			"Historical",
			"Horror",
			"Horror/Mystic",
			"Humor",
			"Indian",
			"Informercial",
			"Melodrama",
			"Military & War",
			"Music Video",
			"Musical",
			"Mystery",
			"Nature",
			"Political Satire",
			"Popular Science",
			"Psychological Thriller",
			"Religion",
			"Science Fiction",
			"Scifi Action",
			"Slapstick",
			"Splatter",
			"Sports",
			"Thriller",
			"Western"
		};

		/// <summary>
		///    Gets a list of standard audio generes.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing standard audio
		///    genres.
		/// </value>
		/// <remarks>
		///    The genres are stored in the same order and with the same
		///    values as in the ID3v1 format.
		/// </remarks>
		public static string [] Audio {
			get {return (string []) audio.Clone ();}
		}
		
		/// <summary>
		///    Gets a list of standard video generes.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing standard video
		///    genres.
		/// </value>
		/// <remarks>
		///    The genres are stored in the same order and with the same
		///    values as in the DivX format.
		/// </remarks>
		public static string [] Video {
			get {return (string []) video.Clone ();}
		}
		
		/// <summary>
		///    Gets the genre index for a specified audio genre.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    genre to look up.
		/// </param>
		/// <returns>
		///    A <see cref="byte" /> value containing the index of the
		///    genre in the audio array or 255 if it could not be found.
		/// </returns>
		public static byte AudioToIndex (string name)
		{
			for (byte i = 0; i < audio.Length; i ++)
				if (name == audio [i])
					return i;
			return 255;
		}
		
		/// <summary>
		///    Gets the genre index for a specified video genre.
		/// </summary>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name of the
		///    genre to look up.
		/// </param>
		/// <returns>
		///    A <see cref="byte" /> value containing the index of the
		///    genre in the video array or 255 if it could not be found.
		/// </returns>
		public static byte VideoToIndex (string name)
		{
			for (byte i = 0; i < video.Length; i ++)
				if (name == video [i])
					return i;
			return 255;
		}
		
		/// <summary>
		///    Gets the audio genre from its index in the array.
		/// </summary>
		/// <param name="index">
		///    A <see cref="byte" /> value containing the index to
		///    aquire the genre from.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the audio genre
		///    found at the index, or <see langword="null" /> if it does
		///    not exist.
		/// </returns>
		public static string IndexToAudio (byte index)
		{
			return (index < audio.Length) ? audio [index] : null;
		}
		
		/// <summary>
		///    Gets the video genre from its index in the array.
		/// </summary>
		/// <param name="index">
		///    A <see cref="byte" /> value containing the index to
		///    aquire the genre from.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the video genre
		///    found at the index, or <see langword="null" /> if it does
		///    not exist.
		/// </returns>
		public static string IndexToVideo (byte index)
		{
			return (index < video.Length) ? video [index] : null;
		}
		
		/// <summary>
		///    Gets the audio genre from its index in the array.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> object, either in the format
		///    <c>"(123)"</c> or <c>"123"</c>.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the audio genre
		///    found at the index, or <see langword="null" /> if it does
		///    not exist.
		/// </returns>
		public static string IndexToAudio (string text)
		{
			return IndexToAudio (StringToByte (text));
		}
		
		/// <summary>
		///    Gets the video genre from its index in the array.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> object, either in the format
		///    <c>"(123)"</c> or <c>"123"</c>.
		/// </param>
		/// <returns>
		///    A <see cref="string" /> object containing the video genre
		///    found at the index, or <see langword="null" /> if it does
		///    not exist.
		/// </returns>
		public static string IndexToVideo (string text)
		{
			return IndexToVideo (StringToByte (text));
		}
		
		/// <summary>
		///    Converts a string, either in the format <c>"(123)"</c> or
		///    <c>"123"</c> into a byte or equal numeric value.
		/// </summary>
		/// <param name="text">
		///    A <see cref="string" /> object, either in the format
		///    <c>"(123)"</c> or <c>"123"</c>, to be converted.
		/// </param>
		/// <returns>
		///    A <see cref="byte" /> value containing the numeric value
		///    of <paramref name="text" /> or 255 if no numeric value
		///    could be extracted.
		/// </returns>
		private static byte StringToByte (string text)
		{
			byte value;
			int last_pos;
			if (text != null && text.Length > 2 && text [0] == '('
				&& (last_pos = text.IndexOf (')')) != -1
				&& byte.TryParse (text.Substring (1,
					last_pos - 1), out value))
				return value;
			
			if (text != null && byte.TryParse (text, out value))
				return value;
				
			return 255;
		}
	}
}
