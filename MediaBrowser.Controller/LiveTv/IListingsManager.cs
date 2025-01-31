using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv;

/// <summary>
/// Service responsible for managing <see cref="IListingsProvider"/>s and mapping
/// their channels to channels provided by <see cref="ITunerHost"/>s.
/// </summary>
public interface IListingsManager
{
    /// <summary>
    /// Saves the listing provider.
    /// </summary>
    /// <param name="info">The listing provider information.</param>
    /// <param name="validateLogin">A value indicating whether to validate login.</param>
    /// <param name="validateListings">A value indicating whether to validate listings..</param>
    /// <returns>Task.</returns>
    Task<ListingsProviderInfo> SaveListingProvider(ListingsProviderInfo info, bool validateLogin, bool validateListings);

    /// <summary>
    /// Deletes the listing provider.
    /// </summary>
    /// <param name="id">The listing provider's id.</param>
    void DeleteListingsProvider(string? id);

    /// <summary>
    /// Gets the lineups.
    /// </summary>
    /// <param name="providerType">Type of the provider.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="country">The country.</param>
    /// <param name="location">The location.</param>
    /// <returns>The available lineups.</returns>
    Task<List<NameIdPair>> GetLineups(string? providerType, string? providerId, string? country, string? location);

    /// <summary>
    /// Gets the programs for a provided channel.
    /// </summary>
    /// <param name="channel">The channel to retrieve programs for.</param>
    /// <param name="startDateUtc">The earliest date to retrieve programs for.</param>
    /// <param name="endDateUtc">The latest date to retrieve programs for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>The available programs.</returns>
    Task<IEnumerable<ProgramInfo>> GetProgramsAsync(
        ChannelInfo channel,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds metadata from the <see cref="IListingsProvider"/>s to the provided channels.
    /// </summary>
    /// <param name="channels">The channels.</param>
    /// <param name="enableCache">A value indicating whether to use the EPG channel cache.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>A task representing the metadata population.</returns>
    Task AddProviderMetadata(IList<ChannelInfo> channels, bool enableCache, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the channel mapping options for a provider.
    /// </summary>
    /// <param name="providerId">The id of the provider to use.</param>
    /// <returns>The channel mapping options.</returns>
    Task<ChannelMappingOptionsDto> GetChannelMappingOptions(string? providerId);

    /// <summary>
    /// Sets the channel mapping.
    /// </summary>
    /// <param name="providerId">The id of the provider for the mapping.</param>
    /// <param name="tunerChannelNumber">The tuner channel number.</param>
    /// <param name="providerChannelNumber">The provider channel number.</param>
    /// <returns>The updated channel mapping.</returns>
    Task<TunerChannelMapping> SetChannelMapping(string providerId, string tunerChannelNumber, string providerChannelNumber);
}
