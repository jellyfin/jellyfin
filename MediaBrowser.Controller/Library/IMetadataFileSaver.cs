#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    public interface IMetadataFileSaver : IMetadataSaver
    {
        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetSavePath(BaseItem item);
    }
}
