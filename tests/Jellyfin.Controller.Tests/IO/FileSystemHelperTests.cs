using System;
using System.IO;
using MediaBrowser.Controller.IO;
using Xunit;

namespace Jellyfin.Controller.Tests.IO;

public class FileSystemHelperTests
{
    private static readonly string _parentPath = Path.Combine(Path.GetTempPath(), "jellyfin-test", "root", "default");

    [Theory]
    [InlineData("Movies")]
    [InlineData("My Movies")]
    [InlineData("..2")]
    [InlineData("a.b")]
    public void GetChildPath_ValidName_ReturnsPathInsideParent(string name)
    {
        var path = FileSystemHelper.GetChildPath(_parentPath, name);

        Assert.Equal(Path.Combine(_parentPath, name), path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData("../../etc")]
    [InlineData("Movies/../..")]
    [InlineData("/var/lib/jellyfin/data")]
    [InlineData("sub/folder")]
    [InlineData("with\0null")]
    public void GetChildPath_EscapingName_ReturnsNull(string name)
    {
        Assert.Null(FileSystemHelper.GetChildPath(_parentPath, name));
    }

    [Theory]
    [InlineData("..\\..")]
    [InlineData("sub\\folder")]
    [InlineData("C:\\Windows")]
    public void GetChildPath_WindowsSeparator_DoesNotEscapeParent(string name)
    {
        var path = FileSystemHelper.GetChildPath(_parentPath, name);

        // On Windows these are rejected outright, on other platforms a backslash is a legal file name character.
        Assert.True(path is null || string.Equals(Path.GetDirectoryName(path), _parentPath, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("...")]
    [InlineData("Movies.")]
    [InlineData("Movies ")]
    public void GetChildPath_TrailingDotOrSpace_RejectedOnWindows(string name)
    {
        var path = FileSystemHelper.GetChildPath(_parentPath, name);

        if (OperatingSystem.IsWindows())
        {
            // Windows trims trailing dots and spaces, so the name would resolve to the parent or to a different child.
            Assert.Null(path);
        }
        else
        {
            Assert.Equal(Path.Combine(_parentPath, name), path);
        }
    }

    [Fact]
    public void GetChildPath_ParentWithTrailingSeparator_ReturnsPathInsideParent()
    {
        var path = FileSystemHelper.GetChildPath(_parentPath + Path.DirectorySeparatorChar, "Movies");

        Assert.Equal(Path.Combine(_parentPath, "Movies"), path);
    }
}
