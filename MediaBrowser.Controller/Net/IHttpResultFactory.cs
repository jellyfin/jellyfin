using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// Interface IHttpResultFactory
    /// </summary>
    public interface IHttpResultFactory
    {
        /// <summary>
        /// Throws the error.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="responseHeaders">The response headers.</param>
        void ThrowError(int statusCode, string errorMessage, IDictionary<string, string> responseHeaders = null);
        
        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        object GetResult(object content, string contentType, IDictionary<string,string> responseHeaders = null);

        object GetAsyncStreamWriter(Func<Stream,Task> streamWriter, IDictionary<string, string> responseHeaders = null);

        /// <summary>
        /// Gets the optimized result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="result">The result.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        object GetOptimizedResult<T>(IRequest requestContext, T result, IDictionary<string, string> responseHeaders = null)
            where T : class;

        /// <summary>
        /// Gets the optimized result using cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory function that creates the response object.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        object GetOptimizedResultUsingCache<T>(IRequest requestContext, Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, IDictionary<string, string> responseHeaders = null)
            where T : class;

        /// <summary>
        /// Gets the cached result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        object GetCachedResult<T>(IRequest requestContext, Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, string contentType, IDictionary<string, string> responseHeaders = null)
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
