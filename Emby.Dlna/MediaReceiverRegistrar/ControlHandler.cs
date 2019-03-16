using System;
using System.Collections.Generic;
using Emby.Dlna.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.MediaReceiverRegistrar
{
    public class ControlHandler : BaseControlHandler
    {
        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, IDictionary<string, string> methodParams)
        {
            if (string.Equals(methodName, "IsAuthorized", StringComparison.OrdinalIgnoreCase))
                return HandleIsAuthorized();
            if (string.Equals(methodName, "IsValidated", StringComparison.OrdinalIgnoreCase))
                return HandleIsValidated();

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private static IEnumerable<KeyValuePair<string, string>> HandleIsAuthorized()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Result", "1" }
            };
        }

        private static IEnumerable<KeyValuePair<string, string>> HandleIsValidated()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Result", "1" }
            };
        }

        public ControlHandler(IServerConfigurationManager config, ILogger logger)
            : base(config, logger)
        {
        }
    }
}
