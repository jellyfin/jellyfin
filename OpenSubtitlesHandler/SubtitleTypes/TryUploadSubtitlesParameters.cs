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
    public class  TryUploadSubtitlesParameters
    {
        private string _subhash = "";
        private string _subfilename = "";
        private string _moviehash = "";
        private string _moviebytesize = "";
        private int _movietimems = 0;
        private int _movieframes = 0;
        private double _moviefps = 0;
        private string _moviefilename = "";

        public string subhash { get { return _subhash; } set { _subhash = value; } }
        public string subfilename { get { return _subfilename; } set { _subfilename = value; } }
        public string moviehash { get { return _moviehash; } set { _moviehash = value; } }
        public string moviebytesize { get { return _moviebytesize; } set { _moviebytesize = value; } }
        public int movietimems { get { return _movietimems; } set { _movietimems = value; } }
        public int movieframes { get { return _movieframes; } set { _movieframes = value; } }
        public double moviefps { get { return _moviefps; } set { _moviefps = value; } }
        public string moviefilename { get { return _moviefilename; } set { _moviefilename = value; } }

        public override string ToString()
        {
            return _subfilename + " (" + _subhash + ")";
        }
    }
}
