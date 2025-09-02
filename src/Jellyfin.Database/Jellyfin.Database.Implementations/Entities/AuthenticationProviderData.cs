using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity that represents arbitrary global (NOT user-specific) data for an authentication provider, as well as whether or not it is enabled globally.
    /// </summary>
    [Table("AuthenticationProviderData")]
    public class AuthenticationProviderData
    {
        /// <summary>
        /// Gets or sets the authentication provider ID. This is equal to the type name of the authentication provider's implementing class.
        /// </summary>
        [Key]
        public required string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Gets the user-specific authentication provider data. External dependencies should not modify this directly.
        /// </summary>
        public ICollection<UserAuthenticationProviderData> UserAuthenticationProviderDatas { get; } = [];

        /// <summary>
        /// Gets the users who have data at this authentication provider. Exists for model completeness and will generally not be accessed directly.
        /// </summary>
        public ICollection<User> Users { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether or not the authentication provider is enabled.
        /// </summary>
        public required bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the JSON string representing this authentication provider's global data.
        /// </summary>
        /// <remarks>
        /// You should generally not set this property directly, and instead use the convenience functions provided by AbstractAuthenticationProvider./>.
        /// </remarks>
        public string? Data { get; set; }
    }
}
