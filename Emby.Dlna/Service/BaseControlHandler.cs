using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using Emby.Dlna.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Emby.Dlna.Didl;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Xml;
using MediaBrowser.Model.Extensions;

namespace Emby.Dlna.Service
{
    public abstract class BaseControlHandler
    {
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";

        protected readonly IServerConfigurationManager Config;
        protected readonly ILogger _logger;
        protected readonly IXmlReaderSettingsFactory XmlReaderSettingsFactory;

        protected BaseControlHandler(IServerConfigurationManager config, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
        {
            Config = config;
            _logger = logger;
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
                _logger.LogError(ex, "Error processing control request");

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

            _logger.LogDebug("Received control request {0}", requestInfo.LocalName);

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

            //logger.LogDebug(xml);

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        private ControlRequestInfo ParseRequest(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.LocalName)
                    {
                        case "Body":
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        return ParseBodyTag(subReader);
                                    }
                                }
                                else
                                {
                                    reader.Read();
                                }
                                break;
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
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    result.LocalName = reader.LocalName;
                    result.NamespaceURI = reader.NamespaceURI;

                    if (!reader.IsEmptyElement)
                    {
                        using (var subReader = reader.ReadSubtree())
                        {
                            ParseFirstBodyChild(subReader, result.Headers);
                            return result;
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return result;
        }

        private void ParseFirstBodyChild(XmlReader reader, IDictionary<string,string> headers)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // TODO: Should we be doing this here, or should it be handled earlier when decoding the request?
                    headers[reader.LocalName.RemoveDiacritics()] = reader.ReadElementContentAsString();
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private class ControlRequestInfo
        {
            public string LocalName;
            public string NamespaceURI;
            public IDictionary<string, string> Headers = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        }

        protected abstract IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, IDictionary<string, string> methodParams);

        private void LogRequest(ControlRequest request)
        {
            if (!Config.GetDlnaConfiguration().EnableDebugLog)
            {
                return;
            }

            var originalHeaders = request.Headers;
            var headers = string.Join(", ", originalHeaders.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());

            _logger.LogDebug("Control request. Headers: {0}", headers);
        }

        private void LogResponse(ControlResponse response)
        {
            if (!Config.GetDlnaConfiguration().EnableDebugLog)
            {
                return;
            }

            var originalHeaders = response.Headers;
            var headers = string.Join(", ", originalHeaders.Select(i => string.Format("{0}={1}", i.Key, i.Value)).ToArray());
            //builder.Append(response.Xml);

            _logger.LogDebug("Control response. Headers: {0}", headers);
        }
    }
}
