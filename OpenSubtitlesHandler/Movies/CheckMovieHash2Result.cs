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
using System.Collections.Generic;

namespace OpenSubtitlesHandler
{
    public class CheckMovieHash2Result
    {
        private string name;
        private List<CheckMovieHash2Data> data = new List<CheckMovieHash2Data>();

        public string Name { get { return name; } set { name = value; } }
        public List<CheckMovieHash2Data> Items { get { return data; } set { data = value; } }

        public override string ToString()
        {
            return name;
        }
    }
}
