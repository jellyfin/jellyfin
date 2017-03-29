using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Model.Services
{
    public interface IRequest
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpRequest
        /// </summary>
        object OriginalRequest { get; }

        IResponse Response { get; }

        /// <summary>
        /// The name of the service being called (e.g. Request DTO Name)
        /// </summary>
        string OperationName { get; set; }

        /// <summary>
        /// The Verb / HttpMethod or Action for this request
        /// </summary>
        string Verb { get; }

        /// <summary>
        /// The Request DTO, after it has been deserialized.
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// The request ContentType
        /// </summary>
        string ContentType { get; }

        bool IsLocal { get; }

        string UserAgent { get; }

        IDictionary<string, Cookie> Cookies { get; }

        /// <summary>
        /// The expected Response ContentType for this request
        /// </summary>
        string ResponseContentType { get; set; }

        /// <summary>
        /// Whether the ResponseContentType has been explicitly overrided or whether it was just the default
        /// </summary>
        bool HasExplicitResponseContentType { get; }

        /// <summary>
        /// Attach any data to this request that all filters and services can access.
        /// </summary>
        Dictionary<string, object> Items { get; }

        QueryParamCollection Headers { get; }

        QueryParamCollection QueryString { get; }

        QueryParamCollection FormData { get; }

        string RawUrl { get; }

        string AbsoluteUri { get; }

        /// <summary>
        /// The Remote Ip as reported by Request.UserHostAddress
        /// </summary>
        string UserHostAddress { get; }

        /// <summary>
        /// The Remote Ip as reported by X-Forwarded-For, X-Real-IP or Request.UserHostAddress
        /// </summary>
        string RemoteIp { get; }

        /// <summary>
        /// The value of the Authorization Header used to send the Api Key, null if not available
        /// </summary>
        string Authorization { get; }

        /// <summary>
        /// e.g. is https or not
        /// </summary>
        bool IsSecureConnection { get; }

        string[] AcceptTypes { get; }

        string PathInfo { get; }

        Stream InputStream { get; }

        long ContentLength { get; }

        /// <summary>
        /// Access to the multi-part/formdata files posted on this request
        /// </summary>
        IHttpFile[] Files { get; }

        /// <summary>
        /// The value of the Referrer, null if not available
        /// </summary>
        Uri UrlReferrer { get; }
    }

    public interface IHttpFile
    {
        string Name { get; }
        string FileName { get; }
        long ContentLength { get; }
        string ContentType { get; }
        Stream InputStream { get; }
    }

    public interface IRequiresRequest
    {
        IRequest Request { get; set; }
    }

    public interface IResponse
    {
        IRequest Request { get; }

        int StatusCode { get; set; }

        string StatusDescription { get; set; }

        string ContentType { get; set; }

        void AddHeader(string name, string value);

        string GetHeader(string name);

        void Redirect(string url);

        Stream OutputStream { get; }

        /// <summary>
        /// Signal that this response has been handled and no more processing should be done.
        /// When used in a request or response filter, no more filters or processing is done on this request.
        /// </summary>
        void Close();

        /// <summary>
        /// Gets a value indicating whether this instance is closed.
        /// </summary>
        bool IsClosed { get; }

        void SetContentLength(long contentLength);

        //Add Metadata to Response
        Dictionary<string, object> Items { get; }

        Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken);
    }
}
