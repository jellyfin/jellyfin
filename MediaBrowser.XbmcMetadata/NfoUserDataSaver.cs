using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata;

/// <summary>
/// <see cref="IHostedService"/> responsible for updating NFO files' user data.
/// </summary>
public sealed class NfoUserDataSaver : IHostedService
{
    private readonly ILogger<NfoUserDataSaver> _logger;
    private readonly IConfigurationManager _config;
    private readonly IUserDataManager _userDataManager;
    private readonly IProviderManager _providerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="NfoUserDataSaver"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
    /// <param name="userDataManager">The <see cref="IUserDataManager"/>.</param>
    /// <param name="providerManager">The <see cref="IProviderManager"/>.</param>
    public NfoUserDataSaver(
        ILogger<NfoUserDataSaver> logger,
        IConfigurationManager config,
        IUserDataManager userDataManager,
        IProviderManager providerManager)
    {
        _logger = logger;
        _config = config;
        _userDataManager = userDataManager;
        _providerManager = providerManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;
        return Task.CompletedTask;
    }

    private async void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (e.SaveReason is not (UserDataSaveReason.PlaybackFinished
            or UserDataSaveReason.TogglePlayed or UserDataSaveReason.UpdateUserRating))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.GetNfoConfiguration().UserId))
        {
            return;
        }

        var item = e.Item;
        if (!item.IsFileProtocol || !item.SupportsLocalMetadata)
        {
            return;
        }

        try
        {
            await _providerManager.SaveMetadataAsync(item, ItemUpdateType.MetadataDownload, [BaseNfoSaver.SaverName])
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metadata for {Path}", item.Path ?? item.Name);
        }
    }
}
