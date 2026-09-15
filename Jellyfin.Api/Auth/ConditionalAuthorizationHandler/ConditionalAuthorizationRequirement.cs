using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.ConditionalAuthorizationHandler;

/// <summary>
/// A conditional authorization requirement for a specific route kind.
/// </summary>
public class ConditionalAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="policyName">The policy name that may require authorization.</param>
    public ConditionalAuthorizationRequirement(string policyName)
    {
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the policy name that may require authorization.
    /// </summary>
    public string PolicyName { get; }
}
