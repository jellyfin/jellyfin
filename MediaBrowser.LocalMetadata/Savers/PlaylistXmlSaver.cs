using System.Security;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.LocalMetadata.Savers
{
    public class PlaylistXmlSaver : IMetadataFileSaver
    {
        public string Name
        {
            get
            {
                return "Media Browser Xml";
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

            return item is Playlist && updateType >= ItemUpdateType.MetadataImport;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var playlist = (Playlist)item;

            var builder = new StringBuilder();

            builder.Append("<Item>");

            if (!string.IsNullOrEmpty(playlist.OwnerUserId))
            {
                builder.Append("<OwnerUserId>" + SecurityElement.Escape(playlist.OwnerUserId) + "</OwnerUserId>");
            }

            if (!string.IsNullOrEmpty(playlist.PlaylistMediaType))
            {
                builder.Append("<PlaylistMediaType>" + SecurityElement.Escape(playlist.PlaylistMediaType) + "</PlaylistMediaType>");
            }
            
            XmlSaverHelpers.AddCommonNodes(playlist, builder);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
            {
                "OwnerUserId",
                "PlaylistMediaType"

            });
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "playlist.xml");
        }
    }
}
