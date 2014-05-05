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
using System.ComponentModel;

namespace OpenSubtitlesHandler
{
    [MethodResponseDescription("AutoUpdate method response",
       "AutoUpdate method response hold all expected values from server.")]
    public class MethodResponseAutoUpdate : IMethodResponse
    {
        public MethodResponseAutoUpdate()
            : base()
        { }
        public MethodResponseAutoUpdate(string name, string message)
            : base(name, message)
        { }

        private string _version;
        private string _url_windows;
        private string _comments;
        private string _url_linux;
        /// <summary>
        /// Latest application version 
        /// </summary>
        [Description("Latest application version"), Category("AutoUpdate")]
        public string version { get { return _version; } set { _version = value; } }
        /// <summary>
        /// Download URL for Windows version 
        /// </summary>
        [Description("Download URL for Windows version"), Category("AutoUpdate")]
        public string url_windows { get { return _url_windows; } set { _url_windows = value; } }
        /// <summary>
        /// Application changelog and other comments  
        /// </summary>
        [Description("Application changelog and other comments"), Category("AutoUpdate")]
        public string comments { get { return _comments; } set { _comments = value; } }
        /// <summary>
        /// Download URL for Linux version 
        /// </summary>
        [Description("Download URL for Linux version"), Category("AutoUpdate")]
        public string url_linux { get { return _url_linux; } set { _url_linux = value; } }
    }
}
