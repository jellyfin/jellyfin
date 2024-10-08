using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

public class PeopleManager
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleManager"/> class.
    /// </summary>
    /// <param name="dbProvider">The EFCore Context factory.</param>
    public PeopleManager(IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    public IReadOnlyList<PersonInfo> GetPeople(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        dbQuery = dbQuery.OrderBy(e => e.ListOrder);
        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return dbQuery.ToList().Select(Map).ToImmutableArray();
    }

    public IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery filter)
    {
        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.Peoples.AsNoTracking(), context, filter);

        dbQuery = dbQuery.OrderBy(e => e.ListOrder);
        if (filter.Limit > 0)
        {
            dbQuery = dbQuery.Take(filter.Limit);
        }

        return dbQuery.Select(e => e.Name).ToImmutableArray();
    }

    /// <inheritdoc />
    public void UpdatePeople(Guid itemId, IReadOnlyList<PersonInfo> people)
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.Peoples.Where(e => e.ItemId.Equals(itemId)).ExecuteDelete();
        context.Peoples.AddRange(people.Select(Map));
        context.SaveChanges();
        transaction.Commit();
    }

    private PersonInfo Map(People people)
    {
        var personInfo = new PersonInfo()
        {
            ItemId = people.ItemId,
            Name = people.Name,
            Role = people.Role,
            SortOrder = people.SortOrder,
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
            ItemId = people.ItemId,
            Name = people.Name,
            Role = people.Role,
            SortOrder = people.SortOrder,
            PersonType = people.Type.ToString()
        };

        return personInfo;
    }

    private IQueryable<People> TranslateQuery(IQueryable<People> query, JellyfinDbContext context, InternalPeopleQuery filter)
    {
        if (filter.User is not null && filter.IsFavorite.HasValue)
        {
            query = query.Where(e => e.PersonType == typeof(Person).FullName)
                .Where(e => context.BaseItems.Where(d => context.UserData.Where(e => e.IsFavorite == filter.IsFavorite && e.UserId.Equals(filter.User.Id)).Any(f => f.Key == d.UserDataKey))
                    .Select(f => f.Name).Contains(e.Name));
        }

        if (!filter.ItemId.IsEmpty())
        {
            query = query.Where(e => e.ItemId.Equals(filter.ItemId));
        }

        if (!filter.AppearsInItemId.IsEmpty())
        {
            query = query.Where(e => context.Peoples.Where(f => f.ItemId.Equals(filter.AppearsInItemId)).Select(e => e.Name).Contains(e.Name));
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

        if (filter.MaxListOrder.HasValue)
        {
            query = query.Where(e => e.ListOrder <= filter.MaxListOrder.Value);
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
