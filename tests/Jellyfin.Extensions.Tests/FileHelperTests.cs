using System.IO;
using Xunit;

namespace Jellyfin.Extensions.Tests;

public static class FileHelperTests
{
    [Fact]
    public static void CreateEmpty_Valid_Correct()
    {
        var path = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        var fileInfo = new FileInfo(path);

        Assert.False(fileInfo.Exists);

        FileHelper.CreateEmpty(path);

        fileInfo.Refresh();
        Assert.True(fileInfo.Exists);

        File.Delete(path);
    }
}
