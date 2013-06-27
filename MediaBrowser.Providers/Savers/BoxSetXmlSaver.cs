using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Providers.Savers
{
    public class BoxSetXmlSaver : IMetadataSaver
    {
        private readonly IServerConfigurationManager _config;

        public BoxSetXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Supports(BaseItem item)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            if (!_config.Configuration.SaveLocalMeta)
            {
                return false;
            }

            return item is BoxSet;
        }

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<Item>");

            XmlSaverHelpers.AddCommonNodes(item, builder);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new string[] { });
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string GetSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "collection.xml");
        }
    }
}
