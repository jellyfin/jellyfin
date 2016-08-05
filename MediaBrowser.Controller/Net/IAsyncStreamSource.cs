using ServiceStack.Web;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IAsyncStreamSource
    /// Enables asynchronous writing to http resonse streams
    /// </summary>
    public interface IAsyncStreamSource
    {
        /// <summary>
        /// Asynchronously write to the response stream.
        /// </summary>
        Task WriteToAsync(Stream responseStream);
    }
}
