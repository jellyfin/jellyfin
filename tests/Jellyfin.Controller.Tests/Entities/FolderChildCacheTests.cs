using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

/// <summary>
/// Covers <see cref="Folder.ReleaseCachedChildren"/>, which a recursive scan calls as it unwinds so
/// the folders it walked do not keep the whole item graph of the library alive behind it.
/// </summary>
public class FolderChildCacheTests
{
    [Fact]
    public void ReleaseCachedChildren_MakesTheNextAccessReload()
    {
        var folder = new TrackingFolder();

        Assert.Empty(folder.Children);
        Assert.Equal(1, folder.LoadCount);

        // Second access is served from the cache on the instance.
        Assert.Empty(folder.Children);
        Assert.Equal(1, folder.LoadCount);

        folder.ReleaseCachedChildren();

        Assert.Empty(folder.Children);
        Assert.Equal(2, folder.LoadCount);
    }

    [Fact]
    public void ReleaseCachedChildren_ReachesEveryLevelBelow()
    {
        var leaf = new TrackingFolder();
        var middle = new TrackingFolder { Source = [leaf] };
        var root = new TrackingFolder { Source = [middle] };

        // Walk the whole tree, as a recursive scan does, so every level holds its children.
        Assert.Single(root.Children);
        Assert.Single(middle.Children);
        Assert.Empty(leaf.Children);
        Assert.Equal(1, root.LoadCount);
        Assert.Equal(1, middle.LoadCount);
        Assert.Equal(1, leaf.LoadCount);

        root.ReleaseCachedChildren();

        Assert.Single(root.Children);
        Assert.Single(middle.Children);
        Assert.Empty(leaf.Children);
        Assert.Equal(2, root.LoadCount);
        Assert.Equal(2, middle.LoadCount);
        Assert.Equal(2, leaf.LoadCount);
    }

    [Fact]
    public void ReleaseCachedChildren_LoadsNothingThatIsNotAlreadyHeld()
    {
        var leaf = new TrackingFolder();
        var root = new TrackingFolder { Source = [leaf] };

        root.ReleaseCachedChildren();

        Assert.Equal(0, root.LoadCount);
        Assert.Equal(0, leaf.LoadCount);
    }

    [Fact]
    public void ReleaseCachedChildren_TerminatesOnACycle()
    {
        var first = new TrackingFolder();
        var second = new TrackingFolder { Source = [first] };
        first.Source = [second];

        Assert.Single(first.Children);
        Assert.Single(second.Children);

        // Clearing before descending is what stops this from recursing forever.
        first.ReleaseCachedChildren();

        Assert.Equal(1, first.LoadCount);
        Assert.Equal(1, second.LoadCount);
    }

    private sealed class TrackingFolder : Folder
    {
        public int LoadCount { get; private set; }

        public IReadOnlyList<BaseItem> Source { get; set; } = [];

        protected override IReadOnlyList<BaseItem> LoadChildren()
        {
            LoadCount++;
            return Source;
        }
    }
}
