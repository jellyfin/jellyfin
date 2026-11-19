using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Localization;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Localization;

public class RatingsStateStoreTests
{
    private readonly ServerConfiguration _serverConfiguration = new() { MetadataCountryCode = "US" };
    private readonly Mock<IServerConfigurationManager> _configurationManager = new();
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly List<VirtualFolderInfo> _libraries = [];

    private RatingsStateData _stored = new();
    private string _version = "v1";
    private int _saveCount;

    public RatingsStateStoreTests()
    {
        _configurationManager.SetupGet(x => x.Configuration).Returns(_serverConfiguration);
        _configurationManager
            .Setup(x => x.GetConfiguration(RatingsStateConfigurationFactory.Key))
            .Returns(() => _stored);
        _configurationManager
            .Setup(x => x.SaveConfiguration(RatingsStateConfigurationFactory.Key, It.IsAny<object>()))
            .Callback<string, object>((_, options) =>
            {
                _stored = (RatingsStateData)options;
                _saveCount++;
            });
        _libraryManager.Setup(x => x.GetVirtualFolders()).Returns(_libraries);
    }

    private RatingsStateStore CreateStore()
        => new(_configurationManager.Object, _libraryManager.Object, () => _version);

    private void AddLibrary(string id, string? country)
        => _libraries.Add(new VirtualFolderInfo { ItemId = id, Name = id, LibraryOptions = new LibraryOptions { MetadataCountryCode = country } });

    [Fact]
    public void IsRecomputeNeeded_NothingStored_True()
    {
        Assert.True(CreateStore().IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_AfterSaveOfCurrentState_False()
    {
        var store = CreateStore();

        store.Save(store.ComputeCurrent());

        Assert.False(store.IsRecomputeNeeded());
        Assert.Equal(1, _saveCount);
    }

    [Fact]
    public void IsRecomputeNeeded_RatingsVersionDiffers_True()
    {
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        _version = "v2";

        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_ServerCountryChanges_True()
    {
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        _serverConfiguration.MetadataCountryCode = "DE";

        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_LibraryCountryChanges_True()
    {
        AddLibrary("movies", null);
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        _libraries[0].LibraryOptions.MetadataCountryCode = "FR";

        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_LibraryWithCountryAdded_True()
    {
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        AddLibrary("shows", "GB");

        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_LibraryWithoutCountryAddedOrRemoved_False()
    {
        AddLibrary("movies", "US");
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        AddLibrary("music", null);
        AddLibrary("books", " ");
        Assert.False(store.IsRecomputeNeeded());

        _libraries.RemoveAt(2);
        Assert.False(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_LibraryCountryRemoved_True()
    {
        AddLibrary("movies", "FR");
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        _libraries[0].LibraryOptions.MetadataCountryCode = null;

        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public void IsRecomputeNeeded_UnrelatedChange_False()
    {
        AddLibrary("movies", "US");
        var store = CreateStore();
        store.Save(store.ComputeCurrent());

        _serverConfiguration.PreferredMetadataLanguage = "de";
        _libraries[0].LibraryOptions.PreferredMetadataLanguage = "de";
        _libraries[0].LibraryOptions.MetadataCountryCode = " us ";

        Assert.False(store.IsRecomputeNeeded());
    }

    [Fact]
    public void ComputeCurrent_LibraryOrderDoesNotMatter()
    {
        AddLibrary("movies", "US");
        AddLibrary("shows", "DE");
        var store = CreateStore();
        var first = store.ComputeCurrent();

        _libraries.Reverse();

        Assert.Equal(first, store.ComputeCurrent());
    }

    [Fact]
    public void ComputeCurrent_FolderWithoutLibraryOptionsIsIgnored()
    {
        var store = CreateStore();
        var without = store.ComputeCurrent();

        _libraries.Add(new VirtualFolderInfo { Name = "unscanned", LibraryOptions = null! });

        Assert.Equal(without, store.ComputeCurrent());
    }

    [Fact]
    public void Save_PersistsBothValues()
    {
        var store = CreateStore();

        store.Save(new RatingsState("abc", "def"));

        Assert.Equal(new RatingsState("abc", "def"), store.GetStored());
    }
}
