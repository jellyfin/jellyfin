using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    [Route("/Items/Filters", "GET", Summary = "Gets branding configuration")]
    public class GetQueryFiltersLegacy : IReturn<QueryFiltersLegacy>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        [ApiMember(Name = "MediaTypes", Description = "Optional filter by MediaType. Allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        public string[] GetMediaTypes()
        {
            return (MediaTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetIncludeItemTypes()
        {
            return (IncludeItemTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    [Route("/Items/Filters2", "GET", Summary = "Gets branding configuration")]
    public class GetQueryFilters : IReturn<QueryFilters>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        [ApiMember(Name = "MediaTypes", Description = "Optional filter by MediaType. Allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        public string[] GetMediaTypes()
        {
            return (MediaTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetIncludeItemTypes()
        {
            return (IncludeItemTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool? IsAiring { get; set; }
        public bool? IsMovie { get; set; }
        public bool? IsSports { get; set; }
        public bool? IsKids { get; set; }
        public bool? IsNews { get; set; }
        public bool? IsSeries { get; set; }
        public bool? Recursive { get; set; }
    }

    [Authenticated]
    public class FilterService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public FilterService(
            ILogger<FilterService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ILibraryManager libraryManager,
            IUserManager userManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public object Get(GetQueryFilters request)
        {
            var parentItem = string.IsNullOrEmpty(request.ParentId) ? null : _libraryManager.GetItemById(request.ParentId);
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            if (string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, typeof(Trailer).Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "Program", StringComparison.OrdinalIgnoreCase))
            {
                parentItem = null;
            }

            var filters = new QueryFilters();

            var genreQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = request.GetIncludeItemTypes(),
                DtoOptions = new Controller.Dto.DtoOptions
                {
                    Fields = new ItemFields[] { },
                    EnableImages = false,
                    EnableUserData = false
                },
                IsAiring = request.IsAiring,
                IsMovie = request.IsMovie,
                IsSports = request.IsSports,
                IsKids = request.IsKids,
                IsNews = request.IsNews,
                IsSeries = request.IsSeries
            };

            // Non recursive not yet supported for library folders
            if ((request.Recursive ?? true) || parentItem is UserView || parentItem is ICollectionFolder)
            {
                genreQuery.AncestorIds = parentItem == null ? Array.Empty<Guid>() : new[] { parentItem.Id };
            }
            else
            {
                genreQuery.Parent = parentItem;
            }

            if (string.Equals(request.IncludeItemTypes, "MusicAlbum", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "MusicVideo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "MusicArtist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "Audio", StringComparison.OrdinalIgnoreCase))
            {
                filters.Genres = _libraryManager.GetMusicGenres(genreQuery).Items.Select(i => new NameGuidPair
                {
                    Name = i.Item1.Name,
                    Id = i.Item1.Id

                }).ToArray();
            }
            else
            {
                filters.Genres = _libraryManager.GetGenres(genreQuery).Items.Select(i => new NameGuidPair
                {
                    Name = i.Item1.Name,
                    Id = i.Item1.Id

                }).ToArray();
            }

            return ToOptimizedResult(filters);
        }

        public object Get(GetQueryFiltersLegacy request)
        {
            var parentItem = string.IsNullOrEmpty(request.ParentId) ? null : _libraryManager.GetItemById(request.ParentId);
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            if (string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, typeof(Trailer).Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.IncludeItemTypes, "Program", StringComparison.OrdinalIgnoreCase))
            {
                parentItem = null;
            }

            var item = string.IsNullOrEmpty(request.ParentId) ?
               user == null ? _libraryManager.RootFolder : _libraryManager.GetUserRootFolder() :
               parentItem;

            var result = ((Folder)item).GetItemList(GetItemsQuery(request, user));

            var filters = GetFilters(result);

            return ToOptimizedResult(filters);
        }

        private QueryFiltersLegacy GetFilters(IReadOnlyCollection<BaseItem> items)
        {
            var result = new QueryFiltersLegacy();

            result.Years = items.Select(i => i.ProductionYear ?? -1)
                .Where(i => i > 0)
                .Distinct()
                .OrderBy(i => i)
                .ToArray();

            result.Genres = items.SelectMany(i => i.Genres)
                .DistinctNames()
                .OrderBy(i => i)
                .ToArray();

            result.Tags = items
                .SelectMany(i => i.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            result.OfficialRatings = items
                .Select(i => i.OfficialRating)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            return result;
        }

        private InternalItemsQuery GetItemsQuery(GetQueryFiltersLegacy request, User user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                Recursive = true,
                EnableTotalRecordCount = false,
                DtoOptions = new Controller.Dto.DtoOptions
                {
                    Fields = new[] { ItemFields.Genres, ItemFields.Tags },
                    EnableImages = false,
                    EnableUserData = false
                }
            };

            return query;
        }
    }
}
