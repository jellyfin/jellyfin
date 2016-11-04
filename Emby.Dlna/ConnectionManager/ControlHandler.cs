using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using Emby.Dlna.Server;
using Emby.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Xml;

namespace Emby.Dlna.ConnectionManager
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly DeviceProfile _profile;

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

        public ControlHandler(IServerConfigurationManager config, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory, DeviceProfile profile) : base(config, logger, xmlReaderSettingsFactory)
        {
            _profile = profile;
        }
    }
}
