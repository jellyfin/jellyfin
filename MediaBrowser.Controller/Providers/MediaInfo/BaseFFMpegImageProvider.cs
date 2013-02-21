using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    public abstract class BaseFFMpegImageProvider<T> : BaseFFMpegProvider<T>
        where T : BaseItem
    {
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
