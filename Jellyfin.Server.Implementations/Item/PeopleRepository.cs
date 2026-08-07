using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
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
/// <param name="queryHelpers">Shared item query helpers.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="PeopleRepository"/> class.
/// </remarks>
public class PeopleRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IItemTypeLookup itemTypeLookup, IItemQueryHelpers queryHelpers) : IPeopleRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;

    /// <inheritdoc/>
    public QueryResult<PersonInfo> GetPeople(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);
        int? distinctNameCount = null;

        // Include PeopleBaseItemMap
        if (!filter.ItemId.IsEmpty())
        {
            dbQuery = dbQuery.Include(p => p.BaseItems!.Where(m => m.ItemId == filter.ItemId))
                .OrderBy(e => e.BaseItems!.Where(m => m.ItemId == filter.ItemId).Min(m => m.ListOrder))
                .ThenBy(e => e.PersonType)
                .ThenBy(e => e.Name);
        }
        else
        {
            // The Peoples table has one row per (CleanName, PersonType), so the same person can
            // appear multiple times (e.g. as Actor and GuestStar). Collapse to one row per
            // clean name so /Persons doesn't return the same BaseItem id repeatedly, keeping the
            // lowest id per clean name so spelling-only duplicates collapse together.
            var candidates = dbQuery;
            dbQuery = candidates
                .Where(p => !candidates.Any(other => other.CleanName == p.CleanName && other.Id < p.Id))
                .OrderBy(e => e.CleanName);

            if (filter.EnableTotalRecordCount)
            {
                distinctNameCount = candidates.Select(e => e.CleanName).Distinct().Count();
            }
        }

        var count = 0;
        if (filter.EnableTotalRecordCount)
        {
            count = distinctNameCount ?? dbQuery.Count();
        }

        if (filter.StartIndex.HasValue && filter.StartIndex > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return new QueryResult<PersonInfo>
        {
            StartIndex = filter.StartIndex ?? 0,
            TotalRecordCount = count,
            Items = dbQuery.AsEnumerable().SelectMany(MapCredits).ToArray(),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();

        IQueryable<string> dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter)
            .Select(e => e.Name)
            .Distinct()
            .OrderBy(e => e);

        if (filter.StartIndex.HasValue && filter.StartIndex > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return dbQuery.ToArray();
    }

    /// <inheritdoc />
    public void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people)
    {
        foreach (var person in people)
        {
            person.Name = person.Name.Trim();
            person.Role = person.Role?.Trim() ?? string.Empty;
        }

        // Project the values every comparison below needs once, so neither the normalisation nor the
        // enum formatting is repeated per candidate. CleanName is what identifies a person: two
        // spellings that only differ in casing, diacritics or punctuation are the same person.
        var credits = people.Select(e => (Person: e, CleanName: e.Name.GetCleanValue(), PersonType: e.Type.ToString(), LoweredRole: e.Role.ToLowerInvariant()));

        // multiple metadata providers can provide the _same_ credit; dedupe on the clean name.
        // The role is part of the key because one person can hold several credits of the same type
        // on an item, e.g. a Writer credited for both the Novel and the Screenplay.
        var distinctCredits = credits.DistinctBy(e => (e.CleanName, e.PersonType, e.LoweredRole)).ToArray();

        var distinctPersons = distinctCredits.DistinctBy(e => (e.CleanName, e.PersonType)).ToArray();
        var cleanNames = distinctPersons.Select(e => e.CleanName).ToArray();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var existingPersons = context.Peoples
            .Where(p => cleanNames.Contains(p.CleanName))
            .ToArray();

        var existingPersonKeys = existingPersons.Select(e => (e.CleanName, e.PersonType ?? string.Empty)).ToHashSet();

        var toAdd = distinctPersons
            .Where(e => !existingPersonKeys.Contains((e.CleanName, e.PersonType)))
            .Select(e => Map(e.Person))
            .ToArray();
        context.Peoples.AddRange(toAdd);
        context.SaveChanges();

        // The Peoples table can still hold duplicates written before the clean name became the key,
        // so keep the first match per key just as the previous First() lookup did.
        var personsEntities = new Dictionary<(string CleanName, string PersonType), People>();
        foreach (var entity in toAdd.Concat(existingPersons))
        {
            personsEntities.TryAdd((entity.CleanName, entity.PersonType ?? string.Empty), entity);
        }

        var existingMaps = context.PeopleBaseItemMap.Include(e => e.People).Where(e => e.ItemId == itemId).ToList();
        var existingMapsByCredit = new Dictionary<(string CleanName, string PersonType, string LoweredRole), PeopleBaseItemMap>();
        foreach (var map in existingMaps)
        {
            existingMapsByCredit.TryAdd((map.People.CleanName, map.People.PersonType ?? string.Empty, map.Role?.ToLowerInvariant() ?? string.Empty), map);
        }

        var listOrder = 0;

        foreach (var credit in distinctCredits)
        {
            var entityPerson = personsEntities[(credit.CleanName, credit.PersonType)];
            if (existingMapsByCredit.TryGetValue((credit.CleanName, credit.PersonType, credit.LoweredRole), out var existingMap))
            {
                // Update the order for existing mappings
                existingMap.ListOrder = listOrder;
                existingMap.SortOrder = credit.Person.SortOrder;
                // person mapping already exists so remove from list
                existingMaps.Remove(existingMap);
            }
            else
            {
                context.PeopleBaseItemMap.Add(new PeopleBaseItemMap()
                {
                    Item = null!,
                    ItemId = itemId,
                    People = null!,
                    PeopleId = entityPerson.Id,
                    ListOrder = listOrder,
                    SortOrder = credit.Person.SortOrder,
                    Role = credit.Person.Role
                });
            }

            listOrder++;
        }

        context.PeopleBaseItemMap.RemoveRange(existingMaps);

        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Guid, IReadOnlyList<string>> GetPeopleNamesByItems(IReadOnlyList<Guid> itemIds, IReadOnlyList<string> personTypes)
    {
        using var context = _dbProvider.CreateDbContext();
        var query = context.PeopleBaseItemMap
            .AsNoTracking()
            .Where(m => itemIds.Contains(m.ItemId));

        if (personTypes.Count > 0)
        {
            query = query.Where(m => personTypes.Contains(m.People.PersonType));
        }

        var rows = query
            .OrderBy(m => m.ListOrder)
            .Select(m => new { m.ItemId, m.People.Name })
            .ToList();

        var result = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var group in rows.GroupBy(r => r.ItemId))
        {
            var names = group
                .Select(r => r.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToArray();

            if (names.Length > 0)
            {
                result[group.Key] = names;
            }
        }

        return result;
    }

    private IEnumerable<PersonInfo> MapCredits(People people)
    {
        var mappings = people.BaseItems;
        if (mappings is null || mappings.Count == 0)
        {
            return [Map(people, null)];
        }

        return mappings.OrderBy(m => m.ListOrder).Select(m => Map(people, m));
    }

    private PersonInfo Map(People people, PeopleBaseItemMap? mapping)
    {
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
            CleanName = people.Name.GetCleanValue(),
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
            var userId = filter.User.Id;
            var isFavorite = filter.IsFavorite.Value;
            var favoriteItemIds = context.UserData
                .Where(u => u.UserId.Equals(userId) && u.IsFavorite == isFavorite)
                .Select(u => u.ItemId);

            var favoriteNames = context.BaseItems
                .Where(b => b.Type == personType && favoriteItemIds.Contains(b.Id))
                .Select(b => b.CleanName);

            query = query.Where(e => favoriteNames.Contains(e.CleanName));
        }

        if (filter.AccessFilter is not null)
        {
            // Keep only people credited on at least one item the user can see.
            var accessibleItems = queryHelpers.ApplyAccessFiltering(context, context.BaseItems.AsNoTracking(), filter.AccessFilter);
            query = query.Where(e => context.PeopleBaseItemMap
                .Any(m => m.PeopleId == e.Id && accessibleItems.Any(i => i.Id == m.ItemId)));
        }

        if (!filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId.Equals(filter.ItemId)));
        }

        if (filter.ParentId != null)
        {
            query = query.Where(e => e.BaseItems!.Any(w => context.AncestorIds.Any(i => i.ParentItemId == filter.ParentId && i.ItemId == w.ItemId)));
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
            query = query.Where(e => !queryExcludePersonTypes.Contains(e.PersonType));
        }

        if (filter.MaxListOrder.HasValue && !filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.BaseItems!.Any(w => w.ItemId == filter.ItemId && w.ListOrder <= filter.MaxListOrder.Value));
        }

        var cleanNameContains = filter.NameContains?.GetCleanValue();
        if (!string.IsNullOrEmpty(cleanNameContains))
        {
            query = query.Where(e => e.CleanName.Contains(cleanNameContains));
        }

        var cleanNameStartsWith = filter.NameStartsWith?.GetCleanValue();
        if (!string.IsNullOrEmpty(cleanNameStartsWith))
        {
            query = query.Where(e => e.CleanName.StartsWith(cleanNameStartsWith));
        }

        var cleanNameLessThan = filter.NameLessThan?.GetCleanValue();
        if (!string.IsNullOrEmpty(cleanNameLessThan))
        {
            query = query.Where(e => e.CleanName.CompareTo(cleanNameLessThan) < 0);
        }

        var cleanNameStartsWithOrGreater = filter.NameStartsWithOrGreater?.GetCleanValue();
        if (!string.IsNullOrEmpty(cleanNameStartsWithOrGreater))
        {
            query = query.Where(e => e.CleanName.CompareTo(cleanNameStartsWithOrGreater) >= 0);
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
