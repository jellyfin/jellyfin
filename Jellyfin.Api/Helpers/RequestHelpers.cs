using System;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Request Extensions.
    /// </summary>
    public static class RequestHelpers
    {
        /// <summary>
        /// Splits a string at a separating character into an array of substrings.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <param name="separator">The char that separates the substrings.</param>
        /// <param name="removeEmpty">Option to remove empty substrings from the array.</param>
        /// <returns>An array of the substrings.</returns>
        internal static string[] Split(string value, char separator, bool removeEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return removeEmpty
                ? value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                : value.Split(separator);
        }

        internal static SessionInfo GetSession(ISessionContext sessionContext)
        {
            // TODO: how do we get a SessionInfo without IRequest?
            SessionInfo session = sessionContext.GetSession("Request");

            if (session == null)
            {
                throw new ArgumentException("Session not found.");
            }

            return session;
        }
    }
}
