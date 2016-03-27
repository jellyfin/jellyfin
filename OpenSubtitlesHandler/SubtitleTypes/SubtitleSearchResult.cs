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
    /// The subtitle search result that comes with server response on SearchSubtitles successed call
    /// </summary>
    public struct SubtitleSearchResult
    {
        private string _IDSubMovieFile;
        private string _MovieHash;
        private string _MovieByteSize;
        private string _MovieTimeMS;
        private string _IDSubtitleFile;
        private string _SubFileName;
        private string _SubActualCD;
        private string _SubSize;
        private string _SubHash;
        private string _IDSubtitle;
        private string _UserID;
        private string _SubLanguageID;
        private string _SubFormat;
        private string _SeriesSeason;
        private string _SeriesEpisode;
        private string _SubSumCD;
        private string _SubAuthorComment;
        private string _SubAddDate;
        private string _SubBad;
        private string _SubRating;
        private string _SubDownloadsCnt;
        private string _MovieReleaseName;
        private string _IDMovie;
        private string _IDMovieImdb;
        private string _MovieName;
        private string _MovieNameEng;
        private string _MovieYear;
        private string _MovieImdbRating;
        private string _UserNickName;
        private string _ISO639;
        private string _LanguageName;
        private string _SubDownloadLink;
        private string _ZipDownloadLink;

        public string IDSubMovieFile
        { get { return _IDSubMovieFile; } set { _IDSubMovieFile = value; } }
        public string MovieHash
        { get { return _MovieHash; } set { _MovieHash = value; } }
        public string MovieByteSize
        { get { return _MovieByteSize; } set { _MovieByteSize = value; } }
        public string MovieTimeMS
        { get { return _MovieTimeMS; } set { _MovieTimeMS = value; } }
        public string IDSubtitleFile
        { get { return _IDSubtitleFile; } set { _IDSubtitleFile = value; } }
        public string SubFileName
        { get { return _SubFileName; } set { _SubFileName = value; } }
        public string SubActualCD
        { get { return _SubActualCD; } set { _SubActualCD = value; } }
        public string SubSize
        { get { return _SubSize; } set { _SubSize = value; } }
        public string SubHash
        { get { return _SubHash; } set { _SubHash = value; } }
        public string IDSubtitle
        { get { return _IDSubtitle; } set { _IDSubtitle = value; } }
        public string UserID
        { get { return _UserID; } set { _UserID = value; } }
        public string SubLanguageID
        { get { return _SubLanguageID; } set { _SubLanguageID = value; } }
        public string SubFormat
        { get { return _SubFormat; } set { _SubFormat = value; } }
        public string SubSumCD
        { get { return _SubSumCD; } set { _SubSumCD = value; } }
        public string SubAuthorComment
        { get { return _SubAuthorComment; } set { _SubAuthorComment = value; } }
        public string SubAddDate
        { get { return _SubAddDate; } set { _SubAddDate = value; } }
        public string SubBad
        { get { return _SubBad; } set { _SubBad = value; } }
        public string SubRating
        { get { return _SubRating; } set { _SubRating = value; } }
        public string SubDownloadsCnt
        { get { return _SubDownloadsCnt; } set { _SubDownloadsCnt = value; } }
        public string MovieReleaseName
        { get { return _MovieReleaseName; } set { _MovieReleaseName = value; } }
        public string IDMovie
        { get { return _IDMovie; } set { _IDMovie = value; } }
        public string IDMovieImdb
        { get { return _IDMovieImdb; } set { _IDMovieImdb = value; } }
        public string MovieName
        { get { return _MovieName; } set { _MovieName = value; } }
        public string MovieNameEng
        { get { return _MovieNameEng; } set { _MovieNameEng = value; } }
        public string MovieYear
        { get { return _MovieYear; } set { _MovieYear = value; } }
        public string MovieImdbRating
        { get { return _MovieImdbRating; } set { _MovieImdbRating = value; } }
        public string UserNickName
        { get { return _UserNickName; } set { _UserNickName = value; } }
        public string ISO639
        { get { return _ISO639; } set { _ISO639 = value; } }
        public string LanguageName
        { get { return _LanguageName; } set { _LanguageName = value; } }
        public string SubDownloadLink
        { get { return _SubDownloadLink; } set { _SubDownloadLink = value; } }
        public string ZipDownloadLink
        { get { return _ZipDownloadLink; } set { _ZipDownloadLink = value; } }
        public string SeriesSeason
        { get { return _SeriesSeason; } set { _SeriesSeason = value; } }
        public string SeriesEpisode
        { get { return _SeriesEpisode; } set { _SeriesEpisode = value; } }
        /// <summary>
        /// SubFileName + " (" + SubFormat + ")"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _SubFileName + " (" + _SubFormat + ")";
        }
    }
}
