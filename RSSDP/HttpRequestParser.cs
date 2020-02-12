using System;
using System.Linq;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Parses a string into a <see cref="HttpRequestMessage"/> or throws an exception.
    /// </summary>
    public sealed class HttpRequestParser : HttpParserBase<HttpRequestMessage>
    {

        #region Fields & Constants

        private readonly string[] ContentHeaderNames = new string[]
                {
                    "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified"
                };

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the specified data into a <see cref="HttpRequestMessage"/> instance.
        /// </summary>
        /// <param name="data">A string containing the data to parse.</param>
        /// <returns>A <see cref="HttpRequestMessage"/> instance containing the parsed data.</returns>
        public override HttpRequestMessage Parse(string data)
        {
            HttpRequestMessage retVal = null;

            try
            {
                retVal = new HttpRequestMessage();

                Parse(retVal, retVal.Headers, data);

                return retVal;
            }
            finally
            {
                if (retVal != null)
                    retVal.Dispose();
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Used to parse the first line of an HTTP request or response and assign the values to the appropriate properties on the <paramref name="message"/>.
        /// </summary>
        /// <param name="data">The first line of the HTTP message to be parsed.</param>
        /// <param name="message">Either a <see cref="HttpResponseMessage"/> or <see cref="HttpRequestMessage"/> to assign the parsed values to.</param>
        protected override void ParseStatusLine(string data, HttpRequestMessage message)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var parts = data.Split(' ');
            if (parts.Length < 2) throw new ArgumentException("Status line is invalid. Insufficient status parts.", nameof(data));

            message.Method = new HttpMethod(parts[0].Trim());
            Uri requestUri;
            if (Uri.TryCreate(parts[1].Trim(), UriKind.RelativeOrAbsolute, out requestUri))
                message.RequestUri = requestUri;
            else
                System.Diagnostics.Debug.WriteLine(parts[1]);

            if (parts.Length >= 3)
            {
                message.Version = ParseHttpVersion(parts[2].Trim());
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the specified HTTP header name represents a content header (true), or a message header (false).
        /// </summary>
        /// <param name="headerName">A string containing the name of the header to return the type of.</param>
        protected override bool IsContentHeader(string headerName)
        {
            return ContentHeaderNames.Contains(headerName, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

    }
}
