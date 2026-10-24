using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class ItemTypeLookupTests
{
    public static TheoryData<BaseItemKind> MappedKinds()
        => new(new ItemTypeLookup().BaseItemKindNames.Keys);

    [Theory]
    [MemberData(nameof(MappedKinds))]
    public void BaseItemKindNames_Kind_NamesAnItemType(BaseItemKind kind)
    {
        var name = new ItemTypeLookup().BaseItemKindNames[kind];

        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t is not null);

        Assert.NotNull(type);
        Assert.True(typeof(BaseItem).IsAssignableFrom(type), $"{name} is not an item type.");
    }
}
