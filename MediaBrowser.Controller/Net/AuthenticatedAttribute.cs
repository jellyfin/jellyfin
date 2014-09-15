using ServiceStack.Web;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Net
{
    public class AuthenticatedAttribute : Attribute, IHasRequestFilter
    {
        public IAuthService AuthService { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to allow local unauthenticated access.
        /// </summary>
        /// <value><c>true</c> if [allow local]; otherwise, <c>false</c>.</value>
        public bool AllowLocal { get; set; }

        public string Roles { get; set; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="request">The http request wrapper</param>
        /// <param name="response">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public void RequestFilter(IRequest request, IResponse response, object requestDto)
        {
            var roles = (Roles ?? string.Empty).Split(',')
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToArray();

            AuthService.Authenticate(request, response, requestDto, AllowLocal, roles);
        }

        /// <summary>
        /// A new shallow copy of this filter is used on every request.
        /// </summary>
        /// <returns>IHasRequestFilter.</returns>
        public IHasRequestFilter Copy()
        {
            return this;
        }

        /// <summary>
        /// Order in which Request Filters are executed.
        /// &lt;0 Executed before global request filters
        /// &gt;0 Executed after global request filters
        /// </summary>
        /// <value>The priority.</value>
        public int Priority
        {
            get { return 0; }
        }
    }
}
