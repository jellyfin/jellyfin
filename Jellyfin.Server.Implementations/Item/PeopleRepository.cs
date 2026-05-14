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
/// <remarks>
/// Initializes a new instance of the <see cref="PeopleRepository"/> class.
/// </remarks>
public class PeopleRepository(IDbContextFactory<JellyfinDbContext> dbProvider, IItemTypeLookup itemTypeLookup) : IPeopleRepository
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;

    /// <inheritdoc/>
    public QueryResult<PersonInfo> GetPeople(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        // Include PeopleBaseItemMap
        if (!filter.ItemId.IsEmpty())
        {
            dbQuery = dbQuery.Include(p => p.BaseItems!.Where(m => m.ItemId == filter.ItemId))
                .OrderBy(e => e.BaseItems!.First(e => e.ItemId == filter.ItemId).ListOrder)
                .ThenBy(e => e.PersonType)
                .ThenBy(e => e.Name);
        }
        else
        {
            // The Peoples table has one row per (Name, PersonType), so the same person can
            // appear multiple times (e.g. as Actor and GuestStar). Collapse to one row per
            // name so /Persons doesn't return the same BaseItem id repeatedly. Lowercase the
            // grouping key so case-only duplicates collapse together.
            var representativeIds = dbQuery
                .GroupBy(e => e.Name.ToLower())
                .Select(g => g.Min(e => e.Id));
            dbQuery = context.Peoples.AsNoTracking()
                .Where(p => representativeIds.Contains(p.Id))
                .OrderBy(e => e.Name);
        }

        var count = dbQuery.Count();
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
            Items = dbQuery.AsEnumerable().Select(Map).ToArray(),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter).Select(e => e.Name).Distinct();

        if (filter.StartIndex.HasValue && filter.StartIndex > 0)
        {
            dbQuery = dbQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.OrderBy(e => e).Take(filter.Limit);
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

        // multiple metadata providers can provide the _same_ person; dedupe case-insensitively.
        people = people.DistinctBy(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();
        var personKeys = people.Select(e => e.Name.ToLowerInvariant() + "-" + e.Type).ToArray();

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

        var toAdd = people
            .Where(e => !existingPersons.Any(f => string.Equals(f.Name, e.Name, StringComparison.OrdinalIgnoreCase) && f.PersonType == e.Type.ToString()))
            .Select(Map);
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
            query = query.Where(e => e.BaseItems!.Where(w => w.ItemId == filter.ItemId).OrderBy(w => w.ListOrder).First().ListOrder <= filter.MaxListOrder.Value);
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
