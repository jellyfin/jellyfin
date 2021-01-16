using System;
using System.Threading.Tasks;
using Jellyfin.KodiMetadata.Configuration;
using Jellyfin.KodiMetadata.Models;
using Jellyfin.KodiMetadata.Savers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata
{
    /// <summary>
    /// Kodi metadata <see cref="IServerEntryPoint"/> for registering UserDataSaved event.
    /// </summary>
    public sealed class EntryPoint : IServerEntryPoint
    {
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger<EntryPoint> _logger;
        private readonly IProviderManager _providerManager;
        private readonly IConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryPoint"/> class.
        /// </summary>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
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
                    SaveMetadataForItem(e.Item, ItemUpdateType.MetadataDownload);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _userDataManager.UserDataSaved -= OnUserDataSaved;
        }

        private void SaveMetadataForItem(BaseItem item, ItemUpdateType updateReason)
        {
            if (!item.IsFileProtocol)
            {
                return;
            }

            if (!item.SupportsLocalMetadata)
            {
                return;
            }

            if (!item.IsSaveLocalMetadataEnabled())
            {
                return;
            }

            try
            {
                _providerManager.SaveMetadata(item, updateReason, new[] { BaseNfoSaver<BaseItem, BaseNfo>.SaverName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving metadata for {path}", item.Path ?? item.Name);
            }
        }
    }
}
