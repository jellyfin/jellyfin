using MediaBrowser.Controller.Entities;
using System.ComponentModel.Composition;

namespace MediaBrowser.Plugins.Trailers.Entities
{
    /// <summary>
    /// Class TrailerCollectionFolder
    /// </summary>
    [Export(typeof(BasePluginFolder))]
    class TrailerCollectionFolder : BasePluginFolder
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return Plugin.Instance.Configuration.FolderName;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public override string Path
        {
            get { return Plugin.Instance.DownloadPath; }
        }
    }
}
