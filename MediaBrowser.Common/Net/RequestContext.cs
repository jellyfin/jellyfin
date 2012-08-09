using System;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net.Handlers;

namespace MediaBrowser.Common.Net
{
    public class RequestContext
    {
        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }

        public string LocalPath
        {
            get
            {
                return Request.Url.LocalPath;
            }
        }

        public RequestContext(HttpListenerContext context)
        {
            Response = context.Response;
            Request = context.Request;
        }

        public void Respond(BaseHandler handler)
        {
            Logger.LogInfo("Http Server received request at: " + Request.Url.ToString());
            Logger.LogInfo("Http Headers: " + string.Join(",", Request.Headers.AllKeys.Select(k => k + "=" + Request.Headers[k])));

            Response.AddHeader("Access-Control-Allow-Origin", "*");

            Response.KeepAlive = true;

            foreach (var header in handler.Headers)
            {
                Response.AddHeader(header.Key, header.Value);
            }

            int statusCode = handler.StatusCode;
            Response.ContentType = handler.ContentType;

            TimeSpan cacheDuration = handler.CacheDuration;

            if (Request.Headers.AllKeys.Contains("If-Modified-Since"))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(Request.Headers["If-Modified-Since"].Replace(" GMT", string.Empty), out ifModifiedSince))
                {
                    // If the cache hasn't expired yet just return a 304
                    if (IsCacheValid(ifModifiedSince, cacheDuration, handler.LastDateModified))
                    {
                        statusCode = 304;
                    }
                }
            }

            Response.StatusCode = statusCode;

            if (statusCode == 200 || statusCode == 206)
            {
                handler.WriteStream(Response.OutputStream);
            }
            else
            {
                Response.SendChunked = false;
                Response.OutputStream.Dispose();
            }
        }

        private bool IsCacheValid(DateTime ifModifiedSince, TimeSpan cacheDuration, DateTime? dateModified)
        {
            if (dateModified.HasValue)
            {
                DateTime lastModified = NormalizeDateForComparison(dateModified.Value);
                ifModifiedSince = NormalizeDateForComparison(ifModifiedSince);

                return lastModified <= ifModifiedSince;
            }

            DateTime cacheExpirationDate = ifModifiedSince.Add(cacheDuration);

            if (DateTime.Now < cacheExpirationDate)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// When the browser sends the IfModifiedDate, it's precision is limited to seconds, so this will account for that
        /// </summary>
        private DateTime NormalizeDateForComparison(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }

    }
}