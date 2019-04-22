using System;
using System.Threading.Tasks;
using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Plugins;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.XbmcMetadata.Configuration;
using Jellyfin.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.XbmcMetadata
{
    public class EntryPoint : IServerEntryPoint
    {
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IConfigurationManager _config;

        public EntryPoint(IUserDataManager userDataManager, ILibraryManager libraryManager, ILogger logger, IProviderManager providerManager, IConfigurationManager config)
        {
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _providerManager = providerManager;
            _config = config;
        }

        public Task RunAsync()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;

            return Task.CompletedTask;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed || e.SaveReason == UserDataSaveReason.UpdateUserRating)
            {
                if (!string.IsNullOrWhiteSpace(_config.GetNfoConfiguration().UserId))
                {
                    SaveMetadataForItem(e.Item, ItemUpdateType.MetadataDownload);
                }
            }
        }

        public void Dispose()
        {
            _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
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
                _providerManager.SaveMetadata(item, updateReason, new[] { BaseNfoSaver.SaverName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving metadata for {path}", item.Path ?? item.Name);
            }
        }
    }
}
