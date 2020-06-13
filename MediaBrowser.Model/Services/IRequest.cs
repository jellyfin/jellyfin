#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Model.Services
{
    public interface IRequest
    {
        HttpResponse Response { get; }

        /// <summary>
        /// The name of the service being called (e.g. Request DTO Name)
        /// </summary>
        string OperationName { get; set; }

        /// <summary>
        /// The Verb / HttpMethod or Action for this request
        /// </summary>
        string Verb { get; }

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
}
