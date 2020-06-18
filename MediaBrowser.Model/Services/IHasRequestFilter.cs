#pragma warning disable CS1591

using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Model.Services
{
    public interface IHasRequestFilter
    {
        /// <summary>
        /// Gets the order in which Request Filters are executed.
        /// &lt;0 Executed before global request filters.
        /// &gt;0 Executed after global request filters.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="req">The http request wrapper.</param>
        /// <param name="res">The http response wrapper.</param>
        /// <param name="requestDto">The request DTO.</param>
        void RequestFilter(IRequest req, HttpResponse res, object requestDto);
    }
}
