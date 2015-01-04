using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Saves movie.xml for movies, trailers and music videos
    /// </summary>
    public class MovieXmlSaver : IMetadataFileSaver
    {
        private readonly IItemRepository _itemRepository;
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;

        public MovieXmlSaver(IItemRepository itemRepository, IServerConfigurationManager config, ILibraryManager libraryManager)
        {
            _itemRepository = itemRepository;
            _config = config;
            _libraryManager = libraryManager;
        }

        public string Name
        {
            get
            {
                return XmlProviderUtils.Name;
            }
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            var video = item as Video;

            // Check parent for null to avoid running this against things like video backdrops
            if (video != null && !(item is Episode) && !video.IsOwnedItem)
            {
                // If it's a plain video, skip if content type is unset (unless editing)
                if (video.GetType() == typeof(Video))
                {
                    if (updateType < ItemUpdateType.MetadataEdit && string.IsNullOrEmpty(_libraryManager.GetContentType(video)))
                    {
                        return false;
                    }
                }
                
                return updateType >= ItemUpdateType.MetadataDownload;
            }

            return false;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            var builder = new StringBuilder();

            builder.Append("<Title>");

            XmlSaverHelpers.AddCommonNodes(video, builder);

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (musicVideo.Artists.Count > 0)
                {
                    builder.Append("<Artist>" + SecurityElement.Escape(string.Join(";", musicVideo.Artists.ToArray())) + "</Artist>");
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    builder.Append("<Album>" + SecurityElement.Escape(musicVideo.Album) + "</Album>");
                }
            }

            var movie = item as Movie;

            if (movie != null)
            {
                if (!string.IsNullOrEmpty(movie.TmdbCollectionName))
                {
                    builder.Append("<TmdbCollectionName>" + SecurityElement.Escape(movie.TmdbCollectionName) + "</TmdbCollectionName>");
                }
            }
            
            XmlSaverHelpers.AddMediaInfo(video, builder, _itemRepository);

            builder.Append("</Title>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    // Deprecated. No longer saving in this field.
                    "IMDBrating",
                    
                    // Deprecated. No longer saving in this field.
                    "Description",

                    "Artist",
                    "Album",
                    "TmdbCollectionName"
                }, _config);
        }

        public string GetSavePath(IHasMetadata item)
        {
            return GetMovieSavePath((Video)item);
        }

        public static string GetMovieSavePath(Video item)
        {
            if (item.IsInMixedFolder)
            {
                return Path.ChangeExtension(item.Path, ".xml");
            }

            return Path.Combine(item.ContainingFolderPath, "movie.xml");
        }
    }
}
