using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItems
    /// </summary>
    [Route("/Users/{UserId}/Items", "GET")]
    [Api(Description = "Gets items based on a query.")]
    public class GetItems : BaseItemsRequest, IReturn<ItemsResult>
    {
        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        [ApiMember(Name = "Person", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Person { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        [ApiMember(Name = "PersonType", Description = "Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonType { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        [ApiMember(Name = "SearchTerm", Description = "Optional. If specified, results will be filtered based on a search term.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SearchTerm { get; set; }

        /// <summary>
        /// The dynamic, localized index function name
        /// </summary>
        /// <value>The index by.</value>
        public string IndexBy { get; set; }

        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        [ApiMember(Name = "SortBy", Description = "Optional. Specify one or more sort orders, comma delimeted. Options: Album, AlbumArtist, Artist, CommunityRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Runtime", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }

        /// <summary>
        /// Filters to apply to the results
        /// </summary>
        /// <value>The filters.</value>
        [ApiMember(Name = "Filters", Description = "Optional. Specify additional filters to apply. This allows multiple, comma delimeted. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsRecentlyAdded, IsResumable, Likes, Dislikes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Filters { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        [ApiMember(Name = "Genres", Description = "Optional. If specified, results will be filtered based on genre. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Genres { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Studios", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Studios { get; set; }

        /// <summary>
        /// Gets or sets the exclude item types.
        /// </summary>
        /// <value>The exclude item types.</value>
        [ApiMember(Name = "ExcludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        [ApiMember(Name = "Years", Description = "Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Years { get; set; }

        /// <summary>
        /// Gets or sets the image types.
        /// </summary>
        /// <value>The image types.</value>
        [ApiMember(Name = "ImageTypes", Description = "Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        [ApiMember(Name = "Ids", Description = "Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Ids { get; set; }

        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        [ApiMember(Name = "MediaTypes", Description = "Optional filter by MediaType. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        [ApiMember(Name = "VideoTypes", Description = "Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string VideoTypes { get; set; }

        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        [ApiMember(Name = "VideoFormats", Description = "Optional filter by VideoFormat (Standard, Digital3D, Sbs3D). Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string VideoFormats { get; set; }

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        /// <value>The series status.</value>
        [ApiMember(Name = "SeriesStatus", Description = "Optional filter by Series Status. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SeriesStatus { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        [ApiMember(Name = "AirDays", Description = "Optional filter by Series Air Days. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string AirDays { get; set; }
    }

    /// <summary>
    /// Class ItemsService
    /// </summary>
    public class ItemsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILibrarySearchEngine _searchEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="searchEngine">The search engine.</param>
        public ItemsService(IUserManager userManager, ILibraryManager libraryManager, ILibrarySearchEngine searchEngine)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _searchEngine = searchEngine;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItems request)
        {
            var result = GetItems(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        private async Task<ItemsResult> GetItems(GetItems request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = GetItemsToSerialize(request, user);

            items = items.AsParallel();

            // Apply filters
            // Run them starting with the ones that are likely to reduce the list the most
            foreach (var filter in GetFilters(request).OrderByDescending(f => (int)f))
            {
                items = ApplyFilter(items, filter, user);
            }

            items = ApplyAdditionalFilters(request, items);

            items = items.AsEnumerable();

            items = ApplySearchTerm(request, items);

            items = ApplySortOrder(request, items, user);

            var itemsArray = items.ToArray();

            var pagedItems = ApplyPaging(request, itemsArray);

            var fields = GetItemFields(request).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userManager);

            var returnItems = await Task.WhenAll(pagedItems.Select(i => dtoBuilder.GetBaseItemDto(i, user, fields))).ConfigureAwait(false);

            return new ItemsResult
            {
                TotalRecordCount = itemsArray.Length,
                Items = returnItems
            };
        }

        /// <summary>
        /// Gets the items to serialize.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private IEnumerable<BaseItem> GetItemsToSerialize(GetItems request, User user)
        {
            var item = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.ParentId, _userManager, _libraryManager, user.Id);

            // Default list type = children

            if (!string.IsNullOrEmpty(request.Ids))
            {
                var idList = request.Ids.Split(',').ToList();

                return idList.Select(i => DtoBuilder.GetItemByClientId(i, _userManager, _libraryManager, user.Id));
            }

            if (request.Recursive)
            {
                return ((Folder)item).GetRecursiveChildren(user);
            }

            return ((Folder)item).GetChildren(user, request.IndexBy);
        }

        /// <summary>
        /// Applies sort order
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplySortOrder(GetItems request, IEnumerable<BaseItem> items, User user)
        {
            var orderBy = GetOrderBy(request).ToArray();

            return orderBy.Length == 0 ? items : _libraryManager.Sort(items, user, orderBy, request.SortOrder ?? SortOrder.Ascending);
        }

        /// <summary>
        /// Applies filtering
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyFilter(IEnumerable<BaseItem> items, ItemFilter filter, User user)
        {
            // Avoids implicitly captured closure
            var currentUser = user;

            switch (filter)
            {
                case ItemFilter.Likes:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                    });

                case ItemFilter.Dislikes:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });

                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsRecentlyAdded:
                    return items.Where(item => item.IsRecentlyAdded(currentUser));

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata != null && userdata.PlaybackPositionTicks > 0;
                    });

                case ItemFilter.IsPlayed:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata != null && userdata.PlayCount > 0;
                    });

                case ItemFilter.IsUnplayed:
                    return items.Where(item =>
                    {
                        var userdata = _userManager.GetUserData(user.Id, item.UserDataId).Result;

                        return userdata == null || userdata.PlayCount == 0;
                    });

                case ItemFilter.IsFolder:
                    return items.Where(item => item.IsFolder);

                case ItemFilter.IsNotFolder:
                    return items.Where(item => !item.IsFolder);
            }

            return items;
        }

        /// <summary>
        /// Applies the additional filters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyAdditionalFilters(GetItems request, IEnumerable<BaseItem> items)
        {
            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                var vals = request.SeriesStatus.Split(',');

                items = items.OfType<Series>().Where(i => i.Status.HasValue && vals.Contains(i.Status.Value.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            // Filter by Series AirDays
            if (!string.IsNullOrEmpty(request.AirDays))
            {
                var days = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true));

                items = items.OfType<Series>().Where(i => i.AirDays != null && days.Any(d => i.AirDays.Contains(d)));
            }

            // Filter by VideoFormat
            if (!string.IsNullOrEmpty(request.VideoFormats))
            {
                var formats = request.VideoFormats.Split(',');

                items = items.OfType<Video>().Where(i => formats.Contains(i.VideoFormat.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            // Filter by VideoType
            if (!string.IsNullOrEmpty(request.VideoTypes))
            {
                var types = request.VideoTypes.Split(',');

                items = items.OfType<Video>().Where(i => types.Contains(i.VideoType.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.MediaTypes))
            {
                var types = request.MediaTypes.Split(',');

                items = items.Where(i => !string.IsNullOrEmpty(i.MediaType) && types.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase));
            }
            
            var imageTypes = GetImageTypes(request).ToArray();
            if (imageTypes.Length > 0)
            {
                items = items.Where(item => imageTypes.Any(imageType => HasImage(item, imageType)));
            }

            // Exclude item types
            var excludeItemTypes = request.ExcludeItemTypes;
            if (!string.IsNullOrEmpty(excludeItemTypes))
            {
                var vals = excludeItemTypes.Split(',');
                items = items.Where(f => !vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            var includeItemTypes = request.IncludeItemTypes;
            if (!string.IsNullOrEmpty(includeItemTypes))
            {
                var vals = includeItemTypes.Split(',');
                items = items.Where(f => vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            var genres = request.Genres;

            // Apply genre filter
            if (!string.IsNullOrEmpty(genres))
            {
                var vals = genres.Split(',');
                items = items.Where(f => f.Genres != null && vals.Any(v => f.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)));
            }

            var studios = request.Studios;

            // Apply studio filter
            if (!string.IsNullOrEmpty(studios))
            {
                var vals = studios.Split(',');
                items = items.Where(f => f.Studios != null && vals.Any(v => f.Studios.Contains(v, StringComparer.OrdinalIgnoreCase)));
            }

            var years = request.Years;

            // Apply year filter
            if (!string.IsNullOrEmpty(years))
            {
                var vals = years.Split(',').Select(int.Parse);
                items = items.Where(f => f.ProductionYear.HasValue && vals.Contains(f.ProductionYear.Value));
            }

            var personName = request.Person;

            // Apply person filter
            if (!string.IsNullOrEmpty(personName))
            {
                var personType = request.PersonType;

                items = !string.IsNullOrEmpty(personType)
                            ? items.Where(item => item.People != null && item.People.Any(p => p.Name.Equals(personName, StringComparison.OrdinalIgnoreCase) && p.Type.Equals(personType, StringComparison.OrdinalIgnoreCase)))
                            : items.Where(item => item.People != null && item.People.Any(p => p.Name.Equals(personName, StringComparison.OrdinalIgnoreCase)));
            }

            return items;
        }

        /// <summary>
        /// Determines whether the specified item has image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns><c>true</c> if the specified item has image; otherwise, <c>false</c>.</returns>
        private bool HasImage(BaseItem item, ImageType imageType)
        {
            if (imageType == ImageType.Backdrop)
            {
                return item.BackdropImagePaths != null && item.BackdropImagePaths.Count > 0;
            }

            if (imageType == ImageType.Screenshot)
            {
                return item.ScreenshotImagePaths != null && item.ScreenshotImagePaths.Count > 0;
            }

            if (imageType == ImageType.Chapter)
            {
                var video = item as Video;

                if (video != null)
                {
                    return video.Chapters != null && video.Chapters.Any(c => !string.IsNullOrEmpty(c.ImagePath));
                }

                return false;
            }

            return item.HasImage(imageType);
        }

        /// <summary>
        /// Applies the search term.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplySearchTerm(GetItems request, IEnumerable<BaseItem> items)
        {
            var term = request.SearchTerm;

            if (!string.IsNullOrEmpty(term))
            {
                items = _searchEngine.Search(items, request.SearchTerm);
            }

            return items;
        }

        /// <summary>
        /// Applies the paging.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyPaging(GetItems request, IEnumerable<BaseItem> items)
        {
            // Start at
            if (request.StartIndex.HasValue)
            {
                items = items.Skip(request.StartIndex.Value);
            }

            // Return limit
            if (request.Limit.HasValue)
            {
                items = items.Take(request.Limit.Value);
            }

            return items;
        }

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ItemFilter}.</returns>
        private IEnumerable<ItemFilter> GetFilters(GetItems request)
        {
            var val = request.Filters;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFilter[] { };
            }

            return val.Split(',').Select(v => (ItemFilter)Enum.Parse(typeof(ItemFilter), v, true));
        }

        /// <summary>
        /// Gets the item fields.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ItemFields}.</returns>
        private IEnumerable<ItemFields> GetItemFields(GetItems request)
        {
            var val = request.Fields;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFields[] { };
            }

            return val.Split(',').Select(v => (ItemFields)Enum.Parse(typeof(ItemFields), v, true));
        }

        /// <summary>
        /// Gets the order by.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ItemSortBy}.</returns>
        private IEnumerable<string> GetOrderBy(GetItems request)
        {
            var val = request.SortBy;

            if (string.IsNullOrEmpty(val))
            {
                return new string[] { };
            }

            return val.Split(',');
        }

        /// <summary>
        /// Gets the image types.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ImageType}.</returns>
        private IEnumerable<ImageType> GetImageTypes(GetItems request)
        {
            var val = request.ImageTypes;

            if (string.IsNullOrEmpty(val))
            {
                return new ImageType[] { };
            }

            return val.Split(',').Select(v => (ImageType)Enum.Parse(typeof(ImageType), v, true));
        }
    }

    /// <summary>
    /// Class DateCreatedComparer
    /// </summary>
    public class DateCreatedComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return x.DateCreated.CompareTo(y.DateCreated);
        }
    }
}
