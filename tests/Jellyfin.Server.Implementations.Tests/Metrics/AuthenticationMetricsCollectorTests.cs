using System;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Metrics;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public class AuthenticationMetricsCollectorTests
{
    [Fact]
    public void RefreshGauges_ExcludesDefaultProviderAndUsesStableProviderIds()
    {
        var defaultProviderId = typeof(DefaultAuthenticationProvider).FullName!;
        var ldapProviderName = "LDAP-" + Guid.NewGuid();

        var users = new[]
        {
            CreateUser("default", defaultProviderId),
            CreateUser("ldap-1", "external-ldap"),
            CreateUser("ldap-2", "external-ldap"),
            CreateUser("oidc-1", "external-oidc")
        };

        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.GetUsers()).Returns(users);
        userManager.Setup(x => x.GetAuthenticationProviders()).Returns(
        [
            new NameIdPair { Id = defaultProviderId, Name = "Default" },
            new NameIdPair { Id = "external-ldap", Name = ldapProviderName },
            new NameIdPair { Id = "external-oidc", Name = ldapProviderName }
        ]);

        var collector = new AuthenticationMetricsCollector(userManager.Object);

        var beforeLdap = JellyfinMetrics.UsersAuthenticatingExternal.WithLabels("external-ldap").Value;
        var beforeOidc = JellyfinMetrics.UsersAuthenticatingExternal.WithLabels("external-oidc").Value;
        var beforeDefault = JellyfinMetrics.UsersAuthenticatingExternal.WithLabels(defaultProviderId).Value;

        collector.RefreshGauges();

        Assert.Equal(beforeLdap + 2d, JellyfinMetrics.UsersAuthenticatingExternal.WithLabels("external-ldap").Value);
        Assert.Equal(beforeOidc + 1d, JellyfinMetrics.UsersAuthenticatingExternal.WithLabels("external-oidc").Value);
        Assert.Equal(beforeDefault, JellyfinMetrics.UsersAuthenticatingExternal.WithLabels(defaultProviderId).Value);
    }

    private static User CreateUser(string username, string authenticationProviderId = "default-provider")
    {
        var user = new User(username, authenticationProviderId, "default-password-reset");
        user.AuthenticationProviderId = authenticationProviderId;
        return user;
    }
}
