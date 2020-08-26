#nullable enable
using System;
using System.Net;
using System.Net.Http;

namespace Emby.Dlna.Net.Parsers
{
    /// <summary>
    /// Parses a string into a <see cref="HttpResponseMessage"/> or throws an exception.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class HttpResponseParser : HttpParserBase<HttpResponseMessage>
    {
        /// <summary>
        /// Parses the specified data into a <see cref="HttpResponseMessage"/> instance.
        /// </summary>
        /// <param name="data">A string containing the data to parse.</param>
        /// <returns>A <see cref="HttpResponseMessage"/> instance containing the parsed data.</returns>
        public override HttpResponseMessage Parse(string data)
        {
            HttpResponseMessage? retVal = null;
            try
            {
                retVal = new HttpResponseMessage();

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
        protected override void ParseStatusLine(string data, HttpResponseMessage message)
        {
            var parts = data.Split(' ');
            if (parts.Length < 2)
            {
                throw new ArgumentException("data status line is invalid. Insufficient status parts.", nameof(data));
            }

            message.Version = ParseHttpVersion(parts[0].Trim());

            if (!int.TryParse(parts[1].Trim(), out var statusCode))
            {
                throw new ArgumentException("data status line is invalid. Status code is not a valid integer.", nameof(data));
            }

            message.StatusCode = (HttpStatusCode)statusCode;

            if (parts.Length >= 3)
            {
                message.ReasonPhrase = parts[2].Trim();
            }
        }
    }
}
