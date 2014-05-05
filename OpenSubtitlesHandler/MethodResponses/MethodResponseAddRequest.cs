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
    [MethodResponseDescription("AddRequest method response",
       "AddRequest method response hold all expected values from server.")]
    public class MethodResponseAddRequest : IMethodResponse
    {
        public MethodResponseAddRequest()
            : base()
        { }
        public MethodResponseAddRequest(string name, string message)
            : base(name, message)
        { }
        private string _request_url;

        public string request_url { get { return _request_url; } set { _request_url = value; } }
    }
}
