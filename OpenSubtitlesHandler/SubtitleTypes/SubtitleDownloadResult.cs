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
    public struct SubtitleDownloadResult
    {
        private string idsubtitlefile;
        private string data;

        public string IdSubtitleFile
        { get { return idsubtitlefile; } set { idsubtitlefile = value; } }
        /// <summary>
        /// Get or set the data of subtitle file. To decode, decode the string to base64 and then decompress with GZIP.
        /// </summary>
        public string Data
        { get { return data; } set { data = value; } }
        /// <summary>
        /// IdSubtitleFile
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return idsubtitlefile.ToString();
        }
    }
}
