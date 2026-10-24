#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.TunerHosts
{
    public abstract class BaseTunerHost
    {
        private readonly ConcurrentDictionary<string, List<ChannelInfo>> _cache;

        protected BaseTunerHost(IServerConfigurationManager config, ILogger<BaseTunerHost> logger, IFileSystem fileSystem)
        {
            Config = config;
            Logger = logger;
            FileSystem = fileSystem;
            _cache = new ConcurrentDictionary<string, List<ChannelInfo>>();
        }

        protected IServerConfigurationManager Config { get; }

        protected ILogger<BaseTunerHost> Logger { get; }

        protected IFileSystem FileSystem { get; }

        public virtual bool IsSupported => true;

        public abstract string Type { get; }

        protected virtual string ChannelIdPrefix => Type + "_";

        protected abstract Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken);

        public async Task<List<ChannelInfo>> GetChannels(TunerHostInfo tuner, bool enableCache, CancellationToken cancellationToken)
        {
            var key = tuner.Id;

            if (enableCache && !string.IsNullOrEmpty(key) && _cache.TryGetValue(key, out List<ChannelInfo> cache))
            {
                return cache;
            }

            var list = await GetChannelsInternal(tuner, cancellationToken).ConfigureAwait(false);
            // logger.LogInformation("Channels from {0}: {1}", tuner.Url, JsonSerializer.SerializeToString(list));

            if (!string.IsNullOrEmpty(key) && list.Count > 0)
            {
                _cache[key] = list;
            }

            return list;
        }

        protected virtual IList<TunerHostInfo> GetTunerHosts()
        {
            return Config.GetLiveTvConfiguration().TunerHosts
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
                            var writeStream = AsyncFile.OpenWrite(channelCacheFile);
                            await using (writeStream.ConfigureAwait(false))
                            {
                                await JsonSerializer.SerializeAsync(writeStream, channels, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
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
                            var readStream = AsyncFile.OpenRead(channelCacheFile);
                            await using (readStream.ConfigureAwait(false))
                            {
                                var channels = await JsonSerializer
                                    .DeserializeAsync<List<ChannelInfo>>(readStream, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);
                                list.AddRange(channels);
                            }
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
            ArgumentException.ThrowIfNullOrEmpty(channelId);

            if (IsValidChannelId(channelId))
            {
                var hosts = GetTunerHosts();

                foreach (var host in hosts)
                {
                    try
                    {
                        var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);
                        var channelInfo = channels.FirstOrDefault(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));

                        if (channelInfo is not null)
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

        protected abstract Task<ILiveStream> GetChannelStream(TunerHostInfo tunerHost, ChannelInfo channel, string streamId, IList<ILiveStream> currentLiveStreams, CancellationToken cancellationToken);

        public async Task<ILiveStream> GetChannelStream(string channelId, string streamId, IList<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(channelId);

            if (!IsValidChannelId(channelId))
            {
                throw new FileNotFoundException();
            }

            var hosts = GetTunerHosts();
            var orderedHosts = OrderHostsForStream(hosts, streamId);

            foreach (var host in orderedHosts)
            {
                ChannelInfo channelInfo;
                try
                {
                    var channels = await GetChannels(host, true, cancellationToken).ConfigureAwait(false);
                    channelInfo = channels.FirstOrDefault(i => string.Equals(i.Id, channelId, StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting channels");
                    continue;
                }

                if (channelInfo is null)
                {
                    continue;
                }

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

            throw new LiveTvConflictException("Unable to find host to play channel");
        }

        /// <summary>
        /// Returns whether <paramref name="streamId"/> was built for <paramref name="host"/>.
        /// The host that produced the media source is tried before the others.
        /// </summary>
        protected virtual bool IsStreamTarget(TunerHostInfo host, string streamId)
        {
            return false;
        }

        private List<TunerHostInfo> OrderHostsForStream(IList<TunerHostInfo> hosts, string streamId)
        {
            if (string.IsNullOrEmpty(streamId) || hosts.Count < 2)
            {
                return hosts as List<TunerHostInfo> ?? hosts.ToList();
            }

            var preferred = new List<TunerHostInfo>();
            var rest = new List<TunerHostInfo>();
            foreach (var host in hosts)
            {
                if (IsStreamTarget(host, streamId))
                {
                    preferred.Add(host);
                }
                else
                {
                    rest.Add(host);
                }
            }

            if (preferred.Count == 0)
            {
                return hosts as List<TunerHostInfo> ?? hosts.ToList();
            }

            if (!ReferenceEquals(preferred[0], hosts[0]))
            {
                Logger.LogInformation("Trying tuner {Url} first because the media source targets it", preferred[0].Url);
            }

            preferred.AddRange(rest);
            return preferred;
        }

        protected virtual bool IsValidChannelId(string channelId)
        {
            ArgumentException.ThrowIfNullOrEmpty(channelId);

            return channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
