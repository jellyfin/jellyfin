namespace Mono.Nat.Upnp
{
    using System.Globalization;
    using System.Xml;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="ResponseMessage" />.
    /// </summary>
    internal class ResponseMessage
    {
        /// <summary>
        /// The Decode.
        /// </summary>
        /// <param name="device">The device<see cref="UpnpNatDevice"/>.</param>
        /// <param name="message">The message<see cref="string"/>.</param>
        /// <param name="logger">ILogger instance.</param>
        /// <returns>The <see cref="ResponseMessage"/>.</returns>
        public static ResponseMessage? Decode(UpnpNatDevice device, string message, ILogger logger)
        {
            XmlNode node;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(message);

            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);

            // Error messages should be found under this namespace
            nsm.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
            nsm.AddNamespace("responseNs", device.ServiceType); // HTTP 1.1
            nsm.AddNamespace("soapNs", "http://schemas.xmlsoap.org/soap/envelope/");

            // Check to see if we have a fault code message.
            if ((node = doc.SelectSingleNode("//errorNs:UPnPError", nsm)) != null)
            {
                string errorCode = node["errorCode"] != null ? node["errorCode"].InnerText : string.Empty;
                string errorDescription = node["errorDescription"] != null ? node["errorDescription"].InnerText : string.Empty;

                throw new MappingException((ErrorCode)int.Parse(errorCode, CultureInfo.InvariantCulture), errorDescription);
            }

            // SOAP response has two Namespaces with the 2nd being the command which was run originally.
            // Since we don't have that, we'll just grab the entire body response.
            node = doc.SelectSingleNode("//soapNs:Body/*", nsm);
            string? responseString = node?.LocalName;
            if (!string.IsNullOrEmpty(responseString))
            {
                if (responseString.Equals("AddPortMappingResponse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new CreatePortMappingResponseMessage();
                }

                if (responseString.Equals("DeletePortMappingResponse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new DeletePortMapResponseMessage();
                }

                if (responseString.Equals("GetExternalIPAddressResponse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new GetExternalIPAddressResponseMessage(node);
                }

                if (responseString.Equals("GetGenericPortMappingEntryResponse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new GetGenericPortMappingEntryResponseMessage(node);
                }

                if (responseString.Equals("GetSpecificPortMappingEntryResponse", System.StringComparison.OrdinalIgnoreCase))
                {
                    return new GetSpecificPortMappingEntryResponseMessage(node);
                }
            }

            // Original Mono.Net code.
            if (doc.SelectSingleNode("//responseNs:AddPortMappingResponse", nsm) != null)
            {
                return new CreatePortMappingResponseMessage();
            }

            if (doc.SelectSingleNode("//responseNs:DeletePortMappingResponse", nsm) != null)
            {
                return new DeletePortMapResponseMessage();
            }

            if ((node = doc.SelectSingleNode("//responseNs:GetExternalIPAddressResponse", nsm)) != null)
            {
                return new GetExternalIPAddressResponseMessage(node);
            }

            if ((node = doc.SelectSingleNode("//responseNs:GetGenericPortMappingEntryResponse", nsm)) != null)
            {
                return new GetGenericPortMappingEntryResponseMessage(node);
            }

            if ((node = doc.SelectSingleNode("//responseNs:GetSpecificPortMappingEntryResponse", nsm)) != null)
            {
                return new GetSpecificPortMappingEntryResponseMessage(node);
            }

            logger.LogDebug("Unknown message returned. Please send me back the following XML:");
            logger.LogDebug(message);
            return null;
        }
    }
}
