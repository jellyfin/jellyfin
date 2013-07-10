using MediaBrowser.Model.Entities;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class MusicVideo : Video
    {
        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        public string Artist { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }
        
        /// <summary>
        /// Should be overridden to return the proper folder where metadata lives
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso || IsMultiPart ? System.IO.Path.GetDirectoryName(Path) : Path;
            }
        }

        /// <summary>
        /// Determines whether the specified name has artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name has artist; otherwise, <c>false</c>.</returns>
        public bool HasArtist(string name)
        {
            return string.Equals(Artist, name, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return this.GetProviderId(MetadataProviders.Tmdb) ?? this.GetProviderId(MetadataProviders.Imdb) ?? base.GetUserDataKey();
        }

        /// <summary>
        /// Needed because the resolver stops at the movie folder and we find the video inside.
        /// </summary>
        /// <value><c>true</c> if [use parent path to create resolve args]; otherwise, <c>false</c>.</value>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return VideoType == VideoType.VideoFile || VideoType == VideoType.Iso || IsMultiPart;
            }
        }
    }
}
