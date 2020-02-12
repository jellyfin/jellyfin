using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Episode
    /// </summary>
    public class Episode : Video, IHasTrailers, IHasLookupInfo<EpisodeInfo>, IHasSeries
    {
        public Episode()
        {
            RemoteTrailers = Array.Empty<MediaUrl>();
            LocalTrailerIds = Array.Empty<Guid>();
            RemoteTrailerIds = Array.Empty<Guid>();
        }

        /// <inheritdoc />
        public IReadOnlyList<Guid> LocalTrailerIds { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<Guid> RemoteTrailerIds { get; set; }

        /// <summary>
        /// Gets the season in which it aired.
        /// </summary>
        /// <value>The aired season.</value>
        public int? AirsBeforeSeasonNumber { get; set; }
        public int? AirsAfterSeasonNumber { get; set; }
        public int? AirsBeforeEpisodeNumber { get; set; }

        /// <summary>
        /// This is the ending episode number for double episodes.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumberEnd { get; set; }

        public string FindSeriesSortName()
        {
            var series = Series;
            return series == null ? SeriesName : series.SortName;
        }

        [JsonIgnore]
        protected override bool SupportsOwnedItems => IsStacked || MediaSourceCount > 1;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        [JsonIgnore]
        public int? AiredSeasonNumber => AirsAfterSeasonNumber ?? AirsBeforeSeasonNumber ?? ParentIndexNumber;

        [JsonIgnore]
        public override Folder LatestItemsIndexContainer => Series;

        [JsonIgnore]
        public override Guid DisplayParentId => SeasonId;

        [JsonIgnore]
        protected override bool EnableDefaultVideoUserDataKeys => false;

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            // hack for tv plugins
            if (SourceType == SourceType.Channel)
            {
                return 0;
            }

            return 16.0 / 9;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var series = Series;
            if (series != null && ParentIndexNumber.HasValue && IndexNumber.HasValue)
            {
                var seriesUserDataKeys = series.GetUserDataKeys();
                var take = seriesUserDataKeys.Count;
                if (seriesUserDataKeys.Count > 1)
                {
                    take--;
                }
                list.InsertRange(0, seriesUserDataKeys.Take(take).Select(i => i + ParentIndexNumber.Value.ToString("000") + IndexNumber.Value.ToString("000")));
            }

            return list;
        }

        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [JsonIgnore]
        public Series Series
        {
            get
            {
                var seriesId = SeriesId;
                if (seriesId.Equals(Guid.Empty))
                {
                    seriesId = FindSeriesId();
                }
                return !seriesId.Equals(Guid.Empty) ? (LibraryManager.GetItemById(seriesId) as Series) : null;
            }
        }

        [JsonIgnore]
        public Season Season
        {
            get
            {
                var seasonId = SeasonId;
                if (seasonId.Equals(Guid.Empty))
                {
                    seasonId = FindSeasonId();
                }
                return !seasonId.Equals(Guid.Empty) ? (LibraryManager.GetItemById(seasonId) as Season) : null;
            }
        }

        [JsonIgnore]
        public bool IsInSeasonFolder => FindParent<Season>() != null;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public string SeasonName { get; set; }

        public string FindSeriesPresentationUniqueKey()
        {
            var series = Series;
            return series == null ? null : series.PresentationUniqueKey;
        }

        public string FindSeasonName()
        {
            var season = Season;

            if (season == null)
            {
                if (ParentIndexNumber.HasValue)
                {
                    return "Season " + ParentIndexNumber.Value.ToString(CultureInfo.InvariantCulture);
                }
                return "Season Unknown";
            }

            return season.Name;
        }

        public string FindSeriesName()
        {
            var series = Series;
            return series == null ? SeriesName : series.Name;
        }

        public Guid FindSeasonId()
        {
            var season = FindParent<Season>();

            // Episodes directly in series folder
            if (season == null)
            {
                var series = Series;

                if (series != null && ParentIndexNumber.HasValue)
                {
                    var findNumber = ParentIndexNumber.Value;

                    season = series.Children
                        .OfType<Season>()
                        .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == findNumber);
                }
            }

            return season == null ? Guid.Empty : season.Id;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("000 - ") : "")
                    + (IndexNumber != null ? IndexNumber.Value.ToString("0000 - ") : "") + Name;
        }

        /// <summary>
        /// Determines whether [contains episode number] [the specified number].
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns><c>true</c> if [contains episode number] [the specified number]; otherwise, <c>false</c>.</returns>
        public bool ContainsEpisodeNumber(int number)
        {
            if (IndexNumber.HasValue)
            {
                if (IndexNumberEnd.HasValue)
                {
                    return number >= IndexNumber.Value && number <= IndexNumberEnd.Value;
                }

                return IndexNumber.Value == number;
            }

            return false;
        }

        [JsonIgnore]
        public override bool SupportsRemoteImageDownloading
        {
            get
            {
                if (IsMissingEpisode)
                {
                    return false;
                }

                return true;
            }
        }

        [JsonIgnore]
        public bool IsMissingEpisode => LocationType == LocationType.Virtual;

        [JsonIgnore]
        public Guid SeasonId { get; set; }
        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public Guid FindSeriesId()
        {
            var series = FindParent<Series>();
            return series == null ? Guid.Empty : series.Id;
        }

        public override IEnumerable<Guid> GetAncestorIds()
        {
            var list = base.GetAncestorIds().ToList();

            var seasonId = SeasonId;

            if (!seasonId.Equals(Guid.Empty) && !list.Contains(seasonId))
            {
                list.Add(seasonId);
            }

            return list;
        }

        public override IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            return new[] {
                new FileSystemMetadata
                {
                    FullName = Path,
                    IsDirectory = IsFolder
                }
            }.Concat(GetLocalMetadataFilesToDelete());
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

        public EpisodeInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<EpisodeInfo>();

            var series = Series;

            if (series != null)
            {
                id.SeriesProviderIds = series.ProviderIds;
                id.SeriesDisplayOrder = series.DisplayOrder;
            }

            id.IsMissingEpisode = IsMissingEpisode;
            id.IndexNumberEnd = IndexNumberEnd;

            return id;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetdata);

            if (!IsLocked)
            {
                if (SourceType == SourceType.Library)
                {
                    try
                    {
                        if (LibraryManager.FillMissingEpisodeNumbersFromPath(this, replaceAllMetdata))
                        {
                            hasChanges = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in FillMissingEpisodeNumbersFromPath. Episode: {Episode}", Path ?? Name ?? Id.ToString());
                    }
                }
            }

            return hasChanges;
        }
    }
}
