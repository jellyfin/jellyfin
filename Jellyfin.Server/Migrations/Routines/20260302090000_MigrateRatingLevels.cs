using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Model.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migrate rating levels.
/// </summary>
[JellyfinMigration("2026-03-02T09:00:00", nameof(MigrateRatingLevels))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class MigrateRatingLevels : IDatabaseMigrationRoutine
{
    private readonly IStartupLogger _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;
    private readonly ILocalizationManager _localizationManager;

    public MigrateRatingLevels(
        IDbContextFactory<JellyfinDbContext> provider,
        IStartupLogger<MigrateRatingLevels> logger,
        ILocalizationManager localizationManager)
    {
        _provider = provider;
        _localizationManager = localizationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        // This routine predates the async interface and has not been ported to async database access yet.
        PerformCore();
        return Task.CompletedTask;
    }

    private void PerformCore()
    {
        _logger.LogInformation("Recalculating parental rating levels based on rating string.");
        using var context = _provider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var ratings = context.BaseItems.AsNoTracking().Select(e => e.OfficialRating).Distinct();
        foreach (var rating in ratings)
        {
            if (string.IsNullOrEmpty(rating))
            {
                int? value = null;
                context.BaseItems
                    .Where(e => e.OfficialRating == null || e.OfficialRating == string.Empty)
                    .ExecuteUpdate(f => f.SetProperty(e => e.InheritedParentalRatingValue, value));
                context.BaseItems
                    .Where(e => e.OfficialRating == null || e.OfficialRating == string.Empty)
                    .ExecuteUpdate(f => f.SetProperty(e => e.InheritedParentalRatingSubValue, value));
            }
            else
            {
                var ratingValue = _localizationManager.GetRatingScore(rating);
                var score = ratingValue?.Score;
                var subScore = ratingValue?.SubScore;
                context.BaseItems
                    .Where(e => e.OfficialRating == rating)
                    .ExecuteUpdate(f => f.SetProperty(e => e.InheritedParentalRatingValue, score));
                context.BaseItems
                    .Where(e => e.OfficialRating == rating)
                    .ExecuteUpdate(f => f.SetProperty(e => e.InheritedParentalRatingSubValue, subScore));
            }
        }

        transaction.Commit();
    }
}
