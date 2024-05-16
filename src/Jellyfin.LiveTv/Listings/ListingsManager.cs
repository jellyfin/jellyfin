using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Guide;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings;

/// <inheritdoc />
public class ListingsManager : IListingsManager
{
    private readonly ILogger<ListingsManager> _logger;
    private readonly IConfigurationManager _config;
    private readonly ITaskManager _taskManager;
    private readonly ITunerHostManager _tunerHostManager;
    private readonly IListingsProvider[] _listingsProviders;

    private readonly ConcurrentDictionary<string, EpgChannelData> _epgChannels = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ListingsManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
    /// <param name="taskManager">The <see cref="ITaskManager"/>.</param>
    /// <param name="tunerHostManager">The <see cref="ITunerHostManager"/>.</param>
    /// <param name="listingsProviders">The <see cref="IListingsProvider"/>.</param>
    public ListingsManager(
        ILogger<ListingsManager> logger,
        IConfigurationManager config,
        ITaskManager taskManager,
        ITunerHostManager tunerHostManager,
        IEnumerable<IListingsProvider> listingsProviders)
    {
        _logger = logger;
        _config = config;
        _taskManager = taskManager;
        _tunerHostManager = tunerHostManager;
        _listingsProviders = listingsProviders.ToArray();
    }

    /// <inheritdoc />
    public async Task<ListingsProviderInfo> SaveListingProvider(ListingsProviderInfo info, bool validateLogin, bool validateListings)
    {
        ArgumentNullException.ThrowIfNull(info);

        var provider = GetProvider(info.Type);
        await provider.Validate(info, validateLogin, validateListings).ConfigureAwait(false);

        var config = _config.GetLiveTvConfiguration();

        var list = config.ListingProviders;
        int index = Array.FindIndex(list, i => string.Equals(i.Id, info.Id, StringComparison.OrdinalIgnoreCase));

        if (index == -1 || string.IsNullOrWhiteSpace(info.Id))
        {
            info.Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            config.ListingProviders = [..list, info];
        }
        else
        {
            config.ListingProviders[index] = info;
        }

        _config.SaveConfiguration("livetv", config);
        _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();

        return info;
    }

    /// <inheritdoc />
    public void DeleteListingsProvider(string? id)
    {
        var config = _config.GetLiveTvConfiguration();

        config.ListingProviders = config.ListingProviders.Where(i => !string.Equals(id, i.Id, StringComparison.OrdinalIgnoreCase)).ToArray();

        _config.SaveConfiguration("livetv", config);
        _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();
    }

