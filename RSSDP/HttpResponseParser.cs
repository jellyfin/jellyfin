using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Parses a string into a <see cref="HttpResponseMessage"/> or throws an exception.
    /// </summary>
    public sealed class HttpResponseParser : HttpParserBase<HttpResponseMessage>
    {

        #region Fields & Constants

        private readonly string[] ContentHeaderNames = new string[]
                {
                    "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified"
                };

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the specified data into a <see cref="HttpResponseMessage"/> instance.
        /// </summary>
        /// <param name="data">A string containing the data to parse.</param>
        /// <returns>A <see cref="HttpResponseMessage"/> instance containing the parsed data.</returns>
        public override HttpResponseMessage Parse(string data)
        {
            HttpResponseMessage retVal = null;
            try
            {
                retVal = new HttpResponseMessage();

                Parse(retVal, retVal.Headers, data);

                return retVal;
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        #endregion

        #region Overrides Methods

        /// <summary>
        /// Returns a boolean indicating whether the specified HTTP header name represents a content header (true), or a message header (false).
        /// </summary>
        /// <param name="headerName">A string containing the name of the header to return the type of.</param>
        /// <returns>A boolean, true if th specified header relates to HTTP content, otherwise false.</returns>
        protected override bool IsContentHeader(string headerName)
        {
            return ContentHeaderNames.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Used to parse the first line of an HTTP request or response and assign the values to the appropriate properties on the <paramref name="message"/>.
        /// </summary>
        /// <param name="data">The first line of the HTTP message to be parsed.</param>
        /// <param name="message">Either a <see cref="HttpResponseMessage"/> or <see cref="HttpRequestMessage"/> to assign the parsed values to.</param>
        protected override void ParseStatusLine(string data, HttpResponseMessage message)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var parts = data.Split(' ');
            if (parts.Length < 2) throw new ArgumentException("data status line is invalid. Insufficient status parts.", nameof(data));

            message.Version = ParseHttpVersion(parts[0].Trim());

            int statusCode = -1;
            if (!Int32.TryParse(parts[1].Trim(), out statusCode))
                throw new ArgumentException("data status line is invalid. Status code is not a valid integer.", nameof(data));

            message.StatusCode = (HttpStatusCode)statusCode;

            if (parts.Length >= 3)
            {
                message.ReasonPhrase = parts[2].Trim();
            }
        }

        #endregion
    }
}
