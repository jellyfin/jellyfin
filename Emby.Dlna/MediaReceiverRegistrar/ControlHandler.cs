using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Emby.Dlna.Server;
using Emby.Dlna.Service;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class ControlHandler : BaseControlHandler
    {
        public ControlHandler(IServerConfigurationManager config, ILogger logger) : base(config, logger)
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams)
        {
            if (string.Equals(methodName, "IsAuthorized", StringComparison.OrdinalIgnoreCase))
                return HandleIsAuthorized();
            if (string.Equals(methodName, "IsValidated", StringComparison.OrdinalIgnoreCase))
                return HandleIsValidated();

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private IEnumerable<KeyValuePair<string, string>> HandleIsAuthorized()
        {
            return new Headers(true)
            {
                { "Result", "1" }
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleIsValidated()
        {
            return new Headers(true)
            {
                { "Result", "1" }
            };
        }
    }
}
