#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.LiveTv;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{

    internal class EpgChannelData
    {
        public EpgChannelData(IEnumerable<ChannelInfo> channels)
        {
            ChannelsById = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);
            ChannelsByNumber = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);
            ChannelsByName = new Dictionary<string, ChannelInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var channel in channels)
            {
                ChannelsById[channel.Id] = channel;

                if (!string.IsNullOrEmpty(channel.Number))
                {
                    ChannelsByNumber[channel.Number] = channel;
                }

                var normalizedName = NormalizeName(channel.Name ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    ChannelsByName[normalizedName] = channel;
                }
            }
        }

        private Dictionary<string, ChannelInfo> ChannelsById { get; set; }

        private Dictionary<string, ChannelInfo> ChannelsByNumber { get; set; }

        private Dictionary<string, ChannelInfo> ChannelsByName { get; set; }

        public ChannelInfo GetChannelById(string id)
        {
            ChannelsById.TryGetValue(id, out var result);

            return result;
        }

        public ChannelInfo GetChannelByNumber(string number)
        {
            ChannelsByNumber.TryGetValue(number, out var result);

            return result;
        }

        public ChannelInfo GetChannelByName(string name)
        {
            ChannelsByName.TryGetValue(name, out var result);

            return result;
        }

        public static string NormalizeName(string value)
        {
            return value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        }
    }
}
