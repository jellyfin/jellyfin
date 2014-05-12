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
using System.ComponentModel;
namespace OpenSubtitlesHandler
{
    [MethodResponseDescription("ServerInfo method response",
             "ServerInfo method response hold all expected values from server.")]
    public class MethodResponseServerInfo : IMethodResponse
    {
        public MethodResponseServerInfo()
            : base()
        { }
        public MethodResponseServerInfo(string name, string message)
            : base(name, message)
        { }
        private string _xmlrpc_version;
        private string _xmlrpc_url;
        private string _application;
        private string _contact;
        private string _website_url;
        private int _users_online_total;
        private int _users_online_program;
        private int _users_loggedin;
        private string _users_max_alltime;
        private string _users_registered;
        private string _subs_downloads;
        private string _subs_subtitle_files;
        private string _movies_total;
        private string _movies_aka;
        private string _total_subtitles_languages;
        private List<string> _last_update_strings = new List<string>();

        /// <summary>
        /// Version of server's XML-RPC API implementation 
        /// </summary>
        [Description("Version of server's XML-RPC API implementation"), Category("OS")]
        public string xmlrpc_version { get { return _xmlrpc_version; } set { _xmlrpc_version = value; } }
        /// <summary>
        /// XML-RPC interface URL 
        /// </summary>
        [Description("XML-RPC interface URL"), Category("OS")]
        public string xmlrpc_url { get { return _xmlrpc_url; } set { _xmlrpc_url = value; } }
        /// <summary>
        /// Server's application name and version
        /// </summary>
        [Description("Server's application name and version"), Category("OS")]
        public string application { get { return _application; } set { _application = value; } }
        /// <summary>
        /// Contact e-mail address for server related quuestions and problems 
        /// </summary>
        [Description("Contact e-mail address for server related quuestions and problems"), Category("OS")]
        public string contact { get { return _contact; } set { _contact = value; } }
        /// <summary>
        /// Main server URL  
        /// </summary>
        [Description("Main server URL"), Category("OS")]
        public string website_url { get { return _website_url; } set { _website_url = value; } }
        /// <summary>
        /// Number of users currently online   
        /// </summary>
        [Description("Number of users currently online"), Category("OS")]
        public int users_online_total { get { return _users_online_total; } set { _users_online_total = value; } }
        /// <summary>
        /// Number of users currently online using a client application (XML-RPC API)   
        /// </summary>
        [Description("Number of users currently online using a client application (XML-RPC API)"), Category("OS")]
        public int users_online_program { get { return _users_online_program; } set { _users_online_program = value; } }
        /// <summary>
        /// Number of currently logged-in users 
        /// </summary>
        [Description("Number of currently logged-in users"), Category("OS")]
        public int users_loggedin { get { return _users_loggedin; } set { _users_loggedin = value; } }
        /// <summary>
        /// Maximum number of users throughout the history
        /// </summary>
        [Description("Maximum number of users throughout the history"), Category("OS")]
        public string users_max_alltime { get { return _users_max_alltime; } set { _users_max_alltime = value; } }
        /// <summary>
        /// Number of registered users
        /// </summary>
        [Description("Number of registered users"), Category("OS")]
        public string users_registered { get { return _users_registered; } set { _users_registered = value; } }
        /// <summary>
        /// Total number of subtitle downloads
        /// </summary>
        [Description("Total number of subtitle downloads"), Category("OS")]
        public string subs_downloads { get { return _subs_downloads; } set { _subs_downloads = value; } }
        /// <summary>
        /// Total number of subtitle files stored on the server 
        /// </summary>
        [Description("Total number of subtitle files stored on the server"), Category("OS")]
        public string subs_subtitle_files { get { return _subs_subtitle_files; } set { _subs_subtitle_files = value; } }
        /// <summary>
        /// Total number of movies in the database
        /// </summary>
        [Description("Total number of movies in the database"), Category("OS")]
        public string movies_total { get { return _movies_total; } set { _movies_total = value; } }
        /// <summary>
        /// Total number of movie A.K.A. titles in the database
        /// </summary>
        [Description("Total number of movie A.K.A. titles in the database"), Category("OS")]
        public string movies_aka { get { return _movies_aka; } set { _movies_aka = value; } }
        /// <summary>
        /// Total number of subtitle languages supported 
        /// </summary>
        [Description("Total number of subtitle languages supported"), Category("OS")]
        public string total_subtitles_languages { get { return _total_subtitles_languages; } set { _total_subtitles_languages = value; } }
        /// <summary>
        /// Structure containing information about last updates of translations.
        /// </summary>
        [Description("Structure containing information about last updates of translations"), Category("OS")]
        public List<string> last_update_strings { get { return _last_update_strings; } set { _last_update_strings = value; } }
    }
}
