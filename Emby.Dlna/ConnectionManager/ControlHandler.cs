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
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        private readonly DeviceProfile _profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/> for use with the <see cref="ControlHandler"/> instance.</param>
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

        /// <summary>
        /// Builds the response to the GetProtocolInfo request.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private void HandleGetProtocolInfo(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Source", _profile.ProtocolInfo);
            xmlWriter.WriteElementString("Sink", string.Empty);
        }
    }
}
