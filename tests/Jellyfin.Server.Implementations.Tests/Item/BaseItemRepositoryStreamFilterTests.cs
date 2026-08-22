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

    // A series in the same library, so the folder branch of the resolution filter has a version group
    // to reach through as well: an SD episode whose second file is 4K.
    private readonly Guid _versionedSeries = Guid.NewGuid();
    private readonly Guid _versionedEpisode = Guid.NewGuid();
    private readonly Guid _episodeAlternate = Guid.NewGuid();

    // An unprobed primary: only its second file carries dimensions, and they are SD.
    private readonly Guid _unprobedMovie = Guid.NewGuid();
    private readonly Guid _unprobedAlternate = Guid.NewGuid();

    // A plain SD movie with no second file, as the control the version groups are read against.
    private readonly Guid _sdMovie = Guid.NewGuid();

    // An unprobed primary whose only second file is HD, so the HD bucket has to place it off nulls.
    private readonly Guid _hdOnlyByVersion = Guid.NewGuid();
    private readonly Guid _hdOnlyAlternate = Guid.NewGuid();

    // Three files for one movie: the HD one would place it in the HD bucket on its own, the 4K one has
    // to win. Only a group holding both can tell the HD bucket's upper guard from its lower one.
    private readonly Guid _threeWayMovie = Guid.NewGuid();
    private readonly Guid _threeWayHd = Guid.NewGuid();
    private readonly Guid _threeWay4K = Guid.NewGuid();

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

    [Fact]
    public void MaxWidth_ExcludesAnItemWhoseAlternateVersionBreachesTheBound()
    {
        // The SD primary is narrow enough on its own, but the 4K second file is what a caller would play.
        Assert.DoesNotContain(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { MaxWidth = 1920 }));
        Assert.Contains(_sdMovie, _repository.GetItemIdsList(new InternalItemsQuery { MaxWidth = 1920 }));
    }

    [Fact]
    public void MaxHeight_ExcludesAnItemWhoseAlternateVersionBreachesTheBound()
    {
        Assert.DoesNotContain(_versionedMovie, _repository.GetItemIdsList(new InternalItemsQuery { MaxHeight = 1080 }));
        Assert.Contains(_sdMovie, _repository.GetItemIdsList(new InternalItemsQuery { MaxHeight = 1080 }));
    }

    [Fact]
    public void IsHD_False_ExcludesAnSdPrimaryWhoseAlternateVersionIsBetter()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { IsHD = false });

        // 720x480 on its own, but the version group tops out at 4K.
        Assert.DoesNotContain(_versionedMovie, ids);
        Assert.Contains(_sdMovie, ids);
    }

    [Fact]
    public void IsHD_False_MatchesAPrimaryPlacedOnlyByItsAlternateVersion()
    {
        // The primary carries no dimensions at all; the SD second file is the group's best.
        Assert.Contains(_unprobedMovie, _repository.GetItemIdsList(new InternalItemsQuery { IsHD = false }));
    }

    [Fact]
    public void IsHD_True_ExcludesAnItemWhoseVersionGroupReaches4K()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { IsHD = true });

        Assert.DoesNotContain(_versionedMovie, ids);
        Assert.DoesNotContain(_unprobedMovie, ids);
        // The 1920-wide second file alone would say HD; the 4K third file is the group's best.
        Assert.DoesNotContain(_threeWayMovie, ids);
    }

    [Fact]
    public void Is4K_MatchesAnItemWhoseVersionGroupHoldsBothHdAnd4K()
    {
        Assert.Contains(_threeWayMovie, _repository.GetItemIdsList(new InternalItemsQuery { Is4K = true }));
    }

    [Fact]
    public void IsHD_True_MatchesAPrimaryPlacedOnlyByItsAlternateVersion()
    {
        // The primary carries no dimensions of its own; the HD second file is the group's best.
        Assert.Contains(_hdOnlyByVersion, _repository.GetItemIdsList(new InternalItemsQuery { IsHD = true }));
    }

    [Fact]
    public void Is4K_MatchesTheSeriesOfAnEpisodeWhoseAlternateVersionIs4K()
    {
        // The folder branch buckets a descendant the same way the item branch buckets a top-level item.
        Assert.Contains(_versionedSeries, _repository.GetItemIdsList(new InternalItemsQuery { Is4K = true }));
    }

    [Fact]
    public void IsHD_False_ExcludesTheSeriesOfAnSdEpisodeWithABetterAlternateVersion()
    {
        // Before the version group was consulted on descendants too, the SD episode alone matched here
        // while the same pair at top level did not.
        Assert.DoesNotContain(_versionedSeries, _repository.GetItemIdsList(new InternalItemsQuery { IsHD = false }));
    }

    [Theory]
    [InlineData("und")]
    [InlineData("UND")]
    public void HasNoAudioTrackWithLanguage_TreatsUndeterminedCaseInsensitively(string language)
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { HasNoAudioTrackWithLanguage = language });

        // The alternate version carries an audio track with no language, which is what "und" stands for,
        // so the item it is reported against does have one.
        Assert.DoesNotContain(_unprobedMovie, ids);
        Assert.Contains(_versionedMovie, ids);
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

        SeedVersionedSeries(context);
        SeedUnprobedVersionGroup(context);

        context.Chapters.Add(new Chapter
        {
            ItemId = _alternateVersion,
            ChapterIndex = 0,
            StartPositionTicks = 0,
            ImagePath = "/alternate-chapter.jpg",
            Item = null!
        });
    }

    // The same SD primary / 4K second file pair one level down, so the resolution filter has to answer
    // for the series off its descendants.
    private void SeedVersionedSeries(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _versionedSeries, Type = FolderType, Name = "Versioned series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _versionedEpisode, Type = MovieType, Name = "Versioned episode", Width = 720, Height = 480 });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _episodeAlternate,
            Type = MovieType,
            Name = "Versioned episode 4K",
            PrimaryVersionId = _versionedEpisode,
            Width = 3840,
            Height = 2160
        });

        context.AncestorIds.Add(new AncestorId
        {
            ItemId = _versionedSeries,
            ParentItemId = _versionLibrary,
            Item = null!,
            ParentItem = null!
        });

        foreach (var itemId in new[] { _versionedEpisode, _episodeAlternate })
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = _versionedSeries,
                Item = null!,
                ParentItem = null!
            });
        }
    }

    // A primary that was never probed, so only its second file can place it in a bucket. Its audio track
    // declares no language, which is what the "und" filters stand in for.
    private void SeedUnprobedVersionGroup(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _sdMovie, Type = MovieType, Name = "SD movie", Width = 720, Height = 480 });
        context.AncestorIds.Add(new AncestorId
        {
            ItemId = _sdMovie,
            ParentItemId = _versionLibrary,
            Item = null!,
            ParentItem = null!
        });

        context.BaseItems.Add(new BaseItemEntity { Id = _unprobedMovie, Type = MovieType, Name = "Unprobed movie" });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _unprobedAlternate,
            Type = MovieType,
            Name = "Unprobed movie SD",
            PrimaryVersionId = _unprobedMovie,
            Width = 720,
            Height = 480
        });

        foreach (var itemId in new[] { _unprobedMovie, _unprobedAlternate })
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
            ItemId = _unprobedAlternate,
            StreamIndex = 0,
            StreamType = MediaStreamTypeEntity.Audio,
            Item = null!
        });

        SeedMixedVersionGroups(context);
    }

    // The two groups that separate the HD bucket's lower bound from its upper one: one that only a 4K
    // third file keeps out of HD, and one that only an HD second file puts into it.
    private void SeedMixedVersionGroups(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _threeWayMovie, Type = MovieType, Name = "Three-way movie", Width = 720, Height = 480 });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _threeWayHd,
            Type = MovieType,
            Name = "Three-way movie HD",
            PrimaryVersionId = _threeWayMovie,
            Width = 1920,
            Height = 1080
        });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _threeWay4K,
            Type = MovieType,
            Name = "Three-way movie 4K",
            PrimaryVersionId = _threeWayMovie,
            Width = 3840,
            Height = 2160
        });

        context.BaseItems.Add(new BaseItemEntity { Id = _hdOnlyByVersion, Type = MovieType, Name = "HD only by version" });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _hdOnlyAlternate,
            Type = MovieType,
            Name = "HD only by version, HD file",
            PrimaryVersionId = _hdOnlyByVersion,
            Width = 1920,
            Height = 1080
        });

        foreach (var itemId in new[] { _threeWayMovie, _threeWayHd, _threeWay4K, _hdOnlyByVersion, _hdOnlyAlternate })
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = _versionLibrary,
                Item = null!,
                ParentItem = null!
            });
        }
    }
}
