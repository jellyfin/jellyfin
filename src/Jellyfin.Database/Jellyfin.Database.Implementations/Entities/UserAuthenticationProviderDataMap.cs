using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity that represents arbitrary user-specific data for an authentication provider, as well as whether or not it is enabled for this specific user.
    /// </summary>
    /// <remarks>
    /// Links <see cref="AuthenticationProviderData"/> to <see cref="User"/>.
    /// </remarks>
    public class UserAuthenticationProviderDataMap
    {
        /// <summary>
        /// Gets or sets the authentication provider ID. This is equal to the type name of the authentication provider's implementing class.
        /// </summary>
        public required string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the authentication provider is enabled for this user.
        /// </summary>
        /// <remarks>
        /// Besides being enabled for a user, an authentication provider needs to be enabled globally for users to successfully log in using them.
        /// </remarks>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the JSON string representing this authentication provider's user-specific data.
        /// </summary>
        /// <remarks>
        /// You should generally not set this property directly, serialization and deserialization is handled by <see cref=""/>.
        /// </remarks>
        public string? Data { get; set; }
    }
}
