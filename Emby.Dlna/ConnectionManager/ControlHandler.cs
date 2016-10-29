using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Dlna.ConnectionManager
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly DeviceProfile _profile;

        public ControlHandler(ILogger logger, DeviceProfile profile, IServerConfigurationManager config)
            : base(config, logger)
        {
            _profile = profile;
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams)
        {
            if (string.Equals(methodName, "GetProtocolInfo", StringComparison.OrdinalIgnoreCase))
            {
                return HandleGetProtocolInfo();
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetProtocolInfo()
        {
            return new Headers(true)
            {
                { "Source", _profile.ProtocolInfo },
                { "Sink", "" }
            };
        }
    }
}
