using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Services;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using IRequest = MediaBrowser.Model.Services.IRequest;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;
using StreamWriter = Emby.Server.Implementations.HttpServer.StreamWriter;

namespace Emby.Server.Implementations.HttpServer
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
        private readonly IMemoryStreamFactory _memoryStreamFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultFactory" /> class.
        /// </summary>
        public HttpResultFactory(ILogManager logManager, IFileSystem fileSystem, IJsonSerializer jsonSerializer, IMemoryStreamFactory memoryStreamFactory)
        {
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _memoryStreamFactory = memoryStreamFactory;
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
            return GetHttpResult(content, contentType, true, responseHeaders);
        }

        public object GetRedirectResult(string url)
        {
            var responseHeaders = new Dictionary<string, string>();
            responseHeaders["Location"] = url;

            var result = new HttpResult(new byte[] { }, "text/plain", HttpStatusCode.Redirect);

            AddResponseHeaders(result, responseHeaders);

            return result;
        }

        /// <summary>
        /// Gets the HTTP result.
        /// </summary>
        private IHasHeaders GetHttpResult(object content, string contentType, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
        {
            IHasHeaders result;

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
                        result = new HttpResult(content, contentType, HttpStatusCode.OK);
                    }
                }
            }
            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>();
            }

            string expires;
            if (addCachePrevention && !responseHeaders.TryGetValue("Expires", out expires))
            {
                responseHeaders["Expires"] = "-1";
            }

            AddResponseHeaders(result, responseHeaders);

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

            var optimizedResult = ToOptimizedResult(requestContext, result);

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            if (addCachePrevention)
            {
                responseHeaders["Expires"] = "-1";
            }

            // Apply headers
            var hasHeaders = optimizedResult as IHasHeaders;

            if (hasHeaders != null)
            {
                AddResponseHeaders(hasHeaders, responseHeaders);
            }

            return optimizedResult;
        }

        public static string GetCompressionType(IRequest request)
        {
            var acceptEncoding = request.Headers["Accept-Encoding"];

            if (!string.IsNullOrWhiteSpace(acceptEncoding))
            {
                if (acceptEncoding.Contains("deflate"))
                    return "deflate";

                if (acceptEncoding.Contains("gzip"))
                    return "gzip";
            }

            return null;
        }

        /// <summary>
        /// Returns the optimized result for the IRequestContext. 
        /// Does not use or store results in any cache.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        public object ToOptimizedResult<T>(IRequest request, T dto)
        {
            var compressionType = GetCompressionType(request);
            if (compressionType == null)
            {
                var contentType = request.ResponseContentType;

                switch (GetRealContentType(contentType))
                {
                    case "application/xml":
                    case "text/xml":
                    case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                        return SerializeToXmlString(dto);

                    case "application/json":
                    case "text/json":
                        return _jsonSerializer.SerializeToString(dto);
                }
            }

            // Do not use the memoryStreamFactory here, they don't place nice with compression
            using (var ms = new MemoryStream())
            {
                var contentType = request.ResponseContentType;
                var writerFn = RequestHelper.GetResponseWriter(HttpListenerHost.Instance, contentType);

                writerFn(dto, ms);

                ms.Position = 0;

                var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                return GetCompressedResult(ms, compressionType, responseHeaders, false, request.ResponseContentType).Result;
            }
        }

        private static Stream GetCompressionStream(Stream outputStream, string compressionType)
        {
            if (compressionType == "deflate")
                return new DeflateStream(outputStream, CompressionMode.Compress, true);
            if (compressionType == "gzip")
                return new GZipStream(outputStream, CompressionMode.Compress, true);

            throw new NotSupportedException(compressionType);
        }

        public static string GetRealContentType(string contentType)
        {
            return contentType == null
                       ? null
                       : contentType.Split(';')[0].ToLower().Trim();
        }

        private string SerializeToXmlString(object from)
        {
            using (var ms = new MemoryStream())
            {
                var xwSettings = new XmlWriterSettings();
                xwSettings.Encoding = new UTF8Encoding(false);
                xwSettings.OmitXmlDeclaration = false;

                using (var xw = XmlWriter.Create(ms, xwSettings))
                {
                    var serializer = new DataContractSerializer(from.GetType());
                    serializer.WriteObject(xw, from);
                    xw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(ms);
                    return reader.ReadToEnd();
                }
            }
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
            var hasHeaders = result as IHasHeaders;

            if (hasHeaders != null)
            {
                AddResponseHeaders(hasHeaders, responseHeaders);
                return hasHeaders;
            }

            return GetHttpResult(result, contentType, false, responseHeaders);
        }

        /// <summary>
        /// Pres the process optimized result.
        /// </summary>
        private object GetCachedResult(IRequest requestContext, IDictionary<string, string> responseHeaders, Guid cacheKey, string cacheKeyString, DateTime? lastDateModified, TimeSpan? cacheDuration, string contentType)
        {
            responseHeaders["ETag"] = string.Format("\"{0}\"", cacheKeyString);

            var noCache = (requestContext.Headers.Get("Cache-Control") ?? string.Empty).IndexOf("no-cache", StringComparison.OrdinalIgnoreCase) != -1;

            if (!noCache)
            {
                if (IsNotModified(requestContext, cacheKey, lastDateModified, cacheDuration))
                {
                    AddAgeHeader(responseHeaders, lastDateModified);
                    AddExpiresHeader(responseHeaders, cacheKeyString, cacheDuration, noCache);

                    var result = new HttpResult(new byte[] { }, contentType ?? "text/html", HttpStatusCode.NotModified);

                    AddResponseHeaders(result, responseHeaders);

                    return result;
                }
            }

            AddCachingHeaders(responseHeaders, cacheKeyString, lastDateModified, cacheDuration, noCache);

            return null;
        }

        public Task<object> GetStaticFileResult(IRequest requestContext,
            string path,
            FileShareMode fileShare = FileShareMode.Read)
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

            if (fileShare != FileShareMode.Read && fileShare != FileShareMode.ReadWrite)
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
        private Stream GetFileStream(string path, FileShareMode fileShare)
        {
            return _fileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, fileShare);
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

            var key = cacheKey.ToString("N");

            // See if the result is already cached in the browser
            var result = GetCachedResult(requestContext, options.ResponseHeaders, cacheKey, key, options.DateLastModified, options.CacheDuration, contentType);

            if (result != null)
            {
                return result;
            }

            var compress = ShouldCompressResponse(requestContext, contentType);
            var hasHeaders = await GetStaticResult(requestContext, options, compress).ConfigureAwait(false);
            AddResponseHeaders(hasHeaders, options.ResponseHeaders);

            return hasHeaders;
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
            if (!string.IsNullOrWhiteSpace(requestContext.Headers.Get("Range")))
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

        private async Task<IHasHeaders> GetStaticResult(IRequest requestContext, StaticResultOptions options, bool compress)
        {
            var isHeadRequest = options.IsHeadRequest;
            var factoryFn = options.ContentFactory;
            var contentType = options.ContentType;
            var responseHeaders = options.ResponseHeaders;

            var requestedCompressionType = GetCompressionType(requestContext);

            if (!compress || string.IsNullOrEmpty(requestedCompressionType))
            {
                var rangeHeader = requestContext.Headers.Get("Range");

                if (!isHeadRequest && !string.IsNullOrWhiteSpace(options.Path))
                {
                    return new FileWriter(options.Path, contentType, rangeHeader, _logger, _fileSystem)
                    {
                        OnComplete = options.OnComplete,
                        OnError = options.OnError,
                        FileShare = options.FileShare
                    };
                }

                if (!string.IsNullOrWhiteSpace(rangeHeader))
                {
                    var stream = await factoryFn().ConfigureAwait(false);

                    return new RangeRequestWriter(rangeHeader, stream, contentType, isHeadRequest, _logger)
                    {
                        OnComplete = options.OnComplete
                    };
                }
                else
                {
                    var stream = await factoryFn().ConfigureAwait(false);

                    responseHeaders["Content-Length"] = stream.Length.ToString(UsCulture);

                    if (isHeadRequest)
                    {
                        stream.Dispose();

                        return GetHttpResult(new byte[] { }, contentType, true);
                    }

                    return new StreamWriter(stream, contentType, _logger)
                    {
                        OnComplete = options.OnComplete,
                        OnError = options.OnError
                    };
                }
            }

            using (var stream = await factoryFn().ConfigureAwait(false))
            {
                return await GetCompressedResult(stream, requestedCompressionType, responseHeaders, isHeadRequest, contentType).ConfigureAwait(false);
            }
        }

        private async Task<IHasHeaders> GetCompressedResult(Stream stream,
            string requestedCompressionType,
            IDictionary<string, string> responseHeaders,
            bool isHeadRequest,
            string contentType)
        {
            using (var reader = new MemoryStream())
            {
                await stream.CopyToAsync(reader).ConfigureAwait(false);

                reader.Position = 0;
                var content = reader.ToArray();

                if (content.Length >= 1024)
                {
                    content = Compress(content, requestedCompressionType);
                    responseHeaders["Content-Encoding"] = requestedCompressionType;
                }

                responseHeaders["Vary"] = "Accept-Encoding";
                responseHeaders["Content-Length"] = content.Length.ToString(UsCulture);

                if (isHeadRequest)
                {
                    return GetHttpResult(new byte[] { }, contentType, true);
                }

                return GetHttpResult(content, contentType, true, responseHeaders);
            }
        }

        private byte[] Compress(byte[] bytes, string compressionType)
        {
            if (compressionType == "deflate")
                return Deflate(bytes);

            if (compressionType == "gzip")
                return GZip(bytes);

            throw new NotSupportedException(compressionType);
        }

        private byte[] Deflate(byte[] bytes)
        {
            // In .NET FX incompat-ville, you can't access compressed bytes without closing DeflateStream
            // Which means we must use MemoryStream since you have to use ToArray() on a closed Stream
            using (var ms = new MemoryStream())
            using (var zipStream = new DeflateStream(ms, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Dispose();

                return ms.ToArray();
            }
        }

        private byte[] GZip(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            using (var zipStream = new GZipStream(ms, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
                zipStream.Dispose();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Adds the caching responseHeaders.
        /// </summary>
        private void AddCachingHeaders(IDictionary<string, string> responseHeaders, string cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration, bool noCache)
        {
            // Don't specify both last modified and Etag, unless caching unconditionally. They are redundant
            // https://developers.google.com/speed/docs/best-practices/caching#LeverageBrowserCaching
            if (lastDateModified.HasValue && (string.IsNullOrEmpty(cacheKey) || cacheDuration.HasValue))
            {
                AddAgeHeader(responseHeaders, lastDateModified);
                responseHeaders["Last-Modified"] = lastDateModified.Value.ToString("r");
            }

            if (!noCache && cacheDuration.HasValue)
            {
                responseHeaders["Cache-Control"] = "public, max-age=" + Convert.ToInt32(cacheDuration.Value.TotalSeconds);
            }
            else if (!noCache && !string.IsNullOrEmpty(cacheKey))
            {
                responseHeaders["Cache-Control"] = "public";
            }
            else
            {
                responseHeaders["Cache-Control"] = "no-cache, no-store, must-revalidate";
                responseHeaders["pragma"] = "no-cache, no-store, must-revalidate";
            }

            AddExpiresHeader(responseHeaders, cacheKey, cacheDuration, noCache);
        }

        /// <summary>
        /// Adds the expires header.
        /// </summary>
        private void AddExpiresHeader(IDictionary<string, string> responseHeaders, string cacheKey, TimeSpan? cacheDuration, bool noCache)
        {
            if (!noCache && cacheDuration.HasValue)
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
            //var isNotModified = true;

            var ifModifiedSinceHeader = requestContext.Headers.Get("If-Modified-Since");

            if (!string.IsNullOrEmpty(ifModifiedSinceHeader))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(ifModifiedSinceHeader, out ifModifiedSince))
                {
                    if (IsNotModified(ifModifiedSince.ToUniversalTime(), cacheDuration, lastDateModified))
                    {
                        return true;
                    }
                }
            }

            var ifNoneMatchHeader = requestContext.Headers.Get("If-None-Match");

            // Validate If-None-Match
            if ((cacheKey.HasValue || !string.IsNullOrEmpty(ifNoneMatchHeader)))
            {
                Guid ifNoneMatch;

                ifNoneMatchHeader = (ifNoneMatchHeader ?? string.Empty).Trim('\"');

                if (Guid.TryParse(ifNoneMatchHeader, out ifNoneMatch))
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
        /// <param name="hasHeaders">The has options.</param>
        /// <param name="responseHeaders">The response headers.</param>
        private void AddResponseHeaders(IHasHeaders hasHeaders, IEnumerable<KeyValuePair<string, string>> responseHeaders)
        {
            foreach (var item in responseHeaders)
            {
                hasHeaders.Headers[item.Key] = item.Value;
            }
        }
    }
}