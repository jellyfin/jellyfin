#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public abstract class BaseTunerHost
    {
        protected readonly IServerConfigurationManager Config;
        protected readonly ILogger Logger;
        protected IJsonSerializer JsonSerializer;
        protected readonly IFileSystem FileSystem;

        private readonly ConcurrentDictionary<string, ChannelCache> _channelCache =
            new ConcurrentDictionary<string, ChannelCache>(StringComparer.OrdinalIgnoreCase);

        protected BaseTunerHost(IServerConfigurationManager config, ILogger logger, IJsonSerializer jsonSerializer, IFileSystem fileSystem)
        {
            Config = config;
            Logger = logger;
            JsonSerializer = jsonSerializer;
            FileSystem = fileSystem;
        }

        public virtual bool IsSupported => true;

        protected abstract Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken);
        public abstract string Type { get; }

        public async Task<List<ChannelInfo>> GetChannels(TunerHostInfo tuner, bool enableCache, CancellationToken cancellationToken)
        {
            ChannelCache cache = null;
            var key = tuner.Id;

            if (enableCache && !string.IsNullOrEmpty(key) && _channelCache.TryGetValue(key, out cache))
            {
                return cache.Channels.ToList();
            }

            var result = await GetChannelsInternal(tuner, cancellationToken).ConfigureAwait(false);
            var list = result.ToList();
            //logger.LogInformation("Channels from {0}: {1}", tuner.Url, JsonSerializer.SerializeToString(list));

            if (!string.IsNullOrEmpty(key) && list.Count > 0)
            {
                cache = cache ?? new ChannelCache();
                cache.Channels = list;
                _channelCache.AddOrUpdate(key, cache, (k, v) => cache);
            }

            return list;
        }

        protected virtual List<TunerHostInfo> GetTunerHosts()
        {
            return GetConfiguration().TunerHosts
                .Where(i => string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<List<ChannelInfo>> GetChannels(bool enableCache, CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            var hosts = GetTunerHosts();

            foreach (var host in hosts)
            {
                var channelCacheFile = Path.Combine(Config.ApplicationPaths.CachePath, host.Id + "_channels");

                try
                {
                    var channels = await GetChannels(host, enableCache, cancellationToken).ConfigureAwait(false);
                    var newChannels = channels.Where(i => !list.Any(l => string.Equals(i.Id, l.Id, StringComparison.OrdinalIgnoreCase))).ToList();

                    list.AddRange(newChannels);

                    if (!enableCache)
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(channelCacheFile));
                            JsonSerializer.SerializeToFile(channels, channelCacheFile);
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting channel list");

                    if (enableCache)
                    {
                        try
                        {
                            var channels = JsonSerializer.DeserializeFromFile<List<ChannelInfo>>(channelCacheFile);
                            list.AddRange(channels);
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
            }

            return list;
        }

        protected abstract Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, ChannelInfo channel, CancellationToken cancellationToken);

        public async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (IsValidChannelId(channelId))
            {
                var hosts = GetTunerHosts();

                foreach (var host in hosts)
                {
                    try
                    {
                        var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);
                        var channelInfo = channels.FirstOrDefault(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

                        if (channelInfo != null)
                        {
                            return await GetChannelStreamMediaSources(host, channelInfo, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error getting channels");
                    }
                }
            }

            return new List<MediaSourceInfo>();
        }

        protected abstract Task<ILiveStream> GetChannelStream(TunerHostInfo tuner, ChannelInfo channel, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken);

        public async Task<ILiveStream> GetChannelStream(string channelId, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (!IsValidChannelId(channelId))
            {
                throw new FileNotFoundException();
            }

            var hosts = GetTunerHosts();

            var hostsWithChannel = new List<Tuple<TunerHostInfo, ChannelInfo>>();

            foreach (var host in hosts)
            {
                try
                {
                    var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);
                    var channelInfo = channels.FirstOrDefault(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

                    if (channelInfo != null)
                    {
                        hostsWithChannel.Add(new Tuple<TunerHostInfo, ChannelInfo>(host, channelInfo));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting channels");
                }
            }

            foreach (var hostTuple in hostsWithChannel)
            {
                var host = hostTuple.Item1;
                var channelInfo = hostTuple.Item2;

                try
                {
                    var liveStream = await GetChannelStream(host, channelInfo, streamId, currentLiveStreams, cancellationToken).ConfigureAwait(false);
                    var startTime = DateTime.UtcNow;
                    await liveStream.Open(cancellationToken).ConfigureAwait(false);
                    var endTime = DateTime.UtcNow;
                    Logger.LogInformation("Live stream opened after {0}ms", (endTime - startTime).TotalMilliseconds);
                    return liveStream;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error opening tuner");
                }
            }

            throw new LiveTvConflictException();
        }

        protected virtual string ChannelIdPrefix => Type + "_";

        protected virtual bool IsValidChannelId(string channelId)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            return channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase);
        }

        protected LiveTvOptions GetConfiguration()
        {
            return Config.GetConfiguration<LiveTvOptions>("livetv");
        }

        private class ChannelCache
        {
            public List<ChannelInfo> Channels;
        }
    }
}
