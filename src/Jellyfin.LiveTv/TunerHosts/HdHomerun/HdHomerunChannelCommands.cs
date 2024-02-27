#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunChannelCommands : IHdHomerunChannelCommands
    {
        private string? _channel;
        private string? _profile;

        public HdHomerunChannelCommands(string? channel, string? profile)
        {
            _channel = channel;
            _profile = profile;
        }

        public IEnumerable<(string CommandName, string CommandValue)> GetCommands()
        {
            if (!string.IsNullOrEmpty(_channel))
            {
                if (!string.IsNullOrEmpty(_profile)
                    && !string.Equals(_profile, "native", StringComparison.OrdinalIgnoreCase))
                {
                    yield return ("vchannel", $"{_channel} transcode={_profile}");
                }
                else
                {
                    yield return ("vchannel", _channel);
                }
            }
        }
    }
}
