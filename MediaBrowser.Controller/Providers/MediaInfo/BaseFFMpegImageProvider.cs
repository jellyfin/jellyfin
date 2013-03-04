using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    public abstract class BaseFFMpegImageProvider<T> : BaseFFMpegProvider<T>
        where T : BaseItem
    {
        protected BaseFFMpegImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager) : base(logManager, configurationManager)
        {
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }
    }
}
