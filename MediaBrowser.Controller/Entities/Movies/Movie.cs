using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Win32;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class Movie
    /// </summary>
    public class Movie : Video
    {
        /// <summary>
        /// Should be overridden to return the proper folder where metadata lives
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso ? System.IO.Path.GetDirectoryName(Path) : Path;
            }
        }

        /// <summary>
        /// Override to use tmdb or imdb id so it will stick if the item moves physical locations
        /// </summary>
        /// <value>The user data id.</value>
        [IgnoreDataMember]
        public override Guid UserDataId
        {
            get
            {
                if (_userDataId == Guid.Empty)
                {
                    var baseId = this.GetProviderId(MetadataProviders.Tmdb) ?? this.GetProviderId(MetadataProviders.Imdb);
                    _userDataId = baseId != null ? baseId.GetMD5() : Id;
                }
                return _userDataId;
            }
        }

        /// <summary>
        /// The _special features
        /// </summary>
        private List<Video> _specialFeatures;
        /// <summary>
        /// The _special features initialized
        /// </summary>
        private bool _specialFeaturesInitialized;
        /// <summary>
        /// The _special features sync lock
        /// </summary>
        private object _specialFeaturesSyncLock = new object();
        /// <summary>
        /// Gets the special features.
        /// </summary>
        /// <value>The special features.</value>
        [IgnoreDataMember]
        public List<Video> SpecialFeatures
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _specialFeatures, ref _specialFeaturesInitialized, ref _specialFeaturesSyncLock, () => LoadSpecialFeatures().ToList());
                return _specialFeatures;
            }
            private set
            {
                _specialFeatures = value;

                if (value == null)
                {
                    _specialFeaturesInitialized = false;
                }
            }
        }

        /// <summary>
        /// Needed because the resolver stops at the movie folder and we find the video inside.
        /// </summary>
        /// <value><c>true</c> if [use parent path to create resolve args]; otherwise, <c>false</c>.</value>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso;
            }
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for special features
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [is new item].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="resetResolveArgs">if set to <c>true</c> [reset resolve args].</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            // Lazy load these again
            SpecialFeatures = null;

            // Kick off a task to refresh the main item
            var result = await base.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders, resetResolveArgs).ConfigureAwait(false);

            var tasks = SpecialFeatures.Select(item => item.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="user">The user.</param>
        /// <returns>BaseItem.</returns>
        public override BaseItem FindItemById(Guid id, User user)
        {
            var item = base.FindItemById(id, user);

            if (item != null)
            {
                return item;
            }

            if (SpecialFeatures != null)
            {
                return SpecialFeatures.FirstOrDefault(i => i.Id == id);
            }

            return null;
        }        
        
        /// <summary>
        /// Loads special features from the file system
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>List{Video}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        private IEnumerable<Video> LoadSpecialFeatures()
        {
            WIN32_FIND_DATA? folder;

            try
            {
                folder = ResolveArgs.GetFileSystemEntryByName("specials");
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new List<Video> { };
            }

            // Path doesn't exist. No biggie
            if (folder == null)
            {
                return new List<Video> { };
            }

            IEnumerable<WIN32_FIND_DATA> files;

            try
            {
                files = FileSystem.GetFiles(folder.Value.Path);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error loading trailers for {0}", ex, Name);
                return new List<Video> { };
            }

            return Kernel.Instance.LibraryManager.GetItems<Video>(files, null).Select(video =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = Kernel.Instance.ItemRepository.RetrieveItem(video.Id) as Video;

                if (dbItem != null)
                {
                    dbItem.ResolveArgs = video.ResolveArgs;
                    video = dbItem;
                }

                return video;
            });
        }

    }
}
