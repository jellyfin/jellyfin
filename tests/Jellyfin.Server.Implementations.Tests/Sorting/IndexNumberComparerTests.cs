using System;
using Emby.Server.Implementations.Sorting;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Sorting;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting;

public class IndexNumberComparerTests
{
    private readonly IndexNumberComparer _cmp = new IndexNumberComparer();

    public static TheoryData<BaseItem?, BaseItem?> Compare_GivenNull_ThrowsArgumentNullException_TestData()
        => new()
        {
            { null, new Audio() },
            { new Audio(), null }
        };

    [Theory]
    [MemberData(nameof(Compare_GivenNull_ThrowsArgumentNullException_TestData))]
    public void Compare_GivenNull_ThrowsArgumentNullException(BaseItem? x, BaseItem? y)
    {
        Assert.Throws<ArgumentNullException>(() => _cmp.Compare(x, y));
    }

    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(0, null, 1)]
    [InlineData(null, 0, -1)]
    [InlineData(1, 1, 0)]
    [InlineData(0, 1, -1)]
    [InlineData(1, 0, 1)]
    public void Compare_ValidIndices_SortsExpected(int? index1, int? index2, int expected)
    {
        BaseItem x = new Audio
        {
            IndexNumber = index1
        };
        BaseItem y = new Audio
        {
            IndexNumber = index2
        };

        Assert.Equal(expected, _cmp.Compare(x, y));
        Assert.Equal(-expected, _cmp.Compare(y, x));
    }
}
