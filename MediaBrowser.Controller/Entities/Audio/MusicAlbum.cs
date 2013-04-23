using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbum
    /// </summary>
    public class MusicAlbum : Folder
    {
        /// <summary>
        /// Songs will group into us so don't also include us in the index
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IncludeInIndex
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Override this to true if class should be grouped under a container in indicies
        /// The container class should be defined via IndexContainer
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The unknwon artist
        /// </summary>
        private static readonly MusicArtist UnknwonArtist = new MusicArtist { Name = "<Unknown>" };

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get { return Parent as MusicArtist ?? UnknwonArtist; }
        }

        /// <summary>
        /// Override to point to first child (song)
        /// </summary>
        /// <value>The production year.</value>
        public override int? ProductionYear
        {
            get
            {
                var child = Children.FirstOrDefault();

                return child == null ? base.ProductionYear : child.ProductionYear;
            }
            set
            {
                base.ProductionYear = value;
            }
        }

        /// <summary>
        /// Override to point to first child (song)
        /// </summary>
        /// <value>The genres.</value>
        public override List<string> Genres
        {
            get
            {
                var child = Children.FirstOrDefault();

                return child == null ? base.Genres : child.Genres;
            }
            set
            {
                base.Genres = value;
            }
        }

        /// <summary>
        /// Override to point to first child (song)
        /// </summary>
        /// <value>The studios.</value>
        public override List<string> Studios
        {
            get
            {
                var child = Children.FirstOrDefault();

                return child == null ? base.Studios : child.Studios;
            }
            set
            {
                base.Studios = value;
            }
        }

        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        /// <value>The images.</value>
        public override Dictionary<string, string> Images
        {
            get
            {
                var images = base.Images;
                string primaryImagePath;

                if (images == null || !images.TryGetValue(ImageType.Primary.ToString(), out primaryImagePath))
                {
                    var image = Children.Select(c => c.PrimaryImagePath).FirstOrDefault(c => !string.IsNullOrEmpty(c));

                    if (!string.IsNullOrEmpty(image))
                    {
                        if (images == null)
                        {
                            images = new Dictionary<string, string>();
                        }
                        images[ImageType.Primary.ToString()] = image;
                    }
                }

                return images;
            }
            set
            {
                base.Images = value;
            }
        }
    }
}
