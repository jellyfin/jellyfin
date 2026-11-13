#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Merges case-only duplicate people. Two passes:
/// 1) Person BaseItems whose Name differs only by casing — Person.GetPath hashes the name
///    verbatim, so two casings produce two distinct Person rows in BaseItems.
/// 2) Peoples lookup rows whose Name differs only by casing within the same PersonType —
///    UpdatePeople used to insert a second Peoples row when a metadata provider returned
///    a different casing than the row already in the table.
/// Both bugs cause the /Persons endpoint to list the same person twice.
/// </summary>
[JellyfinMigration("2026-05-08T13:00:00", nameof(MergeDuplicatePeople))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class MergeDuplicatePeople : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<MergeDuplicatePeople> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeDuplicatePeople"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public MergeDuplicatePeople(
        IStartupLogger<MergeDuplicatePeople> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IItemPersistenceService persistenceService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
        _persistenceService = persistenceService;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var merger = new DuplicatePeopleMerger(_logger, _libraryManager, _persistenceService);
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await merger.MergePersonBaseItemsAsync(context, KeyOf, "case-only", cancellationToken).ConfigureAwait(false);
            await merger.MergePeoplesRowsAsync(context, KeyOf, "case-only", cancellationToken).ConfigureAwait(false);
        }
    }

    private static string KeyOf(string name) => name.ToLowerInvariant();
}
