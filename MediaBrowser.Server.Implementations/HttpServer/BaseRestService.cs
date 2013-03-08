using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MimeTypes = MediaBrowser.Common.Net.MimeTypes;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class BaseRestService
    /// </summary>
    public class BaseRestService : Service, IRestfulService
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether this instance is range request.
        /// </summary>
        /// <value><c>true</c> if this instance is range request; otherwise, <c>false</c>.</value>
        protected bool IsRangeRequest
        {
            get
            {
                return Request.Headers.AllKeys.Contains("Range");
            }
        }

        /// <summary>
        /// To the optimized result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">result</exception>
        protected object ToOptimizedResult<T>(T result)
            where T : class
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            
            Response.AddHeader("Vary", "Accept-Encoding");

            return RequestContext.ToOptimizedResult(result);
        }

        /// <summary>
        /// To the optimized result using cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">cacheKey</exception>
        protected object ToOptimizedResultUsingCache<T>(Guid cacheKey, DateTime lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn)
               where T : class
        {
            if (cacheKey == Guid.Empty)
            {
                throw new ArgumentNullException("cacheKey");
            }
            if (factoryFn == null)
            {
                throw new ArgumentNullException("factoryFn");
            }

            var key = cacheKey.ToString("N");

            var result = PreProcessCachedResult(cacheKey, key, lastDateModified, cacheDuration);

            if (result != null)
            {
                // Return null so that service stack won't do anything
                return null;
            }

            return ToOptimizedResult(factoryFn());
        }

        /// <summary>
        /// To the cached result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">cacheKey</exception>
        protected object ToCachedResult<T>(Guid cacheKey, DateTime lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, string contentType)
          where T : class
        {
            if (cacheKey == Guid.Empty)
            {
                throw new ArgumentNullException("cacheKey");
            }
            if (factoryFn == null)
            {
                throw new ArgumentNullException("factoryFn");
            }

            Response.ContentType = contentType;
            
            var key = cacheKey.ToString("N");

            var result = PreProcessCachedResult(cacheKey, key, lastDateModified, cacheDuration);

            if (result != null)
            {
                // Return null so that service stack won't do anything
                return null;
            }

            return factoryFn();
        }
        
        /// <summary>
        /// To the static file result.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        protected object ToStaticFileResult(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var dateModified = File.GetLastWriteTimeUtc(path);

            var cacheKey = path + dateModified.Ticks;

            return ToStaticResult(cacheKey.GetMD5(), dateModified, null, MimeTypes.GetMimeType(path), () => Task.FromResult(GetFileStream(path)));
        }

        /// <summary>
        /// Gets the file stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Stream.</returns>
        private Stream GetFileStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);
        }
        
        /// <summary>
        /// To the static result.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">cacheKey</exception>
        protected object ToStaticResult(Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, string contentType, Func<Task<Stream>> factoryFn)
        {
            if (cacheKey == Guid.Empty)
            {
                throw new ArgumentNullException("cacheKey");
            }
            if (factoryFn == null)
            {
                throw new ArgumentNullException("factoryFn");
            }

            var key = cacheKey.ToString("N");

            Response.ContentType = contentType;
            
            var result = PreProcessCachedResult(cacheKey, key, lastDateModified, cacheDuration);

            if (result != null)
            {
                // Return null so that service stack won't do anything
                return null;
            }

            var compress = ShouldCompressResponse(contentType);

            if (compress)
            {
                Response.AddHeader("Vary", "Accept-Encoding");
            }

            return ToStaticResult(contentType, factoryFn, compress).Result;
        }

        /// <summary>
        /// Shoulds the compress response.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ShouldCompressResponse(string contentType)
        {
            // It will take some work to support compression with byte range requests
            if (IsRangeRequest)
            {
                return false;
            }

            // Don't compress media
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Don't compress images
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (contentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// To the static result.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="compress">if set to <c>true</c> [compress].</param>
        /// <returns>System.Object.</returns>
        private async Task<object> ToStaticResult(string contentType, Func<Task<Stream>> factoryFn, bool compress)
        {
            if (!compress || string.IsNullOrEmpty(RequestContext.CompressionType))
            {
                Response.ContentType = contentType;

                var stream = await factoryFn().ConfigureAwait(false);

                return new StreamWriter(stream);
            }

            string content;

            using (var stream = await factoryFn().ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            var contents = content.Compress(RequestContext.CompressionType);

            return new CompressedResult(contents, RequestContext.CompressionType, contentType);
        }

        /// <summary>
        /// Pres the process optimized result.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="cacheKeyString">The cache key string.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <returns>System.Object.</returns>
        private object PreProcessCachedResult(Guid cacheKey, string cacheKeyString, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            Response.AddHeader("ETag", cacheKeyString);

            if (IsNotModified(cacheKey, lastDateModified, cacheDuration))
            {
                AddAgeHeader(lastDateModified);
                AddExpiresHeader(cacheKeyString, cacheDuration);
                //ctx.Response.SendChunked = false;

                Response.StatusCode = 304;

                return new byte[]{};
            }

            SetCachingHeaders(cacheKeyString, lastDateModified, cacheDuration);

            return null;
        }

        /// <summary>
        /// Determines whether [is not modified] [the specified cache key].
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <returns><c>true</c> if [is not modified] [the specified cache key]; otherwise, <c>false</c>.</returns>
        private bool IsNotModified(Guid? cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            var isNotModified = true;

            if (Request.Headers.AllKeys.Contains("If-Modified-Since"))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(Request.Headers["If-Modified-Since"], out ifModifiedSince))
                {
                    isNotModified = IsNotModified(ifModifiedSince.ToUniversalTime(), cacheDuration, lastDateModified);
                }
            }

            // Validate If-None-Match
            if (isNotModified && (cacheKey.HasValue || !string.IsNullOrEmpty(Request.Headers["If-None-Match"])))
            {
                Guid ifNoneMatch;

                if (Guid.TryParse(Request.Headers["If-None-Match"] ?? string.Empty, out ifNoneMatch))
                {
                    if (cacheKey.HasValue && cacheKey.Value == ifNoneMatch)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether [is not modified] [the specified if modified since].
        /// </summary>
        /// <param name="ifModifiedSince">If modified since.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns><c>true</c> if [is not modified] [the specified if modified since]; otherwise, <c>false</c>.</returns>
        private bool IsNotModified(DateTime ifModifiedSince, TimeSpan? cacheDuration, DateTime? dateModified)
        {
            if (dateModified.HasValue)
            {
                var lastModified = NormalizeDateForComparison(dateModified.Value);
                ifModifiedSince = NormalizeDateForComparison(ifModifiedSince);

                return lastModified <= ifModifiedSince;
            }

            if (cacheDuration.HasValue)
            {
                var cacheExpirationDate = ifModifiedSince.Add(cacheDuration.Value);

                if (DateTime.UtcNow < cacheExpirationDate)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// When the browser sends the IfModifiedDate, it's precision is limited to seconds, so this will account for that
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        private DateTime NormalizeDateForComparison(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        /// <summary>
        /// Sets the caching headers.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        private void SetCachingHeaders(string cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            // Don't specify both last modified and Etag, unless caching unconditionally. They are redundant
            // https://developers.google.com/speed/docs/best-practices/caching#LeverageBrowserCaching
            if (lastDateModified.HasValue && (string.IsNullOrEmpty(cacheKey) || cacheDuration.HasValue))
            {
                AddAgeHeader(lastDateModified);
                Response.AddHeader("LastModified", lastDateModified.Value.ToString("r"));
            }

            if (cacheDuration.HasValue)
            {
                Response.AddHeader("Cache-Control", "public, max-age=" + Convert.ToInt32(cacheDuration.Value.TotalSeconds));
            }
            else if (!string.IsNullOrEmpty(cacheKey))
            {
                Response.AddHeader("Cache-Control", "public");
            }
            else
            {
                Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                Response.AddHeader("pragma", "no-cache, no-store, must-revalidate");
            }

            AddExpiresHeader(cacheKey, cacheDuration);
        }

        /// <summary>
        /// Adds the expires header.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        private void AddExpiresHeader(string cacheKey, TimeSpan? cacheDuration)
        {
            if (cacheDuration.HasValue)
            {
                Response.AddHeader("Expires", DateTime.UtcNow.Add(cacheDuration.Value).ToString("r"));
            }
            else if (string.IsNullOrEmpty(cacheKey))
            {
                Response.AddHeader("Expires", "-1");
            }
        }

        /// <summary>
        /// Adds the age header.
        /// </summary>
        /// <param name="lastDateModified">The last date modified.</param>
        private void AddAgeHeader(DateTime? lastDateModified)
        {
            if (lastDateModified.HasValue)
            {
                Response.AddHeader("Age", Convert.ToInt64((DateTime.UtcNow - lastDateModified.Value).TotalSeconds).ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets the routes.
        /// </summary>
        /// <returns>IEnumerable{RouteInfo}.</returns>
        public IEnumerable<RouteInfo> GetRoutes()
        {
            return new RouteInfo[] {};
        }
    }
}
