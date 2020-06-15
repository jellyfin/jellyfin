// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Mono.Nat.Upnp
{
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Defines the <see cref="RequestMessage" />.
    /// </summary>
    internal abstract class RequestMessage : IRequestMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessage"/> class.
        /// </summary>
        /// <param name="device">The device<see cref="UpnpNatDevice"/>.</param>
        /// <param name="requestType">The requestType<see cref="string"/>.</param>
        protected RequestMessage(UpnpNatDevice device, string requestType)
        {
            Device = device;
            RequestType = requestType;
        }

        /// <summary>
        /// Gets the RequestType.
        /// </summary>
        internal string RequestType { get; }

        /// <summary>
        /// Gets the Device.
        /// </summary>
        protected UpnpNatDevice Device { get; }

        /// <summary>
        /// The Encode.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>The <see cref="HttpWebRequest"/>.</returns>
        public HttpWebRequest Encode(out byte[] body)
        {
            var builder = new StringBuilder(256);
            var settings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (var writer = XmlWriter.Create(builder, settings))
            {
                Encode(writer);
            }

            return CreateRequest(RequestType, builder.ToString(), out body);
        }

        /// <summary>
        /// The Encode.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        public virtual void Encode(XmlWriter writer)
        {
        }

        /// <summary>
        /// The WriteFullElement.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        /// <param name="element">The element<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="IPAddress"/>.</param>
        protected static void WriteFullElement(XmlWriter writer, string element, IPAddress value)
            => WriteFullElement(writer, element, value.ToString());

        /// <summary>
        /// The WriteFullElement.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        /// <param name="element">The element<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="Protocol"/>.</param>
        protected static void WriteFullElement(XmlWriter writer, string element, Protocol value)
            => WriteFullElement(writer, element, value == Protocol.Tcp ? "TCP" : "UDP");

        /// <summary>
        /// The WriteFullElement.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        /// <param name="element">The element<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="int"/>.</param>
        protected static void WriteFullElement(XmlWriter writer, string element, int value)
            => WriteFullElement(writer, element, value.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// The WriteFullElement.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        /// <param name="element">The element<see cref="string"/>.</param>
        /// <param name="value">The value<see cref="string"/>.</param>
        protected static void WriteFullElement(XmlWriter writer, string element, string value)
        {
            writer.WriteStartElement(element);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        /// <summary>
        /// The CreateRequest.
        /// </summary>
        /// <param name="upnpMethod">The upnpMethod<see cref="string"/>.</param>
        /// <param name="methodParameters">The methodParameters<see cref="string"/>.</param>
        /// <param name="body">The body.</param>
        /// <returns>The <see cref="HttpWebRequest"/>.</returns>
        protected HttpWebRequest CreateRequest(string upnpMethod, string methodParameters, out byte[] body)
        {
            var location = Device.DeviceControlUri;

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(location);
            req.KeepAlive = false;
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", "\"" + Device.ServiceType + "#" + upnpMethod + "\"");

            string bodyString = "<s:Envelope "
               + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
               + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
               + "<s:Body>"
               + "<u:" + upnpMethod + " "
               + "xmlns:u=\"" + Device.ServiceType + "\">"
               + methodParameters
               + "</u:" + upnpMethod + ">"
               + "</s:Body>"
               + "</s:Envelope>\r\n\r\n";

            body = Encoding.UTF8.GetBytes(bodyString);
            return req;
        }
    }
}
