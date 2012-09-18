using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Drawing;

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Provides a base image processor class that plugins can use to process images as they are being writen to http responses
    /// Since this is completely modular with MEF, a plugin only needs to have a subclass in their assembly with the following attribute on the class:
    /// [Export(typeof(BaseImageProcessor))]
    /// This will require a reference to System.ComponentModel.Composition
    /// </summary>
    public abstract class BaseImageProcessor
    {
        /// <summary>
        /// Processes the primary image for a BaseEntity (Person, Studio, User, etc)
        /// </summary>
        /// <param name="bitmap">The bitmap holding the original image, after re-sizing</param>
        /// <param name="graphics">The graphics surface on which the output is drawn</param>
        /// <param name="entity">The entity that owns the image</param>
        public abstract void ProcessImage(Bitmap bitmap, Graphics graphics, BaseEntity entity);

        /// <summary>
        /// Processes an image for a BaseItem
        /// </summary>
        /// <param name="bitmap">The bitmap holding the original image, after re-sizing</param>
        /// <param name="graphics">The graphics surface on which the output is drawn</param>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        public abstract void ProcessImage(Bitmap bitmap, Graphics graphics, BaseItem entity, ImageType imageType, int imageIndex);
    }
}
