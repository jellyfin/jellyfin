using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Manages the storage and retrieval of home sections through Entity Framework.
    /// </summary>
    public sealed class HomeSectionManager : IHomeSectionManager, IAsyncDisposable
    {
        private readonly JellyfinDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeSectionManager"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        public HomeSectionManager(IDbContextFactory<JellyfinDbContext> dbContextFactory)
        {
            _dbContext = dbContextFactory.CreateDbContext();
            // QUESTION FOR MAINTAINERS: How do I handle the db migration?
            // I'm sure you don't want the table to be created lazily like this.
            EnsureTableExists();
        }

        /// <summary>
        /// Ensures that the UserHomeSections table exists in the database.
        /// </summary>
        private void EnsureTableExists()
        {
            try
            {
                // Check if table exists by attempting to query it
                _dbContext.Database.ExecuteSqlRaw("SELECT 1 FROM UserHomeSections LIMIT 1");
            }
            catch
            {
                // Table doesn't exist, create it
                _dbContext.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS UserHomeSections (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT NOT NULL,
                        SectionId TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        SectionType INTEGER NOT NULL,
                        Priority INTEGER NOT NULL,
                        MaxItems INTEGER NOT NULL,
                        SortOrder INTEGER NOT NULL,
                        SortBy INTEGER NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_UserHomeSections_UserId_SectionId ON UserHomeSections(UserId, SectionId);
                ");

                // Add the migration record to __EFMigrationsHistory
                _dbContext.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                        MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                        ProductVersion TEXT NOT NULL
                    );
                    INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                    VALUES ('20250331000000_AddUserHomeSections', '3.1.0');
                ");
            }
        }

        /// <inheritdoc />
        public IList<HomeSectionOptions> GetHomeSections(Guid userId)
        {
            return _dbContext.UserHomeSections
                .Where(section => section.UserId.Equals(userId))
                .OrderBy(section => section.Priority)
                .Select(section => new HomeSectionOptions
                {
                    Name = section.Name,
                    SectionType = section.SectionType,
                    Priority = section.Priority,
                    MaxItems = section.MaxItems,
                    SortOrder = section.SortOrder,
                    SortBy = (Jellyfin.Database.Implementations.Enums.SortOrder)section.SortBy
                })
                .ToList();
        }

        /// <inheritdoc />
        public HomeSectionOptions? GetHomeSection(Guid userId, Guid sectionId)
        {
            var section = _dbContext.UserHomeSections
                .FirstOrDefault(section => section.UserId.Equals(userId) && section.SectionId.Equals(sectionId));

            if (section == null)
            {
                return null;
            }

            return new HomeSectionOptions
            {
                Name = section.Name,
                SectionType = section.SectionType,
                Priority = section.Priority,
                MaxItems = section.MaxItems,
                SortOrder = section.SortOrder,
                SortBy = (Jellyfin.Database.Implementations.Enums.SortOrder)section.SortBy
            };
        }

        /// <inheritdoc />
        public Guid CreateHomeSection(Guid userId, HomeSectionOptions options)
        {
            var sectionId = Guid.NewGuid();
            var section = new UserHomeSection
            {
                UserId = userId,
                SectionId = sectionId,
                Name = options.Name,
                SectionType = options.SectionType,
                Priority = options.Priority,
                MaxItems = options.MaxItems,
                SortOrder = options.SortOrder,
                SortBy = (int)options.SortBy
            };

            _dbContext.UserHomeSections.Add(section);
            return sectionId;
        }

        /// <inheritdoc />
        public bool UpdateHomeSection(Guid userId, Guid sectionId, HomeSectionOptions options)
        {
            var section = _dbContext.UserHomeSections
                .FirstOrDefault(section => section.UserId.Equals(userId) && section.SectionId.Equals(sectionId));

            if (section == null)
            {
                return false;
            }

            section.Name = options.Name;
            section.SectionType = options.SectionType;
            section.Priority = options.Priority;
            section.MaxItems = options.MaxItems;
            section.SortOrder = options.SortOrder;
            section.SortBy = (int)options.SortBy;

            return true;
        }

        /// <inheritdoc />
        public bool DeleteHomeSection(Guid userId, Guid sectionId)
        {
            var section = _dbContext.UserHomeSections
                .FirstOrDefault(section => section.UserId.Equals(userId) && section.SectionId.Equals(sectionId));

            if (section == null)
            {
                return false;
            }

            _dbContext.UserHomeSections.Remove(section);
            return true;
        }

        /// <inheritdoc />
        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync().ConfigureAwait(false);
        }
    }
}
