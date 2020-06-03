#pragma warning disable CS1591

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
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using IRequest = MediaBrowser.Model.Services.IRequest;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class HttpResultFactory.
    /// </summary>
    public class HttpResultFactory : IHttpResultFactory
    {
        // Last-Modified and If-Modified-Since must follow strict date format,
        // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Modified-Since
        private const string HttpDateFormat = "ddd, dd MMM yyyy HH:mm:ss \"GMT\"";
        // We specifically use en-US culture because both day of week and month names require it
        private static readonly CultureInfo _enUSculture = new CultureInfo("en-US", false);

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IStreamHelper _streamHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResultFactory" /> class.
        /// </summary>
        public HttpResultFactory(ILoggerFactory loggerfactory, IFileSystem fileSystem, IJsonSerializer jsonSerializer, IStreamHelper streamHelper)
        {
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _streamHelper = streamHelper;
            _logger = loggerfactory.CreateLogger("HttpResultFactory");
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
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
            responseHeaders[HeaderNames.Location] = url;

            var result = new HttpResult(Array.Empty<byte>(), "text/plain", HttpStatusCode.Redirect);

            AddResponseHeaders(result, responseHeaders);

            return result;
        }

        /// <summary>
        /// Gets the HTTP result.
        /// </summary>
        private IHasHeaders GetHttpResult(IRequest requestContext, Stream content, string contentType, bool addCachePrevention, IDictionary<string, string> responseHeaders = null)
        {
            var result = new StreamWriter(content, contentType);

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>();
            }

            if (addCachePrevention && !responseHeaders.TryGetValue(HeaderNames.Expires, out string expires))
            {
                responseHeaders[HeaderNames.Expires] = "0";
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

            if (requestContext != null)
            {
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

                result = new StreamWriter(content, contentType, contentLength);
            }
            else
            {
                result = GetCompressedResult(content, compressionType, responseHeaders, isHeadRequest, contentType);
            }

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>();
            }

            if (addCachePrevention && !responseHeaders.TryGetValue(HeaderNames.Expires, out string _))
            {
                responseHeaders[HeaderNames.Expires] = "0";
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

                result = new StreamWriter(bytes, contentType, contentLength);
            }
            else
            {
                result = GetCompressedResult(bytes, compressionType, responseHeaders, isHeadRequest, contentType);
            }

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>();
            }

            if (addCachePrevention && !responseHeaders.TryGetValue(HeaderNames.Expires, out string _))
            {
                responseHeaders[HeaderNames.Expires] = "0";
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
                throw new ArgumentNullException(nameof(result));
            }

            if (responseHeaders == null)
            {
                responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            responseHeaders[HeaderNames.Expires] = "0";

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

        private static string GetCompressionType(IRequest request)
        {
            var acceptEncoding = request.Headers[HeaderNames.AcceptEncoding].ToString();

            if (!string.IsNullOrEmpty(acceptEncoding))
            {
                // if (_brotliCompressor != null && acceptEncoding.IndexOf("br", StringComparison.OrdinalIgnoreCase) != -1)
                //    return "br";

                if (acceptEncoding.Contains("deflate", StringComparison.OrdinalIgnoreCase))
                {
                    return "deflate";
                }

                if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    return "gzip";
                }
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
            // TODO: @bond use Span and .Equals
            var contentType = request.ResponseContentType?.Split(';')[0].Trim().ToLowerInvariant();

            switch (contentType)
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
            responseHeaders[HeaderNames.ContentEncoding] = requestedCompressionType;

            responseHeaders[HeaderNames.Vary] = HeaderNames.AcceptEncoding;

            var contentLength = content.Length;

            if (isHeadRequest)
            {
                var result = new StreamWriter(Array.Empty<byte>(), contentType, contentLength);
                AddResponseHeaders(result, responseHeaders);
                return result;
            }
            else
            {
                var result = new StreamWriter(content, contentType, contentLength);
                AddResponseHeaders(result, responseHeaders);
                return result;
            }
        }

        private byte[] Compress(byte[] bytes, string compressionType)
        {
            if (string.Equals(compressionType, "deflate", StringComparison.OrdinalIgnoreCase))
            {
                return Deflate(bytes);
            }

            if (string.Equals(compressionType, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                return GZip(bytes);
            }

            throw new NotSupportedException(compressionType);
        }

        private static byte[] Deflate(byte[] bytes)
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

        private static byte[] GZip(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            using (var zipStream = new GZipStream(ms, CompressionMode.Compress))
            {
                zipStream.Write(buffer, 0, buffer.Length);
                zipStream.Dispose();

                return ms.ToArray();
            }
        }

        private static string SerializeToXmlString(object from)
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
                    using (var reader = new StreamReader(ms))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// Pres the process optimized result.
        /// </summary>
        private object GetCachedResult(IRequest requestContext, IDictionary<string, string> responseHeaders, StaticResultOptions options)
        {
            bool noCache = (requestContext.Headers[HeaderNames.CacheControl].ToString()).IndexOf("no-cache", StringComparison.OrdinalIgnoreCase) != -1;
            AddCachingHeaders(responseHeaders, options.CacheDuration, noCache, options.DateLastModified);

            if (!noCache)
            {
                if (!DateTime.TryParseExact(requestContext.Headers[HeaderNames.IfModifiedSince], HttpDateFormat, _enUSculture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ifModifiedSinceHeader))
                {
                    _logger.LogDebug("Failed to parse If-Modified-Since header date: {0}", requestContext.Headers[HeaderNames.IfModifiedSince]);
                    return null;
                }

                if (IsNotModified(ifModifiedSinceHeader, options.CacheDuration, options.DateLastModified))
                {
                    AddAgeHeader(responseHeaders, options.DateLastModified);

                    var result = new HttpResult(Array.Empty<byte>(), options.ContentType ?? "text/html", HttpStatusCode.NotModified);

                    AddResponseHeaders(result, responseHeaders);

                    return result;
                }
            }

            return null;
        }

        public Task<object> GetStaticFileResult(IRequest requestContext,
            string path,
            FileShare fileShare = FileShare.Read)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            return GetStaticFileResult(requestContext, new StaticFileResultOptions
            {
                Path = path,
                FileShare = fileShare
            });
        }

        public Task<object> GetStaticFileResult(IRequest requestContext, StaticFileResultOptions options)
        {
            var path = options.Path;
            var fileShare = options.FileShare;

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path can't be empty.", nameof(options));
            }

            if (fileShare != FileShare.Read && fileShare != FileShare.ReadWrite)
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
        private Stream GetFileStream(string path, FileShare fileShare)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, fileShare);
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
                ContentFactory = factoryFn,
                ContentType = contentType,
                DateLastModified = lastDateModified,
                IsHeadRequest = isHeadRequest,
                ResponseHeaders = responseHeaders
            });
        }

        public async Task<object> GetStaticResult(IRequest requestContext, StaticResultOptions options)
        {
            options.ResponseHeaders = options.ResponseHeaders ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var contentType = options.ContentType;
            if (!StringValues.IsNullOrEmpty(requestContext.Headers[HeaderNames.IfModifiedSince]))
            {
                // See if the result is already cached in the browser
                var result = GetCachedResult(requestContext, options.ResponseHeaders, options);

                if (result != null)
                {
                    return result;
                }
            }

            // TODO: We don't really need the option value
            var isHeadRequest = options.IsHeadRequest || string.Equals(requestContext.Verb, "HEAD", StringComparison.OrdinalIgnoreCase);
            var factoryFn = options.ContentFactory;
            var responseHeaders = options.ResponseHeaders;
            AddCachingHeaders(responseHeaders, options.CacheDuration, false, options.DateLastModified);
            AddAgeHeader(responseHeaders, options.DateLastModified);

            var rangeHeader = requestContext.Headers[HeaderNames.Range];

            if (!isHeadRequest && !string.IsNullOrEmpty(options.Path))
            {
                var hasHeaders = new FileWriter(options.Path, contentType, rangeHeader, _logger, _fileSystem, _streamHelper)
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
                    responseHeaders["Content-Length"] = totalContentLength.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (isHeadRequest)
                {
                    using (stream)
                    {
                        return GetHttpResult(requestContext, Array.Empty<byte>(), contentType, true, responseHeaders);
                    }
                }

                var hasHeaders = new StreamWriter(stream, contentType)
                {
                    OnComplete = options.OnComplete,
                    OnError = options.OnError
                };

                AddResponseHeaders(hasHeaders, options.ResponseHeaders);
                return hasHeaders;
            }
        }

        /// <summary>
        /// Adds the caching responseHeaders.
        /// </summary>
        private void AddCachingHeaders(IDictionary<string, string> responseHeaders, TimeSpan? cacheDuration,
            bool noCache, DateTime? lastModifiedDate)
        {
            if (noCache)
            {
                responseHeaders[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
                responseHeaders[HeaderNames.Pragma] = "no-cache, no-store, must-revalidate";
                return;
            }

            if (cacheDuration.HasValue)
            {
                responseHeaders[HeaderNames.CacheControl] = "public, max-age=" + cacheDuration.Value.TotalSeconds;
            }
            else
            {
                responseHeaders[HeaderNames.CacheControl] = "public";
            }

            if (lastModifiedDate.HasValue)
            {
                responseHeaders[HeaderNames.LastModified] = lastModifiedDate.Value.ToUniversalTime().ToString(HttpDateFormat, _enUSculture);
            }
        }

        /// <summary>
        /// Adds the age header.
        /// </summary>
        /// <param name="responseHeaders">The responseHeaders.</param>
        /// <param name="lastDateModified">The last date modified.</param>
        private static void AddAgeHeader(IDictionary<string, string> responseHeaders, DateTime? lastDateModified)
        {
            if (lastDateModified.HasValue)
            {
                responseHeaders[HeaderNames.Age] = Convert.ToInt64((DateTime.UtcNow - lastDateModified.Value).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }
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
        private static DateTime NormalizeDateForComparison(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        /// <summary>
        /// Adds the response headers.
        /// </summary>
        /// <param name="hasHeaders">The has options.</param>
        /// <param name="responseHeaders">The response headers.</param>
        private static void AddResponseHeaders(IHasHeaders hasHeaders, IEnumerable<KeyValuePair<string, string>> responseHeaders)
        {
            foreach (var item in responseHeaders)
            {
                hasHeaders.Headers[item.Key] = item.Value;
            }
        }
    }
}
