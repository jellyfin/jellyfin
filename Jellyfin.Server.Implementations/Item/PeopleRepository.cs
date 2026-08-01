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
            // The Peoples table has one row per (Name, PersonType), so the same person can
            // appear multiple times (e.g. as Actor and GuestStar). Collapse to one row per
            // name so /Persons doesn't return the same BaseItem id repeatedly, keeping the
            // lowest id per lowercased name so case-only duplicates collapse together.
            var candidates = dbQuery;
            dbQuery = candidates
                .Where(p => !candidates.Any(other => other.Name.ToLower() == p.Name.ToLower() && other.Id < p.Id))
                .OrderBy(e => e.Name.ToLower());

            if (filter.EnableTotalRecordCount)
            {
                distinctNameCount = candidates.Select(e => e.Name.ToLower()).Distinct().Count();
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

        // multiple metadata providers can provide the _same_ credit; dedupe case-insensitively.
        // The role is part of the key because one person can hold several credits of the same type
        // on an item, e.g. a Writer credited for both the Novel and the Screenplay.
        people = people.DistinctBy(e => e.Name.ToLowerInvariant() + "-" + e.Type + "-" + e.Role.ToLowerInvariant()).ToArray();

        var distinctPersons = people.DistinctBy(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();
        var personKeys = distinctPersons.Select(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();
        var existingPersons = context.Peoples.Select(e => new
        {
            item = e,
            SelectionKey = e.Name.ToLower() + "-" + e.PersonType
        })
            .Where(p => personKeys.Contains(p.SelectionKey))
            .Select(f => f.item)
            .ToArray();

        var toAdd = distinctPersons
            .Where(e => !existingPersons.Any(f => string.Equals(f.Name, e.Name, StringComparison.OrdinalIgnoreCase) && f.PersonType == e.Type.ToString()))
            .Select(Map)
            .ToArray();
        context.Peoples.AddRange(toAdd);
        context.SaveChanges();

        var personsEntities = toAdd.Concat(existingPersons).ToArray();

        var existingMaps = context.PeopleBaseItemMap.Include(e => e.People).Where(e => e.ItemId == itemId).ToList();

        var listOrder = 0;

        foreach (var person in people)
        {
            var entityPerson = personsEntities.First(e => string.Equals(e.Name, person.Name, StringComparison.OrdinalIgnoreCase) && e.PersonType == person.Type.ToString());
            var existingMap = existingMaps.FirstOrDefault(e => string.Equals(e.People.Name, person.Name, StringComparison.OrdinalIgnoreCase) && e.People.PersonType == person.Type.ToString() && e.Role == person.Role);
            if (existingMap is null)
            {
                context.PeopleBaseItemMap.Add(new PeopleBaseItemMap()
                {
                    Item = null!,
                    ItemId = itemId,
                    People = null!,
                    PeopleId = entityPerson.Id,
                    ListOrder = listOrder,
                    SortOrder = person.SortOrder,
                    Role = person.Role
                });
            }
            else
            {
                // Update the order for existing mappings
                existingMap.ListOrder = listOrder;
                existingMap.SortOrder = person.SortOrder;
                // person mapping already exists so remove from list
                existingMaps.Remove(existingMap);
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

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
        {
            var nameContainsUpper = filter.NameContains.ToUpper();
            query = query.Where(e => e.Name.ToUpper().Contains(nameContainsUpper));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            query = query.Where(e => e.Name.StartsWith(filter.NameStartsWith.ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            query = query.Where(e => e.Name.CompareTo(filter.NameLessThan.ToLowerInvariant()) < 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            query = query.Where(e => e.Name.CompareTo(filter.NameStartsWithOrGreater.ToLowerInvariant()) >= 0);
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
