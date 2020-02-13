using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpResultFactory
    /// </summary>
    public interface IHttpResultFactory
    {
        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        object GetResult(string content, string contentType, IDictionary<string, string> responseHeaders = null);

        object GetResult(IRequest requestContext, byte[] content, string contentType, IDictionary<string, string> responseHeaders = null);
        object GetResult(IRequest requestContext, Stream content, string contentType, IDictionary<string, string> responseHeaders = null);
        object GetResult(IRequest requestContext, string content, string contentType, IDictionary<string, string> responseHeaders = null);

        object GetRedirectResult(string url);

        object GetResult<T>(IRequest requestContext, T result, IDictionary<string, string> responseHeaders = null)
            where T : class;

        /// <summary>
        /// Gets the static result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>System.Object.</returns>
        Task<object> GetStaticResult(IRequest requestContext,
            Guid cacheKey,
            DateTime? lastDateModified,
            TimeSpan? cacheDuration,
            string contentType, Func<Task<Stream>> factoryFn,
            IDictionary<string, string> responseHeaders = null,
            bool isHeadRequest = false);

        /// <summary>
        /// Gets the static result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.Object.</returns>
        Task<object> GetStaticResult(IRequest requestContext, StaticResultOptions options);

        /// <summary>
        /// Gets the static file result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="path">The path.</param>
        /// <param name="fileShare">The file share.</param>
        /// <returns>System.Object.</returns>
        Task<object> GetStaticFileResult(IRequest requestContext, string path, FileShare fileShare = FileShare.Read);

        /// <summary>
        /// Gets the static file result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.Object.</returns>
        Task<object> GetStaticFileResult(IRequest requestContext,
            StaticFileResultOptions options);
    }
}
