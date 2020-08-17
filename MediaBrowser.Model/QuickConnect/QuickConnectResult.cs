using System;

namespace MediaBrowser.Model.QuickConnect
{
    /// <summary>
    /// Stores the result of an incoming quick connect request.
    /// </summary>
    public class QuickConnectResult
    {
        /// <summary>
        /// Gets a value indicating whether this request is authorized.
        /// </summary>
        public bool Authenticated => !string.IsNullOrEmpty(Authentication);

        /// <summary>
        /// Gets or sets the secret value used to uniquely identify this request. Can be used to retrieve authentication information.
        /// </summary>
        public string? Secret { get; set; }

        /// <summary>
        /// Gets or sets the user facing code used so the user can quickly differentiate this request from others.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the private access token.
        /// </summary>
        public string? Authentication { get; set; }

        /// <summary>
        /// Gets or sets an error message.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the DateTime that this request was created.
        /// </summary>
        public DateTime? DateAdded { get; set; }
    }
}
