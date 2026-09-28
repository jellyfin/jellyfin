using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.TV;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.TV;

/// <summary>
/// Covers reconciling missing episodes against the episodes the user has, which cannot be done on episode
/// numbers alone: aired, DVD and absolute orders number the same episodes differently, so the tail of the
/// longer order is the episodes the user owns rather than the ones they lack.
/// </summary>
public class SeriesMetadataServiceTests
{
    public SeriesMetadataServiceTests()
    {
        // An episode with a path asks the file system what kind of location it is. Only set when nothing else
        // in the assembly has, since it is process-wide.
        BaseItem.FileSystem ??= Mock.Of<IFileSystem>();
    }

    [Fact]
    public void IsAlreadyPresent_VirtualEpisodeCarriesTheIdOfAPresentOne_ReturnsTrue()
    {
        // The Six Million Dollar Man S3: 21 files numbered in DVD order, 23 episodes in aired order, so
        // aired 22 and 23 are the files stored as 20 and 21.
        var present = SeriesMetadataService.GetPresentEpisodeIds(
        [
            Physical(20, (MetadataProvider.Tvdb, "236715")),
            Physical(21, (MetadataProvider.Tvdb, "236716"))
        ]);

        Assert.True(SeriesMetadataService.IsAlreadyPresent(Virtual(22, (MetadataProvider.Tvdb, "236715")), present));
        Assert.True(SeriesMetadataService.IsAlreadyPresent(Virtual(23, (MetadataProvider.Tvdb, "236716")), present));
    }

    [Fact]
    public void IsAlreadyPresent_GenuinelyMissingEpisode_ReturnsFalse()
    {
        // The aired-only two-parter the user really does not have keeps its missing episodes.
        var present = SeriesMetadataService.GetPresentEpisodeIds(
        [
            Physical(20, (MetadataProvider.Tvdb, "236715")),
            Physical(21, (MetadataProvider.Tvdb, "236716"))
        ]);

        Assert.False(SeriesMetadataService.IsAlreadyPresent(Virtual(15, (MetadataProvider.Tvdb, "4273533")), present));
    }

    [Fact]
    public void GetPresentEpisodeIds_IdSharedBySeveralEpisodes_IsNotAnIdentity()
    {
        // A series' own IMDb id lands on every episode, and TMDb reports an unknown TVRage id as 0. Taking
        // either for an episode identity would let one file delete every missing episode of the series.
        var present = SeriesMetadataService.GetPresentEpisodeIds(
        [
            Physical(1, (MetadataProvider.Imdb, "tt0836592"), (MetadataProvider.TvRage, "0"), (MetadataProvider.Tvdb, "1001")),
            Physical(2, (MetadataProvider.Imdb, "tt0836592"), (MetadataProvider.TvRage, "0"), (MetadataProvider.Tvdb, "1002"))
        ]);

        Assert.False(SeriesMetadataService.IsAlreadyPresent(Virtual(3, (MetadataProvider.Imdb, "tt0836592")), present));
        Assert.False(SeriesMetadataService.IsAlreadyPresent(Virtual(4, (MetadataProvider.TvRage, "0")), present));
        Assert.True(SeriesMetadataService.IsAlreadyPresent(Virtual(5, (MetadataProvider.Tvdb, "1001")), present));
    }

    [Fact]
    public void GetPresentEpisodeIds_VirtualEpisode_DoesNotCountAsPresent()
    {
        // Two virtual episodes for the same episode must not cancel each other out into "present".
        var present = SeriesMetadataService.GetPresentEpisodeIds(
        [
            Virtual(22, (MetadataProvider.Tvdb, "236715"))
        ]);

        Assert.Empty(present);
    }

    [Fact]
    public void GetPresentEpisodeIds_BlankStoredId_IsIgnored()
    {
        // SetProviderId refuses these, but a row written by an older version can still hold one.
        var episode = Physical(1);
        episode.ProviderIds["Tvdb"] = " ";

        Assert.Empty(SeriesMetadataService.GetPresentEpisodeIds([episode]));
    }

    private static Episode Physical(int indexNumber, params (MetadataProvider Provider, string Id)[] providerIds)
        => WithIds(new Episode { IndexNumber = indexNumber, Path = $"/media/show/Season 03/E{indexNumber}.mkv" }, providerIds);

    private static Episode Virtual(int indexNumber, params (MetadataProvider Provider, string Id)[] providerIds)
        => WithIds(new Episode { IndexNumber = indexNumber, IsVirtualItem = true }, providerIds);

    private static Episode WithIds(Episode episode, IEnumerable<(MetadataProvider Provider, string Id)> providerIds)
    {
        foreach (var (provider, id) in providerIds)
        {
            episode.SetProviderId(provider, id);
        }

        return episode;
    }
}
