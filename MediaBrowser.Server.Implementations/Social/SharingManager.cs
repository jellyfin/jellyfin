using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Social;
using MediaBrowser.Model.Social;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Social
{
    public class SharingManager : ISharingManager
    {
        private readonly SharingRepository _repository;
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerApplicationHost _appHost;

        public SharingManager(SharingRepository repository, IServerConfigurationManager config, ILibraryManager libraryManager, IServerApplicationHost appHost)
        {
            _repository = repository;
            _config = config;
            _libraryManager = libraryManager;
            _appHost = appHost;
        }

        public async Task<SocialShareInfo> CreateShare(string itemId, string userId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var item = _libraryManager.GetItemById(itemId);

            if (item == null)
            {
                throw new ResourceNotFoundException();
            }

            var externalUrl = (await _appHost.GetSystemInfo().ConfigureAwait(false)).WanAddress;

            if (string.IsNullOrWhiteSpace(externalUrl))
            {
                throw new InvalidOperationException("No external server address is currently available.");
            }

            var info = new SocialShareInfo
            {
                Id = Guid.NewGuid().ToString("N"),
                ExpirationDate = DateTime.UtcNow.AddDays(_config.Configuration.SharingExpirationDays),
                ItemId = itemId,
                UserId = userId
            };

            AddShareInfo(info, externalUrl);
            
            await _repository.CreateShare(info).ConfigureAwait(false);

            return info;
        }

        private string GetTitle(BaseItem item)
        {
            return item.Name;
        }

        public SocialShareInfo GetShareInfo(string id)
        {
            var info = _repository.GetShareInfo(id);

            AddShareInfo(info, _appHost.GetSystemInfo().Result.WanAddress);

            return info;
        }

        private void AddShareInfo(SocialShareInfo info, string externalUrl)
        {
            info.ImageUrl = externalUrl + "/Social/Shares/Public/" + info.Id + "/Image";
            info.Url = externalUrl + "/emby/web/shared.html?id=" + info.Id;

            var item = _libraryManager.GetItemById(info.ItemId);

            if (item != null)
            {
                info.Overview = item.Overview;
                info.Name = GetTitle(item);
            }
        }

        public Task DeleteShare(string id)
        {
            return _repository.DeleteShare(id);
        }
    }
}
