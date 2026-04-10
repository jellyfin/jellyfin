#nullable disable

#pragma warning disable CA1724, CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Represents an audiobook as a folder containing audio track children.
    /// Follows the same parent-child pattern as MusicAlbum.
    /// </summary>
    [Common.RequiresSourceSerialisation]
    public class AudioBook : Folder, IHasSeries, IHasLookupInfo<BookInfo>
    {
        /// <summary>
        /// Gets the audio tracks belonging to this audiobook.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Audio.Audio> Tracks => GetRecursiveChildren(i => i is Audio.Audio).Cast<Audio.Audio>();

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        [JsonIgnore]
        public override bool SupportsCumulativeRunTimeTicks => true;

        [JsonIgnore]
        public override bool SupportsPeople => true;

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

        public BookInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<BookInfo>();
            info.SeriesName = SeriesName;
            return info;
        }
    }
}
