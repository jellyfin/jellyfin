using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class InternalItemsQueryTests
{
    public static TheoryData<ItemFilter[]> ApplyFilters_Invalid()
    {
        var data = new TheoryData<ItemFilter[]>();
        data.Add([ItemFilter.IsFolder, ItemFilter.IsNotFolder]);
        data.Add([ItemFilter.IsPlayed, ItemFilter.IsUnplayed]);
        data.Add([ItemFilter.Likes, ItemFilter.Dislikes]);
        return data;
    }

    [Theory]
    [MemberData(nameof(ApplyFilters_Invalid))]
    public void ApplyFilters_Invalid_ThrowsArgumentException(ItemFilter[] filters)
    {
        var query = new InternalItemsQuery();
        Assert.Throws<ArgumentException>(() => query.ApplyFilters(filters));
    }
}
