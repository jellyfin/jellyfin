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
using System;

namespace OpenSubtitlesHandler
{
    public struct UploadSubtitleParameters
    {
        private string _subhash;
        private string _subfilename;
        private string _moviehash;
        private double _moviebytesize;
        private int _movietimems;
        private int _movieframes;
        private double _moviefps;
        private string _moviefilename;
        private string _subcontent;

        public string subhash { get { return _subhash; } set { _subhash = value; } }
        public string subfilename { get { return _subfilename; } set { _subfilename = value; } }
        public string moviehash { get { return _moviehash; } set { _moviehash = value; } }
        public double moviebytesize { get { return _moviebytesize; } set { _moviebytesize = value; } }
        public int movietimems { get { return _movietimems; } set { _movietimems = value; } }
        public int movieframes { get { return _movieframes; } set { _movieframes = value; } }
        public double moviefps { get { return _moviefps; } set { _moviefps = value; } }
        public string moviefilename { get { return _moviefilename; } set { _moviefilename = value; } }
        /// <summary>
        /// Sub content. Note: this value must be the subtitle file gziped and then base64 decoded.
        /// </summary>
        public string subcontent { get { return _subcontent; } set { _subcontent = value; } }

        public override string ToString()
        {
            return _subfilename + " (" + _subhash + ")";
        }
    }
}
