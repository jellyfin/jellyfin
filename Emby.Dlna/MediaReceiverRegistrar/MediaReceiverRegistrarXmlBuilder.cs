#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Service;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public static class MediaReceiverRegistrarXmlBuilder
    {
        public static string GetXml()
        {
            return ServiceXmlBuilder.GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
        }

        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>
            {
                new StateVariable
                {
                    Name = "AuthorizationGrantedUpdateID",
                    DataType = "ui4",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_DeviceID",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "AuthorizationDeniedUpdateID",
                    DataType = "ui4",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "ValidationSucceededUpdateID",
                    DataType = "ui4",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_RegistrationRespMsg",
                    DataType = "bin.base64",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_RegistrationReqMsg",
                    DataType = "bin.base64",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "ValidationRevokedUpdateID",
                    DataType = "ui4",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Result",
                    DataType = "int",
                    SendsEvents = false
                }
            };

            return list;
        }
    }
}
