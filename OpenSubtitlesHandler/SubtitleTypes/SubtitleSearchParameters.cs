/* This file is part of OpenSubtitles Handler
   A library that handle OpenSubtitles.org XML-RPC methods.

   Copyright © Ala Ibrahim Hadid 2013

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace OpenSubtitlesHandler
{
    /// <summary>
    /// Paramaters for subtitle search call
    /// </summary>
    public struct SubtitleSearchParameters
    {
        public SubtitleSearchParameters(string subLanguageId, string query = "", string season = "", string episode = "", string movieHash = "", long movieByteSize = 0, string imdbid = "")
        {
            this.subLanguageId = subLanguageId;
            this.movieHash = movieHash;
            this.movieByteSize = movieByteSize;
            this.imdbid = imdbid;
            this._episode = episode;
            this._season = season;
            this._query = query;
        }

        private string subLanguageId;
        private string movieHash;
        private long movieByteSize;
        private string imdbid;
        private string _query;
        private string _episode;

        public string Episode {
            get { return _episode; }
            set { _episode = value; }
        }

        public string Season {
            get { return _season; }
            set { _season = value; }
        }

        private string _season;

        public string Query {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// List of language ISO639-3 language codes to search for, divided by ',' (e.g. 'cze,eng,slo')
        /// </summary>
        public string SubLangaugeID { get { return subLanguageId; } set { subLanguageId = value; } }
        /// <summary>
        /// Video file hash as calculated by one of the implementation functions as seen on http://trac.opensubtitles.org/projects/opensubtitles/wiki/HashSourceCodes
        /// </summary>
        public string MovieHash { get { return movieHash; } set { movieHash = value; } }
        /// <summary>
        /// Size of video file in bytes 
        /// </summary>
        public long MovieByteSize { get { return movieByteSize; } set { movieByteSize = value; } }
        /// <summary>
        /// ​IMDb ID of movie this video is part of, belongs to.
        /// </summary>
        public string IMDbID  { get { return imdbid; } set { imdbid = value; } }
    }
}
