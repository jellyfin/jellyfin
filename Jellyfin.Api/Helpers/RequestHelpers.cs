#nullable enable

using System;
using System.Linq;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Request Helpers.
    /// </summary>
    public static class RequestHelpers
    {
        /// <summary>
        /// Get Guid array from string.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>Guid array.</returns>
        public static Guid[] GetGuids(string? value)
        {
            if (value == null)
            {
                return Array.Empty<Guid>();
            }

            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => new Guid(i))
                .ToArray();
        }
    }
}
