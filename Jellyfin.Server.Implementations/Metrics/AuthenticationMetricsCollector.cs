using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects authentication provider metrics for Jellyfin users.
    /// </summary>
    public sealed class AuthenticationMetricsCollector
    {
        private readonly IUserManager _userManager;
        private readonly HashSet<string> _providerKeys = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationMetricsCollector"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        public AuthenticationMetricsCollector(IUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Refreshes external authentication provider counts from the current users.
        /// </summary>
        public void RefreshGauges()
        {
            var users = _userManager.GetUsers().ToArray();
            var defaultProviderId = typeof(DefaultAuthenticationProvider).FullName ?? string.Empty;
            var providerNames = _userManager.GetAuthenticationProviders()
                .Where(p => !string.IsNullOrWhiteSpace(p.Id))
                .ToDictionary(p => p.Id, p => p.Name, StringComparer.OrdinalIgnoreCase);

            var snapshot = users
                .Where(u => !string.IsNullOrWhiteSpace(u.AuthenticationProviderId))
                .Select(u =>
                {
                    var providerId = u.AuthenticationProviderId;
                    var providerName = providerNames.TryGetValue(providerId, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : providerId;
                    return providerName;
                })
                .Where(providerName =>
                    !string.Equals(defaultProviderId, providerName, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals("Default", providerName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(MetricsSnapshot.Sanitize)
                .ToDictionary(group => group.Key, group => (double)group.Count(), StringComparer.OrdinalIgnoreCase);

            MetricsSnapshot.SetSnapshot(JellyfinMetrics.UsersAuthenticatingExternal, snapshot, _providerKeys);
        }
    }
}
