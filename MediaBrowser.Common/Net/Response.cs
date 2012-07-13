using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace MediaBrowser.Common.Net
{
    public abstract class Response
    {
        protected RequestContext RequestContext { get; private set; }

        protected NameValueCollection QueryString
        {
            get
            {
                return RequestContext.Request.QueryString;
            }
        }

        public Response(RequestContext ctx)
        {
            RequestContext = ctx;

            WriteStream = s => { };
            Headers = new Dictionary<string, string>();
        }

        public abstract string ContentType { get; }

        public virtual int StatusCode
        {
            get
            {
                return 200;
            }
        }

        public virtual TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromTicks(0);
            }
        }

        public virtual DateTime? LastDateModified
        {
            get
            {
                return null;
            }
        }

        public IDictionary<string, string> Headers { get; set; }
        public Action<Stream> WriteStream { get; set; }
    }

    /*public class ByteResponse : Response
    {
        public ByteResponse(byte[] bytes)
        {
            WriteStream = async s => 
            {
                await s.WriteAsync(bytes, 0, bytes.Length);
                s.Close();
            };
        }
    }

    public class StringResponse : ByteResponse
    {
        public StringResponse(string message)
            : base(Encoding.UTF8.GetBytes(message))
        {
        }
    }*/
}