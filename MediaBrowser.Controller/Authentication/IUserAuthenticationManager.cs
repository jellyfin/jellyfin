using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Handles authentication of users. If you simply want to add a new authentication provider,
    /// you probably want to implement <see cref="IAuthenticationProvider"/> or one of the helper
    /// classes <see cref="AbstractAuthenticationProvider{TData, TPrivateAttemptData, TIntermediateAttemptData, TPersistentUserData}"/> or <see cref="AbstractSimpleAuthenticationProvider{TData, TPersistentUserData}"/>.
    /// </summary>
    public interface IUserAuthenticationManager
    {
        Task<(User, IAuthenticationProvider)?> Authenticate(User? user, dynamic? payloadData);
    }
}
