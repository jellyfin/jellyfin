#pragma warning disable CS1591

using System.Threading;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Movies
{
    public class MovieMetadataService : MetadataService<Movie, MovieInfo>
    {
        public MovieMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<MovieMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
            UserManager = userManager;
            UserDataManager = userDataManager;
        }

        protected IUserManager UserManager { get; }

        protected IUserDataManager UserDataManager { get; }

        /// <inheritdoc />
        protected override void ImportUserData(Movie item, List<UserItemData> userDataList, CancellationToken cancellationToken)
        {
            var logName = !item.IsFileProtocol ? item.Name ?? item.Path : item.Path ?? item.Name;
            foreach (var userData in userDataList) {
                var user = UserManager.GetUserById(userData.UserId);
                UserDataManager.SaveUserData(user, item, userData, UserDataSaveReason.Import, cancellationToken);
            }
        }

        /// <inheritdoc />
        protected override bool IsFullLocalMetadata(Movie item)
        {
            if (string.IsNullOrWhiteSpace(item.Overview))
            {
                return false;
            }

            if (!item.ProductionYear.HasValue)
            {
                return false;
            }

            return base.IsFullLocalMetadata(item);
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Movie> source, MetadataResult<Movie> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.CollectionName))
            {
                targetItem.CollectionName = sourceItem.CollectionName;
            }
        }
    }
}
