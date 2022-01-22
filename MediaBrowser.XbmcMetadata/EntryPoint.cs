#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata
{
    public sealed class EntryPoint : IServerEntryPoint
    {
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger<EntryPoint> _logger;
        private readonly IProviderManager _providerManager;
        private readonly IConfigurationManager _config;

        public EntryPoint(
            IUserDataManager userDataManager,
            ILogger<EntryPoint> logger,
            IProviderManager providerManager,
            IConfigurationManager config)
        {
            _userDataManager = userDataManager;
            _logger = logger;
            _providerManager = providerManager;
            _config = config;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _userDataManager.UserDataSaved += OnUserDataSaved;

            return Task.CompletedTask;
        }

        private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed || e.SaveReason == UserDataSaveReason.UpdateUserRating)
            {
                if (!string.IsNullOrWhiteSpace(_config.GetNfoConfiguration().UserId))
                {
                    _ = SaveMetadataForItemAsync(e.Item, ItemUpdateType.MetadataDownload);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _userDataManager.UserDataSaved -= OnUserDataSaved;
        }

        private async Task SaveMetadataForItemAsync(BaseItem item, ItemUpdateType updateReason)
        {
            if (!item.IsFileProtocol || !item.SupportsLocalMetadata)
            {
                return;
            }

            try
            {
                await _providerManager.SaveMetadataAsync(item, updateReason, new[] { BaseNfoSaver.SaverName }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving metadata for {Path}", item.Path ?? item.Name);
            }
        }
    }
}
