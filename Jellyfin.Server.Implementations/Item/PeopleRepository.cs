using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;
#pragma warning disable RS0030 // Do not use banned APIs

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

        return dbQuery.AsEnumerable().Select(Map).ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        // dbQuery = dbQuery.OrderBy(e => e.ListOrder);
        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return dbQuery.Select(e => e.Name).ToArray();
    }

    /// <inheritdoc />
    public void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people)
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.PeopleBaseItemMap.Where(e => e.ItemId == itemId).ExecuteDelete();
        // TODO: yes for __SOME__ reason there can be duplicates.
        foreach (var item in people.DistinctBy(e => e.Id))
        {
            var personEntity = Map(item);
            var existingEntity = context.Peoples.FirstOrDefault(e => e.Id == personEntity.Id);
            if (existingEntity is null)
            {
                context.Peoples.Add(personEntity);
                existingEntity = personEntity;
            }

            context.PeopleBaseItemMap.Add(new PeopleBaseItemMap()
            {
                Item = null!,
                ItemId = itemId,
                People = existingEntity,
                PeopleId = existingEntity.Id,
                ListOrder = item.SortOrder,
                SortOrder = item.SortOrder,
                Role = item.Role
            });
        }

        context.SaveChanges();
        transaction.Commit();
    }

    private PersonInfo Map(People people)
    {
        var personInfo = new PersonInfo()
        {
            Id = people.Id,
            Name = people.Name,
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
            query = query.Where(e => e.PersonType == personType)
                .Where(e => context.BaseItems.Where(d => d.UserData!.Any(w => w.IsFavorite == filter.IsFavorite && w.UserId.Equals(filter.User.Id)))
                    .Select(f => f.Name).Contains(e.Name));
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
            query = query.Where(e => e.Name.Contains(filter.NameContains));
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
