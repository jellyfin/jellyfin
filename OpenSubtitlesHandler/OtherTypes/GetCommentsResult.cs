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
    public struct GetCommentsResult
    {
        private string _IDSubtitle;
        private string _UserID;
        private string _UserNickName;
        private string _Comment;
        private string _Created;

        public string IDSubtitle { get { return _IDSubtitle; } set { _IDSubtitle = value; } }
        public string UserID { get { return _UserID; } set { _UserID = value; } }
        public string UserNickName { get { return _UserNickName; } set { _UserNickName = value; } }
        public string Comment { get { return _Comment; } set { _Comment = value; } }
        public string Created { get { return _Created; } set { _Created = value; } }
    }
}
