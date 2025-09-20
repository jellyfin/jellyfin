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
        using var transaction = context.Database.BeginTransaction();

        // Step 1: Get people that need IDs
        var peopleWithoutIds = people.Where(p => p.Id.IsEmpty()).ToArray();

        // Step 2: Check database for existing people and assign their IDs
        if (peopleWithoutIds.Length > 0)
        {
            var nameTypePairs = peopleWithoutIds
                .Select(x => new { x.Name, PersonType = x.Type.ToString() })
                .ToArray();

            var names = nameTypePairs.Select(x => x.Name).ToArray();
            var personTypes = nameTypePairs.Select(x => x.PersonType).ToArray();
            var existingPeople = context.Peoples.AsNoTracking()
                .Where(p => names.Contains(p.Name) && personTypes.Contains(p.PersonType))
                .Select(p => new { p.Id, p.Name, p.PersonType })
                .AsEnumerable()
                .Where(p => nameTypePairs.Any(pair => p.Name == pair.Name && p.PersonType == pair.PersonType))
                .ToList();

            // Assign existing IDs to found people
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

        // Step 3: For people that are truly missing, assign new GUIDs and insert
        var missingPeople = people.Where(p => p.Id.IsEmpty()).ToList();
        if (missingPeople.Count > 0)
        {
            foreach (var person in missingPeople)
            {
                person.Id = Guid.NewGuid();
            }

            // Insert missing records
            context.Peoples.AddRange(missingPeople.Select(person => new People
            {
                Id = person.Id,
                Name = person.Name,
                PersonType = person.Type.ToString()
            }));
            context.SaveChanges();
        }

        // Clear change tracker before mapping operations to avoid tracking conflicts
        context.ChangeTracker.Clear();

        // Now all people have valid IDs, process PeopleBaseItemMap
        var allPeopleIds = people.Select(p => p.Id).ToArray();

        // Get all existing mappings for this item in one query
        var existingMappings = context.PeopleBaseItemMap
            .Where(m => m.ItemId == itemId)
            .ToDictionary(m => m.PeopleId, m => m);

        // Remove obsolete mappings (people no longer in the list)
        var obsoleteMappings = existingMappings.Values
            .Where(m => !allPeopleIds.Contains(m.PeopleId))
            .ToList();
        context.PeopleBaseItemMap.RemoveRange(obsoleteMappings);

        // Upsert mappings for all people
        foreach (var person in people)
        {
            if (existingMappings.TryGetValue(person.Id, out var existing))
            {
                // Update existing mapping
                existing.Role = person.Role;
                existing.SortOrder = person.SortOrder;
                existing.ListOrder = person.SortOrder;
            }
            else
            {
                // Add new mapping
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
        transaction.Commit();
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
