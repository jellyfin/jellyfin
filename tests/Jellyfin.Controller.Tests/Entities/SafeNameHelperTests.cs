using System;
using System.IO;
using MediaBrowser.Controller.Extensions;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class SafeNameHelperTests
{
    private static readonly string _baseDir = OperatingSystem.IsWindows()
        ? @"C:\config\metadata\artists"
        : "/config/metadata/artists";

    [Fact]
    public void EnsureSafeName_ShortName_ReturnsUnchanged()
    {
        var result = SafeNameHelper.EnsureSafeName(_baseDir, "Metallica");
        Assert.Equal("Metallica", result);
    }

    [Fact]
    public void EnsureSafeName_NameExceedsMaxLength_Truncates()
    {
        var longName = new string('X', 300);
        var result = SafeNameHelper.EnsureSafeName(_baseDir, longName);

        Assert.Equal(SafeNameHelper.MaxNameLength, result.Length);
        Assert.Equal(new string('X', SafeNameHelper.MaxNameLength), result);
    }

    [Fact]
    public void EnsureSafeName_NameExactlyAtMaxLength_ReturnsUnchanged()
    {
        var name = new string('A', SafeNameHelper.MaxNameLength);
        var result = SafeNameHelper.EnsureSafeName(_baseDir, name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void EnsureSafeName_FullPathExceedsMaxFullPath_TruncatesFurther()
    {
        var deepBaseDir = new string('d', SafeNameHelper.MaxFullPathLength - 50);
        var name = new string('N', 200);
        var result = SafeNameHelper.EnsureSafeName(deepBaseDir, name);

        var fullPath = Path.Combine(deepBaseDir, result);
        Assert.True(fullPath.Length <= SafeNameHelper.MaxFullPathLength);
    }

    [Theory]
    [InlineData("")]
    [InlineData("短")]
    [InlineData("🎵")]
    public void EnsureSafeName_EdgeCaseNames_DoesNotThrow(string name)
    {
        var result = SafeNameHelper.EnsureSafeName(_baseDir, name);
        Assert.NotNull(result);
    }

    [Fact]
    public void EnsureSafeName_ConcatenatedTagString_Truncates()
    {
        var longName = new string('A', 50)
            + ", " + new string('B', 50)
            + ", " + new string('C', 50)
            + ", " + new string('D', 50)
            + ", " + new string('E', 50)
            + ", " + new string('F', 50);

        var result = SafeNameHelper.EnsureSafeName(_baseDir, longName);

        Assert.True(result.Length <= SafeNameHelper.MaxNameLength);
        var fullPath = Path.Combine(_baseDir, result);
        Assert.True(fullPath.Length <= SafeNameHelper.MaxFullPathLength);
    }

    [Fact]
    public void EnsureSafeName_Deterministic_SameInputSameOutput()
    {
        var longName = new string('Z', 300);

        var result1 = SafeNameHelper.EnsureSafeName(_baseDir, longName);
        var result2 = SafeNameHelper.EnsureSafeName(_baseDir, longName);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void MaxNameLength_IsWithinFilesystemLimits()
    {
        Assert.InRange(SafeNameHelper.MaxNameLength, 1, 255);
    }

    [Fact]
    public void MaxFullPathLength_IsPlatformAware()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(248, SafeNameHelper.MaxFullPathLength);
        }
        else
        {
            Assert.Equal(4069, SafeNameHelper.MaxFullPathLength);
        }
    }
}
