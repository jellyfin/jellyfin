using System;
using System.Collections.Generic;
using System.Xml;
using Emby.Dlna.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
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

        /// <summary>
        /// Records that the handle is authorized in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleIsAuthorized(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");

        /// <summary>
        /// Records that the handle is validated in the xml stream.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleIsValidated(XmlWriter xmlWriter)
            => xmlWriter.WriteElementString("Result", "1");
    }
}
