#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Xml;
using Emby.Dlna.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class ControlHandler : BaseControlHandler
    {
        public ControlHandler(IServerConfigurationManager config, ILogger logger)
            : base(config, logger)
        {
        }

        /// <inheritdoc />
        protected override void WriteResult(string methodName, IDictionary<string, string> methodParams, XmlWriter xmlWriter)
        {
            if (string.Equals(methodName, "IsAuthorized", StringComparison.OrdinalIgnoreCase))
            {
                HandleIsAuthorized(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "IsValidated", StringComparison.OrdinalIgnoreCase))
            {
                HandleIsValidated(xmlWriter);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private static void HandleIsAuthorized(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");

        private static void HandleIsValidated(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");
    }
}
