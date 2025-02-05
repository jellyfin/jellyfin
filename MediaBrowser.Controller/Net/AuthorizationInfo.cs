using System;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Data.Entities;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// The request authorization info.
    /// </summary>
    public class AuthorizationInfo
    {
        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId => User?.Id ?? Guid.Empty;

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the device.
        /// </summary>
        /// <value>The device.</value>
        public string? Device { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>The client.</value>
        public string? Client { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>The token.</value>
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the authorization is from an api key.
        /// </summary>
        public bool IsApiKey { get; set; }

        /// <summary>
        /// Gets or sets the user making the request.
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the token is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets a value indicating whether the request has a token.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Token))]
        public bool HasToken => !string.IsNullOrWhiteSpace(Token);
    }
}
