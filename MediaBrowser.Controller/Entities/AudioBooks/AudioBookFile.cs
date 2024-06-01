#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

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

        // For audiobook file metadata, the title is often "[book title] - [chapter #]" and the album is the book title alone
        public string BookTitle => Album;

        public int Chapter => IndexNumber.HasValue ? IndexNumber.Value : 0;

        public IReadOnlyList<string> Authors => Artists;

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

        // TODO: Behavior may be more consistent if we use "Album" metadata and push up to parent
        public string FindAudioBookName()
        {
            return AudioBookEntity is null ? "AudioBook Unknown" : AudioBookEntity.Name;
        }

        public Guid FindAudioBookId()
        {
            return AudioBookEntity is null ? Guid.Empty : AudioBookEntity.Id;
        }

        /// <summary>
        /// Creates the string by which this item will be sorted amongst other children. Logically, AudioBookFiles
        /// must already be named in a unique, indexable way so we can just return the name.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return Name;
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

        // TODO: I'm still not sure where this fits into the larger framework.
        public new AudioBookFileInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<AudioBookFileInfo>();

            id.BookTitle = Album;
            id.Artists = Artists;
            id.Container = Container;
            id.Chapter = (int)IndexNumber;

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
            return [(this, MediaSourceType.Default)];
        }

        public void SetFilesPlayed(User user, IUserDataManager userDataManager)
        {
            foreach (var chapter in AudioBookEntity.Chapters)
            {
                if (chapter.Name == Name)
                {
                    continue;
                }

                var changed = false;
                var userData = userDataManager.GetUserData(user, chapter);

                // TODO: DECISION: Should we leave PlaybackPositionTicks or set to 0?
                if (chapter.Chapter > Chapter && userData.Played)
                {
                    userData.Played = false;
                    userData.PlaybackPositionTicks = 0;
                    changed = true;
                }
                else if (!userData.Played)
                {
                    userData.Played = true;
                    userData.PlaybackPositionTicks = 0;
                    changed = true;
                }

                if (changed)
                {
                    userDataManager.SaveUserData(user, chapter, userData, UserDataSaveReason.PlaybackStart, CancellationToken.None);
                }
            }
        }
    }
}
