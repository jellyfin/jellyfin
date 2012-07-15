using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Entities
{
    public abstract class BaseItem
    {
        public virtual string Name { get; set; }
        public virtual string SortName { get; set; }

        public virtual Guid Id { get; set; }

        public virtual DateTime DateCreated { get; set; }

        public virtual DateTime DateModified { get; set; }

        public virtual string Path { get; set; }

        [IgnoreDataMember]
        public Folder Parent { get; set; }

        public virtual string PrimaryImagePath { get; set; }
        public virtual string LogoImagePath { get; set; }
        public virtual string ArtImagePath { get; set; }
        public virtual string ThumbnailImagePath { get; set; }
        public virtual string BannerImagePath { get; set; }

        public virtual IEnumerable<string> BackdropImagePaths { get; set; }

        public virtual string OfficialRating { get; set; }

        public virtual string CustomRating { get; set; }
        public virtual string CustomPin { get; set; }

        public virtual string Overview { get; set; }
        public virtual string Tagline { get; set; }

        [IgnoreDataMember]
        public virtual IEnumerable<PersonInfo> People { get; set; }

        public virtual IEnumerable<string> Studios { get; set; }

        public virtual IEnumerable<string> Genres { get; set; }

        public virtual string DisplayMediaType { get; set; }

        public virtual float? UserRating { get; set; }
        public virtual TimeSpan? RunTime { get; set; }

        public virtual string AspectRatio { get; set; }
        public virtual int? ProductionYear { get; set; }

        public virtual IEnumerable<Video> LocalTrailers { get; set; }

        public virtual string TrailerUrl { get; set; }
        
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// This is strictly to enhance json output, until I can find a way to customize service stack to add this without having to use a property
        /// </summary>
        public virtual bool IsFolder
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// This is strictly to enhance json output, until I can find a way to customize service stack to add this without having to use a property
        /// </summary>
        public string Type
        {
            get
            {
                return GetType().Name;
            }
        }
    }
}
