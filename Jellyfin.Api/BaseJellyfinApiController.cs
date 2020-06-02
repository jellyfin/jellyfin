using System;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api
{
    /// <summary>
    /// Base api controller for the API setting a default route.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class BaseJellyfinApiController : ControllerBase
    {
        /// <summary>
        /// Splits a string at a seperating character into an array of substrings.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <param name="separator">The char that seperates the substrings.</param>
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
    }
}
