using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public abstract class BaseTunerHost
    {
        protected readonly IServerConfigurationManager Config;
        protected readonly ILogger Logger;
        protected IJsonSerializer JsonSerializer;
        protected readonly IMediaEncoder MediaEncoder;

        private readonly ConcurrentDictionary<string, ChannelCache> _channelCache =
            new ConcurrentDictionary<string, ChannelCache>(StringComparer.OrdinalIgnoreCase);

        protected BaseTunerHost(IServerConfigurationManager config, ILogger logger, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder)
        {
            Config = config;
            Logger = logger;
            JsonSerializer = jsonSerializer;
            MediaEncoder = mediaEncoder;
        }

        protected abstract Task<IEnumerable<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken);
        public abstract string Type { get; }

        public async Task<IEnumerable<ChannelInfo>> GetChannels(TunerHostInfo tuner, bool enableCache, CancellationToken cancellationToken)
        {
            ChannelCache cache = null;
            var key = tuner.Id;

            if (enableCache && !string.IsNullOrWhiteSpace(key) && _channelCache.TryGetValue(key, out cache))
            {
                if (DateTime.UtcNow - cache.Date < TimeSpan.FromMinutes(60))
                {
                    return cache.Channels.ToList();
                }
            }

            var result = await GetChannelsInternal(tuner, cancellationToken).ConfigureAwait(false);
            var list = result.ToList();
            Logger.Debug("Channels from {0}: {1}", tuner.Url, JsonSerializer.SerializeToString(list));

            if (!string.IsNullOrWhiteSpace(key) && list.Count > 0)
            {
                cache = cache ?? new ChannelCache();
                cache.Date = DateTime.UtcNow;
                cache.Channels = list;
                _channelCache.AddOrUpdate(key, cache, (k, v) => cache);
            }

            return list;
        }

        protected virtual List<TunerHostInfo> GetTunerHosts()
        {
            return GetConfiguration().TunerHosts
                .Where(i => i.IsEnabled && string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannels(bool enableCache, CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            var hosts = GetTunerHosts();

            foreach (var host in hosts)
            {
                try
                {
                    var channels = await GetChannels(host, enableCache, cancellationToken).ConfigureAwait(false);
                    var newChannels = channels.Where(i => !list.Any(l => string.Equals(i.Id, l.Id, StringComparison.OrdinalIgnoreCase))).ToList();

                    list.AddRange(newChannels);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error getting channel list", ex);
                }
            }

            return list;
        }

        protected abstract Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken);

        public async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            if (IsValidChannelId(channelId))
            {
                var hosts = GetTunerHosts();

                var hostsWithChannel = new List<TunerHostInfo>();

                foreach (var host in hosts)
                {
                    try
                    {
                        var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);

                        if (channels.Any(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase)))
                        {
                            hostsWithChannel.Add(host);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting channels", ex);
                    }
                }

                foreach (var host in hostsWithChannel)
                {
                    try
                    {
                        // Check to make sure the tuner is available
                        // If there's only one tuner, don't bother with the check and just let the tuner be the one to throw an error
                        if (hostsWithChannel.Count > 1 &&
                            !await IsAvailable(host, channelId, cancellationToken).ConfigureAwait(false))
                        {
                            Logger.Error("Tuner is not currently available");
                            continue;
                        }

                        var mediaSources = await GetChannelStreamMediaSources(host, channelId, cancellationToken).ConfigureAwait(false);

                        // Prefix the id with the host Id so that we can easily find it
                        foreach (var mediaSource in mediaSources)
                        {
                            mediaSource.Id = host.Id + mediaSource.Id;
                        }

                        return mediaSources;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error opening tuner", ex);
                    }
                }
            }

            return new List<MediaSourceInfo>();
        }

        protected abstract Task<LiveStream> GetChannelStream(TunerHostInfo tuner, string channelId, string streamId, CancellationToken cancellationToken);

        public async Task<LiveStream> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken)
        {
            if (!IsValidChannelId(channelId))
            {
                throw new FileNotFoundException();
            }

            var hosts = GetTunerHosts();

            var hostsWithChannel = new List<TunerHostInfo>();

            foreach (var host in hosts)
            {
                if (string.IsNullOrWhiteSpace(streamId))
                {
                    try
                    {
                        var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);

                        if (channels.Any(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase)))
                        {
                            hostsWithChannel.Add(host);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error getting channels", ex);
                    }
                }
                else if (streamId.StartsWith(host.Id, StringComparison.OrdinalIgnoreCase))
                {
                    hostsWithChannel = new List<TunerHostInfo> { host };
                    streamId = streamId.Substring(host.Id.Length);
                    break;
                }
            }

            foreach (var host in hostsWithChannel)
            {
                try
                {
                    var liveStream = await GetChannelStream(host, channelId, streamId, cancellationToken).ConfigureAwait(false);
                    await liveStream.Open(cancellationToken).ConfigureAwait(false);
                    return liveStream;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error opening tuner", ex);
                }
            }

            throw new LiveTvConflictException();
        }

        protected virtual bool EnableMediaProbing
        {
            get { return false; }
        }

        protected async Task<bool> IsAvailable(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken)
        {
            try
            {
                return await IsAvailableInternal(tuner, channelId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error checking tuner availability", ex);
                return false;
            }
        }

        protected abstract Task<bool> IsAvailableInternal(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken);

        protected abstract bool IsValidChannelId(string channelId);

        protected LiveTvOptions GetConfiguration()
        {
            return Config.GetConfiguration<LiveTvOptions>("livetv");
        }

        private class ChannelCache
        {
            public DateTime Date;
            public List<ChannelInfo> Channels;
        }
    }
}
