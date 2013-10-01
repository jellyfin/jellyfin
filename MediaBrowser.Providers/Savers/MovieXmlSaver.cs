using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.Providers.Savers
{
    /// <summary>
    /// Saves movie.xml for movies, trailers and music videos
    /// </summary>
    public class MovieXmlSaver : IMetadataSaver
    {
        private readonly IServerConfigurationManager _config;
        private readonly IItemRepository _itemRepository;

        public MovieXmlSaver(IServerConfigurationManager config, IItemRepository itemRepository)
        {
            _config = config;
            _itemRepository = itemRepository;
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            var wasMetadataEdited = (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit;
            var wasMetadataDownloaded = (updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload;

            // If new metadata has been downloaded and save local is on, OR metadata was manually edited, proceed
            if ((_config.Configuration.SaveLocalMeta && (wasMetadataEdited || wasMetadataDownloaded)) || wasMetadataEdited)
            {
                var trailer = item as Trailer;

                // Don't support local trailers
                if (trailer != null)
                {
                    return !trailer.IsLocalTrailer;
                }

                return item is Movie || item is MusicVideo || item is AdultVideo;
            }

            return false;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<Title>");

            XmlSaverHelpers.AddCommonNodes(item, builder);

            if (item.CommunityRating.HasValue)
            {
                builder.Append("<IMDBrating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</IMDBrating>");
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                builder.Append("<Description><![CDATA[" + item.Overview + "]]></Description>");
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (!string.IsNullOrEmpty(musicVideo.Artist))
                {
                    builder.Append("<Artist>" + SecurityElement.Escape(musicVideo.Artist) + "</Artist>");
                }
                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    builder.Append("<Album>" + SecurityElement.Escape(musicVideo.Album) + "</Album>");
                }
            }

            var video = (Video)item;

            XmlSaverHelpers.AddMediaInfo(video, builder, _itemRepository);

            builder.Append("</Title>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "IMDBrating",
                    "Description",
                    "Artist",
                    "Album"
                });

            // Set last refreshed so that the provider doesn't trigger after the file save
            MovieProviderFromXml.Current.SetLastRefreshed(item, DateTime.UtcNow);
        }

        public string GetSavePath(BaseItem item)
        {
            return GetMovieSavePath(item);
        }

        public static string GetMovieSavePath(BaseItem item)
        {
            if (item.IsInMixedFolder)
            {
                return Path.ChangeExtension(item.Path, ".xml");
            }

            var filename = GetXmlFilename(item);

            return Path.Combine(item.MetaLocation, filename);
        }

        private static string GetXmlFilename(BaseItem item)
        {
            const string filename = "movie.xml";

            return Path.Combine(item.MetaLocation, filename);
        }
    }
}
