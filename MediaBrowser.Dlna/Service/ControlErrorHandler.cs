using MediaBrowser.Controller.Dlna;
using System;
using System.Xml;

namespace MediaBrowser.Dlna.Service
{
    public class ControlErrorHandler
    {
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        
        public ControlResponse GetResponse(Exception ex)
        {
            var env = new XmlDocument();
            env.AppendChild(env.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            var envelope = env.CreateElement("SOAP-ENV", "Envelope", NS_SOAPENV);
            env.AppendChild(envelope);
            envelope.SetAttribute("encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

            var rbody = env.CreateElement("SOAP-ENV:Body", NS_SOAPENV);
            env.DocumentElement.AppendChild(rbody);

            var fault = env.CreateElement("SOAP-ENV", "Fault", NS_SOAPENV);
            var faultCode = env.CreateElement("faultcode");
            faultCode.InnerText = "500";
            fault.AppendChild(faultCode);
            var faultString = env.CreateElement("faultstring");
            faultString.InnerText = ex.ToString();
            fault.AppendChild(faultString);
            var detail = env.CreateDocumentFragment();
            detail.InnerXml = "<detail><UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>401</errorCode><errorDescription>Invalid Action</errorDescription></UPnPError></detail>";
            fault.AppendChild(detail);
            rbody.AppendChild(fault);

            return new ControlResponse
            {
                Xml = env.OuterXml,
                IsSuccessful = false
            };
        }
    }
}
