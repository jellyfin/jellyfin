using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.ShareLinks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// Unit tests for the media sharing policy matrix: every server-wide policy crossed with
/// every per-user preference, plus the "default is No / default is Default" guarantees.
/// </summary>
public sealed class ShareLinkPolicyEvaluatorTests
{
    [Theory]
    [InlineData(MediaSharingPolicy.Disabled, UserMediaSharingPolicy.Default, false)]
    [InlineData(MediaSharingPolicy.Disabled, UserMediaSharingPolicy.Enabled, false)]
    [InlineData(MediaSharingPolicy.Disabled, UserMediaSharingPolicy.Disabled, false)]
    [InlineData(MediaSharingPolicy.Enabled, UserMediaSharingPolicy.Default, true)]
    [InlineData(MediaSharingPolicy.Enabled, UserMediaSharingPolicy.Enabled, true)]
    [InlineData(MediaSharingPolicy.Enabled, UserMediaSharingPolicy.Disabled, true)]
    [InlineData(MediaSharingPolicy.PerUserDefaultNo, UserMediaSharingPolicy.Default, false)]
    [InlineData(MediaSharingPolicy.PerUserDefaultNo, UserMediaSharingPolicy.Enabled, true)]
    [InlineData(MediaSharingPolicy.PerUserDefaultNo, UserMediaSharingPolicy.Disabled, false)]
    [InlineData(MediaSharingPolicy.PerUserDefaultYes, UserMediaSharingPolicy.Default, true)]
    [InlineData(MediaSharingPolicy.PerUserDefaultYes, UserMediaSharingPolicy.Enabled, true)]
    [InlineData(MediaSharingPolicy.PerUserDefaultYes, UserMediaSharingPolicy.Disabled, false)]
    public void IsSharingAllowed_EvaluatesFullMatrix(
        MediaSharingPolicy serverPolicy,
        UserMediaSharingPolicy userPolicy,
        bool expected)
    {
        Assert.Equal(expected, ShareLinkPolicyEvaluator.IsSharingAllowed(serverPolicy, userPolicy));
    }

    [Theory]
    [InlineData(MediaSharingPolicy.Disabled, true)]
    [InlineData(MediaSharingPolicy.Enabled, true)]
    [InlineData(MediaSharingPolicy.PerUserDefaultNo, false)]
    [InlineData(MediaSharingPolicy.PerUserDefaultYes, false)]
    public void IsServerPolicyHard_IsTrueOnlyForHardAnswers(MediaSharingPolicy serverPolicy, bool expected)
    {
        Assert.Equal(expected, ShareLinkPolicyEvaluator.IsServerPolicyHard(serverPolicy));
    }

    [Fact]
    public void Defaults_AreSharingDisabledAndPerUserUnset()
    {
        Assert.Equal(MediaSharingPolicy.Disabled, default(MediaSharingPolicy));
        Assert.Equal(MediaSharingPolicy.Disabled, new ServerConfiguration().MediaSharingPolicy);
        Assert.Equal(UserMediaSharingPolicy.Default, default(UserMediaSharingPolicy));
        Assert.Equal(UserMediaSharingPolicy.Default, new UserPolicy().MediaSharingPolicy);
        Assert.Equal(
            UserMediaSharingPolicy.Default,
            new User("alice", "auth-provider", "reset-provider").MediaSharingPolicy);
    }
}
