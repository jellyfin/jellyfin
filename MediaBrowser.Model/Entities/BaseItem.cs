using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Entities
{
    public abstract class BaseItem
    {
        public string Name { get; set; }
        public string SortName { get; set; }

        public Guid Id { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateModified { get; set; }

        /// <summary>
        /// When the item first debuted. For movies this could be premiere date, episodes would be first aired
        /// </summary>
        public DateTime? PremiereDate { get; set; }

        public string Path { get; set; }

        [IgnoreDataMember]
        public Folder Parent { get; set; }

        public string PrimaryImagePath { get; set; }
        public string LogoImagePath { get; set; }
        public string ArtImagePath { get; set; }
        public string ThumbnailImagePath { get; set; }
        public string BannerImagePath { get; set; }

        public IEnumerable<string> BackdropImagePaths { get; set; }

        public string OfficialRating { get; set; }

        public string CustomRating { get; set; }
        public string CustomPin { get; set; }

        public string Overview { get; set; }
        public string Tagline { get; set; }

        [IgnoreDataMember]
        public IEnumerable<PersonInfo> People { get; set; }

        public IEnumerable<string> Studios { get; set; }

        public IEnumerable<string> Genres { get; set; }

        public string DisplayMediaType { get; set; }

        public float? UserRating { get; set; }
        public int? RunTimeInSeconds { get; set; }

        public string AspectRatio { get; set; }
        public int? ProductionYear { get; set; }

        /// <summary>
        /// If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        public int? IndexNumber { get; set; }

        [IgnoreDataMember]
        public IEnumerable<Video> LocalTrailers { get; set; }

        public string TrailerUrl { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
