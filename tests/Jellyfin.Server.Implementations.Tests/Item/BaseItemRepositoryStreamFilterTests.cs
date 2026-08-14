using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the filters resolving "folders with a matching descendant" through
/// <see cref="DescendantQueryHelper.GetFolderIdsMatching"/>, positive and negated.
/// </summary>
public sealed class BaseItemRepositoryStreamFilterTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _withSubtitles = Guid.NewGuid();
    private readonly Guid _withoutSubtitles = Guid.NewGuid();
    private readonly Guid _collection = Guid.NewGuid();
    private readonly Guid _linkedSeries = Guid.NewGuid();
    private readonly Guid _linkedEpisode = Guid.NewGuid();

    // A version group in a library of its own, so it cannot move the assertions above: an SD primary
    // that carries nothing, and a 4K second file carrying the subtitles, chapter image and audio.
    private readonly Guid _versionLibrary = Guid.NewGuid();
    private readonly Guid _versionedMovie = Guid.NewGuid();
    private readonly Guid _alternateVersion = Guid.NewGuid();

    public BaseItemRepositoryStreamFilterTests()
    {
        using (var ctx = CreateDbContext())
        {
            Seed(ctx);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void HasSubtitles_MatchesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = true });

        Assert.Contains(_withSubtitles, ids);
        // The library is a folder, and it has a descendant with subtitles.
        Assert.Contains(_library, ids);
        Assert.DoesNotContain(_withoutSubtitles, ids);
    }

    [Fact]
    public void HasSubtitles_Negated_ExcludesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = false });

        Assert.Contains(_withoutSubtitles, ids);
        Assert.DoesNotContain(_withSubtitles, ids);
        Assert.DoesNotContain(_library, ids);
    }

    [Fact]
    public void SubtitleLanguages_MatchesTheRequestedLanguageOnly()
    {
        Assert.Contains(_withSubtitles, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["ger"] }));
        Assert.DoesNotContain(_withSubtitles, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["fre"] }));
    }

    [Fact]
    public void HasNoSubtitleTrackWithLanguage_ExcludesTheMatchingItemAndFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasNoSubtitleTrackWithLanguage = "ger" });

        Assert.Contains(_withoutSubtitles, ids);
        Assert.DoesNotContain(_withSubtitles, ids);
        Assert.DoesNotContain(_library, ids);
    }

    [Fact]
    public void HasSubtitles_MatchesACollectionLinkingAFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = true });

        Assert.Contains(_linkedSeries, ids);
        Assert.Contains(_collection, ids);
    }

    [Fact]
    public void HasSubtitles_Negated_ExcludesACollectionLinkingAFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = false });

        Assert.DoesNotContain(_linkedSeries, ids);
        Assert.DoesNotContain(_collection, ids);
    }

    [Fact]
    public void HasChapterImages_MatchesTheItemAndItsParentFolder()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasChapterImages = true });

        Assert.Contains(_withSubtitles, ids);
        Assert.Contains(_library, ids);
        Assert.DoesNotContain(_withoutSubtitles, ids);
    }

    [Fact]
    public void HasSubtitles_MatchesAnItemWhoseAlternateVersionCarriesThem()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = true });

        Assert.Contains(_versionedMovie, ids);
        Assert.Contains(_versionLibrary, ids);
        // The second file is never listed on its own, which is why its tracks have to count for the primary.
        Assert.DoesNotContain(_alternateVersion, ids);
    }

    [Fact]
    public void HasSubtitles_Negated_ExcludesAnItemWhoseAlternateVersionCarriesThem()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasSubtitles = false });

        Assert.DoesNotContain(_versionedMovie, ids);
        Assert.DoesNotContain(_versionLibrary, ids);
    }

    [Fact]
    public void SubtitleLanguages_MatchesTheLanguageOnAnAlternateVersion()
    {
        Assert.Contains(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["ger"] }));
        Assert.DoesNotContain(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { SubtitleLanguages = ["fre"] }));
    }

    [Fact]
    public void HasNoSubtitleTrackWithLanguage_ExcludesAnItemWhoseAlternateVersionHasIt()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasNoSubtitleTrackWithLanguage = "ger" });

        Assert.DoesNotContain(_versionedMovie, ids);
        Assert.DoesNotContain(_versionLibrary, ids);
    }

    [Fact]
    public void AudioLanguages_MatchesTheLanguageOnAnAlternateVersion()
    {
        Assert.Contains(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { AudioLanguages = ["fre"] }));
    }

    [Fact]
    public void HasNoAudioTrackWithLanguage_ExcludesAnItemWhoseAlternateVersionHasIt()
    {
        Assert.DoesNotContain(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { HasNoAudioTrackWithLanguage = "fre" }));
    }

    [Fact]
    public void HasChapterImages_MatchesAnItemWhoseAlternateVersionCarriesThem()
    {
        Assert.Contains(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { HasChapterImages = true }));
    }

    [Fact]
    public void Is4K_MatchesAnItemWhoseAlternateVersionIs4K()
    {
        // The primary file is SD; the resolution a caller can actually play is the 4K second file's.
        Assert.Contains(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { Is4K = true }));
    }

    [Fact]
    public void MinWidth_MatchesAnItemWhoseAlternateVersionIsWideEnough()
    {
        Assert.Contains(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { MinWidth = 3000 }));
        Assert.DoesNotContain(_withSubtitles, _repository.GetItemIdsList(new InternalItemsQuery { MinWidth = 3000 }));
    }

    private void Seed(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = FolderType, Name = "Library", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _withSubtitles, Type = MovieType, Name = "With subtitles" });
        context.BaseItems.Add(new BaseItemEntity { Id = _withoutSubtitles, Type = MovieType, Name = "Without subtitles" });

        foreach (var itemId in new[] { _withSubtitles, _withoutSubtitles })
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = _library,
                Item = null!,
                ParentItem = null!
            });

            context.MediaStreamInfos.Add(new MediaStreamInfo
            {
                ItemId = itemId,
                StreamIndex = 0,
                StreamType = MediaStreamTypeEntity.Video,
                Item = null!
            });
        }

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _withSubtitles,
            StreamIndex = 1,
            StreamType = MediaStreamTypeEntity.Subtitle,
            Language = "ger",
            Item = null!
        });

        // A collection linking a folder: the match is two edges away, one link then one closure hop.
        context.BaseItems.Add(new BaseItemEntity { Id = _collection, Type = BoxSetType, Name = "Collection", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _linkedSeries, Type = FolderType, Name = "Linked series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _linkedEpisode, Type = MovieType, Name = "Linked episode" });

        context.AncestorIds.Add(new AncestorId
        {
            ItemId = _linkedEpisode,
            ParentItemId = _linkedSeries,
            Item = null!,
            ParentItem = null!
        });

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _collection,
            ChildId = _linkedSeries,
            ChildType = LinkedChildType.Manual,
            SortOrder = 0
        });

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _linkedEpisode,
            StreamIndex = 0,
            StreamType = MediaStreamTypeEntity.Subtitle,
            Language = "ger",
            Item = null!
        });

        context.Chapters.Add(new Chapter
        {
            ItemId = _withSubtitles,
            ChapterIndex = 0,
            StartPositionTicks = 0,
            ImagePath = "/chapter.jpg",
            Item = null!
        });

        SeedVersionGroup(context);

        context.SaveChanges();
    }

    // An SD primary whose only extras live on a 4K second file, so every filter has to reach through
    // PrimaryVersionId to answer correctly.
    private void SeedVersionGroup(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _versionLibrary, Type = FolderType, Name = "Version library", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _versionedMovie, Type = MovieType, Name = "Versioned movie", Width = 720, Height = 480 });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _alternateVersion,
            Type = MovieType,
            Name = "Versioned movie 4K",
            PrimaryVersionId = _versionedMovie,
            Width = 3840,
            Height = 2160
        });

        foreach (var itemId in new[] { _versionedMovie, _alternateVersion })
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = _versionLibrary,
                Item = null!,
                ParentItem = null!
            });
        }

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _alternateVersion,
            StreamIndex = 0,
            StreamType = MediaStreamTypeEntity.Subtitle,
            Language = "ger",
            Item = null!
        });

        context.MediaStreamInfos.Add(new MediaStreamInfo
        {
            ItemId = _alternateVersion,
            StreamIndex = 1,
            StreamType = MediaStreamTypeEntity.Audio,
            Language = "fre",
            Item = null!
        });

        context.Chapters.Add(new Chapter
        {
            ItemId = _alternateVersion,
            ChapterIndex = 0,
            StartPositionTicks = 0,
            ImagePath = "/alternate-chapter.jpg",
            Item = null!
        });
    }
}
