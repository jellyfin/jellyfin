using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Represents a response to an authentication attempt via <see cref="IUserAuthenticationManager"/>.
    /// </summary>
    public abstract record AuthenticationResponse
    {
        /// <summary>
        /// Represents a failure response.
        /// </summary>
        public record Failure : AuthenticationResponse;

        /// <summary>
        /// Represents a continue response.
        /// </summary>
        public record Continue(Uri URI) : AuthenticationResponse;

        /// <summary>
        /// Represents a success response.
        /// </summary>
        public record Success(User User) : AuthenticationResponse;
    }
}
