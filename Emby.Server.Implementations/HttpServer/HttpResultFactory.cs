using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using Microsoft.Extensions.Logging;
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
using Emby.Server.Implementations.Services;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using IRequest = MediaBrowser.Model.Services.IRequest;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

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

        private IBrotliCompressor _brotliCompressor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultFactory" /> class.
        /// </summary>
        public HttpResultFactory(ILoggerFactory loggerfactory, IFileSystem fileSystem, IJsonSerializer jsonSerializer, IBrotliCompressor brotliCompressor)
        {
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _brotliCompressor = brotliCompressor;
            _logger = loggerfactory.CreateLogger("HttpResultFactory");
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <returns>System.Object.</returns>
        public object GetResult(IRequest requestContext, byte[] content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            return GetHttpResult(requestContext, content, contentType, true, responseHeaders);
        }

        public object GetResult(string content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            return GetHttpResult(null, content, contentType, true, responseHeaders);
        }

        public object GetResult(IRequest requestContext, Stream content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            return GetHttpResult(requestContext, content, contentType, true, responseHeaders);
        }

        public object GetResult(IRequest requestContext, string content, string contentType, IDictionary<string, string> responseHeaders = null)
        {
            return GetHttpResult(requestContext, content, contentType, true, responseHeaders);
        }

        public object GetRedirectResult(string url)
        {
            var responseHeaders = new Dictionary<string, string>();
            responseHeaders["Location"] = url;

            var result = new HttpResult(Array.Empty<byte>(), "text/plain", HttpStatusCode.Redirect);

            AddResponseHeaders(result, responseHeaders);

            return result;
        }

        /// <summary>
        /// Gets the HTTP result.
        /// </summary>
        private IHasHeaders GetHttpResult(IRequest requestContext, Stream content, string contentType, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
        {
            var result = new StreamWriter(content, contentType, _logger);

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
        /// Gets the HTTP result.
        /// </summary>
        private IHasHeaders GetHttpResult(IRequest requestContext, byte[] content, string contentType, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
        {
            string compressionType = null;
            bool isHeadRequest = false;

            if (requestContext != null) {
                compressionType = GetCompressionType(requestContext, content, contentType);
                isHeadRequest = string.Equals(requestContext.Verb, "head", StringComparison.OrdinalIgnoreCase);
            }

            IHasHeaders result;
            if (string.IsNullOrEmpty(compressionType))
            {
                var contentLength = content.Length;

                if (isHeadRequest)
                {
                    content = Array.Empty<byte>();
                }

                result = new StreamWriter(content, contentType, contentLength, _logger);
            }
            else
            {
                result = GetCompressedResult(content, compressionType, responseHeaders, isHeadRequest, contentType);
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
        /// Gets the HTTP result.
        /// </summary>
        private IHasHeaders GetHttpResult(IRequest requestContext, string content, string contentType, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
        {
            IHasHeaders result;

            var bytes = Encoding.UTF8.GetBytes(content);

            var compressionType = requestContext == null ? null : GetCompressionType(requestContext, bytes, contentType);

            var isHeadRequest = requestContext == null ? false : string.Equals(requestContext.Verb, "head", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(compressionType))
            {
                var contentLength = bytes.Length;

                if (isHeadRequest)
                {
                    bytes = Array.Empty<byte>();
                }

                result = new StreamWriter(bytes, contentType, contentLength, _logger);
            }
            else
            {
                result = GetCompressedResult(bytes, compressionType, responseHeaders, isHeadRequest, contentType);
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
        public object GetResult<T>(IRequest requestContext, T result, IDictionary<string, string> responseHeaders = null)
            where T : class
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            responseHeaders["Expires"] = "-1";

            return ToOptimizedResultInternal(requestContext, result, responseHeaders);
        }

        private string GetCompressionType(IRequest request, byte[] content, string responseContentType)
        {
            if (responseContentType == null)
            {
                return null;
            }

            // Per apple docs, hls manifests must be compressed
            if (!responseContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
                responseContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) == -1 &&
                responseContentType.IndexOf("javascript", StringComparison.OrdinalIgnoreCase) == -1 &&
                responseContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) == -1 &&
                responseContentType.IndexOf("application/x-mpegURL", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return null;
            }

            if (content.Length < 1024)
            {
                return null;
            }

            return GetCompressionType(request);
        }

        private string GetCompressionType(IRequest request)
        {
            var acceptEncoding = request.Headers["Accept-Encoding"];

            if (acceptEncoding != null)
            {
                //if (_brotliCompressor != null && acceptEncoding.IndexOf("br", StringComparison.OrdinalIgnoreCase) != -1)
                //    return "br";

                if (acceptEncoding.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1)
                    return "deflate";

                if (acceptEncoding.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1)
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
            return ToOptimizedResultInternal(request, dto);
        }

        private object ToOptimizedResultInternal<T>(IRequest request, T dto, IDictionary<string, string> responseHeaders = null)
        {
            var contentType = request.ResponseContentType;

            switch (GetRealContentType(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return GetHttpResult(request, SerializeToXmlString(dto), contentType, false, responseHeaders);

                case "application/json":
                case "text/json":
                    return GetHttpResult(request, _jsonSerializer.SerializeToString(dto), contentType, false, responseHeaders);
                default:
                    break;
            }

            var isHeadRequest = string.Equals(request.Verb, "head", StringComparison.OrdinalIgnoreCase);

            var ms = new MemoryStream();
            var writerFn = RequestHelper.GetResponseWriter(HttpListenerHost.Instance, contentType);

            writerFn(dto, ms);

            ms.Position = 0;

            if (isHeadRequest)
            {
                using (ms)
                {
                    return GetHttpResult(request, Array.Empty<byte>(), contentType, true, responseHeaders);
                }
            }

            return GetHttpResult(request, ms, contentType, true, responseHeaders);
        }

        private IHasHeaders GetCompressedResult(byte[] content,
            string requestedCompressionType,
            IDictionary<string, string> responseHeaders,
            bool isHeadRequest,
            string contentType)
        {
            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            content = Compress(content, requestedCompressionType);
            responseHeaders["Content-Encoding"] = requestedCompressionType;

            responseHeaders["Vary"] = "Accept-Encoding";

            var contentLength = content.Length;

            if (isHeadRequest)
            {
                var result = new StreamWriter(Array.Empty<byte>(), contentType, contentLength, _logger);
                AddResponseHeaders(result, responseHeaders);
                return result;
            }
            else
            {
                var result = new StreamWriter(content, contentType, contentLength, _logger);
                AddResponseHeaders(result, responseHeaders);
                return result;
            }
        }

        private byte[] Compress(byte[] bytes, string compressionType)
        {
            if (string.Equals(compressionType, "br", StringComparison.OrdinalIgnoreCase))
                return CompressBrotli(bytes);

            if (string.Equals(compressionType, "deflate", StringComparison.OrdinalIgnoreCase))
                return Deflate(bytes);

            if (string.Equals(compressionType, "gzip", StringComparison.OrdinalIgnoreCase))
                return GZip(bytes);

            throw new NotSupportedException(compressionType);
        }

        private byte[] CompressBrotli(byte[] bytes)
        {
            return _brotliCompressor.Compress(bytes);
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
                    AddExpiresHeader(responseHeaders, cacheKeyString, cacheDuration);

                    var result = new HttpResult(Array.Empty<byte>(), contentType ?? "text/html", HttpStatusCode.NotModified);

                    AddResponseHeaders(result, responseHeaders);

                    return result;
                }
            }

            AddCachingHeaders(responseHeaders, cacheKeyString, lastDateModified, cacheDuration);

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

            if (string.IsNullOrEmpty(options.ContentType))
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

            options.ResponseHeaders = options.ResponseHeaders ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            if (!cacheKey.Equals(Guid.Empty))
            {
                var key = cacheKey.ToString("N");

                // See if the result is already cached in the browser
                var result = GetCachedResult(requestContext, options.ResponseHeaders, cacheKey, key, options.DateLastModified, options.CacheDuration, contentType);

                if (result != null)
                {
                    return result;
                }
            }

            // TODO: We don't really need the option value
            var isHeadRequest = options.IsHeadRequest || string.Equals(requestContext.Verb, "HEAD", StringComparison.OrdinalIgnoreCase);
            var factoryFn = options.ContentFactory;
            var responseHeaders = options.ResponseHeaders;

            //var requestedCompressionType = GetCompressionType(requestContext);

            var rangeHeader = requestContext.Headers.Get("Range");

            if (!isHeadRequest && !string.IsNullOrEmpty(options.Path))
            {
                var hasHeaders = new FileWriter(options.Path, contentType, rangeHeader, _logger, _fileSystem)
                {
                    OnComplete = options.OnComplete,
                    OnError = options.OnError,
                    FileShare = options.FileShare
                };

                AddResponseHeaders(hasHeaders, options.ResponseHeaders);
                return hasHeaders;
            }

            var stream = await factoryFn().ConfigureAwait(false);

            var totalContentLength = options.ContentLength;
            if (!totalContentLength.HasValue)
            {
                try
                {
                    totalContentLength = stream.Length;
                }
                catch (NotSupportedException)
                {

                }
            }

            if (!string.IsNullOrWhiteSpace(rangeHeader) && totalContentLength.HasValue)
            {
                var hasHeaders = new RangeRequestWriter(rangeHeader, totalContentLength.Value, stream, contentType, isHeadRequest, _logger)
                {
                    OnComplete = options.OnComplete
                };

                AddResponseHeaders(hasHeaders, options.ResponseHeaders);
                return hasHeaders;
            }
            else
            {
                if (totalContentLength.HasValue)
                {
                    responseHeaders["Content-Length"] = totalContentLength.Value.ToString(UsCulture);
                }

                if (isHeadRequest)
                {
                    using (stream)
                    {
                        return GetHttpResult(requestContext, Array.Empty<byte>(), contentType, true, responseHeaders);
                    }
                }

                var hasHeaders = new StreamWriter(stream, contentType, _logger)
                {
                    OnComplete = options.OnComplete,
                    OnError = options.OnError
                };

                AddResponseHeaders(hasHeaders, options.ResponseHeaders);
                return hasHeaders;
            }
        }

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Adds the caching responseHeaders.
        /// </summary>
        private void AddCachingHeaders(IDictionary<string, string> responseHeaders, string cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
        {
            // Don't specify both last modified and Etag, unless caching unconditionally. They are redundant
            // https://developers.google.com/speed/docs/best-practices/caching#LeverageBrowserCaching
            if (lastDateModified.HasValue && (string.IsNullOrEmpty(cacheKey) || cacheDuration.HasValue))
            {
                AddAgeHeader(responseHeaders, lastDateModified);
                responseHeaders["Last-Modified"] = lastDateModified.Value.ToString("r");
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
        private bool IsNotModified(IRequest requestContext, Guid cacheKey, DateTime? lastDateModified, TimeSpan? cacheDuration)
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

            var hasCacheKey = !cacheKey.Equals(Guid.Empty);

            // Validate If-None-Match
            if ((hasCacheKey || !string.IsNullOrEmpty(ifNoneMatchHeader)))
            {
                Guid ifNoneMatch;

                ifNoneMatchHeader = (ifNoneMatchHeader ?? string.Empty).Trim('\"');

                if (Guid.TryParse(ifNoneMatchHeader, out ifNoneMatch))
                {
                    if (hasCacheKey && cacheKey.Equals(ifNoneMatch))
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

    public interface IBrotliCompressor
    {
        byte[] Compress(byte[] content);
    }
}
