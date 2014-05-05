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
    public class UploadSubtitleInfoParameters
    {
        private string _idmovieimdb;
        private string _moviereleasename;
        private string _movieaka;
        private string _sublanguageid;
        private string _subauthorcomment;
        private bool _hearingimpaired;
        private bool _highdefinition;
        private bool _automatictranslation;
        private List<UploadSubtitleParameters> cds;

        public string idmovieimdb { get { return _idmovieimdb; } set { _idmovieimdb = value; } }
        public string moviereleasename { get { return _moviereleasename; } set { _moviereleasename = value; } }
        public string movieaka { get { return _movieaka; } set { _movieaka = value; } }
        public string sublanguageid { get { return _sublanguageid; } set { _sublanguageid = value; } }
        public string subauthorcomment { get { return _subauthorcomment; } set { _subauthorcomment = value; } }
        public bool hearingimpaired { get { return _hearingimpaired; } set { _hearingimpaired = value; } }
        public bool highdefinition { get { return _highdefinition; } set { _highdefinition = value; } }
        public bool automatictranslation { get { return _automatictranslation; } set { _automatictranslation = value; } }
        public List<UploadSubtitleParameters> CDS { get { return cds; } set { cds = value; } }
    }
}
