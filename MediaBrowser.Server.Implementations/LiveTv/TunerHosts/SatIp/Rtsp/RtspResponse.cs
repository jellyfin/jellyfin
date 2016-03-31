/*  
    Copyright (C) <2007-2016>  <Kay Diefenthal>

    SatIp.RtspSample is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SatIp.RtspSample is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SatIp.RtspSample.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.SatIp.Rtsp
{
    /// <summary>
    /// A simple class that can be used to deserialise RTSP responses.
    /// </summary>
    public class RtspResponse
    {
        private static readonly Regex RegexStatusLine = new Regex(@"RTSP/(\d+)\.(\d+)\s+(\d+)\s+([^.]+?)\r\n(.*)", RegexOptions.Singleline);

        private int _majorVersion = 1;
        private int _minorVersion;
        private RtspStatusCode _statusCode;
        private string _reasonPhrase;
        private IDictionary<string, string> _headers;
        private string _body;

        /// <summary>
        /// Initialise a new instance of the <see cref="RtspResponse"/> class.
        /// </summary>
        private RtspResponse()
        {
        }

        /// <summary>
        /// Get the response major version number.
        /// </summary>
        public int MajorVersion
        {
            get
            {
                return _majorVersion;
            }
        }

        /// <summary>
        /// Get the response minor version number.
        /// </summary>
        public int MinorVersion
        {
            get
            {
                return _minorVersion;
            }
        }

        /// <summary>
        /// Get the response status code.
        /// </summary>
        public RtspStatusCode StatusCode
        {
            get
            {
                return _statusCode;
            }
        }

        /// <summary>
        /// Get the response reason phrase.
        /// </summary>
        public string ReasonPhrase
        {
            get
            {
                return _reasonPhrase;
            }
        }

        /// <summary>
        /// Get the response headers.
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get
            {
                return _headers;
            }
        }

        /// <summary>
        /// Get the response body.
        /// </summary>
        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
            }
        }

        /// <summary>
        /// Deserialise/parse an RTSP response.
        /// </summary>
        /// <param name="responseBytes">The raw response bytes.</param>
        /// <param name="responseByteCount">The number of valid bytes in the response.</param>
        /// <returns>a response object</returns>
        public static RtspResponse Deserialise(byte[] responseBytes, int responseByteCount)
        {
            var response = new RtspResponse();
            var responseString = Encoding.UTF8.GetString(responseBytes, 0, responseByteCount);

            var m = RegexStatusLine.Match(responseString);
            if (m.Success)
            {
                response._majorVersion = int.Parse(m.Groups[1].Captures[0].Value);
                response._minorVersion = int.Parse(m.Groups[2].Captures[0].Value);
                response._statusCode = (RtspStatusCode)int.Parse(m.Groups[3].Captures[0].Value);
                response._reasonPhrase = m.Groups[4].Captures[0].Value;
                responseString = m.Groups[5].Captures[0].Value;
            }

            var sections = responseString.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None);
            response._body = sections[1];
            var headers = sections[0].Split(new[] { "\r\n" }, StringSplitOptions.None);
            response._headers = new Dictionary<string, string>();
            foreach (var headerInfo in headers.Select(header => header.Split(':')))
            {
                response._headers.Add(headerInfo[0], headerInfo[1].Trim());
            }
            return response;
        }
    }
}