    /// <inheritdoc />
    public Task<List<NameIdPair>> GetLineups(string? providerType, string? providerId, string? country, string? location)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return GetProvider(providerType).GetLineups(null, country, location);
        }

        var info = _config.GetLiveTvConfiguration().ListingProviders
            .FirstOrDefault(i => string.Equals(i.Id, providerId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ResourceNotFoundException();

        return GetProvider(info.Type).GetLineups(info, country, location);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(
        ChannelInfo channel,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);

        foreach (var (provider, providerInfo) in GetListingProviders())
        {
            if (!IsListingProviderEnabledForTuner(providerInfo, channel.TunerHostId))
            {
                _logger.LogDebug(
                    "Skipping getting programs for channel {0}-{1} from {2}-{3}, because it's not enabled for this tuner.",
                    channel.Number,
                    channel.Name,
                    provider.Name,
                    providerInfo.ListingsId ?? string.Empty);
                continue;
            }

            _logger.LogDebug(
                "Getting programs for channel {0}-{1} from {2}-{3}",
                channel.Number,
                channel.Name,
                provider.Name,
                providerInfo.ListingsId ?? string.Empty);

            var epgChannels = await GetEpgChannels(provider, providerInfo, true, cancellationToken).ConfigureAwait(false);

            var epgChannel = GetEpgChannelFromTunerChannel(providerInfo.ChannelMappings, channel, epgChannels);
            if (epgChannel is null)
            {
                _logger.LogDebug("EPG channel not found for tuner channel {0}-{1} from {2}-{3}", channel.Number, channel.Name, provider.Name, providerInfo.ListingsId ?? string.Empty);
                continue;
            }

            var programs = (await provider
                .GetProgramsAsync(providerInfo, epgChannel.Id, startDateUtc, endDateUtc, cancellationToken).ConfigureAwait(false))
                .ToList();

            // Replace the value that came from the provider with a normalized value
            foreach (var program in programs)
            {
                program.ChannelId = channel.Id;
                program.Id += "_" + channel.Id;
            }

            if (programs.Count > 0)
            {
                return programs;
            }
        }

        return Enumerable.Empty<ProgramInfo>();
    }

    /// <inheritdoc />
    public async Task AddProviderMetadata(IList<ChannelInfo> channels, bool enableCache, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channels);

        foreach (var (provider, providerInfo) in GetListingProviders())
        {
            var enabledChannels = channels
                .Where(i => IsListingProviderEnabledForTuner(providerInfo, i.TunerHostId))
                .ToList();

            if (enabledChannels.Count == 0)
            {
                continue;
            }

            try
            {
                await AddMetadata(provider, providerInfo, enabledChannels, enableCache, cancellationToken).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding metadata");
            }
        }
    }

    /// <inheritdoc />
    public async Task<ChannelMappingOptionsDto> GetChannelMappingOptions(string? providerId)
    {
        var listingsProviderInfo = _config.GetLiveTvConfiguration().ListingProviders
            .First(info => string.Equals(providerId, info.Id, StringComparison.OrdinalIgnoreCase));

        var provider = GetProvider(listingsProviderInfo.Type);

        var tunerChannels = await GetChannelsForListingsProvider(listingsProviderInfo, CancellationToken.None)
            .ConfigureAwait(false);

        var providerChannels = await provider.GetChannels(listingsProviderInfo, default)
            .ConfigureAwait(false);

        var mappings = listingsProviderInfo.ChannelMappings;

        return new ChannelMappingOptionsDto
        {
            TunerChannels = tunerChannels.Select(i => GetTunerChannelMapping(i, mappings, providerChannels)).ToList(),
            ProviderChannels = providerChannels.Select(i => new NameIdPair
            {
                Name = i.Name,
                Id = i.Id
            }).ToList(),
            Mappings = mappings,
            ProviderName = provider.Name
        };
    }

    /// <inheritdoc />
    public async Task<TunerChannelMapping> SetChannelMapping(string providerId, string tunerChannelNumber, string providerChannelNumber)
    {
        var config = _config.GetLiveTvConfiguration();

        var listingsProviderInfo = config.ListingProviders
            .First(info => string.Equals(providerId, info.Id, StringComparison.OrdinalIgnoreCase));

        listingsProviderInfo.ChannelMappings = listingsProviderInfo.ChannelMappings
            .Where(pair => !string.Equals(pair.Name, tunerChannelNumber, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (!string.Equals(tunerChannelNumber, providerChannelNumber, StringComparison.OrdinalIgnoreCase))
        {
            var newItem = new NameValuePair
            {
                Name = tunerChannelNumber,
                Value = providerChannelNumber
            };
            listingsProviderInfo.ChannelMappings = [..listingsProviderInfo.ChannelMappings, newItem];
        }

        _config.SaveConfiguration("livetv", config);

        var tunerChannels = await GetChannelsForListingsProvider(listingsProviderInfo, CancellationToken.None)
            .ConfigureAwait(false);

        var providerChannels = await GetProvider(listingsProviderInfo.Type).GetChannels(listingsProviderInfo, default)
            .ConfigureAwait(false);

        var tunerChannelMappings = tunerChannels
            .Select(i => GetTunerChannelMapping(i, listingsProviderInfo.ChannelMappings, providerChannels)).ToList();

        _taskManager.CancelIfRunningAndQueue<RefreshGuideScheduledTask>();

        return tunerChannelMappings.First(i => string.Equals(i.Id, tunerChannelNumber, StringComparison.OrdinalIgnoreCase));
    }

    private List<(IListingsProvider Provider, ListingsProviderInfo ProviderInfo)> GetListingProviders()
        => _config.GetLiveTvConfiguration().ListingProviders
            .Select(info => (
                Provider: _listingsProviders.FirstOrDefault(l
                    => string.Equals(l.Type, info.Type, StringComparison.OrdinalIgnoreCase)),
                ProviderInfo: info))
            .Where(i => i.Provider is not null)
            .ToList()!; // Already filtered out null

    private async Task AddMetadata(
        IListingsProvider provider,
        ListingsProviderInfo info,
        IEnumerable<ChannelInfo> tunerChannels,
        bool enableCache,
        CancellationToken cancellationToken)
    {
        var epgChannels = await GetEpgChannels(provider, info, enableCache, cancellationToken).ConfigureAwait(false);

        foreach (var tunerChannel in tunerChannels)
        {
            var epgChannel = GetEpgChannelFromTunerChannel(info.ChannelMappings, tunerChannel, epgChannels);
            if (epgChannel is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(epgChannel.ImageUrl))
            {
                tunerChannel.ImageUrl = epgChannel.ImageUrl;
            }
        }
    }

    private static bool IsListingProviderEnabledForTuner(ListingsProviderInfo info, string tunerHostId)
    {
        if (info.EnableAllTuners)
        {
            return true;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tunerHostId);

        return info.EnabledTuners.Contains(tunerHostId, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetMappedChannel(string channelId, NameValuePair[] mappings)
    {
        foreach (NameValuePair mapping in mappings)
        {
            if (string.Equals(mapping.Name, channelId, StringComparison.OrdinalIgnoreCase))
            {
                return mapping.Value;
            }
        }

        return channelId;
    }

    private async Task<EpgChannelData> GetEpgChannels(
        IListingsProvider provider,
        ListingsProviderInfo info,
        bool enableCache,
        CancellationToken cancellationToken)
    {
        if (enableCache && _epgChannels.TryGetValue(info.Id, out var result))
        {
            return result;
        }

        var channels = await provider.GetChannels(info, cancellationToken).ConfigureAwait(false);
        foreach (var channel in channels)
        {
            _logger.LogInformation("Found epg channel in {0} {1} {2} {3}", provider.Name, info.ListingsId, channel.Name, channel.Id);
        }

        result = new EpgChannelData(channels);
        _epgChannels.AddOrUpdate(info.Id, result, (_, _) => result);

        return result;
    }

    private static ChannelInfo? GetEpgChannelFromTunerChannel(
        NameValuePair[] mappings,
        ChannelInfo tunerChannel,
        EpgChannelData epgChannelData)
    {
        if (!string.IsNullOrWhiteSpace(tunerChannel.Id))
        {
            var mappedTunerChannelId = GetMappedChannel(tunerChannel.Id, mappings);
            if (string.IsNullOrWhiteSpace(mappedTunerChannelId))
            {
                mappedTunerChannelId = tunerChannel.Id;
            }

            var channel = epgChannelData.GetChannelById(mappedTunerChannelId);
            if (channel is not null)
            {
                return channel;
            }
        }

        if (!string.IsNullOrWhiteSpace(tunerChannel.TunerChannelId))
        {
            var tunerChannelId = tunerChannel.TunerChannelId;
            if (tunerChannelId.Contains(".json.schedulesdirect.org", StringComparison.OrdinalIgnoreCase))
            {
                tunerChannelId = tunerChannelId.Replace(".json.schedulesdirect.org", string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart('I');
            }

            var mappedTunerChannelId = GetMappedChannel(tunerChannelId, mappings);
            if (string.IsNullOrWhiteSpace(mappedTunerChannelId))
            {
                mappedTunerChannelId = tunerChannelId;
            }

            var channel = epgChannelData.GetChannelById(mappedTunerChannelId);
            if (channel is not null)
            {
                return channel;
            }
        }

        if (!string.IsNullOrWhiteSpace(tunerChannel.Number))
        {
            var tunerChannelNumber = GetMappedChannel(tunerChannel.Number, mappings);
            if (string.IsNullOrWhiteSpace(tunerChannelNumber))
            {
                tunerChannelNumber = tunerChannel.Number;
            }

            var channel = epgChannelData.GetChannelByNumber(tunerChannelNumber);
            if (channel is not null)
            {
                return channel;
            }
        }

        if (!string.IsNullOrWhiteSpace(tunerChannel.Name))
        {
            var normalizedName = EpgChannelData.NormalizeName(tunerChannel.Name);

            var channel = epgChannelData.GetChannelByName(normalizedName);
            if (channel is not null)
            {
                return channel;
            }
        }

        return null;
    }

    private static TunerChannelMapping GetTunerChannelMapping(ChannelInfo tunerChannel, NameValuePair[] mappings, IList<ChannelInfo> providerChannels)
    {
        var result = new TunerChannelMapping
        {
            Name = tunerChannel.Name,
            Id = tunerChannel.Id
        };

        if (!string.IsNullOrWhiteSpace(tunerChannel.Number))
        {
            result.Name = tunerChannel.Number + " " + result.Name;
        }

        var providerChannel = GetEpgChannelFromTunerChannel(mappings, tunerChannel, new EpgChannelData(providerChannels));
        if (providerChannel is not null)
        {
            result.ProviderChannelName = providerChannel.Name;
            result.ProviderChannelId = providerChannel.Id;
        }

        return result;
    }

    private async Task<List<ChannelInfo>> GetChannelsForListingsProvider(ListingsProviderInfo info, CancellationToken cancellationToken)
    {
        var channels = new List<ChannelInfo>();
        foreach (var hostInstance in _tunerHostManager.TunerHosts)
        {
            try
            {
                var tunerChannels = await hostInstance.GetChannels(false, cancellationToken).ConfigureAwait(false);

                channels.AddRange(tunerChannels.Where(channel => IsListingProviderEnabledForTuner(info, channel.TunerHostId)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting channels");
            }
        }

        return channels;
    }

    private IListingsProvider GetProvider(string? providerType)
        => _listingsProviders.FirstOrDefault(i => string.Equals(providerType, i.Type, StringComparison.OrdinalIgnoreCase))
           ?? throw new ResourceNotFoundException($"Couldn't find provider of type {providerType}");
}
