#pragma warning disable CS1591

using System.Net;

namespace MediaBrowser.Model.Services
{
    public interface IHttpResult : IHasHeaders
    {
        /// <summary>
        /// Gets or sets the HTTP Response Status.
        /// </summary>
        int Status { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Response Status Code.
        /// </summary>
        HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Response ContentType.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Gets or sets response DTO.
        /// </summary>
        object Response { get; set; }

        /// <summary>
        /// Gets or sets holds the request call context.
        /// </summary>
        IRequest RequestContext { get; set; }
    }
}
