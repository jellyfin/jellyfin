using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy
{
    public class FirstTimeSetupOrElevatedRequirement : IAuthorizationRequirement
    {
    }
}
