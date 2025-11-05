#nullable disable

#pragma warning disable CA1724, CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Entities
{
    [Common.RequiresSourceSerialisation]
    public class AudioBook : Audio.Audio, IHasSeries, IHasLookupInfo<SongInfo>
    {
        public AudioBook()
        {
            AdditionalParts = Array.Empty<string>();
            LocalAlternateVersions = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the additional parts for multi-file audiobooks.
        /// </summary>
        /// <value>The paths to additional audio files that are part of this audiobook.</value>
        public string[] AdditionalParts { get; set; }

        /// <summary>
        /// Gets or sets the local alternate versions (e.g., different editions or formats).
        /// </summary>
        /// <value>The paths to alternate versions of this audiobook.</value>
        public string[] LocalAlternateVersions { get; set; }

        [JsonIgnore]
        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        /// <summary>
        /// Gets a value indicating whether this audiobook is stacked (has multiple parts).
        /// </summary>
        [JsonIgnore]
        public bool IsStacked => AdditionalParts.Length > 0;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public string FindSeriesSortName()
        {
            return SeriesName;
        }

        public string FindSeriesName()
        {
            return SeriesName;
        }

        public string FindSeriesPresentationUniqueKey()
        {
            return SeriesPresentationUniqueKey;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        public Guid FindSeriesId()
        {
            return SeriesId;
        }

        public override bool CanDownload()
        {
            return IsFileProtocol;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Book;
        }

        /// <summary>
        /// Gets the additional part IDs for this audiobook.
        /// </summary>
        /// <returns>An enumerable of GUIDs representing the additional parts.</returns>
        public IEnumerable<Guid> GetAdditionalPartIds()
        {
            return AdditionalParts.Select(i => LibraryManager.GetNewItemId(i, typeof(AudioBook)));
        }

        /// <summary>
        /// Gets the additional parts as AudioBook items.
        /// </summary>
        /// <returns>An ordered enumerable of AudioBook items representing the additional parts.</returns>
        public IOrderedEnumerable<AudioBook> GetAdditionalParts()
        {
            return GetAdditionalPartIds()
                .Select(i => LibraryManager.GetItemById(i))
                .Where(i => i is not null)
                .OfType<AudioBook>()
                .OrderBy(i => i.SortName);
        }

        /// <summary>
        /// Gets the local alternate version IDs for this audiobook.
        /// </summary>
        /// <returns>An enumerable of GUIDs representing the local alternate versions.</returns>
        public IEnumerable<Guid> GetLocalAlternateVersionIds()
        {
            return LocalAlternateVersions.Select(i => LibraryManager.GetNewItemId(i, typeof(AudioBook)));
        }

        protected override IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            var list = new List<(BaseItem, MediaSourceType)>
            {
                (this, MediaSourceType.Default)
            };

            // Add additional parts for stacked audiobooks
            if (IsStacked)
            {
                var additionalParts = GetAdditionalPartIds()
                    .Select(LibraryManager.GetItemById)
                    .Where(i => i is not null)
                    .OrderBy(i => i.SortName)
                    .ToList();

                list.AddRange(additionalParts.Select(i => (i, MediaSourceType.Default)));
            }

            var localAlternates = GetLocalAlternateVersionIds()
                .Select(LibraryManager.GetItemById)
                .Where(i => i is not null)
                .ToList();

            list.AddRange(localAlternates.Select(i => (i, MediaSourceType.Default)));

            return list;
        }

        internal override ItemUpdateType UpdateFromResolvedItem(BaseItem newItem)
        {
            var updateType = base.UpdateFromResolvedItem(newItem);

            if (newItem is AudioBook newAudioBook)
            {
                if (!AdditionalParts.SequenceEqual(newAudioBook.AdditionalParts, StringComparer.Ordinal))
                {
                    AdditionalParts = newAudioBook.AdditionalParts;

                    // When AdditionalParts changes, we need to recalculate the total duration
                    // Clear the runtime so AudioFileProber will recalculate it
                    if (AdditionalParts.Length > 0)
                    {
                        RunTimeTicks = null;
                        updateType |= ItemUpdateType.MetadataImport;
                    }
                    else
                    {
                        updateType |= ItemUpdateType.MetadataImport;
                    }
                }

                if (!LocalAlternateVersions.SequenceEqual(newAudioBook.LocalAlternateVersions, StringComparer.Ordinal))
                {
                    LocalAlternateVersions = newAudioBook.LocalAlternateVersions;
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }
    }
}
