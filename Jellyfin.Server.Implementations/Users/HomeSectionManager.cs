using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Users;

/// <summary>
/// Manages the storage and retrieval of home sections through Entity Framework.
/// </summary>
public sealed class HomeSectionManager : IHomeSectionManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeSectionManager"/> class.
    /// </summary>
    /// <param name="dbContextFactory">The database context factory.</param>
    public HomeSectionManager(IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public IList<HomeSectionOptions> GetHomeSections(Guid userId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.UserHomeSections
            .Where(section => section.UserId.Equals(userId))
            .OrderBy(section => section.Priority)
            .Select(section => new HomeSectionOptions
            {
                Name = section.Name,
                SectionType = section.SectionType,
                Priority = section.Priority,
                MaxItems = section.MaxItems,
                SortOrder = section.SortOrder,
                SortBy = (Jellyfin.Database.Implementations.Enums.SortOrder)section.SortBy,
                CollectionId = section.CollectionId
            })
            .ToList();
    }

    /// <inheritdoc />
    public HomeSectionOptions? GetHomeSection(Guid userId, Guid sectionId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var section = dbContext.UserHomeSections
            .FirstOrDefault(s => s.UserId.Equals(userId) && s.SectionId.Equals(sectionId));

        if (section is null)
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
            SortBy = (Jellyfin.Database.Implementations.Enums.SortOrder)section.SortBy,
            CollectionId = section.CollectionId
        };
    }

    /// <inheritdoc />
    public Guid CreateHomeSection(Guid userId, HomeSectionOptions options)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var sectionId = Guid.NewGuid();
        dbContext.UserHomeSections.Add(new UserHomeSection
        {
            UserId = userId,
            SectionId = sectionId,
            Name = options.Name,
            SectionType = options.SectionType,
            Priority = options.Priority,
            MaxItems = options.MaxItems,
            SortOrder = options.SortOrder,
            SortBy = (int)options.SortBy,
            CollectionId = options.CollectionId
        });

        dbContext.SaveChanges();
        return sectionId;
    }

    /// <inheritdoc />
    public bool UpdateHomeSection(Guid userId, Guid sectionId, HomeSectionOptions options)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var section = dbContext.UserHomeSections
            .FirstOrDefault(s => s.UserId.Equals(userId) && s.SectionId.Equals(sectionId));

        if (section is null)
        {
            return false;
        }

        section.Name = options.Name;
        section.SectionType = options.SectionType;
        section.Priority = options.Priority;
        section.MaxItems = options.MaxItems;
        section.SortOrder = options.SortOrder;
        section.SortBy = (int)options.SortBy;
        section.CollectionId = options.CollectionId;

        dbContext.SaveChanges();
        return true;
    }

    /// <inheritdoc />
    public bool DeleteHomeSection(Guid userId, Guid sectionId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var section = dbContext.UserHomeSections
            .FirstOrDefault(s => s.UserId.Equals(userId) && s.SectionId.Equals(sectionId));

        if (section is null)
        {
            return false;
        }

        dbContext.UserHomeSections.Remove(section);
        dbContext.SaveChanges();
        return true;
    }
}
