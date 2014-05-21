using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Globalization;

namespace MediaBrowser.Dlna.ConnectionManager
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly DeviceProfile _profile;

        public ControlHandler(ILogger logger, DeviceProfile profile, IServerConfigurationManager config)
            : base(config, logger)
        {
            _profile = profile;
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams)
        {
            var deviceId = "test";

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }
    }
}
