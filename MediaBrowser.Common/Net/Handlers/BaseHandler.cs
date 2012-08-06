using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseHandler
    {
        /// <summary>
        /// Response headers
        /// </summary>
        public IDictionary<string, string> Headers = new Dictionary<string, string>();

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
        public virtual Action<Stream> WriteStream
        {
            get
            {
                return s =>
                {
                    WriteReponse(s);

                    if (!IsAsyncHandler)
                    {
                        s.Dispose();
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

        private void WriteReponse(Stream stream)
        {
            if (CompressResponse)
            {
                using (DeflateStream compressedStream = new DeflateStream(stream, CompressionLevel.Fastest, false))
                {
                    WriteResponseToOutputStream(compressedStream);
                }
            }
            else
            {
                WriteResponseToOutputStream(stream);
            }
        }

        protected abstract void WriteResponseToOutputStream(Stream stream);

    }
}