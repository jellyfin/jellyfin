using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="MediaReceiverRegistrarXmlBuilder" />.
    /// </summary>
    public static class MediaReceiverRegistrarXmlBuilder
    {
        /// <summary>
        /// Retrieves an XML description of this service.
        /// </summary>
        /// <returns>An XML representation of this service.</returns>
        public static string GetXml()
        {
            return ServiceXmlBuilder.GetXml(
                ServiceActionListBuilder.GetActions(),
                GetStateVariables());
        }

        /// <summary>
        /// The a list of all the state variables for this invocation.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
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
