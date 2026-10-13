using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the repair of the PrimaryVersionId the item queries hide an alternate version by,
/// including the link shapes that would otherwise leave a whole version group hidden.
/// </summary>
public sealed class RepairAlternateVersionLinksTests : DatabaseTestFixture
{
    [Fact]
    public async Task PerformAsync_VersionLinkedToPrimary_PointsItAtThePrimary()
    {
        var primaryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var versionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Seed(
            [(primaryId, null), (versionId, null)],
            [(primaryId, versionId)]);

        await PerformAsync();

        using var context = CreateDbContext();
        AssertIsVersionOf(context, versionId, primaryId);
        AssertIsPrimary(context, primaryId);
    }

    [Fact]
    public async Task PerformAsync_VideosLinkedAsEachOthersVersion_KeepsOneOfThemVisible()
    {
        var firstId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var secondId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        // Each one claims the other as its version, so pointing both at their link would hide the
        // group in its entirety.
        Seed(
            [(firstId, null), (secondId, null)],
            [(firstId, secondId), (secondId, firstId)]);

        await PerformAsync();

        using var context = CreateDbContext();
        var first = Get(context, firstId);
        var second = Get(context, secondId);

        var primary = first.PrimaryVersionId is null ? first : second;
        var version = first.PrimaryVersionId is null ? second : first;

        Assert.Null(primary.PrimaryVersionId);
        AssertIsPrimary(context, primary.Id);
        AssertIsVersionOf(context, version.Id, primary.Id);
    }

    [Fact]
    public async Task PerformAsync_PrimaryStillPointingAtItsOwnVersion_ClearsTheStalePrimary()
    {
        var primaryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var versionId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        // The primary was promoted over the version it now heads, but kept the pointer to it: the
        // repair below would point the version back and leave both hidden.
        Seed(
            [(primaryId, versionId), (versionId, null)],
            [(primaryId, versionId)]);

        await PerformAsync();

        using var context = CreateDbContext();
        AssertIsPrimary(context, primaryId);
        AssertIsVersionOf(context, versionId, primaryId);
    }

    [Fact]
    public async Task PerformAsync_ChainedLinks_PointsEveryVersionAtTheHeadOfTheChain()
    {
        var headId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var middleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var tailId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // The middle one is a version of the head and a primary of the tail at the same time.
        Seed(
            [(headId, null), (middleId, null), (tailId, null)],
            [(headId, middleId), (middleId, tailId)]);

        await PerformAsync();

        using var context = CreateDbContext();
        AssertIsPrimary(context, headId);
        AssertIsVersionOf(context, middleId, headId);
        AssertIsVersionOf(context, tailId, headId);
    }

    [Fact]
    public async Task PerformAsync_VersionLinkedToItself_LeavesItVisible()
    {
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        Seed([(itemId, null)], [(itemId, itemId)]);

        await PerformAsync();

        using var context = CreateDbContext();
        Assert.Null(Get(context, itemId).PrimaryVersionId);
    }

    private static void AssertIsPrimary(JellyfinDbContext context, Guid id)
    {
        var item = Get(context, id);
        Assert.Null(item.PrimaryVersionId);
        Assert.Equal(id.ToString("N", CultureInfo.InvariantCulture), item.PresentationUniqueKey);
    }

    private static void AssertIsVersionOf(JellyfinDbContext context, Guid id, Guid primaryId)
    {
        var item = Get(context, id);
        Assert.Equal(primaryId, item.PrimaryVersionId);

        // Presentation-key grouping has to collapse the version onto its primary as well.
        Assert.Equal(primaryId.ToString("N", CultureInfo.InvariantCulture), item.PresentationUniqueKey);
    }

    private static BaseItemEntity Get(JellyfinDbContext context, Guid id)
        => context.BaseItems.AsNoTracking().First(e => e.Id.Equals(id));

    private void Seed(
        (Guid Id, Guid? PrimaryVersionId)[] items,
        (Guid ParentId, Guid ChildId)[] links)
    {
        using var context = CreateDbContext();

        foreach (var (id, primaryVersionId) in items)
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                Type = "MediaBrowser.Controller.Entities.Movies.Movie",
                Name = "Movie",
                PrimaryVersionId = primaryVersionId,
                PresentationUniqueKey = (primaryVersionId ?? id).ToString("N", CultureInfo.InvariantCulture)
            });
        }

        foreach (var (parentId, childId) in links)
        {
            context.LinkedChildren.Add(new LinkedChildEntity
            {
                ParentId = parentId,
                ChildId = childId,
                ChildType = LinkedChildType.LinkedAlternateVersion
            });
        }

        context.SaveChanges();
    }

    private Task PerformAsync()
    {
        var migration = new RepairAlternateVersionLinks(
            new StartupLogger<RepairAlternateVersionLinks>(NullLogger<RepairAlternateVersionLinks>.Instance),
            CreateDbContextFactory());

        return migration.PerformAsync(CancellationToken.None);
    }
}
