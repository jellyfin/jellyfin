using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Controller.Net
{
    public class Response
    {
        protected RequestContext RequestContext { get; private set; }
        
        public Response(RequestContext ctx)
        {
            RequestContext = ctx;
            
            WriteStream = s => { };
            StatusCode = 200;
            Headers = new Dictionary<string, string>();
            CacheDuration = TimeSpan.FromTicks(0);
            ContentType = "text/html";
        }

        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public TimeSpan CacheDuration { get; set; }
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