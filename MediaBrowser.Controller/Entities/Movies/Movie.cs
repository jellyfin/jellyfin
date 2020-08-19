#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class Movie.
    /// </summary>
    public class Movie : Video, IHasSpecialFeatures, IHasTrailers, IHasLookupInfo<MovieInfo>, ISupportsBoxSetGrouping
    {
        public Movie()
        {
            SpecialFeatureIds = Array.Empty<Guid>();
            RemoteTrailers = Array.Empty<MediaUrl>();
            LocalTrailerIds = Array.Empty<Guid>();
            RemoteTrailerIds = Array.Empty<Guid>();
        }

        /// <inheritdoc />
        public IReadOnlyList<Guid> SpecialFeatureIds { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<Guid> LocalTrailerIds { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<Guid> RemoteTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the name of the TMDB collection.
        /// </summary>
        /// <value>The name of the TMDB collection.</value>
        public string TmdbCollectionName { get; set; }

        [JsonIgnore]
        public string CollectionName
        {
            get => TmdbCollectionName;
            set => TmdbCollectionName = value;
        }

        [JsonIgnore]
        public override bool StopRefreshIfLocalMetadataFound => false;

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            // hack for tv plugins
            if (SourceType == SourceType.Channel)
            {
                return 0;
            }

            return 2.0 / 3;
        }

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var hasChanges = await base.RefreshedOwnedItems(options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

            // Must have a parent to have special features
            // In other words, it must be part of the Parent/Child tree
            if (IsFileProtocol && SupportsOwnedItems && !IsInMixedFolder)
            {
                var specialFeaturesChanged = await RefreshSpecialFeatures(options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

                if (specialFeaturesChanged)
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private async Task<bool> RefreshSpecialFeatures(MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newItems = LibraryManager.FindExtras(this, fileSystemChildren, options.DirectoryService).ToList();
            var newItemIds = newItems.Select(i => i.Id).ToArray();

            var itemsChanged = !SpecialFeatureIds.SequenceEqual(newItemIds);

            var ownerId = Id;

            var tasks = newItems.Select(i =>
            {
                var subOptions = new MetadataRefreshOptions(options);

                if (i.OwnerId != ownerId)
                {
                    i.OwnerId = ownerId;
                    subOptions.ForceSave = true;
                }

                return RefreshMetadataForOwnedItem(i, false, subOptions, cancellationToken);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            SpecialFeatureIds = newItemIds;

            return itemsChanged;
        }

        /// <inheritdoc />
        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Movie;
        }

        public MovieInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<MovieInfo>();

            if (!IsInMixedFolder)
            {
                var name = System.IO.Path.GetFileName(ContainingFolderPath);

                if (VideoType == VideoType.VideoFile || VideoType == VideoType.Iso)
                {
                    if (string.Equals(name, System.IO.Path.GetFileName(Path), StringComparison.OrdinalIgnoreCase))
                    {
                        // if the folder has the file extension, strip it
                        name = System.IO.Path.GetFileNameWithoutExtension(name);
                    }
                }

                info.Name = name;
            }

            return info;
        }

        /// <inheritdoc />
        public override bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetdata);

            if (!ProductionYear.HasValue)
            {
                var info = LibraryManager.ParseName(Name);

                var yearInName = info.Year;

                if (yearInName.HasValue)
                {
                    ProductionYear = yearInName;
                    hasChanges = true;
                }
                else
                {
                    // Try to get the year from the folder name
                    if (!IsInMixedFolder)
                    {
                        info = LibraryManager.ParseName(System.IO.Path.GetFileName(ContainingFolderPath));

                        yearInName = info.Year;

                        if (yearInName.HasValue)
                        {
                            ProductionYear = yearInName;
                            hasChanges = true;
                        }
                    }
                }
            }

            return hasChanges;
        }

        /// <inheritdoc />
        public override List<ExternalUrl> GetRelatedUrls()
        {
            var list = base.GetRelatedUrls();

            var imdbId = this.GetProviderId(MetadataProvider.Imdb);
            if (!string.IsNullOrEmpty(imdbId))
            {
                list.Add(new ExternalUrl
                {
                    Name = "Trakt",
                    Url = string.Format(CultureInfo.InvariantCulture, "https://trakt.tv/movies/{0}", imdbId)
                });
            }

            return list;
        }
    }
}
