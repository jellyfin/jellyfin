using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommonIO;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class HttpResultFactory
    /// </summary>
    public class HttpResultFactory : IHttpResultFactory
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultFactory" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        public HttpResultFactory(ILogManager logManager, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger("HttpResultFactory");
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        public object GetResult(object content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            return GetHttpResult(content, contentType, responseHeaders);
        }

        /// <summary>
        /// Gets the HTTP result.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>IHasOptions.</returns>
        private IHasOptions GetHttpResult(object content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            IHasOptions result;

            var stream = content as Stream;

            if (stream != null)
            {
                result = new StreamWriter(stream, contentType, _logger);
            }

            else
            {
                var bytes = content as byte[];

                if (bytes != null)
                {
                    result = new StreamWriter(bytes, contentType, _logger);
                }
                else
                {
                    var text = content as string;

                    if (text != null)
                    {
                        result = new StreamWriter(Encoding.UTF8.GetBytes(text), contentType, _logger);
                    }
                    else
                    {
                        result = new HttpResult(content, contentType);
                    }
                }
            }

            if (responseHeaders != null)
            {
                AddResponseHeaders(result, responseHeaders);
            }

            return result;
        }

        /// <summary>
        /// Gets the optimized result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="result">The result.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">result</exception>
        public object GetOptimizedResult<T>(IRequest requestContext, T result, IDictionary<string, string> responseHeaders = null)
            where T : class
        {
            return GetOptimizedResultInternal<T>(requestContext, result, true, responseHeaders);
        }

        private object GetOptimizedResultInternal<T>(IRequest requestContext, T result, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
          where T : class
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            var optimizedResult = requestContext.ToOptimizedResult(result);

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            if (addCachePrevention)
            {
                responseHeaders["Expires"] = "-1";
            }

            // Apply headers
            var hasOptions = optimizedResult as IHasOptions;

            if (hasOptions != null)
            {
                AddResponseHeaders(hasOptions, responseHeaders);
            }

            return optimizedResult;
        }
        
        /// <summary>
        /// Gets the optimized result using cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="factoryFn">The factory fn.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">cacheKey
        /// or
        /// factoryFn</exception>
        public object GetOptimizedResultUsingCache<T>(IRequest requestContext, Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, IDictionary<string, string> responseHeaders = null)
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

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            // See if the result is already cached in the browser
            var result = GetCachedResult(requestContext, responseHeaders, cacheKey, key, lastDateModified, cacheDuration, null);

            if (result != null)
            {
                return result;
            }

            return GetOptimizedResultInternal(requestContext, factoryFn(), false, responseHeaders);
        }

        /// <summary>
        /// To the cached result.
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
        /// <exception cref="System.ArgumentNullException">cacheKey</exception>
        public object GetCachedResult<T>(IRequest requestContext, Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, Func<T> factoryFn, string contentType, IDictionary<string, string> responseHeaders = null)
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

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            // See if the result is already cached in the browser
            var result = GetCachedResult(requestContext, responseHeaders, cacheKey, key, lastDateModified, cacheDuration, contentType);

            if (result != null)
            {
                return result;
            }

            result = factoryFn();

            // Apply caching headers
            var hasOptions = result as IHasOptions;

            if (hasOptions != null)
            {
                AddResponseHeaders(hasOptions, responseHeaders);
                return hasOptions;
            }

            IHasOptions httpResult;

            var stream = result as Stream;

            if (stream != null)
            {
                httpResult = new StreamWriter(stream, contentType, _logger);
            }
            else
            {
                // Otherwise wrap into an HttpResult
                httpResult = new HttpResult(result, contentType ?? "text/html", HttpStatusCode.NotModified);
            }

            AddResponseHeaders(httpResult, responseHeaders);

            return httpResult;
        }

        /// <summary>
        /// Pres the process optimized result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="responseHeaders">The responseHeaders.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="cacheKeyString">The cache key string.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>System.Object.</returns>
        private object GetCachedResult(IRequest requestContext, IDictionary<string, string> responseHeaders, Guid cacheKey, string cacheKeyString, DateTime? lastDateModified, TimeSpan? cacheDuration, string contentType)
        {
            responseHeaders["ETag"] = cacheKeyString;

            if (IsNotModified(requestContext, cacheKey, lastDateModified, cacheDuration))
            {
                AddAgeHeader(responseHeaders, lastDateModified);
                AddExpiresHeader(responseHeaders, cacheKeyString, cacheDuration);

                var result = new HttpResult(new byte[] { }, contentType ?? "text/html", HttpStatusCode.NotModified);

                AddResponseHeaders(result, responseHeaders);

                return result;
            }

            AddCachingHeaders(responseHeaders, cacheKeyString, lastDateModified, cacheDuration);

            return null;
        }

        public Task<object> GetStaticFileResult(IRequest requestContext,
            string path,
            FileShare fileShare = FileShare.Read)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            return GetStaticFileResult(requestContext, new StaticFileResultOptions
            {
                Path = path,
                FileShare = fileShare
            });
        }

        public Task<object> GetStaticFileResult(IRequest requestContext,
            StaticFileResultOptions options)
        {
            var path = options.Path;
            var fileShare = options.FileShare;

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (fileShare != FileShare.Read && fileShare != FileShare.ReadWrite)
            {
                throw new ArgumentException("FileShare must be either Read or ReadWrite");
            }

            if (string.IsNullOrWhiteSpace(options.ContentType))
            {
                options.ContentType = MimeTypes.GetMimeType(path);
            }

            if (!options.DateLastModified.HasValue)
            {
                options.DateLastModified = _fileSystem.GetLastWriteTimeUtc(path);
            }

            var cacheKey = path + options.DateLastModified.Value.Ticks;

            options.CacheKey = cacheKey.GetMD5();
            options.ContentFactory = () => Task.FromResult(GetFileStream(path, fileShare));

            return GetStaticResult(requestContext, options);
        }

        /// <summary>
        /// Gets the file stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileShare">The file share.</param>
        /// <returns>Stream.</returns>
        private Stream GetFileStream(string path, FileShare fileShare)
        {
            return _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, fileShare);
        }

        public Task<object> GetStaticResult(IRequest requestContext,
            Guid cacheKey,
            DateTime? lastDateModified,
            TimeSpan? cacheDuration,
            string contentType,
            Func<Task<Stream>> factoryFn,
            IDictionary<string, string> responseHeaders = null,
            bool isHeadRequest = false)
        {
            return GetStaticResult(requestContext, new StaticResultOptions
            {
                CacheDuration = cacheDuration,
                CacheKey = cacheKey,
                ContentFactory = factoryFn,
                ContentType = contentType,
                DateLastModified = lastDateModified,
                IsHeadRequest = isHeadRequest,
                ResponseHeaders = responseHeaders
            });
        }

        public async Task<object> GetStaticResult(IRequest requestContext, StaticResultOptions options)
        {
            var cacheKey = options.CacheKey;
            options.ResponseHeaders = options.ResponseHeaders ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var contentType = options.ContentType;

            if (cacheKey == Guid.Empty)
            {
                throw new ArgumentNullException("cacheKey");
            }
            if (options.ContentFactory == null)
            {
                throw new ArgumentNullException("factoryFn");
            }

            var key = cacheKey.ToString("N");

            // See if the result is already cached in the browser
            var result = GetCachedResult(requestContext, options.ResponseHeaders, cacheKey, key, options.DateLastModified, options.CacheDuration, contentType);

            if (result != null)
            {
                return result;
            }

            var compress = ShouldCompressResponse(requestContext, contentType);
            var hasOptions = await GetStaticResult(requestContext, options, compress).ConfigureAwait(false);
            AddResponseHeaders(hasOptions, options.ResponseHeaders);

            return hasOptions;
        }

        /// <summary>
        /// Shoulds the compress response.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ShouldCompressResponse(IRequest requestContext, string contentType)
        {
            // It will take some work to support compression with byte range requests
            if (!string.IsNullOrEmpty(requestContext.GetHeader("Range")))
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
                if (string.Equals(contentType, "application/x-javascript", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (string.Equals(contentType, "application/xml", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private async Task<IHasOptions> GetStaticResult(IRequest requestContext, StaticResultOptions options, bool compress)
        {
            var isHeadRequest = options.IsHeadRequest;
            var factoryFn = options.ContentFactory;
            var contentType = options.ContentType;
            var responseHeaders = options.ResponseHeaders;

            var requestedCompressionType = requestContext.GetCompressionType();

            if (!compress || string.IsNullOrEmpty(requestedCompressionType))
            {
                var rangeHeader = requestContext.GetHeader("Range");

                var stream = await factoryFn().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(rangeHeader))
                {
                    return new RangeRequestWriter(rangeHeader, stream, contentType, isHeadRequest, _logger)
                    {
                        OnComplete = options.OnComplete
                    };
                }

                responseHeaders["Content-Length"] = stream.Length.ToString(UsCulture);

                if (isHeadRequest)
                {
                    stream.Dispose();

                    return GetHttpResult(new byte[] { }, contentType);
                }

                return new StreamWriter(stream, contentType, _logger)
                {
                    OnComplete = options.OnComplete,
                    OnError = options.OnError
                };
            }

            string content;

            using (var stream = await factoryFn().ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            var contents = content.Compress(requestedCompressionType);

            responseHeaders["Content-Length"] = contents.Length.ToString(UsCulture);

            if (isHeadRequest)
            {
                return GetHttpResult(new byte[] { }, contentType);
            }

            return new CompressedResult(contents, requestedCompressionType, contentType);
        }

        /// <summary>
        /// Adds the caching responseHeaders.
        /// </summary>
        /// <param name="responseHeaders">The responseHeaders.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        private void AddCachingHeaders(IDictionary<string, string> responseHeaders, string cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            // Don't specify both last modified and Etag, unless caching unconditionally. They are redundant
            // https://developers.google.com/speed/docs/best-practices/caching#LeverageBrowserCaching
            if (lastDateModified.HasValue && (string.IsNullOrEmpty(cacheKey) || cacheDuration.HasValue))
            {
                AddAgeHeader(responseHeaders, lastDateModified);
                responseHeaders["LastModified"] = lastDateModified.Value.ToString("r");
            }

            if (cacheDuration.HasValue)
            {
                responseHeaders["Cache-Control"] = "public, max-age=" + Convert.ToInt32(cacheDuration.Value.TotalSeconds);
            }
            else if (!string.IsNullOrEmpty(cacheKey))
            {
                responseHeaders["Cache-Control"] = "public";
            }
            else
            {
                responseHeaders["Cache-Control"] = "no-cache, no-store, must-revalidate";
                responseHeaders["pragma"] = "no-cache, no-store, must-revalidate";
            }

            AddExpiresHeader(responseHeaders, cacheKey, cacheDuration);
        }

        /// <summary>
        /// Adds the expires header.
        /// </summary>
        /// <param name="responseHeaders">The responseHeaders.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        private void AddExpiresHeader(IDictionary<string, string> responseHeaders, string cacheKey, TimeSpan? cacheDuration)
        {
            if (cacheDuration.HasValue)
            {
                responseHeaders["Expires"] = DateTime.UtcNow.Add(cacheDuration.Value).ToString("r");
            }
            else if (string.IsNullOrEmpty(cacheKey))
            {
                responseHeaders["Expires"] = "-1";
            }
        }

        /// <summary>
        /// Adds the age header.
        /// </summary>
        /// <param name="responseHeaders">The responseHeaders.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        private void AddAgeHeader(IDictionary<string, string> responseHeaders, DateTime? lastDateModified)
        {
            if (lastDateModified.HasValue)
            {
                responseHeaders["Age"] = Convert.ToInt64((DateTime.UtcNow - lastDateModified.Value).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }
        }
        /// <summary>
        /// Determines whether [is not modified] [the specified cache key].
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <returns><c>true</c> if [is not modified] [the specified cache key]; otherwise, <c>false</c>.</returns>
        private bool IsNotModified(IRequest requestContext, Guid? cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            var isNotModified = true;

            var ifModifiedSinceHeader = requestContext.GetHeader("If-Modified-Since");

            if (!string.IsNullOrEmpty(ifModifiedSinceHeader))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(ifModifiedSinceHeader, out ifModifiedSince))
                {
                    isNotModified = IsNotModified(ifModifiedSince.ToUniversalTime(), cacheDuration, lastDateModified);
                }
            }

            var ifNoneMatchHeader = requestContext.GetHeader("If-None-Match");

            // Validate If-None-Match
            if (isNotModified && (cacheKey.HasValue || !string.IsNullOrEmpty(ifNoneMatchHeader)))
            {
                Guid ifNoneMatch;

                if (Guid.TryParse(ifNoneMatchHeader ?? string.Empty, out ifNoneMatch))
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
        /// Adds the response headers.
        /// </summary>
        /// <param name="hasOptions">The has options.</param>
        /// <param name="responseHeaders">The response headers.</param>
        private void AddResponseHeaders(IHasOptions hasOptions, IEnumerable<KeyValuePair<string, string>> responseHeaders)
        {
            foreach (var item in responseHeaders)
            {
                hasOptions.Options[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Gets the error result.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        public void ThrowError(int statusCode, string errorMessage, IDictionary<string, string> responseHeaders = null)
        {
            var error = new HttpError
            {
                Status = statusCode,
                ErrorCode = errorMessage
            };

            if (responseHeaders != null)
            {
                AddResponseHeaders(error, responseHeaders);
            }

            throw error;
        }

        public object GetAsyncStreamWriter(Func<Stream, Task> streamWriter, IDictionary<string, string> responseHeaders = null)
        {
            return new AsyncStreamWriterFunc(streamWriter, responseHeaders);
        }
    }
}