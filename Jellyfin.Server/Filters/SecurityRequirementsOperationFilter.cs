using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Security requirement operation filter.
/// </summary>
public class SecurityRequirementsOperationFilter : IOperationFilter
{
    private const string DefaultAuthPolicy = "DefaultAuthorization";
    private static readonly Type _attributeType = typeof(AuthorizeAttribute);

    private readonly IAuthorizationPolicyProvider _authorizationPolicyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityRequirementsOperationFilter"/> class.
    /// </summary>
    /// <param name="authorizationPolicyProvider">The authorization policy provider.</param>
    public SecurityRequirementsOperationFilter(IAuthorizationPolicyProvider authorizationPolicyProvider)
    {
        _authorizationPolicyProvider = authorizationPolicyProvider;
    }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiredScopes = new List<string>();

        var requiresAuth = false;
        // Add all method scopes.
        foreach (var authorizeAttribute in context.MethodInfo.GetCustomAttributes(_attributeType, true).Cast<AuthorizeAttribute>())
        {
            requiresAuth = true;
            var policy = authorizeAttribute.Policy ?? DefaultAuthPolicy;
            if (!requiredScopes.Contains(policy, StringComparer.Ordinal))
            {
                requiredScopes.Add(policy);
            }
        }

        // Add controller scopes if any.
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(_attributeType, true).Cast<AuthorizeAttribute>();
        if (controllerAttributes is not null)
        {
            foreach (var authorizeAttribute in controllerAttributes)
            {
                requiresAuth = true;
                var policy = authorizeAttribute.Policy ?? DefaultAuthPolicy;
                if (!requiredScopes.Contains(policy, StringComparer.Ordinal))
                {
                    requiredScopes.Add(policy);
                }
            }
        }

        if (!requiresAuth)
        {
            return;
        }

        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        }

        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
        }

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = AuthenticationSchemes.CustomAuthentication
            },
        };

        // Add DefaultAuthorization scope to any endpoint that has a policy with a requirement that is a subset of DefaultAuthorization.
        if (!requiredScopes.Contains(DefaultAuthPolicy.AsSpan(), StringComparison.Ordinal))
        {
            foreach (var scope in requiredScopes)
            {
                var authorizationPolicy = _authorizationPolicyProvider.GetPolicyAsync(scope).GetAwaiter().GetResult();
                if (authorizationPolicy is not null
                    && authorizationPolicy.Requirements.Any(r => r is DefaultAuthorizationRequirement))
                {
                    requiredScopes.Add(DefaultAuthPolicy);
                    break;
                }
            }
        }

        operation.Security = [new OpenApiSecurityRequirement { [scheme] = requiredScopes }];
    }
}
