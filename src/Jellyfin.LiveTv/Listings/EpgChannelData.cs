#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.LiveTv;

namespace Jellyfin.LiveTv.Listings
{
    internal class EpgChannelData
    {
        private readonly Dictionary<string, ChannelInfo> _channelsById;

        private readonly Dictionary<string, ChannelInfo> _channelsByNumber;

        private readonly Dictionary<string, ChannelInfo> _channelsByName;

        public EpgChannelData(IEnumerable<ChannelInfo> channels)
        {
            _channelsById = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);
            _channelsByNumber = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);
            _channelsByName = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var channel in channels)
            {
                _channelsById[channel.Id] = channel;

                if (!string.IsNullOrEmpty(channel.Number))
                {
                    _channelsByNumber[channel.Number] = channel;
                }

                var normalizedName = NormalizeName(channel.Name ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    _channelsByName[normalizedName] = channel;
                }
            }
        }

        public ChannelInfo? GetChannelById(string id)
            => _channelsById.GetValueOrDefault(id);

        public ChannelInfo? GetChannelByNumber(string number)
            => _channelsByNumber.GetValueOrDefault(number);

        public ChannelInfo? GetChannelByName(string name)
            => _channelsByName.GetValueOrDefault(name);

        public static string NormalizeName(string value)
        {
            return value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        }
    }
}
