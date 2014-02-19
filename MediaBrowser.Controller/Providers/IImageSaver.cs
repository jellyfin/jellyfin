using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public interface IImageSaver
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }

    public interface IImageFileSaver : IImageSaver
    {
        /// <summary>
        /// Gets the save paths.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="format">The format.</param>
        /// <param name="index">The index.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        IEnumerable<string> GetSavePaths(IHasImages item, ImageType type, ImageFormat format, int index);
    }
}
