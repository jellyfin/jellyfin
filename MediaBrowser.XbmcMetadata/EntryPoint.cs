using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using MediaBrowser.XbmcMetadata.Savers;
using System;
using System.Linq;

namespace MediaBrowser.XbmcMetadata
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

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;
            _libraryManager.ItemUpdated += _libraryManager_ItemUpdated;
        }

        void _libraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (e.UpdateReason >= ItemUpdateType.ImageUpdate)
            {
                var person = e.Item as Person;

                if (person != null)
                {
                    var config = _config.GetNfoConfiguration();

                    if (!config.SaveImagePathsInNfo)
                    {
                        return;
                    }

                    var items = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        Person = person.Name

                    }).ToList();

                    foreach (var item in items)
                    {
                        SaveMetadataForItem(item, e.UpdateReason);
                    }
                }
            }
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed || e.SaveReason == UserDataSaveReason.UpdateUserRating)
            {
                var item = e.Item as BaseItem;

                if (!string.IsNullOrWhiteSpace(_config.GetNfoConfiguration().UserId))
                {
                    SaveMetadataForItem(item, ItemUpdateType.MetadataDownload);
                }
            }
        }

        public void Dispose()
        {
            _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
        }

        private async void SaveMetadataForItem(BaseItem item, ItemUpdateType updateReason)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote ||
                locationType == LocationType.Virtual)
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
                await _providerManager.SaveMetadata(item, updateReason, new[] { BaseNfoSaver.SaverName }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving metadata for {0}", ex, item.Path ?? item.Name);
            }
        }
    }
}
