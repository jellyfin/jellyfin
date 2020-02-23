#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.ConnectionManager
{
    public class ConnectionManagerXmlBuilder
    {
        public string GetXml()
        {
            return new ServiceXmlBuilder().GetXml(new ServiceActionListBuilder().GetActions(), GetStateVariables());
        }

        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>();

            list.Add(new StateVariable
            {
                Name = "SourceProtocolInfo",
                DataType = "string",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "SinkProtocolInfo",
                DataType = "string",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "CurrentConnectionIDs",
                DataType = "string",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_ConnectionStatus",
                DataType = "string",
                SendsEvents = false,

                AllowedValues = new string[]
                {
                    "OK",
                    "ContentFormatMismatch",
                    "InsufficientBandwidth",
                    "UnreliableChannel",
                    "Unknown"
                }
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_ConnectionManager",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Direction",
                DataType = "string",
                SendsEvents = false,

                AllowedValues = new string[]
                {
                    "Output",
                    "Input"
                }
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_ProtocolInfo",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_ConnectionID",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_AVTransportID",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_RcsID",
                DataType = "ui4",
                SendsEvents = false
            });

            return list;
        }
    }
}
