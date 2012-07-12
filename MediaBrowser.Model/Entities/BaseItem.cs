using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MediaBrowser.Model.Entities
{
    public abstract class BaseItem
    {
        public string Name { get; set; }
        public string SortName { get; set; }

        public Guid Id { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public string Path { get; set; }

        [JsonIgnore]
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

        [JsonIgnore]
        public IEnumerable<PersonInfo> People { get; set; }

        public IEnumerable<string> Studios { get; set; }

        public IEnumerable<string> Genres { get; set; }

        public string DisplayMediaType { get; set; }

        public float? UserRating { get; set; }
        public TimeSpan? RunTime { get; set; }

        public string AspectRatio { get; set; }
        public int? ProductionYear { get; set; }

        public IEnumerable<Video> LocalTrailers { get; set; }
        
        public string TrailerUrl { get; set; }
        
        public override string ToString()
        {
            return Name;
        }
    }
}
