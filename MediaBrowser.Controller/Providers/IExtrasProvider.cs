using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public interface IExtrasProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Supports(IHasMetadata item);
    }

    public enum ExtraSource
    {
        Local = 1,
        Metadata = 2,
        Remote = 3
    }

    public class ExtraInfo
    {
        public string Path { get; set; }

        public LocationType LocationType { get; set; }

        public bool IsDownloadable { get; set; }

        public ExtraType ExtraType { get; set; }
    }
}
