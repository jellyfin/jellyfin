using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
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