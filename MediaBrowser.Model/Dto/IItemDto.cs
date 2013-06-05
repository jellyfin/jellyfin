
namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Interface IItemDto
    /// </summary>
    public interface IItemDto
    {
        /// <summary>
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the original primary image aspect ratio.
        /// </summary>
        /// <value>The original primary image aspect ratio.</value>
        double? OriginalPrimaryImageAspectRatio { get; set; }
    }
}
