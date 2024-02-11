#pragma warning disable CS1591

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jellyfin.LiveTv.TunerHosts.HdHomerun
{
    public partial class LegacyHdHomerunChannelCommands : IHdHomerunChannelCommands
    {
        private string? _channel;
        private string? _program;

        public LegacyHdHomerunChannelCommands(string url)
        {
            // parse url for channel and program
            var match = ChannelAndProgramRegex().Match(url);
            if (match.Success)
            {
                _channel = match.Groups[1].Value;
                _program = match.Groups[2].Value;
            }
        }

        [GeneratedRegex(@"\/ch([0-9]+)-?([0-9]*)")]
        private static partial Regex ChannelAndProgramRegex();

        public IEnumerable<(string CommandName, string CommandValue)> GetCommands()
        {
            if (!string.IsNullOrEmpty(_channel))
            {
                yield return ("channel", _channel);
            }

            if (!string.IsNullOrEmpty(_program))
            {
                yield return ("program", _program);
            }
        }
    }
}
