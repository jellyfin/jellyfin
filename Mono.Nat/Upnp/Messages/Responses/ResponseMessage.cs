namespace Mono.Nat.Upnp
{
    using System.Globalization;
    using System.Xml;

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
        /// <returns>The <see cref="ResponseMessage"/>.</returns>
        public static ResponseMessage Decode(UpnpNatDevice device, string message)
        {
            XmlNode node;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(message);

            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);

            // Error messages should be found under this namespace
            nsm.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
            nsm.AddNamespace("responseNs", device.ServiceType);

            // Check to see if we have a fault code message.
            if ((node = doc.SelectSingleNode("//errorNs:UPnPError", nsm)) != null)
            {
                string errorCode = node["errorCode"] != null ? node["errorCode"].InnerText : string.Empty;
                string errorDescription = node["errorDescription"] != null ? node["errorDescription"].InnerText : string.Empty;

                throw new MappingException((ErrorCode)int.Parse(errorCode, CultureInfo.InvariantCulture), errorDescription);
            }

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

            NatUtility.LogDebug("Unknown message returned. Please send me back the following XML:");
            NatUtility.LogDebug(message);
            return null;
        }
    }
}
