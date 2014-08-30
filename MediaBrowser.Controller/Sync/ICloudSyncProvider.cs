
namespace MediaBrowser.Controller.Sync
{
    public interface ICloudSyncProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}
