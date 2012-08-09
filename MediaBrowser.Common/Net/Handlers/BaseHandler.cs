using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseHandler
    {
        /// <summary>
        /// Response headers
        /// </summary>
        public IDictionary<string, string> Headers = new Dictionary<string, string>();

        private Stream CompressedStream { get; set; }

        public virtual bool UseChunkedEncoding
        {
            get
            {
                return true;
            }
        }

        public virtual long? ContentLength
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true or false indicating if the handler writes to the stream asynchronously.
        /// If so the subclass will be responsible for disposing the stream when complete.
        /// </summary>
        protected virtual bool IsAsyncHandler
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// The action to write the response to the output stream
        /// </summary>
        public Action<Stream> WriteStream
        {
            get
            {
                return s =>
                {
                    WriteReponse(s);

                    if (!IsAsyncHandler)
                    {
                        DisposeResponseStream();
                    }
                };
            }
        }

        /// <summary>
        /// The original RequestContext
        /// </summary>
        public RequestContext RequestContext { get; set; }

        /// <summary>
        /// The original QueryString
        /// </summary>
        protected NameValueCollection QueryString
        {
            get
            {
                return RequestContext.Request.QueryString;
            }
        }

        /// <summary>
        /// Gets the MIME type to include in the response headers
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Gets the status code to include in the response headers
        /// </summary>
        public virtual int StatusCode
        {
            get
            {
                return 200;
            }
        }

        /// <summary>
        /// Gets the cache duration to include in the response headers
        /// </summary>
        public virtual TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromTicks(0);
            }
        }

        /// <summary>
        /// Gets the last date modified of the content being returned, if this can be determined.
        /// This will be used to invalidate the cache, so it's not needed if CacheDuration is 0.
        /// </summary>
        public virtual DateTime? LastDateModified
        {
            get
            {
                return null;
            }
        }

        public virtual bool CompressResponse
        {
            get
            {
                return true;
            }
        }

        private bool ClientSupportsCompression
        {
            get
            {
                string enc = RequestContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                return enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1 || enc.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1;
            }
        }

        private string CompressionMethod
        {
            get
            {
                string enc = RequestContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                if (enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return "deflate";
                }
                if (enc.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return "gzip";
                }

                return null;
            }
        }

        protected virtual void PrepareResponseBeforeWriteOutput(HttpListenerResponse response)
        {
            // Don't force this to true. HttpListener will default it to true if supported by the client.
            if (!UseChunkedEncoding)
            {
                response.SendChunked = false;
            }

            if (ContentLength.HasValue)
            {
                response.ContentLength64 = ContentLength.Value;
            }

            if (CompressResponse && ClientSupportsCompression)
            {
                response.AddHeader("Content-Encoding", CompressionMethod);
            }

            TimeSpan cacheDuration = CacheDuration;
            
            if (cacheDuration.Ticks > 0)
            {
                CacheResponse(response, cacheDuration, LastDateModified);
            }
        }

        private void CacheResponse(HttpListenerResponse response, TimeSpan duration, DateTime? dateModified)
        {
            DateTime lastModified = dateModified ?? DateTime.Now;

            response.Headers[HttpResponseHeader.CacheControl] = "public, max-age=" + Convert.ToInt32(duration.TotalSeconds);
            response.Headers[HttpResponseHeader.Expires] = DateTime.Now.Add(duration).ToString("r");
            response.Headers[HttpResponseHeader.LastModified] = lastModified.ToString("r");
        }

        private void WriteReponse(Stream stream)
        {
            PrepareResponseBeforeWriteOutput(RequestContext.Response);

            if (CompressResponse && ClientSupportsCompression)
            {
                if (CompressionMethod.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                {
                    CompressedStream = new DeflateStream(stream, CompressionLevel.Fastest, false);
                }
                else
                {
                    CompressedStream = new GZipStream(stream, CompressionLevel.Fastest, false);
                }

                WriteResponseToOutputStream(CompressedStream);
            }
            else
            {
                WriteResponseToOutputStream(stream);
            }
        }

        protected abstract void WriteResponseToOutputStream(Stream stream);

        protected void DisposeResponseStream()
        {
            if (CompressedStream != null)
            {
                CompressedStream.Dispose();
            }

            RequestContext.Response.OutputStream.Dispose();
        }
    }
}