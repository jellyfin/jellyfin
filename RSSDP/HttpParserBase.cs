using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// A base class for the <see cref="HttpResponseParser"/> and <see cref="HttpRequestParser"/> classes. Not intended for direct use.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HttpParserBase<T> where T : new()
    {

        #region Fields

        private readonly string[] LineTerminators = new string[] { "\r\n", "\n" };
        private readonly char[] SeparatorCharacters = new char[] { ',', ';' };

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the <paramref name="data"/> provided into either a <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/> object.
        /// </summary>
        /// <param name="data">A string containing the HTTP message to parse.</param>
        /// <returns>Either a <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/> object containing the parsed data.</returns>
        public abstract T Parse(string data);

        /// <summary>
        /// Parses a string containing either an HTTP request or response into a <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/> object.
        /// </summary>
        /// <param name="message">A <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/> object representing the parsed message.</param>
        /// <param name="headers">A reference to the <see cref="System.Net.Http.Headers.HttpHeaders"/> collection for the <paramref name="message"/> object.</param>
        /// <param name="data">A string containing the data to be parsed.</param>
        /// <returns>An <see cref="HttpContent"/> object containing the content of the parsed message.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Honestly, it's fine. MemoryStream doesn't mind.")]
        protected virtual void Parse(T message, System.Net.Http.Headers.HttpHeaders headers, string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) throw new ArgumentException("data cannot be an empty string.", nameof(data));
            if (!LineTerminators.Any(data.Contains)) throw new ArgumentException("data is not a valid request, it does not contain any CRLF/LF terminators.", nameof(data));

            using (var retVal = new ByteArrayContent(Array.Empty<byte>()))
            {
                var lines = data.Split(LineTerminators, StringSplitOptions.None);

                //First line is the 'request' line containing http protocol details like method, uri, http version etc.
                ParseStatusLine(lines[0], message);

                ParseHeaders(headers, retVal.Headers, lines);
            }
        }

        /// <summary>
        /// Used to parse the first line of an HTTP request or response and assign the values to the appropriate properties on the <paramref name="message"/>.
        /// </summary>
        /// <param name="data">The first line of the HTTP message to be parsed.</param>
        /// <param name="message">Either a <see cref="HttpResponseMessage"/> or <see cref="HttpRequestMessage"/> to assign the parsed values to.</param>
        protected abstract void ParseStatusLine(string data, T message);

        /// <summary>
        /// Returns a boolean indicating whether the specified HTTP header name represents a content header (true), or a message header (false).
        /// </summary>
        /// <param name="headerName">A string containing the name of the header to return the type of.</param>
        protected abstract bool IsContentHeader(string headerName);

        /// <summary>
        /// Parses the HTTP version text from an HTTP request or response status line and returns a <see cref="Version"/> object representing the parsed values.
        /// </summary>
        /// <param name="versionData">A string containing the HTTP version, from the message status line.</param>
        /// <returns>A <see cref="Version"/> object containing the parsed version data.</returns>
        protected Version ParseHttpVersion(string versionData)
        {
            if (versionData == null) throw new ArgumentNullException(nameof(versionData));

            var versionSeparatorIndex = versionData.IndexOf('/');
            if (versionSeparatorIndex <= 0 || versionSeparatorIndex == versionData.Length) throw new ArgumentException("request header line is invalid. Http Version not supplied or incorrect format.", nameof(versionData));

            return Version.Parse(versionData.Substring(versionSeparatorIndex + 1));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses a line from an HTTP request or response message containing a header name and value pair.
        /// </summary>
        /// <param name="line">A string containing the data to be parsed.</param>
        /// <param name="headers">A reference to a <see cref="System.Net.Http.Headers.HttpHeaders"/> collection to which the parsed header will be added.</param>
        /// <param name="contentHeaders">A reference to a <see cref="System.Net.Http.Headers.HttpHeaders"/> collection for the message content, to which the parsed header will be added.</param>
        private void ParseHeader(string line, System.Net.Http.Headers.HttpHeaders headers, System.Net.Http.Headers.HttpHeaders contentHeaders)
        {
            //Header format is
            //name: value
            var headerKeySeparatorIndex = line.IndexOf(":", StringComparison.OrdinalIgnoreCase);
            var headerName = line.Substring(0, headerKeySeparatorIndex).Trim();
            var headerValue = line.Substring(headerKeySeparatorIndex + 1).Trim();

            //Not sure how to determine where request headers and and content headers begin,
            //at least not without a known set of headers (general headers first the content headers)
            //which seems like a bad way of doing it. So we'll assume if it's a known content header put it there
            //else use request headers.

            var values = ParseValues(headerValue);
            var headersToAddTo = IsContentHeader(headerName) ? contentHeaders : headers;

            if (values.Count > 1)
                headersToAddTo.TryAddWithoutValidation(headerName, values);
            else
                headersToAddTo.TryAddWithoutValidation(headerName, values.First());
        }

        private int ParseHeaders(System.Net.Http.Headers.HttpHeaders headers, System.Net.Http.Headers.HttpHeaders contentHeaders, string[] lines)
        {
            //Blank line separates headers from content, so read headers until we find blank line.
            int lineIndex = 1;
            string line = null, nextLine = null;
            while (lineIndex + 1 < lines.Length && !String.IsNullOrEmpty((line = lines[lineIndex++])))
            {
                //If the following line starts with space or tab (or any whitespace), it is really part of this header but split for human readability.
                //Combine these lines into a single comma separated style header for easier parsing.
                while (lineIndex < lines.Length && !String.IsNullOrEmpty((nextLine = lines[lineIndex])))
                {
                    if (nextLine.Length > 0 && Char.IsWhiteSpace(nextLine[0]))
                    {
                        line += "," + nextLine.TrimStart();
                        lineIndex++;
                    }
                    else
                        break;
                }

                ParseHeader(line, headers, contentHeaders);
            }
            return lineIndex;
        }

        private IList<string> ParseValues(string headerValue)
        {
            // This really should be better and match the HTTP 1.1 spec,
            // but this should actually be good enough for SSDP implementations
            // I think.
            var values = new List<string>();

            if (headerValue == "\"\"")
            {
                values.Add(String.Empty);
                return values;
            }

            var indexOfSeparator = headerValue.IndexOfAny(SeparatorCharacters);
            if (indexOfSeparator <= 0)
                values.Add(headerValue);
            else
            {
                var segments = headerValue.Split(SeparatorCharacters);
                if (headerValue.Contains("\""))
                {
                    for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
                    {
                        var segment = segments[segmentIndex];
                        if (segment.Trim().StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                            segment = CombineQuotedSegments(segments, ref segmentIndex, segment);

                        values.Add(segment);
                    }
                }
                else
                    values.AddRange(segments);
            }

            return values;
        }

        private string CombineQuotedSegments(string[] segments, ref int segmentIndex, string segment)
        {
            var trimmedSegment = segment.Trim();
            for (int index = segmentIndex; index < segments.Length; index++)
            {
                if (trimmedSegment == "\"\"" ||
                    (
                        trimmedSegment.EndsWith("\"", StringComparison.OrdinalIgnoreCase)
                        && !trimmedSegment.EndsWith("\"\"", StringComparison.OrdinalIgnoreCase)
                        && !trimmedSegment.EndsWith("\\\"", StringComparison.OrdinalIgnoreCase))
                    )
                {
                    segmentIndex = index;
                    return trimmedSegment.Substring(1, trimmedSegment.Length - 2);
                }

                if (index + 1 < segments.Length)
                    trimmedSegment += "," + segments[index + 1].TrimEnd();
            }

            segmentIndex = segments.Length;
            if (trimmedSegment.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && trimmedSegment.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                return trimmedSegment.Substring(1, trimmedSegment.Length - 2);
            else
                return trimmedSegment;
        }

        #endregion

    }
}
