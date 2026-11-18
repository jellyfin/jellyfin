using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Users;
using Xunit;

namespace Jellyfin.Model.Tests.Users;

public class UserPolicyTests
{
    [Fact]
    public void MaxActiveVideoStreams_DefaultsToUnlimitedWithoutBeingSupplied()
    {
        var policy = new UserPolicy();

        Assert.Equal(0, policy.MaxActiveVideoStreams);
        Assert.False(policy.HasMaxActiveVideoStreams);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(-1, false)]
    public void MaxActiveVideoStreams_ValidatesNonNegativeValues(int value, bool expectedValid)
    {
        var policy = new UserPolicy { MaxActiveVideoStreams = value };
        var context = new ValidationContext(policy) { MemberName = nameof(UserPolicy.MaxActiveVideoStreams) };

        Assert.Equal(expectedValid, Validator.TryValidateProperty(value, context, new List<ValidationResult>()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaxActiveVideoStreams_JsonDistinguishesOmissionFromExplicitZero(bool camelCase)
    {
        var options = camelCase ? JsonDefaults.CamelCaseOptions : JsonDefaults.PascalCaseOptions;
        var propertyName = camelCase ? "maxActiveVideoStreams" : "MaxActiveVideoStreams";
        var absent = JsonSerializer.Deserialize<UserPolicy>("{}", options)!;
        var explicitZero = JsonSerializer.Deserialize<UserPolicy>($"{{\"{propertyName}\":0}}", options)!;

        Assert.False(absent.HasMaxActiveVideoStreams);
        Assert.Equal(0, absent.MaxActiveVideoStreams);
        Assert.True(explicitZero.HasMaxActiveVideoStreams);
        Assert.Equal(0, explicitZero.MaxActiveVideoStreams);

        using var output = JsonDocument.Parse(JsonSerializer.Serialize(explicitZero, options));
        Assert.Equal(0, output.RootElement.GetProperty(propertyName).GetInt32());
        Assert.False(output.RootElement.TryGetProperty("HasMaxActiveVideoStreams", out _));
        Assert.False(output.RootElement.TryGetProperty("hasMaxActiveVideoStreams", out _));
    }
}
