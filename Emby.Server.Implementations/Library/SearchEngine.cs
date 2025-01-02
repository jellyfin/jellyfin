#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;

namespace Emby.Server.Implementations.Library
{
    public class SearchEngine : ISearchEngine
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public SearchEngine(ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public QueryResult<SearchHintInfo> GetSearchHints(SearchQuery query)
        {
            User? user = null;
            if (!query.UserId.IsEmpty())
            {
                user = _userManager.GetUserById(query.UserId);
            }

            var results = GetSearchHints(query, user);
            var totalRecordCount = results.Count;

            if (query.StartIndex.HasValue)
            {
                results = results.GetRange(query.StartIndex.Value, totalRecordCount - query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                results = results.GetRange(0, Math.Min(query.Limit.Value, results.Count));
            }

            return new QueryResult<SearchHintInfo>(
                query.StartIndex,
                totalRecordCount,
                results);
        }

        private static void AddIfMissing(List<BaseItemKind> list, BaseItemKind value)
        {
            if (!list.Contains(value))
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
        /// <exception cref="ArgumentException"><c>query.SearchTerm</c> is <c>null</c> or empty.</exception>
        private List<SearchHintInfo> GetSearchHints(SearchQuery query, User? user)
        {
            var searchTerm = query.SearchTerm;

            ArgumentException.ThrowIfNullOrEmpty(searchTerm);

            searchTerm = searchTerm.Trim().RemoveDiacritics();

            var excludeItemTypes = query.ExcludeItemTypes.ToList();
            var includeItemTypes = query.IncludeItemTypes.ToList();

            excludeItemTypes.Add(BaseItemKind.Year);
            excludeItemTypes.Add(BaseItemKind.Folder);

            if (query.IncludeGenres && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Genre)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, BaseItemKind.Genre);
                    AddIfMissing(includeItemTypes, BaseItemKind.MusicGenre);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, BaseItemKind.Genre);
                AddIfMissing(excludeItemTypes, BaseItemKind.MusicGenre);
            }

            if (query.IncludePeople && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Person)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, BaseItemKind.Person);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, BaseItemKind.Person);
            }

            if (query.IncludeStudios && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.Studio)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, BaseItemKind.Studio);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, BaseItemKind.Studio);
            }

            if (query.IncludeArtists && (includeItemTypes.Count == 0 || includeItemTypes.Contains(BaseItemKind.MusicArtist)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, BaseItemKind.MusicArtist);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, BaseItemKind.MusicArtist);
            }

            AddIfMissing(excludeItemTypes, BaseItemKind.CollectionFolder);
            AddIfMissing(excludeItemTypes, BaseItemKind.Folder);
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
                IncludeItemsByName = !query.ParentId.HasValue,
                ParentId = query.ParentId ?? Guid.Empty,
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

            IReadOnlyList<BaseItem> mediaItems;

            if (searchQuery.IncludeItemTypes.Length == 1 && searchQuery.IncludeItemTypes[0] == BaseItemKind.MusicArtist)
            {
                if (!searchQuery.ParentId.IsEmpty())
                {
                    searchQuery.AncestorIds = [searchQuery.ParentId];
                    searchQuery.ParentId = Guid.Empty;
                }

                searchQuery.IncludeItemsByName = true;
                searchQuery.IncludeItemTypes = Array.Empty<BaseItemKind>();
                mediaItems = _libraryManager.GetAllArtists(searchQuery).Items.Select(i => i.Item).ToList();
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
