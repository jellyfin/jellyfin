using Jellyfin.Model.Services;

namespace Jellyfin.Controller.Net
{
    /// <summary>
    /// Interface IHasResultFactory
    /// Services that require a ResultFactory should implement this
    /// </summary>
    public interface IHasResultFactory : IRequiresRequest
    {
        /// <summary>
        /// Gets or sets the result factory.
        /// </summary>
        /// <value>The result factory.</value>
        IHttpResultFactory ResultFactory { get; set; }
    }
}
