#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Xml;
using Emby.Dlna.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.ConnectionManager
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly DeviceProfile _profile;

        public ControlHandler(IServerConfigurationManager config, ILogger logger, DeviceProfile profile)
            : base(config, logger)
        {
            _profile = profile;
        }

        /// <inheritdoc />
        protected override void WriteResult(string methodName, IDictionary<string, string> methodParams, XmlWriter xmlWriter)
        {
            if (string.Equals(methodName, "GetProtocolInfo", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetProtocolInfo(xmlWriter);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private void HandleGetProtocolInfo(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Source", _profile.ProtocolInfo);
            xmlWriter.WriteElementString("Sink", string.Empty);
        }
    }
}
