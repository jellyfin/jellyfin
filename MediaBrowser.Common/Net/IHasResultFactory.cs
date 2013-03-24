using ServiceStack.ServiceHost;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IHasResultFactory
    /// Services that require a ResultFactory should implement this
    /// </summary>
    public interface IHasResultFactory : IRequiresRequestContext
    {
        /// <summary>
        /// Gets or sets the result factory.
        /// </summary>
        /// <value>The result factory.</value>
        IHttpResultFactory ResultFactory { get; set; }
    }
}
