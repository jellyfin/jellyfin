#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class Movie.
    /// </summary>
    public class Movie : Video, IHasSpecialFeatures, IHasTrailers, IHasLookupInfo<MovieInfo>, ISupportsBoxSetGrouping
    {
        /// <inheritdoc />
        [JsonIgnore]
        public IReadOnlyList<Guid> SpecialFeatureIds => GetExtras()
            .Where(extra => extra.ExtraType is not null && extra is Video)
            .Select(extra => extra.Id)
            .ToArray();

        /// <inheritdoc />
        [JsonIgnore]
        public IReadOnlyList<BaseItem> LocalTrailers => GetExtras([Model.Entities.ExtraType.Trailer]).ToArray();

        /// <summary>
        /// Gets or sets the name of the TMDb collection.
        /// </summary>
        /// <value>The name of the TMDb collection.</value>
        public string TmdbCollectionName { get; set; }

        [JsonIgnore]
        public string CollectionName
        {
            get => TmdbCollectionName;
            set => TmdbCollectionName = value;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            // hack for tv plugins
            if (SourceType == SourceType.Channel)
            {
                return 0;
            }

            return 2.0 / 3;
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

        protected override async Task RefreshMetadataForVersions(MetadataRefreshOptions options, bool copyTitleMetadata, string path, CancellationToken cancellationToken)
        {
            var newOptions = new MetadataRefreshOptions(options)
            {
                SearchResult = null
            };

            var id = LibraryManager.GetNewItemId(path, typeof(Movie));

            // Check if the file still exists
            if (!FileSystem.FileExists(path))
            {
                // File was removed - clean up any orphaned database entry
                if (LibraryManager.GetItemById(id) is Movie orphanedMovie && orphanedMovie.OwnerId.Equals(Id))
                {
                    Logger.LogInformation("Alternate version file no longer exists, removing orphaned item: {Path}", path);
                    LibraryManager.DeleteItem(orphanedMovie, new DeleteOptions { DeleteFileLocation = false });
                }

                return;
            }

            if (LibraryManager.GetItemById(id) is not Movie movie)
            {
                movie = LibraryManager.ResolvePath(FileSystem.GetFileSystemInfo(path)) as Movie;

                newOptions.ForceSave = true;
            }

            if (movie is null)
            {
                return;
            }

            if (movie.OwnerId.Equals(Guid.Empty))
            {
                movie.OwnerId = Id;
            }

            await RefreshMetadataForOwnedItem(movie, copyTitleMetadata, newOptions, cancellationToken).ConfigureAwait(false);

            // Create LinkedChild entry for this local alternate version
            LibraryManager.UpsertLinkedChild(Id, movie.Id, LinkedChildType.LocalAlternateVersion);
        }

        /// <inheritdoc />
        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

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
    }
}
