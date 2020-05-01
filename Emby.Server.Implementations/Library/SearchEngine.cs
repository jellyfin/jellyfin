#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library
{
    public class SearchEngine : ISearchEngine
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public SearchEngine(ILogger<SearchEngine> logger, ILibraryManager libraryManager, IUserManager userManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public QueryResult<SearchHintInfo> GetSearchHints(SearchQuery query)
        {
            User user = null;

            if (query.UserId.Equals(Guid.Empty))
            {
            }
            else
            {
                user = _userManager.GetUserById(query.UserId);
            }

            var results = GetSearchHints(query, user);
            var totalRecordCount = results.Count;

            if (query.StartIndex.HasValue)
            {
                results = results.Skip(query.StartIndex.Value).ToList();
            }

            if (query.Limit.HasValue)
            {
                results = results.Take(query.Limit.Value).ToList();
            }

            return new QueryResult<SearchHintInfo>
            {
                TotalRecordCount = totalRecordCount,

                Items = results.ToArray()
            };
        }

        private static void AddIfMissing(List<string> list, string value)
        {
            if (!list.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{SearchHintResult}.</returns>
        /// <exception cref="ArgumentNullException">searchTerm</exception>
        private List<SearchHintInfo> GetSearchHints(SearchQuery query, User user)
        {
            var searchTerm = query.SearchTerm;

            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new ArgumentNullException("SearchTerm can't be empty.", nameof(searchTerm));
            }

            searchTerm = searchTerm.Trim().RemoveDiacritics();

            var excludeItemTypes = query.ExcludeItemTypes.ToList();
            var includeItemTypes = (query.IncludeItemTypes ?? Array.Empty<string>()).ToList();

            excludeItemTypes.Add(typeof(Year).Name);
            excludeItemTypes.Add(typeof(Folder).Name);

            if (query.IncludeGenres && (includeItemTypes.Count == 0 || includeItemTypes.Contains("Genre", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Genre).Name);
                    AddIfMissing(includeItemTypes, typeof(MusicGenre).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Genre).Name);
                AddIfMissing(excludeItemTypes, typeof(MusicGenre).Name);
            }

            if (query.IncludePeople && (includeItemTypes.Count == 0 || includeItemTypes.Contains("People", StringComparer.OrdinalIgnoreCase) || includeItemTypes.Contains("Person", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Person).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Person).Name);
            }

            if (query.IncludeStudios && (includeItemTypes.Count == 0 || includeItemTypes.Contains("Studio", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Studio).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Studio).Name);
            }

            if (query.IncludeArtists && (includeItemTypes.Count == 0 || includeItemTypes.Contains("MusicArtist", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(MusicArtist).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(MusicArtist).Name);
            }

            AddIfMissing(excludeItemTypes, typeof(CollectionFolder).Name);
            AddIfMissing(excludeItemTypes, typeof(Folder).Name);
            var mediaTypes = query.MediaTypes.ToList();

            if (includeItemTypes.Count > 0)
            {
                excludeItemTypes.Clear();
                mediaTypes.Clear();
            }

            var searchQuery = new InternalItemsQuery(user)
            {
                SearchTerm = searchTerm,
                ExcludeItemTypes = excludeItemTypes.ToArray(),
                IncludeItemTypes = includeItemTypes.ToArray(),
                Limit = query.Limit,
                IncludeItemsByName = string.IsNullOrEmpty(query.ParentId),
                ParentId = string.IsNullOrEmpty(query.ParentId) ? Guid.Empty : new Guid(query.ParentId),
                OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                Recursive = true,

                IsKids = query.IsKids,
                IsMovie = query.IsMovie,
                IsNews = query.IsNews,
                IsSeries = query.IsSeries,
                IsSports = query.IsSports,
                MediaTypes = mediaTypes.ToArray(),

                DtoOptions = new DtoOptions
                {
                    Fields = new ItemFields[]
                    {
                         ItemFields.AirTime,
                         ItemFields.DateCreated,
                         ItemFields.ChannelInfo,
                         ItemFields.ParentId
                    }
                }
            };

            List<BaseItem> mediaItems;

            if (searchQuery.IncludeItemTypes.Length == 1 && string.Equals(searchQuery.IncludeItemTypes[0], "MusicArtist", StringComparison.OrdinalIgnoreCase))
            {
                if (!searchQuery.ParentId.Equals(Guid.Empty))
                {
                    searchQuery.AncestorIds = new[] { searchQuery.ParentId };
                }
                searchQuery.ParentId = Guid.Empty;
                searchQuery.IncludeItemsByName = true;
                searchQuery.IncludeItemTypes = Array.Empty<string>();
                mediaItems = _libraryManager.GetAllArtists(searchQuery).Items.Select(i => i.Item1).ToList();
            }
            else
            {
                mediaItems = _libraryManager.GetItemList(searchQuery);
            }

            return mediaItems.Select(i => new SearchHintInfo
            {
                Item = i

            }).ToList();
        }
    }
}
