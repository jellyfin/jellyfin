using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Api.Movies;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Api.Library
{
    [Route("/Items/{Id}/File", "GET", Summary = "Gets the original file of an item")]
    [Authenticated]
    public class GetFile
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetCriticReviews
    /// </summary>
    [Route("/Items/{Id}/CriticReviews", "GET", Summary = "Gets critic reviews for an item")]
    [Authenticated]
    public class GetCriticReviews : IReturn<QueryResult<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    /// <summary>
    /// Class GetThemeSongs
    /// </summary>
    [Route("/Items/{Id}/ThemeSongs", "GET", Summary = "Gets theme songs for an item")]
    [Authenticated]
    public class GetThemeSongs : IReturn<ThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    /// <summary>
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeVideos", "GET", Summary = "Gets theme videos for an item")]
    [Authenticated]
    public class GetThemeVideos : IReturn<ThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    /// <summary>
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeMedia", "GET", Summary = "Gets theme videos and songs for an item")]
    [Authenticated]
    public class GetThemeMedia : IReturn<AllThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    [Route("/Library/Refresh", "POST", Summary = "Starts a library scan")]
    [Authenticated(Roles = "Admin")]
    public class RefreshLibrary : IReturnVoid
    {
    }

    [Route("/Items/{Id}", "DELETE", Summary = "Deletes an item from the library and file system")]
    [Authenticated]
    public class DeleteItem : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Items", "DELETE", Summary = "Deletes an item from the library and file system")]
    [Authenticated]
    public class DeleteItems : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Ids", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Ids { get; set; }
    }

    [Route("/Items/Counts", "GET")]
    [Authenticated]
    public class GetItemCounts : IReturn<ItemCounts>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Get counts from a specific user's library.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "IsFavorite", Description = "Optional. Get counts of favorite items", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsFavorite { get; set; }
    }

    [Route("/Items/{Id}/Ancestors", "GET", Summary = "Gets all parents of an item")]
    [Authenticated]
    public class GetAncestors : IReturn<BaseItemDto[]>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetPhyscialPaths
    /// </summary>
    [Route("/Library/PhysicalPaths", "GET", Summary = "Gets a list of physical paths from virtual folders")]
    [Authenticated(Roles = "Admin")]
    public class GetPhyscialPaths : IReturn<List<string>>
    {
    }

    [Route("/Library/MediaFolders", "GET", Summary = "Gets all user media folders.")]
    [Authenticated]
    public class GetMediaFolders : IReturn<QueryResult<BaseItemDto>>
    {
        [ApiMember(Name = "IsHidden", Description = "Optional. Filter by folders that are marked hidden, or not.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? IsHidden { get; set; }
    }

    [Route("/Library/Series/Added", "POST", Summary = "Reports that new episodes of a series have been added by an external source")]
    [Route("/Library/Series/Updated", "POST", Summary = "Reports that new episodes of a series have been added by an external source")]
    [Authenticated]
    public class PostUpdatedSeries : IReturnVoid
    {
        [ApiMember(Name = "TvdbId", Description = "Tvdb Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string TvdbId { get; set; }
    }

    [Route("/Library/Movies/Added", "POST", Summary = "Reports that new movies have been added by an external source")]
    [Route("/Library/Movies/Updated", "POST", Summary = "Reports that new movies have been added by an external source")]
    [Authenticated]
    public class PostUpdatedMovies : IReturnVoid
    {
        [ApiMember(Name = "TmdbId", Description = "Tmdb Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string TmdbId { get; set; }
        [ApiMember(Name = "ImdbId", Description = "Imdb Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string ImdbId { get; set; }
    }

    public class MediaUpdateInfo
    {
        public string Path { get; set; }

        // Created, Modified, Deleted
        public string UpdateType { get; set; }
    }

    [Route("/Library/Media/Updated", "POST", Summary = "Reports that new movies have been added by an external source")]
    [Authenticated]
    public class PostUpdatedMedia : IReturnVoid
    {
        [ApiMember(Name = "Updates", Description = "A list of updated media paths", IsRequired = false, DataType = "string", ParameterType = "body", Verb = "POST")]
        public List<MediaUpdateInfo> Updates { get; set; }
    }

    [Route("/Items/{Id}/Download", "GET", Summary = "Downloads item media")]
    [Authenticated(Roles = "download")]
    public class GetDownload
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Artists/{Id}/Similar", "GET", Summary = "Finds albums similar to a given album.")]
    [Route("/Items/{Id}/Similar", "GET", Summary = "Gets similar items")]
    [Route("/Albums/{Id}/Similar", "GET", Summary = "Finds albums similar to a given album.")]
    [Route("/Shows/{Id}/Similar", "GET", Summary = "Finds tv shows similar to a given one.")]
    [Route("/Movies/{Id}/Similar", "GET", Summary = "Finds movies and trailers similar to a given movie.")]
    [Route("/Trailers/{Id}/Similar", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    [Authenticated]
    public class GetSimilarItems : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Libraries/AvailableOptions", "GET")]
    [Authenticated(AllowBeforeStartupWizard = true)]
    public class GetLibraryOptionsInfo : IReturn<LibraryOptionsResult>
    {
        public string LibraryContentType { get; set; }
        public bool IsNewLibrary { get; set; }
    }

    public class LibraryOptionInfo
    {
        public string Name { get; set; }
        public bool DefaultEnabled { get; set; }
    }

    public class LibraryOptionsResult
    {
        public LibraryOptionInfo[] MetadataSavers { get; set; }
        public LibraryOptionInfo[] MetadataReaders { get; set; }
        public LibraryOptionInfo[] SubtitleFetchers { get; set; }
        public LibraryTypeOptions[] TypeOptions { get; set; }
    }

    public class LibraryTypeOptions
    {
        public string Type { get; set; }
        public LibraryOptionInfo[] MetadataFetchers { get; set; }
        public LibraryOptionInfo[] ImageFetchers { get; set; }
        public ImageType[] SupportedImageTypes { get; set; }
        public ImageOption[] DefaultImageOptions { get; set; }
    }

    /// <summary>
    /// Class LibraryService
    /// </summary>
    public class LibraryService : BaseApiService
    {
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly ILibraryMonitor _libraryMonitor;

        private readonly ILogger<MoviesService> _moviesServiceLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        public LibraryService(
            ILogger<LibraryService> logger,
            ILogger<MoviesService> moviesServiceLogger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IProviderManager providerManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            IActivityManager activityManager,
            ILocalizationManager localization,
            ILibraryMonitor libraryMonitor)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _providerManager = providerManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _authContext = authContext;
            _activityManager = activityManager;
            _localization = localization;
            _libraryMonitor = libraryMonitor;
            _moviesServiceLogger = moviesServiceLogger;
        }

        private string[] GetRepresentativeItemTypes(string contentType)
        {
            return contentType switch
            {
                CollectionType.BoxSets => new[] {"BoxSet"},
                CollectionType.Playlists => new[] {"Playlist"},
                CollectionType.Movies => new[] {"Movie"},
                CollectionType.TvShows => new[] {"Series", "Season", "Episode"},
                CollectionType.Books => new[] {"Book"},
                CollectionType.Music => new[] {"MusicAlbum", "MusicArtist", "Audio", "MusicVideo"},
                CollectionType.HomeVideos => new[] {"Video", "Photo"},
                CollectionType.Photos => new[] {"Video", "Photo"},
                CollectionType.MusicVideos => new[] {"MusicVideo"},
                _ => new[] {"Series", "Season", "Episode", "Movie"}
            };
        }

        private bool IsSaverEnabledByDefault(string name, string[] itemTypes, bool isNewLibrary)
        {
            if (isNewLibrary)
            {
                return false;
            }

            var metadataOptions = ServerConfigurationManager.Configuration.MetadataOptions
                .Where(i => itemTypes.Contains(i.ItemType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (metadataOptions.Length == 0)
            {
                return true;
            }

            return metadataOptions.Any(i => !i.DisabledMetadataSavers.Contains(name, StringComparer.OrdinalIgnoreCase));
        }

        private bool IsMetadataFetcherEnabledByDefault(string name, string type, bool isNewLibrary)
        {
            if (isNewLibrary)
            {
                if (string.Equals(name, "TheMovieDb", StringComparison.OrdinalIgnoreCase))
                {
                    return !(string.Equals(type, "Season", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(type, "Episode", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(type, "MusicVideo", StringComparison.OrdinalIgnoreCase));
                }

                return string.Equals(name, "TheTVDB", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "TheAudioDB", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "MusicBrainz", StringComparison.OrdinalIgnoreCase);
            }

            var metadataOptions = ServerConfigurationManager.Configuration.MetadataOptions
                .Where(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return metadataOptions.Length == 0
               || metadataOptions.Any(i => !i.DisabledMetadataFetchers.Contains(name, StringComparer.OrdinalIgnoreCase));
        }

        private bool IsImageFetcherEnabledByDefault(string name, string type, bool isNewLibrary)
        {
            if (isNewLibrary)
            {
                if (string.Equals(name, "TheMovieDb", StringComparison.OrdinalIgnoreCase))
                {
                    return !string.Equals(type, "Series", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(type, "Season", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(type, "Episode", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(type, "MusicVideo", StringComparison.OrdinalIgnoreCase);
                }

                return string.Equals(name, "TheTVDB", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(name, "Screen Grabber", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(name, "TheAudioDB", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(name, "Emby Designs", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(name, "Image Extractor", StringComparison.OrdinalIgnoreCase);
            }

            var metadataOptions = ServerConfigurationManager.Configuration.MetadataOptions
                .Where(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (metadataOptions.Length == 0)
            {
                return true;
            }

            return metadataOptions.Any(i => !i.DisabledImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase));
        }

        public object Get(GetLibraryOptionsInfo request)
        {
            var result = new LibraryOptionsResult();

            var types = GetRepresentativeItemTypes(request.LibraryContentType);
            var isNewLibrary = request.IsNewLibrary;
            var typesList = types.ToList();

            var plugins = _providerManager.GetAllMetadataPlugins()
                .Where(i => types.Contains(i.ItemType, StringComparer.OrdinalIgnoreCase))
                .OrderBy(i => typesList.IndexOf(i.ItemType))
                .ToList();

            result.MetadataSavers = plugins
                .SelectMany(i => i.Plugins.Where(p => p.Type == MetadataPluginType.MetadataSaver))
                .Select(i => new LibraryOptionInfo
                {
                    Name = i.Name,
                    DefaultEnabled = IsSaverEnabledByDefault(i.Name, types, isNewLibrary)
                })
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray();

            result.MetadataReaders = plugins
                .SelectMany(i => i.Plugins.Where(p => p.Type == MetadataPluginType.LocalMetadataProvider))
                .Select(i => new LibraryOptionInfo
                {
                    Name = i.Name,
                    DefaultEnabled = true
                })
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray();

            result.SubtitleFetchers = plugins
                .SelectMany(i => i.Plugins.Where(p => p.Type == MetadataPluginType.SubtitleFetcher))
                .Select(i => new LibraryOptionInfo
                {
                    Name = i.Name,
                    DefaultEnabled = true
                })
                .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray();

            var typeOptions = new List<LibraryTypeOptions>();

            foreach (var type in types)
            {
                TypeOptions.DefaultImageOptions.TryGetValue(type, out var defaultImageOptions);

                typeOptions.Add(new LibraryTypeOptions
                {
                    Type = type,

                    MetadataFetchers = plugins
                    .Where(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(i => i.Plugins.Where(p => p.Type == MetadataPluginType.MetadataFetcher))
                    .Select(i => new LibraryOptionInfo
                    {
                        Name = i.Name,
                        DefaultEnabled = IsMetadataFetcherEnabledByDefault(i.Name, type, isNewLibrary)
                    })
                    .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToArray(),

                    ImageFetchers = plugins
                    .Where(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(i => i.Plugins.Where(p => p.Type == MetadataPluginType.ImageFetcher))
                    .Select(i => new LibraryOptionInfo
                    {
                        Name = i.Name,
                        DefaultEnabled = IsImageFetcherEnabledByDefault(i.Name, type, isNewLibrary)
                    })
                    .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToArray(),

                    SupportedImageTypes = plugins
                    .Where(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(i => i.SupportedImageTypes ?? Array.Empty<ImageType>())
                    .Distinct()
                    .ToArray(),

                    DefaultImageOptions = defaultImageOptions ?? Array.Empty<ImageOption>()
                });
            }

            result.TypeOptions = typeOptions.ToArray();

            return result;
        }

        public object Get(GetSimilarItems request)
        {
            var item = string.IsNullOrEmpty(request.Id) ?
                (!request.UserId.Equals(Guid.Empty) ? _libraryManager.GetUserRootFolder() :
                _libraryManager.RootFolder) : _libraryManager.GetItemById(request.Id);

            var program = item as IHasProgramAttributes;

            if (item is Movie || (program != null && program.IsMovie) || item is Trailer)
            {
                return new MoviesService(
                    _moviesServiceLogger,
                    ServerConfigurationManager,
                    ResultFactory,
                    _userManager,
                    _libraryManager,
                    _dtoService,
                    _authContext)
                {
                    Request = Request,

                }.GetSimilarItemsResult(request);
            }

            if (program != null && program.IsSeries)
            {
                return GetSimilarItemsResult(request, new[] { typeof(Series).Name });
            }

            if (item is Episode || (item is IItemByName && !(item is MusicArtist)))
            {
                return new QueryResult<BaseItemDto>();
            }

            return GetSimilarItemsResult(request, new[] { item.GetType().Name });
        }

        private QueryResult<BaseItemDto> GetSimilarItemsResult(BaseGetSimilarItemsFromItem request, string[] includeItemTypes)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (!request.UserId.Equals(Guid.Empty) ? _libraryManager.GetUserRootFolder() :
                _libraryManager.RootFolder) : _libraryManager.GetItemById(request.Id);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var query = new InternalItemsQuery(user)
            {
                Limit = request.Limit,
                IncludeItemTypes = includeItemTypes,
                SimilarTo = item,
                DtoOptions = dtoOptions,
                EnableTotalRecordCount = false
            };

            // ExcludeArtistIds
            if (!string.IsNullOrEmpty(request.ExcludeArtistIds))
            {
                query.ExcludeArtistIds = GetGuids(request.ExcludeArtistIds);
            }

            List<BaseItem> itemsResult;

            if (item is MusicArtist)
            {
                query.IncludeItemTypes = Array.Empty<string>();

                itemsResult = _libraryManager.GetArtists(query).Items.Select(i => i.Item1).ToList();
            }
            else
            {
                itemsResult = _libraryManager.GetItemList(query);
            }

            var returnList = _dtoService.GetBaseItemDtos(itemsResult, dtoOptions, user);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnList,
                TotalRecordCount = itemsResult.Count
            };

            return result;
        }

        public object Get(GetMediaFolders request)
        {
            var items = _libraryManager.GetUserRootFolder().Children.Concat(_libraryManager.RootFolder.VirtualChildren).OrderBy(i => i.SortName).ToList();

            if (request.IsHidden.HasValue)
            {
                var val = request.IsHidden.Value;

                items = items.Where(i => i.IsHidden == val).ToList();
            }

            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = new QueryResult<BaseItemDto>
            {
                TotalRecordCount = items.Count,

                Items = items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions)).ToArray()
            };

            return result;
        }

        public void Post(PostUpdatedSeries request)
        {
            var series = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }

            }).Where(i => string.Equals(request.TvdbId, i.GetProviderId(MetadataProviders.Tvdb), StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var item in series)
            {
                _libraryMonitor.ReportFileSystemChanged(item.Path);
            }
        }

        public void Post(PostUpdatedMedia request)
        {
            if (request.Updates != null)
            {
                foreach (var item in request.Updates)
                {
                    _libraryMonitor.ReportFileSystemChanged(item.Path);
                }
            }
        }

        public void Post(PostUpdatedMovies request)
        {
            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Movie).Name },
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }

            });

            if (!string.IsNullOrWhiteSpace(request.ImdbId))
            {
                movies = movies.Where(i => string.Equals(request.ImdbId, i.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(request.TmdbId))
            {
                movies = movies.Where(i => string.Equals(request.TmdbId, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                movies = new List<BaseItem>();
            }

            foreach (var item in movies)
            {
                _libraryMonitor.ReportFileSystemChanged(item.Path);
            }
        }

        public Task<object> Get(GetDownload request)
        {
            var item = _libraryManager.GetItemById(request.Id);
            var auth = _authContext.GetAuthorizationInfo(Request);

            var user = auth.User;

            if (user != null)
            {
                if (!item.CanDownload(user))
                {
                    throw new ArgumentException("Item does not support downloading");
                }
            }
            else
            {
                if (!item.CanDownload())
                {
                    throw new ArgumentException("Item does not support downloading");
                }
            }

            var headers = new Dictionary<string, string>();

            if (user != null)
            {
                LogDownload(item, user, auth);
            }

            var path = item.Path;

            // Quotes are valid in linux. They'll possibly cause issues here
            var filename = (Path.GetFileName(path) ?? string.Empty).Replace("\"", string.Empty);
            if (!string.IsNullOrWhiteSpace(filename))
            {
                // Kestrel doesn't support non-ASCII characters in headers
                if (Regex.IsMatch(filename, @"[^\p{IsBasicLatin}]"))
                {
                    // Manually encoding non-ASCII characters, following https://tools.ietf.org/html/rfc5987#section-3.2.2
                    headers[HeaderNames.ContentDisposition] = "attachment; filename*=UTF-8''" + WebUtility.UrlEncode(filename);
                }
                else
                {
                    headers[HeaderNames.ContentDisposition] = "attachment; filename=\"" + filename + "\"";
                }
            }

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = path,
                ResponseHeaders = headers
            });
        }

        private void LogDownload(BaseItem item, User user, AuthorizationInfo auth)
        {
            try
            {
                _activityManager.Create(new Jellyfin.Data.Entities.ActivityLog(
                    string.Format(_localization.GetLocalizedString("UserDownloadingItemWithValues"), user.Name, item.Name),
                    "UserDownloadingContent",
                    auth.UserId)
                {
                    ShortOverview = string.Format(_localization.GetLocalizedString("AppDeviceValues"), auth.Client, auth.Device),
                });
            }
            catch
            {
                // Logged at lower levels
            }
        }

        public Task<object> Get(GetFile request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            return ResultFactory.GetStaticFileResult(Request, item.Path);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPhyscialPaths request)
        {
            var result = _libraryManager.RootFolder.Children
                .SelectMany(c => c.PhysicalLocations)
                .ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAncestors request)
        {
            var result = GetAncestors(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the ancestors.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        public List<BaseItemDto> GetAncestors(GetAncestors request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var baseItemDtos = new List<BaseItemDto>();

            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var dtoOptions = GetDtoOptions(_authContext, request);

            BaseItem parent = item.GetParent();

            while (parent != null)
            {
                if (user != null)
                {
                    parent = TranslateParentItem(parent, user);
                }

                baseItemDtos.Add(_dtoService.GetBaseItemDto(parent, dtoOptions, user));

                parent = parent.GetParent();
            }

            return baseItemDtos;
        }

        private BaseItem TranslateParentItem(BaseItem item, User user)
        {
            return item.GetParent() is AggregateFolder
                ? _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .FirstOrDefault(i => i.PhysicalLocations.Contains(item.Path))
                : item;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCriticReviews request)
        {
            return new QueryResult<BaseItemDto>();
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemCounts request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            var counts = new ItemCounts
            {
                AlbumCount = GetCount(typeof(MusicAlbum), user, request),
                EpisodeCount = GetCount(typeof(Episode), user, request),
                MovieCount = GetCount(typeof(Movie), user, request),
                SeriesCount = GetCount(typeof(Series), user, request),
                SongCount = GetCount(typeof(Audio), user, request),
                MusicVideoCount = GetCount(typeof(MusicVideo), user, request),
                BoxSetCount = GetCount(typeof(BoxSet), user, request),
                BookCount = GetCount(typeof(Book), user, request)
            };

            return ToOptimizedResult(counts);
        }

        private int GetCount(Type type, User user, GetItemCounts request)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { type.Name },
                Limit = 0,
                Recursive = true,
                IsVirtualItem = false,
                IsFavorite = request.IsFavorite,
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }
            };

            return _libraryManager.GetItemsResult(query).TotalRecordCount;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task Post(RefreshLibrary request)
        {
            try
            {
                await _libraryManager.ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refreshing library");
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItems request)
        {
            var ids = string.IsNullOrWhiteSpace(request.Ids)
                ? Array.Empty<string>()
                : request.Ids.Split(',');

            foreach (var i in ids)
            {
                var item = _libraryManager.GetItemById(i);
                var auth = _authContext.GetAuthorizationInfo(Request);
                var user = auth.User;

                if (!item.CanDelete(user))
                {
                    if (ids.Length > 1)
                    {
                        throw new SecurityException("Unauthorized access");
                    }

                    continue;
                }

                _libraryManager.DeleteItem(item, new DeleteOptions
                {
                    DeleteFileLocation = true
                }, true);
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItem request)
        {
            Delete(new DeleteItems
            {
                Ids = request.Id
            });
        }

        public object Get(GetThemeMedia request)
        {
            var themeSongs = GetThemeSongs(new GetThemeSongs
            {
                InheritFromParent = request.InheritFromParent,
                Id = request.Id,
                UserId = request.UserId

            });

            var themeVideos = GetThemeVideos(new GetThemeVideos
            {
                InheritFromParent = request.InheritFromParent,
                Id = request.Id,
                UserId = request.UserId

            });

            return ToOptimizedResult(new AllThemeMediaResult
            {
                ThemeSongsResult = themeSongs,
                ThemeVideosResult = themeVideos,

                SoundtrackSongsResult = new ThemeMediaResult()
            });
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeSongs request)
        {
            var result = GetThemeSongs(request);

            return ToOptimizedResult(result);
        }

        private ThemeMediaResult GetThemeSongs(GetThemeSongs request)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (!request.UserId.Equals(Guid.Empty)
                                  ? _libraryManager.GetUserRootFolder()
                                  : _libraryManager.RootFolder)
                           : _libraryManager.GetItemById(request.Id);

            if (item == null)
            {
                throw new ResourceNotFoundException("Item not found.");
            }

            IEnumerable<BaseItem> themeItems;

            while (true)
            {
                themeItems = item.GetThemeSongs();

                if (themeItems.Any() || !request.InheritFromParent)
                {
                    break;
                }

                var parent = item.GetParent();
                if (parent == null)
                {
                    break;
                }
                item = parent;
            }

            var dtoOptions = GetDtoOptions(_authContext, request);
            var items = themeItems
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item))
                .ToArray();

            return new ThemeMediaResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = item.Id
            };
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeVideos request)
        {
            return ToOptimizedResult(GetThemeVideos(request));
        }

        public ThemeMediaResult GetThemeVideos(GetThemeVideos request)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (!request.UserId.Equals(Guid.Empty)
                                  ? _libraryManager.GetUserRootFolder()
                                  : _libraryManager.RootFolder)
                           : _libraryManager.GetItemById(request.Id);

            if (item == null)
            {
                throw new ResourceNotFoundException("Item not found.");
            }

            IEnumerable<BaseItem> themeItems;

            while (true)
            {
                themeItems = item.GetThemeVideos();

                if (themeItems.Any() || !request.InheritFromParent)
                {
                    break;
                }

                var parent = item.GetParent();
                if (parent == null)
                {
                    break;
                }
                item = parent;
            }

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = themeItems
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item))
                .ToArray();

            return new ThemeMediaResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = item.Id
            };
        }
    }
}
