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
    public class InsertMovieHashParameters
    {
        private string _moviehash = "";
        private string _moviebytesize = "";
        private string _imdbid = "";
        private string _movietimems = "";
        private string _moviefps = "";
        private string _moviefilename = "";

        public string moviehash { get { return _moviehash; } set { _moviehash = value; } }
        public string moviebytesize { get { return _moviebytesize; } set { _moviebytesize = value; } }
        public string imdbid { get { return _imdbid; } set { _imdbid = value; } }
        public string movietimems { get { return _movietimems; } set { _movietimems = value; } }
        public string moviefps { get { return _moviefps; } set { _moviefps = value; } }
        public string moviefilename { get { return _moviefilename; } set { _moviefilename = value; } }
    }
}
