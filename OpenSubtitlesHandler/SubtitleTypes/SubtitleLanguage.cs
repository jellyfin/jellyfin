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
    public struct SubtitleLanguage
    {
        private string _SubLanguageID;
        private string _LanguageName;
        private string _ISO639;

        public string SubLanguageID { get { return _SubLanguageID; } set { _SubLanguageID = value; } }
        public string LanguageName { get { return _LanguageName; } set { _LanguageName = value; } }
        public string ISO639 { get { return _ISO639; } set { _ISO639 = value; } }
        /// <summary>
        /// LanguageName [SubLanguageID]
        /// </summary>
        /// <returns>LanguageName [SubLanguageID]</returns>
        public override string ToString()
        {
            return _LanguageName + " [" + _SubLanguageID + "]";
        }
    }
}
