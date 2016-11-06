using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Emby.Dlna.Server;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Emby.Dlna.Didl;
using MediaBrowser.Model.Xml;

namespace Emby.Dlna.Service
{
    public abstract class BaseControlHandler
    {
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        
        protected readonly IServerConfigurationManager Config;
        protected readonly ILogger Logger;
        protected readonly IXmlReaderSettingsFactory XmlReaderSettingsFactory;

        protected BaseControlHandler(IServerConfigurationManager config, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
        {
            Config = config;
            Logger = logger;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
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
            ControlRequestInfo requestInfo = null;

            using (var streamReader = new StreamReader(request.InputXml))
            {
                var readerSettings = XmlReaderSettingsFactory.Create(false);

                readerSettings.CheckCharacters = false;
                readerSettings.IgnoreProcessingInstructions = true;
                readerSettings.IgnoreComments = true;

                using (var reader = XmlReader.Create(streamReader, readerSettings))
                {
                    requestInfo = ParseRequest(reader);
                }
            }

            Logger.Debug("Received control request {0}", requestInfo.LocalName);

            var result = GetResult(requestInfo.LocalName, requestInfo.Headers);

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("SOAP-ENV", "Envelope", NS_SOAPENV);
                writer.WriteAttributeString(string.Empty, "encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

                writer.WriteStartElement("SOAP-ENV", "Body", NS_SOAPENV);
                writer.WriteStartElement("u", requestInfo.LocalName + "Response", requestInfo.NamespaceURI);
                foreach (var i in result)
                {
                    writer.WriteStartElement(i.Key);
                    writer.WriteString(i.Value);
                    writer.WriteFullEndElement();
                }
                writer.WriteFullEndElement();
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteEndDocument();
            }

            var xml = builder.ToString().Replace("xmlns:m=", "xmlns:u=");
            
            var controlResponse = new ControlResponse
            {
                Xml = xml,
                IsSuccessful = true
            };

            //Logger.Debug(xml);

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        private ControlRequestInfo ParseRequest(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.LocalName)
                    {
                        case "Body":
                        {
                            using (var subReader = reader.ReadSubtree())
                            {
                                return ParseBodyTag(subReader);
                            }
                        }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return new ControlRequestInfo();
        }

        private ControlRequestInfo ParseBodyTag(XmlReader reader)
        {
            var result = new ControlRequestInfo();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    result.LocalName = reader.LocalName;
                    result.NamespaceURI = reader.NamespaceURI;

                    using (var subReader = reader.ReadSubtree())
                    {
                        result.Headers = ParseFirstBodyChild(subReader);

                        return result;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return result;
        }

        private Headers ParseFirstBodyChild(XmlReader reader)
        {
            var result = new Headers();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    result.Add(reader.LocalName, reader.ReadElementContentAsString());
                }
                else
                {
                    reader.Read();
                }
            }

            return result;
        }

        private class ControlRequestInfo
        {
            public string LocalName;
            public string NamespaceURI;
            public Headers Headers = new Headers();
        }

        protected abstract IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams);

        private void LogRequest(ControlRequest request)
        {
            var builder = new StringBuilder();

            var headers = string.Join(", ", request.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());
            builder.AppendFormat("Headers: {0}", headers);
            builder.AppendLine();
            //builder.Append(request.InputXml);

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
