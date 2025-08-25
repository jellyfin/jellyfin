using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity that represents arbitrary global (NOT user-specific) data for an authentication provider, as well as whether or not it is enabled globally.
    /// </summary>
    public class AuthenticationProviderData
    {
        /// <summary>
        /// Gets or sets the authentication provider ID. This is equal to the type name of the authentication provider's implementing class.
        /// </summary>
        public required string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the authentication provider is enabled.
        /// </summary>
        /// <remarks>
        /// Besides being enabled globally, an authentication provider needs to be enabled on a per-user basis for users to successfully log in using them.
        /// </remarks>
        public required bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the JSON string representing this authentication provider's global data.
        /// </summary>
        /// <remarks>
        /// You should generally not set this property directly, serialization and deserialization is handled by <see cref=""/>.
        /// </remarks>
        public string? Data { get; set; }
    }
}
