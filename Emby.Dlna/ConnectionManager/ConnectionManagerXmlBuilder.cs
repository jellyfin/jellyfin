#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.ConnectionManager
{
    /// <summary>
    /// Defines the <see cref="ConnectionManagerXmlBuilder" />.
    /// </summary>
    public static class ConnectionManagerXmlBuilder
    {
        /// <summary>
        /// Gets the ConnectionManager:1 service template.
        /// See http://upnp.org/specs/av/UPnP-av-ConnectionManager-v1-Service.pdf.
        /// </summary>
        /// <returns>An XML description of this service.</returns>
        public static string GetXml()
        {
            return new ServiceXmlBuilder().GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
        }

        /// <summary>
        /// Get the list of state variables for this invocation.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>
            {
                new StateVariable
                {
                    Name = "SourceProtocolInfo",
                    DataType = "string",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "SinkProtocolInfo",
                    DataType = "string",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "CurrentConnectionIDs",
                    DataType = "string",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_ConnectionStatus",
                    DataType = "string",
                    SendsEvents = false,

                    AllowedValues = new[]
                {
                    "OK",
                    "ContentFormatMismatch",
                    "InsufficientBandwidth",
                    "UnreliableChannel",
                    "Unknown"
                }
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_ConnectionManager",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Direction",
                    DataType = "string",
                    SendsEvents = false,

                    AllowedValues = new[]
                {
                    "Output",
                    "Input"
                }
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_ProtocolInfo",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_ConnectionID",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_AVTransportID",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_RcsID",
                    DataType = "ui4",
                    SendsEvents = false
                }
            };

            return list;
        }
    }
}
