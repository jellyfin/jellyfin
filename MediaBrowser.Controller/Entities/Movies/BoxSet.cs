using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder, IHasTrailers, IHasTags, IHasPreferredMetadataLanguage
    {
        public BoxSet()
        {
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            Tags = new List<string>();
        }

        public List<Guid> LocalTrailerIds { get; set; }
        
        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedMovies;
        }
    }
}
