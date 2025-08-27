using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Authentication;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Default implementation of <see cref="IUserAuthenticationManager"/>.
    /// </summary>
    public class UserAuthenticationManager : IUserAuthenticationManager
    {
        /// <inheritdoc/>
        public Task<(IAuthenticationProvider<TPayload> Provider, AuthenticationResponse Response)?> Authenticate<TPayload>(User? user, TPayload payloadData)
            where TPayload : struct
        {
            
        }
    }
}
