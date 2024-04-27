#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Entities.AudioBooks
{
    /// <summary>
    /// Class Season.
    /// </summary>
    public class AudioBook : Folder, IHasLookupInfo<AudioBookFolderInfo>, IMetadataContainer, IHasMediaSources
    {
        public AudioBook()
        {
            Authors = Array.Empty<string>();
        }

        [JsonIgnore]
        public override MediaType MediaType => MediaType.Audio;

        public IReadOnlyList<string> Authors { get; set; }

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        [JsonIgnore]
        public override bool SupportsDateLastMediaAdded => false;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public override bool SupportsPositionTicksResume => true;

        /// <summary>
        /// Gets the tracks.
        /// </summary>
        /// <value>The tracks.</value>
        [JsonIgnore]
        public IEnumerable<Audio.Audio> Tracks => GetRecursiveChildren(i => i is AudioBookFile).Cast<Audio.Audio>();

        [JsonIgnore]
        public IEnumerable<AudioBookFile> Chapters => GetRecursiveChildren(i => i is AudioBookFile).Cast<AudioBookFile>();

        public override int GetChildCount(User user)
        {
            var result = GetChildren(user, true).Count;

            return result;
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Tracks;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber is not null ? IndexNumber.Value.ToString("0000", CultureInfo.InvariantCulture) : Name;
        }

        /// <summary>
        /// Gets the lookup information.
        /// </summary>
        /// <returns>SeasonInfo.</returns>
        public AudioBookFolderInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AudioBookFolderInfo>();

            return id;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        public override bool CanDownload()
        {
            return IsFileProtocol;
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = GetRecursiveChildren();

            var totalItems = items.Count;
            var numComplete = 0;

            var childUpdateType = ItemUpdateType.None;

            // Refresh AudioBook files/chapters only
            foreach (var item in items.OfType<AudioBookFile>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updateType = await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                childUpdateType = childUpdateType | updateType;

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 95);
            }

            // get album LUFS
            LUFS = items.OfType<AudioBookFile>().Max(item => item.LUFS);

            var parentRefreshOptions = refreshOptions;
            if (childUpdateType > ItemUpdateType.None)
            {
                parentRefreshOptions = new MetadataRefreshOptions(refreshOptions)
                {
                    MetadataRefreshMode = MetadataRefreshMode.FullRefresh
                };
            }

            // Refresh current item
            await RefreshMetadata(parentRefreshOptions, cancellationToken).ConfigureAwait(false);

            // if (!refreshOptions.IsAutomated)
            // {
            // await RefreshArtists(refreshOptions, cancellationToken).ConfigureAwait(false);
            // }
        }

        // private async Task RefreshArtists(MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        // {
        // foreach (var i in this.GetAllArtists())
        // {
        // This should not be necessary but we're seeing some cases of it
        // if (string.IsNullOrEmpty(i))
        // {
        // continue;
        // }

        // var artist = LibraryManager.GetArtist(i);

        // if (!artist.IsAccessedByName)
        // {
        // continue;
        // }

        // await artist.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
        // }
        // }
        protected override IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            // var tracks = Enumerable.Empty<(BaseItem Item, MediaSourceType MediaSourceType)>;
            var tracks = new List<(BaseItem Item, MediaSourceType MediaSourceType)>();
            foreach (var track in Tracks)
            {
                tracks.Add((track, MediaSourceType.Default));
            }

            return tracks;
        }
    }
}
