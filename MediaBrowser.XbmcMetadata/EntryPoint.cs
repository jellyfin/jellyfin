using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
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
            // TODO: Need a more accurate check here to see if xbmc metadata saving is enabled.
            // This is probably good enough, but no guarantee
            var userId = _config.GetNfoConfiguration().UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            if (e.UpdateReason == ItemUpdateType.ImageUpdate)
            {
                var person = e.Item as Person;

                if (person != null)
                {
                    var items = _libraryManager.RootFolder.RecursiveChildren;
                    items = person.GetTaggedItems(items).ToList();

                    foreach (var item in items)
                    {
                        SaveMetadataForItem(item, ItemUpdateType.MetadataEdit);
                    }
                }
            }
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            var userId = _config.GetNfoConfiguration().UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed)
            {
                var item = e.Item as BaseItem;

                if (item != null)
                {
                    if (!item.IsFolder && !(item is IItemByName))
                    {
                        SaveMetadataForItem(item, ItemUpdateType.MetadataEdit);
                    }
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

            try
            {
                await _providerManager.SaveMetadata(item, updateReason).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving metadata for {0}", ex, item.Path ?? item.Name);
            }
        }
    }
}
