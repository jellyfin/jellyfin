using MediaBrowser.Common.Kernel;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    /// <summary>
    /// Interface IHttpServerHandler
    /// </summary>
    public interface IHttpServerHandler
    {
        /// <summary>
        /// Initializes the specified kernel.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        void Initialize(IKernel kernel);
        
        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool HandlesRequest(HttpListenerRequest request);

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        Task ProcessRequest(HttpListenerContext ctx);
    }
}
