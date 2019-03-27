using System;
using System.Collections.Generic;
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

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, IDictionary<string, string> methodParams)
        {
            if (string.Equals(methodName, "GetProtocolInfo", StringComparison.OrdinalIgnoreCase))
            {
                return HandleGetProtocolInfo();
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetProtocolInfo()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Source", _profile.ProtocolInfo },
                { "Sink", "" }
            };
        }

        public ControlHandler(IServerConfigurationManager config, ILogger logger, DeviceProfile profile)
            : base(config, logger)
        {
            _profile = profile;
        }
    }
}
