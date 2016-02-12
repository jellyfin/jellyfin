using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MediaBrowser.Dlna.Service
{
    public abstract class BaseControlHandler
    {
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        
        protected readonly IServerConfigurationManager Config;
        protected readonly ILogger Logger;

        protected BaseControlHandler(IServerConfigurationManager config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            try
            {
                var enableDebugLogging = Config.GetDlnaConfiguration().EnableDebugLog;

                if (enableDebugLogging)
                {
                    LogRequest(request);
                }

                var response = ProcessControlRequestInternal(request);

                if (enableDebugLogging)
                {
                    LogResponse(response);
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error processing control request", ex);

                return new ControlErrorHandler().GetResponse(ex);
            }
        }

        private ControlResponse ProcessControlRequestInternal(ControlRequest request)
        {
            var soap = new XmlDocument();
            soap.LoadXml(request.InputXml);
            var sparams = new Headers();
            var body = soap.GetElementsByTagName("Body", NS_SOAPENV).Item(0);

            var method = body.FirstChild;

            foreach (var p in method.ChildNodes)
            {
                var e = p as XmlElement;
                if (e == null)
                {
                    continue;
                }
                sparams.Add(e.LocalName, e.InnerText.Trim());
            }

            Logger.Debug("Received control request {0}", method.LocalName);

            var result = GetResult(method.LocalName, sparams);

            var env = new XmlDocument();
            env.AppendChild(env.CreateXmlDeclaration("1.0", "utf-8", string.Empty));
            var envelope = env.CreateElement("SOAP-ENV", "Envelope", NS_SOAPENV);
            env.AppendChild(envelope);
            envelope.SetAttribute("encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

            var rbody = env.CreateElement("SOAP-ENV:Body", NS_SOAPENV);
            env.DocumentElement.AppendChild(rbody);

            var response = env.CreateElement(String.Format("u:{0}Response", method.LocalName), method.NamespaceURI);
            rbody.AppendChild(response);

            foreach (var i in result)
            {
                var ri = env.CreateElement(i.Key);
                ri.InnerText = i.Value;
                response.AppendChild(ri);
            }

            var xml = env.OuterXml.Replace("xmlns:m=", "xmlns:u=");
            
            var controlResponse = new ControlResponse
            {
                Xml = xml,
                IsSuccessful = true
            };

            //Logger.Debug(xml);

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        protected abstract IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams);

        private void LogRequest(ControlRequest request)
        {
            var builder = new StringBuilder();

            var headers = string.Join(", ", request.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());
            builder.AppendFormat("Headers: {0}", headers);
            builder.AppendLine();
            builder.Append(request.InputXml);

            Logger.LogMultiline("Control request", LogSeverity.Debug, builder);
        }

        private void LogResponse(ControlResponse response)
        {
            var builder = new StringBuilder();

            var headers = string.Join(", ", response.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());
            builder.AppendFormat("Headers: {0}", headers);
            builder.AppendLine();
            builder.Append(response.Xml);

            Logger.LogMultiline("Control response", LogSeverity.Debug, builder);
        }
    }
}
