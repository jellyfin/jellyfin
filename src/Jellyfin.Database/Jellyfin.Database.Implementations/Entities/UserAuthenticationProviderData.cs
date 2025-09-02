using System;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity that represents arbitrary user-specific data for an authentication provider.
    /// </summary>
    /// <remarks>
    /// Links <see cref="AuthenticationProviderData"/> to <see cref="User"/>.
    /// </remarks>
    public class UserAuthenticationProviderData
    {
        /// <summary>
        /// Gets or sets the authentication provider ID. This is equal to the full type name of the authentication provider's implementing class.
        /// </summary>
        public required string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public required Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the JSON string representing this authentication provider's user-specific data.
        /// </summary>
        /// <remarks>
        /// You should generally not set this property directly, serialization and deserialization is handled by AbstractAuthenticationProvider and related helper classes./>.
        /// </remarks>
        public string? Data { get; set; }
    }
}
