using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public class AuthenticatedAttribute : Attribute, IHasRequestFilter, IAuthenticationAttributes
    {
        public static IAuthService AuthService { get; set; }

        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        /// <value>The roles.</value>
        public string Roles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [escape parental control].
        /// </summary>
        /// <value><c>true</c> if [escape parental control]; otherwise, <c>false</c>.</value>
        public bool EscapeParentalControl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow before startup wizard].
        /// </summary>
        /// <value><c>true</c> if [allow before startup wizard]; otherwise, <c>false</c>.</value>
        public bool AllowBeforeStartupWizard { get; set; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="request">The http request wrapper</param>
        /// <param name="response">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public void RequestFilter(IRequest request, IResponse response, object requestDto)
        {
            var serviceRequest = new ServiceRequest(request);

            AuthService.Authenticate(serviceRequest, this);
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

        public IEnumerable<string> GetRoles()
        {
            return (Roles ?? string.Empty).Split(',')
                .Where(i => !string.IsNullOrWhiteSpace(i));
        }
    }

    public interface IAuthenticationAttributes
    {
        bool EscapeParentalControl { get; }
        bool AllowBeforeStartupWizard { get; }

        IEnumerable<string> GetRoles();
    }
}
