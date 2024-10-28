using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Movies controller.
/// </summary>
[Authorize]
public class MoviesController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoviesController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public MoviesController(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        IServerConfigurationManager serverConfigurationManager)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <summary>
    /// Gets movie recommendations.
    /// </summary>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. The fields to return.</param>
    /// <param name="categoryLimit">The max number of categories to return.</param>
    /// <param name="itemLimit">The max number of items to return per category.</param>
    /// <response code="200">Movie recommendations returned.</response>
    /// <returns>The list of movie recommendations.</returns>
    [HttpGet("Recommendations")]
    public ActionResult<IEnumerable<RecommendationDto>> GetMovieRecommendations(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] int categoryLimit = 5,
        [FromQuery] int itemLimit = 8)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User);

        var categories = new List<RecommendationDto>();

        var parentIdGuid = parentId ?? Guid.Empty;

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[]
            {
                BaseItemKind.Movie,
                // nameof(Trailer),
                // nameof(LiveTvProgram)
            },
            // IsMovie = true
            OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.Random, SortOrder.Descending) },
            Limit = 7,
            ParentId = parentIdGuid,
            Recursive = true,
            IsPlayed = true,
            DtoOptions = dtoOptions
        };

        var recentlyPlayedMovies = _libraryManager.GetItemList(query);

        var itemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            itemTypes.Add(BaseItemKind.Trailer);
            itemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        var likedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = itemTypes.ToArray(),
            IsMovie = true,
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Descending) },
            Limit = 10,
            IsFavoriteOrLiked = true,
            ExcludeItemIds = recentlyPlayedMovies.Select(i => i.Id).ToArray(),
            EnableGroupByMetadataKey = true,
            ParentId = parentIdGuid,
            Recursive = true,
            DtoOptions = dtoOptions
        });

        var mostRecentMovies = recentlyPlayedMovies.Take(Math.Min(recentlyPlayedMovies.Count, 6)).ToList();
        // Get recently played directors
        var recentDirectors = GetDirectors(mostRecentMovies)
            .ToList();

        // Get recently played actors
        var recentActors = GetActors(mostRecentMovies)
            .ToList();

        var similarToRecentlyPlayed = GetSimilarTo(user, recentlyPlayedMovies, itemLimit, dtoOptions, RecommendationType.SimilarToRecentlyPlayed).GetEnumerator();
        var similarToLiked = GetSimilarTo(user, likedMovies, itemLimit, dtoOptions, RecommendationType.SimilarToLikedItem).GetEnumerator();

        var hasDirectorFromRecentlyPlayed = GetWithDirector(user, recentDirectors, itemLimit, dtoOptions, RecommendationType.HasDirectorFromRecentlyPlayed).GetEnumerator();
        var hasActorFromRecentlyPlayed = GetWithActor(user, recentActors, itemLimit, dtoOptions, RecommendationType.HasActorFromRecentlyPlayed).GetEnumerator();

        var categoryTypes = new List<IEnumerator<RecommendationDto>>
            {
                // Give this extra weight
                similarToRecentlyPlayed,
                similarToRecentlyPlayed,

                // Give this extra weight
                similarToLiked,
                similarToLiked,
                hasDirectorFromRecentlyPlayed,
                hasActorFromRecentlyPlayed
            };

        while (categories.Count < categoryLimit)
        {
            var allEmpty = true;

            foreach (var category in categoryTypes)
            {
                if (category.MoveNext())
                {
                    categories.Add(category.Current);
                    allEmpty = false;

                    if (categories.Count >= categoryLimit)
                    {
                        break;
                    }
                }
            }

            if (allEmpty)
            {
                break;
            }
        }

        return Ok(categories.OrderBy(i => i.RecommendationType).AsEnumerable());
    }

    private IEnumerable<RecommendationDto> GetWithDirector(
        User? user,
        IEnumerable<string> names,
        int itemLimit,
        DtoOptions dtoOptions,
        RecommendationType type)
    {
        var itemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            itemTypes.Add(BaseItemKind.Trailer);
            itemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        foreach (var name in names)
        {
            var items = _libraryManager.GetItemList(
                new InternalItemsQuery(user)
                {
                    Person = name,
                    // Account for duplicates by IMDb id, since the database doesn't support this yet
                    Limit = itemLimit + 2,
                    PersonTypes = new[] { PersonType.Director },
                    IncludeItemTypes = itemTypes.ToArray(),
                    IsMovie = true,
                    EnableGroupByMetadataKey = true,
                    DtoOptions = dtoOptions
                }).DistinctBy(i => i.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Imdb) ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
                .Take(itemLimit)
                .ToList();

            if (items.Count > 0)
            {
                var returnItems = _dtoService.GetBaseItemDtos(items, dtoOptions, user);

                yield return new RecommendationDto
                {
                    BaselineItemName = name,
                    CategoryId = name.GetMD5(),
                    RecommendationType = type,
                    Items = returnItems
                };
            }
        }
    }

    private IEnumerable<RecommendationDto> GetWithActor(User? user, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
    {
        var itemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            itemTypes.Add(BaseItemKind.Trailer);
            itemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        foreach (var name in names)
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Person = name,
                // Account for duplicates by IMDb id, since the database doesn't support this yet
                Limit = itemLimit + 2,
                IncludeItemTypes = itemTypes.ToArray(),
                IsMovie = true,
                EnableGroupByMetadataKey = true,
                DtoOptions = dtoOptions
            }).DistinctBy(i => i.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Imdb) ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
                .Take(itemLimit)
                .ToList();

            if (items.Count > 0)
            {
                var returnItems = _dtoService.GetBaseItemDtos(items, dtoOptions, user);

                yield return new RecommendationDto
                {
                    BaselineItemName = name,
                    CategoryId = name.GetMD5(),
                    RecommendationType = type,
                    Items = returnItems
                };
            }
        }
    }

    private IEnumerable<RecommendationDto> GetSimilarTo(User? user, IEnumerable<BaseItem> baselineItems, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
    {
        var itemTypes = new List<BaseItemKind> { BaseItemKind.Movie };
        if (_serverConfigurationManager.Configuration.EnableExternalContentInSuggestions)
        {
            itemTypes.Add(BaseItemKind.Trailer);
            itemTypes.Add(BaseItemKind.LiveTvProgram);
        }

        foreach (var item in baselineItems)
        {
            var similar = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Limit = itemLimit,
                IncludeItemTypes = itemTypes.ToArray(),
                IsMovie = true,
                EnableGroupByMetadataKey = true,
                DtoOptions = dtoOptions
            });

            if (similar.Count > 0)
            {
                var returnItems = _dtoService.GetBaseItemDtos(similar, dtoOptions, user);

                yield return new RecommendationDto
                {
                    BaselineItemName = item.Name,
                    CategoryId = item.Id,
                    RecommendationType = type,
                    Items = returnItems
                };
            }
        }
    }

    private IEnumerable<string> GetActors(IEnumerable<BaseItem> items)
    {
        var people = _libraryManager.GetPeople(new InternalPeopleQuery(Array.Empty<string>(), new[] { PersonType.Director })
        {
            MaxListOrder = 3
        });

        var itemIds = items.Select(i => i.Id).ToList();

        return people
            .Where(i => itemIds.Contains(i.ItemId))
            .Select(i => i.Name)
            .DistinctNames();
    }

    private IEnumerable<string> GetDirectors(IEnumerable<BaseItem> items)
    {
        var people = _libraryManager.GetPeople(new InternalPeopleQuery(
            new[] { PersonType.Director },
            Array.Empty<string>()));

        var itemIds = items.Select(i => i.Id).ToList();

        return people
            .Where(i => itemIds.Contains(i.ItemId))
            .Select(i => i.Name)
            .DistinctNames();
    }
}
