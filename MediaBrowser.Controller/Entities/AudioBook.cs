#nullable disable

#pragma warning disable CA1724, CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.Entities
{
    public class AudioBook : Audio.Audio, IHasSeries, IHasLookupInfo<SongInfo>
    {
        public string[] AdditionalParts { get; set; } = Array.Empty<string>();

        public long[] PartRunTimeTicks { get; set; } = Array.Empty<long>();

        [JsonIgnore]
        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public override IReadOnlyList<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            if (AdditionalParts.Length == 0 || PartRunTimeTicks.Length < AdditionalParts.Length + 1)
            {
                return base.GetMediaSources(enablePathSubstitution);
            }

            var sources = new List<MediaSourceInfo>(base.GetMediaSources(enablePathSubstitution));
            if (sources.Count > 0)
            {
                sources[0].RunTimeTicks = PartRunTimeTicks[0];
            }

            var template = sources.Count > 0 ? sources[0] : null;
            for (int i = 0; i < AdditionalParts.Length; i++)
            {
                sources.Add(new MediaSourceInfo
                {
                    Id = LibraryManager.GetNewItemId(AdditionalParts[i], typeof(AudioBook)).ToString("N", CultureInfo.InvariantCulture),
                    Path = AdditionalParts[i],
                    Protocol = template?.Protocol ?? MediaProtocol.File,
                    RunTimeTicks = PartRunTimeTicks[i + 1],
                    Container = template?.Container,
                    Type = MediaSourceType.Default,
                    MediaStreams = template?.MediaStreams ?? new List<MediaStream>(),
                    IsInfiniteStream = false,
                    SupportsDirectPlay = template?.SupportsDirectPlay ?? true,
                    SupportsDirectStream = template?.SupportsDirectStream ?? true,
                    SupportsTranscoding = template?.SupportsTranscoding ?? true,
                    Bitrate = template?.Bitrate,
                    DefaultAudioStreamIndex = template?.DefaultAudioStreamIndex,
                    Name = System.IO.Path.GetFileNameWithoutExtension(AdditionalParts[i]),
                });
            }

            return sources;
        }

        internal override ItemUpdateType UpdateFromResolvedItem(BaseItem newItem)
        {
            var updateType = base.UpdateFromResolvedItem(newItem);

            if (newItem is AudioBook newAudioBook
                && !AdditionalParts.SequenceEqual(newAudioBook.AdditionalParts, StringComparer.Ordinal))
            {
                AdditionalParts = newAudioBook.AdditionalParts;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

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
    }
}
