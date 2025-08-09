using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Libraries;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;
#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

/// <summary>
/// Manager for handling people.
/// </summary>
/// <param name="dbProvider">Efcore Factory.</param>
/// <param name="itemTypeLookup">Items lookup service.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="PeopleRepository"/> class.
/// </remarks>
public class PeopleRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IItemTypeLookup itemTypeLookup) : IPeopleRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;

    /// <inheritdoc/>
    public IReadOnlyList<PersonInfo> GetPeople(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        // dbQuery = dbQuery.OrderBy(e => e.ListOrder);
        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        // Include PeopleBaseItemMap
        if (!filter.ItemId.IsEmpty())
        {
            dbQuery = dbQuery.Include(p => p.BaseItems!.Where(m => m.ItemId == filter.ItemId));
        }

        return dbQuery.AsEnumerable().Select(Map).ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter).Select(e => e.Name).Distinct();

        // dbQuery = dbQuery.OrderBy(e => e.ListOrder);
        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return dbQuery.ToArray();
    }

    /// <inheritdoc />
    public void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people)
    {
        using var context = _dbProvider.CreateDbContext();

        // Step 1: Get people that need IDs
        var peopleWithoutIds = people.Where(p => p.Id == Guid.Empty).ToArray();

        // Step 2: For people without IDs, try to get existing IDs first
        if (peopleWithoutIds.Length > 0)
        {
            var nameTypePairs = peopleWithoutIds
                .Select(x => new { x.Name, PersonType = x.Type.ToString() })
                .ToArray();

            var names = nameTypePairs.Select(x => x.Name).ToArray();
            var existingPeople = context.Peoples
                .Where(p => names.Contains(p.Name))
                .Select(p => new { p.Id, p.Name, p.PersonType })
                .ToList()
                .Where(p => nameTypePairs.Any(pair => p.Name == pair.Name && p.PersonType == pair.PersonType))
                .ToList();

            // Update people that we found existing IDs for
            foreach (var person in peopleWithoutIds)
            {
                var existing = existingPeople.FirstOrDefault(e =>
                    e.Name == person.Name && e.PersonType == person.Type.ToString());
                if (existing != null)
                {
                    person.Id = existing.Id;
                }
            }
        }

        // Step 3: For remaining people without IDs, insert them (with individual exception handling)
        var stillNeedIds = people.Where(p => p.Id == Guid.Empty).ToArray();
        foreach (var person in stillNeedIds)
        {
            var newId = Guid.NewGuid();
            try
            {
                context.Peoples.Add(new People
                {
                    Id = newId,
                    Name = person.Name,
                    PersonType = person.Type.ToString()
                });
                context.SaveChanges();
                person.Id = newId; // SUCCESS: Use the new ID we created
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Handle race condition - someone else inserted this person
                context.ChangeTracker.Clear();
                var existingId = context.Peoples
                    .Where(p => p.Name == person.Name && p.PersonType == person.Type.ToString())
                    .Select(p => p.Id)
                    .Single();
                person.Id = existingId; // DUPLICATE: Use existing ID
            }
        }

        // Now all people have valid IDs, process PeopleBaseItemMap
        var allPeopleIds = people.Select(p => p.Id).ToArray();

        // Remove obsolete mappings
        context.PeopleBaseItemMap
            .Where(m => m.ItemId == itemId && !allPeopleIds.Contains(m.PeopleId))
            .ExecuteDelete();

        // Upsert mappings for all people
        foreach (var person in people)
        {
            var updated = context.PeopleBaseItemMap
                .Where(m => m.ItemId == itemId && m.PeopleId == person.Id)
                .ExecuteUpdate(setters => setters
                    .SetProperty(m => m.Role, person.Role)
                    .SetProperty(m => m.SortOrder, person.SortOrder)
                    .SetProperty(m => m.ListOrder, person.SortOrder));

            if (updated == 0)
            {
                context.PeopleBaseItemMap.Add(new PeopleBaseItemMap()
                {
                    Item = null!,
                    ItemId = itemId,
                    People = null!,
                    PeopleId = person.Id,
                    ListOrder = person.SortOrder,
                    SortOrder = person.SortOrder,
                    Role = person.Role
                });
            }
        }

        context.SaveChanges();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex == null)
        {
            return false;
        }

        // SQLite
        if (ex.InnerException is SqliteException sqliteEx)
        {
            const int SQLITE_CONSTRAINT_UNIQUE = 19;
            return sqliteEx.SqliteErrorCode == SQLITE_CONSTRAINT_UNIQUE;
        }

        // PostgreSQL (check by type name since we can't reference Npgsql directly)
        if (ex.InnerException?.GetType().Name == "PostgresException")
        {
            // Use reflection to check SqlState property
            var sqlStateProperty = ex.InnerException.GetType().GetProperty("SqlState");
            if (sqlStateProperty?.GetValue(ex.InnerException) is string sqlState)
            {
                return sqlState == "23505"; // unique_violation
            }
        }

        // Fallback: check inner exception message for generic keywords
        var innerMessage = ex.InnerException?.Message ?? string.Empty;
        return innerMessage.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
               innerMessage.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
               innerMessage.Contains("23505", StringComparison.OrdinalIgnoreCase); // PostgreSQL unique violation code
    }

    private PersonInfo Map(People people)
    {
        var mapping = people.BaseItems?.FirstOrDefault();
        var personInfo = new PersonInfo()
        {
            Id = people.Id,
            Name = people.Name,
            Role = mapping?.Role,
            SortOrder = mapping?.SortOrder
        };
        if (Enum.TryParse<PersonKind>(people.PersonType, out var kind))
        {
            personInfo.Type = kind;
        }

        return personInfo;
    }

    private People Map(PersonInfo people)
    {
        var personInfo = new People()
        {
            Name = people.Name,
            PersonType = people.Type.ToString(),
            Id = people.Id,
        };

        return personInfo;
    }

    private IQueryable<People> TranslateQuery(IQueryable<People> query, JellyfinDbContext context, InternalPeopleQuery filter)
    {
        if (filter.User is not null && filter.IsFavorite.HasValue)
        {
            var personType = itemTypeLookup.BaseItemKindNames[BaseItemKind.Person];
            var oldQuery = query;

            query = context.UserData
                .Where(u => u.Item!.Type == personType && u.IsFavorite == filter.IsFavorite && u.UserId.Equals(filter.User.Id))
                .Join(oldQuery, e => e.Item!.Name, e => e.Name, (item, person) => person)
                .Distinct()
                .AsNoTracking();
        }

        if (!filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId.Equals(filter.ItemId)));
        }

        if (!filter.AppearsInItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId.Equals(filter.AppearsInItemId)));
        }

        var queryPersonTypes = filter.PersonTypes.Where(IsValidPersonType).ToList();
        if (queryPersonTypes.Count > 0)
        {
            query = query.Where(e => queryPersonTypes.Contains(e.PersonType));
        }

        var queryExcludePersonTypes = filter.ExcludePersonTypes.Where(IsValidPersonType).ToList();

        if (queryExcludePersonTypes.Count > 0)
        {
            query = query.Where(e => !queryPersonTypes.Contains(e.PersonType));
        }

        if (filter.MaxListOrder.HasValue && !filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.First(w => w.ItemId == filter.ItemId).ListOrder <= filter.MaxListOrder.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var nameContainsUpper = filter.NameContains.ToUpper();
            query = query.Where(e => e.Name.ToUpper().Contains(nameContainsUpper));
        }

        return query;
    }

    private bool IsAlphaNumeric(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false;
        }

        for (int i = 0; i < str.Length; i++)
        {
            if (!char.IsLetter(str[i]) && !char.IsNumber(str[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidPersonType(string value)
    {
        return IsAlphaNumeric(value);
    }
}
