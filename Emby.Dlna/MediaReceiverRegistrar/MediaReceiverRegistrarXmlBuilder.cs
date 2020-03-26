#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class MediaReceiverRegistrarXmlBuilder
    {
        public string GetXml()
        {
            return new ServiceXmlBuilder().GetXml(new ServiceActionListBuilder().GetActions(),
                GetStateVariables());
        }

        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>();

            list.Add(new StateVariable
            {
                Name = "AuthorizationGrantedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_DeviceID",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "AuthorizationDeniedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "ValidationSucceededUpdateID",
                DataType = "ui4",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_RegistrationRespMsg",
                DataType = "bin.base64",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_RegistrationReqMsg",
                DataType = "bin.base64",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "ValidationRevokedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Result",
                DataType = "int",
                SendsEvents = false
            });

            return list;
        }
    }
}
