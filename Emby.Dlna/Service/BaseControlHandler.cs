#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Emby.Dlna.Didl;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Service
{
    public abstract class BaseControlHandler
    {
        private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

        protected BaseControlHandler(IServerConfigurationManager config, ILogger logger)
        {
            Config = config;
            Logger = logger;
        }

        protected IServerConfigurationManager Config { get; }

        protected ILogger Logger { get; }

        public async Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            try
            {
                LogRequest(request);

                var response = await ProcessControlRequestInternalAsync(request).ConfigureAwait(false);
                LogResponse(response);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing control request");

                return ControlErrorHandler.GetResponse(ex);
            }
        }

        private async Task<ControlResponse> ProcessControlRequestInternalAsync(ControlRequest request)
        {
            ControlRequestInfo requestInfo;

            using (var streamReader = new StreamReader(request.InputXml, Encoding.UTF8))
            {
                var readerSettings = new XmlReaderSettings()
                {
                    ValidationType = ValidationType.None,
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    Async = true
                };

                using var reader = XmlReader.Create(streamReader, readerSettings);
                requestInfo = await ParseRequestAsync(reader).ConfigureAwait(false);
            }

            Logger.LogDebug("Received control request {LocalName}, params: {@Headers}", requestInfo.LocalName, requestInfo.Headers);

            return CreateControlResponse(requestInfo);
        }

        private ControlResponse CreateControlResponse(ControlRequestInfo requestInfo)
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

                writer.WriteStartElement("SOAP-ENV", "Envelope", NsSoapEnv);
                writer.WriteAttributeString(string.Empty, "encodingStyle", NsSoapEnv, "http://schemas.xmlsoap.org/soap/encoding/");

                writer.WriteStartElement("SOAP-ENV", "Body", NsSoapEnv);
                writer.WriteStartElement("u", requestInfo.LocalName + "Response", requestInfo.NamespaceURI);

                WriteResult(requestInfo.LocalName, requestInfo.Headers, writer);

                writer.WriteFullEndElement();
                writer.WriteFullEndElement();

                writer.WriteFullEndElement();
                writer.WriteEndDocument();
            }

            var xml = builder.ToString().Replace("xmlns:m=", "xmlns:u=", StringComparison.Ordinal);

            var controlResponse = new ControlResponse(xml, true);

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        private async Task<ControlRequestInfo> ParseRequestAsync(XmlReader reader)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (string.Equals(reader.LocalName, "Body", StringComparison.Ordinal))
                    {
                        if (reader.IsEmptyElement)
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                            continue;
                        }

                        using var subReader = reader.ReadSubtree();
                        return await ParseBodyTagAsync(subReader).ConfigureAwait(false);
                    }

                    await reader.SkipAsync().ConfigureAwait(false);
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            throw new EndOfStreamException("Stream ended but no body tag found.");
        }

        private async Task<ControlRequestInfo> ParseBodyTagAsync(XmlReader reader)
        {
            string? namespaceURI = null, localName = null;

            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    localName = reader.LocalName;
                    namespaceURI = reader.NamespaceURI;

                    if (reader.IsEmptyElement)
                    {
                        await reader.ReadAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        var result = new ControlRequestInfo(localName, namespaceURI);
                        using var subReader = reader.ReadSubtree();
                        await ParseFirstBodyChildAsync(subReader, result.Headers).ConfigureAwait(false);
                        return result;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            if (localName != null && namespaceURI != null)
            {
                return new ControlRequestInfo(localName, namespaceURI);
            }

            throw new EndOfStreamException("Stream ended but no control found.");
        }

        private async Task ParseFirstBodyChildAsync(XmlReader reader, IDictionary<string, string> headers)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // TODO: Should we be doing this here, or should it be handled earlier when decoding the request?
                    headers[reader.LocalName.RemoveDiacritics()] = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        protected abstract void WriteResult(string methodName, IReadOnlyDictionary<string, string> methodParams, XmlWriter xmlWriter);

        private void LogRequest(ControlRequest request)
        {
            if (!Config.GetDlnaConfiguration().EnableDebugLog)
            {
                return;
            }

            Logger.LogDebug("Control request. Headers: {@Headers}", request.Headers);
        }

        private void LogResponse(ControlResponse response)
        {
            if (!Config.GetDlnaConfiguration().EnableDebugLog)
            {
                return;
            }

            Logger.LogDebug("Control response. Headers: {@Headers}\n{Xml}", response.Headers, response.Xml);
        }

        private class ControlRequestInfo
        {
            public ControlRequestInfo(string localName, string namespaceUri)
            {
                LocalName = localName;
                NamespaceURI = namespaceUri;
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public string LocalName { get; set; }

            public string NamespaceURI { get; set; }

            public Dictionary<string, string> Headers { get; }
        }
    }
}
