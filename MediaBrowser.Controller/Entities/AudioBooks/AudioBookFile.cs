#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

// TODO: Use ChapterInfo?.
namespace MediaBrowser.Controller.Entities.AudioBooks
{
    /// <summary>
    /// Class AudioBookFile.
    /// </summary>
    public class AudioBookFile : Audio.Audio, IHasLookupInfo<AudioBookFileInfo>
    {
        [JsonIgnore]
        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public override MediaType MediaType => MediaType.Audio;

        public string BookTitle { get; set; }

        public int Chapter { get; set; }

        public IReadOnlyList<string> Authors { get; set; }

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        protected override bool SupportsOwnedItems => false;

        [JsonIgnore]
        public override Folder LatestItemsIndexContainer => AudioBookEntity;

        [JsonIgnore]
        public AudioBook AudioBookEntity => FindParent<AudioBook>();

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public string SeasonName { get; set; }

        [JsonIgnore]
        public override bool SupportsRemoteImageDownloading => true;

        public IEnumerable<AudioBookFile> GetNextChapters()
        {
            var chapters = AudioBookEntity.Chapters;
            var nextChapters = new List<AudioBookFile>();
            foreach (var chapter in chapters)
            {
                if (chapter.Chapter > Chapter)
                {
                    nextChapters.Add(chapter);
                }
            }

            return nextChapters;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        /// <summary>
        /// Get user data keys.
        /// </summary>
        /// <returns>User data keys associated with this item.</returns>
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            return list;
        }

        public string FindSeriesPresentationUniqueKey()
        {
            return AudioBookEntity?.PresentationUniqueKey;
        }

        public string FindAudioBookName()
        {
            return AudioBookEntity is null ? "AudioBook Unknown" : AudioBookEntity.Name;
        }

        public Guid FindAudioBookId()
        {
            return AudioBookEntity is null ? Guid.Empty : AudioBookEntity.Id;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber is not null ? ParentIndexNumber.Value.ToString("000 - ", CultureInfo.InvariantCulture) : string.Empty)
                    + (IndexNumber is not null ? IndexNumber.Value.ToString("0000 - ", CultureInfo.InvariantCulture) : string.Empty) + Name;
        }

        public override IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            return new[]
            {
                new FileSystemMetadata
                {
                    FullName = Path,
                    IsDirectory = IsFolder
                }
            }.Concat(GetLocalMetadataFilesToDelete());
        }

        public new AudioBookFileInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AudioBookFileInfo>();

            id.BookTitle = BookTitle;
            id.Authors = Authors;
            id.Container = Container;

            return id;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

            return hasChanges;
        }

        public override List<ExternalUrl> GetRelatedUrls()
        {
            var list = base.GetRelatedUrls();

            // TODO: Is there one of these for books and are those changes worth it?

            return list;
        }

        protected override IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            var chapters = AudioBookEntity.Chapters;
            var nextChapters = new List<(BaseItem Item, MediaSourceType MediaSourceType)>();
            foreach (var chapter in chapters)
            {
                if (chapter.Chapter >= this.Chapter)
                {
                    nextChapters.Add((chapter, MediaSourceType.Default));
                }
            }

            return nextChapters;
        }
    }
}
