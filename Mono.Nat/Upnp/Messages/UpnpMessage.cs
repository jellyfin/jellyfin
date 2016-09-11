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
using System.Diagnostics;
using System.Xml;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;

namespace Mono.Nat.Upnp
{
    internal abstract class MessageBase
    {
        internal static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        protected UpnpNatDevice device;

        protected MessageBase(UpnpNatDevice device)
        {
            this.device = device;
        }

        protected WebRequest CreateRequest(string upnpMethod, string methodParameters, out byte[] body)
        {
            string ss = "http://" + this.device.HostEndPoint.ToString() + this.device.ControlUrl;
            NatUtility.Log("Initiating request to: {0}", ss);
            Uri location = new Uri(ss);

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(location);
            req.KeepAlive = false;
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", "\"" + device.ServiceType + "#" + upnpMethod + "\"");

            string bodyString = "<s:Envelope "
               + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
               + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
               + "<s:Body>"
               + "<u:" + upnpMethod + " "
               + "xmlns:u=\"" + device.ServiceType + "\">"
               + methodParameters
               + "</u:" + upnpMethod + ">"
               + "</s:Body>"
               + "</s:Envelope>\r\n\r\n";

			body = System.Text.Encoding.UTF8.GetBytes(bodyString);
            return req;
        }

        public static MessageBase Decode(UpnpNatDevice device, string message)
        {
            XmlNode node;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(message);

            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);

            // Error messages should be found under this namespace
            nsm.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
            nsm.AddNamespace("responseNs", device.ServiceType);

            // Check to see if we have a fault code message.
			if ((node = doc.SelectSingleNode("//errorNs:UPnPError", nsm)) != null) {
				string errorCode = node["errorCode"] != null ? node["errorCode"].InnerText : "";
				string errorDescription = node["errorDescription"] != null ? node["errorDescription"].InnerText : "";

				return new ErrorMessage(Convert.ToInt32(errorCode, CultureInfo.InvariantCulture), errorDescription);
			}

	        if ((doc.SelectSingleNode("//responseNs:AddPortMappingResponse", nsm)) != null)
                return new CreatePortMappingResponseMessage();

            if ((doc.SelectSingleNode("//responseNs:DeletePortMappingResponse", nsm)) != null)
                return new DeletePortMapResponseMessage();

			if ((node = doc.SelectSingleNode("//responseNs:GetExternalIPAddressResponse", nsm)) != null) {
				string newExternalIPAddress = node["NewExternalIPAddress"] != null ? node["NewExternalIPAddress"].InnerText : "";
				return new GetExternalIPAddressResponseMessage(newExternalIPAddress);
			}

	        if ((node = doc.SelectSingleNode("//responseNs:GetGenericPortMappingEntryResponse", nsm)) != null)
                return new GetGenericPortMappingEntryResponseMessage(node, true);

            if ((node = doc.SelectSingleNode("//responseNs:GetSpecificPortMappingEntryResponse", nsm)) != null)
                return new GetGenericPortMappingEntryResponseMessage(node, false);

            NatUtility.Log("Unknown message returned. Please send me back the following XML:");
            NatUtility.Log(message);
            return null;
        }

        public abstract WebRequest Encode(out byte[] body);

        internal static void WriteFullElement(XmlWriter writer, string element, string value)
        {
            writer.WriteStartElement(element);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        internal static XmlWriter CreateWriter(StringBuilder sb)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            return XmlWriter.Create(sb, settings);
        }
    }
}
