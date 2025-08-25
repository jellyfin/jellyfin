using System;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Deprecated class intended only for use by the deprecated non-generic IAuthenticationProvider.
    /// </summary>
    [Obsolete("Only for use by the deprecated non-generic IAuthenticationProvider.")]
    public record UsernamePasswordData(string Username, string Password);
}
