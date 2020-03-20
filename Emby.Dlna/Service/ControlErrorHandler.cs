#pragma warning disable CS1591

using System;
using System.IO;
using System.Text;
using System.Xml;
using Emby.Dlna.Didl;

namespace Emby.Dlna.Service
{
    public static class ControlErrorHandler
    {
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";

        public static ControlResponse GetResponse(Exception ex)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8);

            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("SOAP-ENV", "Envelope", NS_SOAPENV);
                writer.WriteAttributeString(string.Empty, "encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

                writer.WriteStartElement("SOAP-ENV", "Body", NS_SOAPENV);
                writer.WriteStartElement("SOAP-ENV", "Fault", NS_SOAPENV);

                writer.WriteElementString("faultcode", "500");
                writer.WriteElementString("faultstring", ex.Message);

                writer.WriteStartElement("detail");
                writer.WriteRaw("<UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>401</errorCode><errorDescription>Invalid Action</errorDescription></UPnPError>");
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteEndDocument();
            }

            return new ControlResponse
            {
                Xml = builder.ToString(),
                IsSuccessful = false
            };
        }
    }
}
