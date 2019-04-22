using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Model.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Model.Services
{
    public interface IRequest
    {
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

        /// <summary>
        /// The expected Response ContentType for this request
        /// </summary>
        string ResponseContentType { get; set; }

        /// <summary>
        /// Attach any data to this request that all filters and services can access.
        /// </summary>
        Dictionary<string, object> Items { get; }

        IHeaderDictionary Headers { get; }

        IQueryCollection QueryString { get; }

        Task<QueryParamCollection> GetFormData();

        string RawUrl { get; }

        string AbsoluteUri { get; }

        /// <summary>
        /// The Remote Ip as reported by X-Forwarded-For, X-Real-IP or Request.UserHostAddress
        /// </summary>
        string RemoteIp { get; }

        /// <summary>
        /// The value of the Authorization Header used to send the Api Key, null if not available
        /// </summary>
        string Authorization { get; }

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
        HttpResponse OriginalResponse { get; }

        int StatusCode { get; set; }

        string StatusDescription { get; set; }

        string ContentType { get; set; }

        void AddHeader(string name, string value);

        void Redirect(string url);

        Stream OutputStream { get; }

        Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, IFileSystem fileSystem, IStreamHelper streamHelper, CancellationToken cancellationToken);

        bool SendChunked { get; set; }
    }
}
