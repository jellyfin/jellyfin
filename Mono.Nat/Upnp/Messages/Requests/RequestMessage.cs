//
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
//

using System;
using System.Net;
using System.Text;
using System.Xml;

namespace Mono.Nat.Upnp
{
    abstract class RequestMessage : IRequestMessage
    {
        protected UpnpNatDevice Device { get; }

        string RequestType { get; }

        protected RequestMessage (UpnpNatDevice device, string requestType)
        {
            Device = device;
            RequestType = requestType;
        }

        protected HttpWebRequest CreateRequest (string upnpMethod, string methodParameters, out byte[] body)
        {
            var location = Device.DeviceControlUri;

            HttpWebRequest req = (HttpWebRequest) WebRequest.Create (location);
            req.KeepAlive = false;
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add ("SOAPACTION", "\"" + Device.ServiceType + "#" + upnpMethod + "\"");

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

            body = Encoding.UTF8.GetBytes (bodyString);
            return req;
        }

        public HttpWebRequest Encode (out byte[] body)
        {
            var builder = new StringBuilder (256);
            var settings = new XmlWriterSettings {
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (var writer = XmlWriter.Create (builder, settings))
                Encode (writer);
            return CreateRequest (RequestType, builder.ToString (), out body);
        }

        public virtual void Encode (XmlWriter writer)
        {

        }

        protected static void WriteFullElement (XmlWriter writer, string element, IPAddress value)
            => WriteFullElement (writer, element, value.ToString ());

        protected static void WriteFullElement (XmlWriter writer, string element, Protocol value)
            => WriteFullElement (writer, element, value == Protocol.Tcp ? "TCP" : "UDP");

        protected static void WriteFullElement (XmlWriter writer, string element, int value)
            => WriteFullElement (writer, element, value.ToString ());

        protected static void WriteFullElement (XmlWriter writer, string element, string value)
        {
            writer.WriteStartElement (element);
            writer.WriteString (value);
            writer.WriteEndElement ();
        }
    }
}
