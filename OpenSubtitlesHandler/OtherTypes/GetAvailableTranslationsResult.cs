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
    public struct GetAvailableTranslationsResult
    {
        private string _language;
        private string _LastCreated;
        private string _StringsNo;

        public string LanguageID { get { return _language; } set { _language = value; } }
        public string LastCreated { get { return _LastCreated; } set { _LastCreated = value; } }
        public string StringsNo { get { return _StringsNo; } set { _StringsNo = value; } }
        /// <summary>
        /// LanguageID (LastCreated)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _language + " (" + _LastCreated + ")";
        }
    }
}
