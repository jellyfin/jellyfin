using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using MediaBrowser.Model.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migrate rating levels.
    /// </summary>
    [JellyfinMigration("2025-04-20T22:00:00", nameof(MigrateRatingLevels), "98724538-EB11-40E3-931A-252C55BDDE7A")]
    internal class MigrateRatingLevels : IDatabaseMigrationRoutine
    {
        private readonly ILogger<MigrateRatingLevels> _logger;
        private readonly IDbContextFactory<JellyfinDbContext> _provider;
        private readonly ILocalizationManager _localizationManager;

        public MigrateRatingLevels(
            IDbContextFactory<JellyfinDbContext> provider,
            ILoggerFactory loggerFactory,
            ILocalizationManager localizationManager)
        {
            _provider = provider;
            _localizationManager = localizationManager;
            _logger = loggerFactory.CreateLogger<MigrateRatingLevels>();
        }

        /// <inheritdoc/>
        public void Perform()
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
}
