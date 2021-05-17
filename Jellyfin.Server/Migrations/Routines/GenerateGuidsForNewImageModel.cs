using System;
using System.IO;
using System.Linq;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Data.Enums;
using Jellyfin.Server.Implementations;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the user database to EF Core.
    /// </summary>
    public class GenerateGuidsForNewImageModel : IMigrationRoutine
    {
        private readonly ILogger<GenerateGuidsForNewImageModel> _logger;
        private readonly JellyfinDbProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateGuidsForNewImageModel"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="provider">The database provider.</param>
        public GenerateGuidsForNewImageModel(
            ILogger<GenerateGuidsForNewImageModel> logger,
            JellyfinDbProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("E1946C0B-857D-4C71-BA58-7F70FFEF2DD9");

        /// <inheritdoc/>
        public string Name => "GenerateGuidsForNewImageModel";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            using var dbContext = _provider.CreateContext();
            _logger.LogInformation("Generating GUIDs for the new image model");
            var users = dbContext.Users.ToList();

            foreach (var user in users)
            {
                if (user.ProfileImage != null)
                {
                    user.ProfileImage = new Image(user.ProfileImage.Path, ImageType.Profile);
                }
            }

            dbContext.SaveChanges();
        }
    }
}
