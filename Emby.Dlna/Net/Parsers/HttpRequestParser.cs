#nullable enable
using System;
using System.Net.Http;

namespace Emby.Dlna.Net.Parsers
{
    /// <summary>
    /// Parses a string into a <see cref="HttpRequestMessage"/> or throws an exception.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class HttpRequestParser : HttpParserBase<HttpRequestMessage>
    {
        /// <summary>
        /// Parses the specified data into a <see cref="HttpRequestMessage"/> instance.
        /// </summary>
        /// <param name="data">A string containing the data to parse.</param>
        /// <returns>A <see cref="HttpRequestMessage"/> instance containing the parsed data.</returns>
        public override HttpRequestMessage Parse(string data)
        {
            HttpRequestMessage? retVal = null;

            try
            {
                retVal = new HttpRequestMessage();

                Parse(retVal, retVal.Headers, data);

                return retVal;
            }
            catch
            {
                if (retVal != null)
                {
                    retVal.Dispose();
                }

                throw;
            }
        }

        /// <summary>
        /// Used to parse the first line of an HTTP request or response and assign the values to the appropriate properties on the <paramref name="message"/>.
        /// </summary>
        /// <param name="data">The first line of the HTTP message to be parsed.</param>
        /// <param name="message">Either a <see cref="HttpResponseMessage"/> or <see cref="HttpRequestMessage"/> to assign the parsed values to.</param>
        protected override void ParseStatusLine(string data, HttpRequestMessage message)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var parts = data.Split(' ');
            if (parts.Length < 2)
            {
                throw new ArgumentException("Status line is invalid. Insufficient status parts.", nameof(data));
            }

            message.Method = new HttpMethod(parts[0].Trim());
            if (Uri.TryCreate(parts[1].Trim(), UriKind.RelativeOrAbsolute, out var requestUri))
            {
                message.RequestUri = requestUri;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(parts[1]);
            }

            if (parts.Length >= 3)
            {
                message.Version = ParseHttpVersion(parts[2].Trim());
            }
        }
    }
}
