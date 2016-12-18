using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Services
{
    public interface IHttpResult : IHasHeaders
    {
        /// <summary>
        /// The HTTP Response Status
        /// </summary>
        int Status { get; set; }

        /// <summary>
        /// The HTTP Response Status Code
        /// </summary>
        HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The HTTP Response ContentType
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Additional HTTP Cookies
        /// </summary>
        List<Cookie> Cookies { get; }

        /// <summary>
        /// Response DTO
        /// </summary>
        object Response { get; set; }

        /// <summary>
        /// Holds the request call context
        /// </summary>
        IRequest RequestContext { get; set; }
    }
}
