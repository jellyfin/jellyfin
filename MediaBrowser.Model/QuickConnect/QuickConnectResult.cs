using System;

namespace MediaBrowser.Model.QuickConnect
{
    /// <summary>
    /// Stores the state of an quick connect request.
    /// </summary>
    public class QuickConnectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectResult"/> class.
        /// </summary>
        /// <param name="secret">The secret used to query the request state.</param>
        /// <param name="code">The code used to allow the request.</param>
        /// <param name="dateAdded">The time when the request was created.</param>
        public QuickConnectResult(string secret, string code, DateTime dateAdded)
        {
            Secret = secret;
            Code = code;
            DateAdded = dateAdded;
        }

        /// <summary>
        /// Gets a value indicating whether this request is authorized.
        /// </summary>
        public bool Authenticated => Authentication != null;

        /// <summary>
        /// Gets the secret value used to uniquely identify this request. Can be used to retrieve authentication information.
        /// </summary>
        public string Secret { get; }

        /// <summary>
        /// Gets the user facing code used so the user can quickly differentiate this request from others.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets or sets the private access token.
        /// </summary>
        public Guid? Authentication { get; set; }

        /// <summary>
        /// Gets or sets the DateTime that this request was created.
        /// </summary>
        public DateTime DateAdded { get; set; }
    }
}
