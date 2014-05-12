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
    [MethodResponseDescription("NoOperation method response",
           "NoOperation method response hold all expected values from server.")]
    public class MethodResponseNoOperation : IMethodResponse
    {
        public MethodResponseNoOperation()
            : base() { }
        public MethodResponseNoOperation(string name, string message)
            : base(name, message)
        { }

        private string _global_wrh_download_limit;
        private string _client_ip;
        private string _limit_check_by;
        private string _client_24h_download_count;
        private string _client_downlaod_quota;
        private string _client_24h_download_limit;

        public string global_wrh_download_limit
        { get { return _global_wrh_download_limit; } set { _global_wrh_download_limit = value; } }
        public string client_ip
        { get { return _client_ip; } set { _client_ip = value; } }
        public string limit_check_by
        { get { return _limit_check_by; } set { _limit_check_by = value; } }
        public string client_24h_download_count
        { get { return _client_24h_download_count; } set { _client_24h_download_count = value; } }
        public string client_downlaod_quota
        { get { return _client_downlaod_quota; } set { _client_downlaod_quota = value; } }
        public string client_24h_download_limit
        { get { return _client_24h_download_limit; } set { _client_24h_download_limit = value; } }
    }
}
