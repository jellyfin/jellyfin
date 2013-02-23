using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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
    public class GetItems : IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        public string ParentId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Whether or not to perform the query recursively
        /// </summary>
        /// <value><c>true</c> if recursive; otherwise, <c>false</c>.</value>
        public bool Recursive { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        public string Person { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        public string PersonType { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        public string SearchTerm { get; set; }

        /// <summary>
        /// The dynamic, localized index function name
        /// </summary>
        /// <value>The index by.</value>
        public string IndexBy { get; set; }

        /// <summary>
        /// The dynamic, localized sort function name
        /// </summary>
        /// <value>The dynamic sort by.</value>
        public string DynamicSortBy { get; set; }

        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        public string SortBy { get; set; }

        /// <summary>
        /// The sort order to return results with
        /// </summary>
        /// <value>The sort order.</value>
        public string SortOrder { get; set; }

        /// <summary>
        /// Filters to apply to the results
        /// </summary>
        /// <value>The filters.</value>
        public string Filters { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public string Fields { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        public string Genres { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        public string Studios { get; set; }

        /// <summary>
        /// Gets or sets the exclude item types.
        /// </summary>
        /// <value>The exclude item types.</value>
        public string ExcludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        public string IncludeItemTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        public string Years { get; set; }

        /// <summary>
        /// Gets or sets the image types.
        /// </summary>
        /// <value>The image types.</value>
        public string ImageTypes { get; set; }
    }

    /// <summary>
    /// Class ItemsService
    /// </summary>
    public class ItemsService : BaseRestService
    {
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
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var items = GetItemsToSerialize(request, user);

            // Apply filters
            // Run them starting with the ones that are likely to reduce the list the most
            foreach (var filter in GetFilters(request).OrderByDescending(f => (int)f))
            {
                items = ApplyFilter(items, filter, user);
            }

            items = ApplyAdditionalFilters(request, items);

            items = ApplySearchTerm(request, items);

            items = ApplySortOrder(request, items, user);

            var itemsArray = items.ToArray();

            var pagedItems = ApplyPaging(request, itemsArray);

            var fields = GetItemFields(request).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var returnItems = await Task.WhenAll(pagedItems.Select(i => dtoBuilder.GetDtoBaseItem(i, user, fields))).ConfigureAwait(false);

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
            var item = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.ParentId, user.Id);

            // Default list type = children

            if (request.Recursive)
            {
                return ((Folder)item).GetRecursiveChildren(user);
            }

            return ((Folder)item).GetChildren(user, request.IndexBy, request.DynamicSortBy, GetSortOrder(request));
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
            var isFirst = true;
            var descending = (GetSortOrder(request) ?? SortOrder.Ascending) == SortOrder.Descending;

            IOrderedEnumerable<BaseItem> orderedItems = null;

            foreach (var orderBy in GetOrderBy(request).Select(o => GetComparer(o, user)))
            {
                if (isFirst)
                {
                    orderedItems = descending ? items.OrderByDescending(i => i, orderBy) : items.OrderBy(i => i, orderBy);
                }
                else
                {
                    orderedItems = descending ? orderedItems.ThenByDescending(i => i, orderBy) : orderedItems.ThenBy(i => i, orderBy);
                }

                isFirst = false;
            }

            return orderedItems ?? items;
        }

        /// <summary>
        /// Gets the comparer.
        /// </summary>
        /// <param name="sortBy">The sort by.</param>
        /// <param name="user">The user.</param>
        /// <returns>IComparer{BaseItem}.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        private IComparer<BaseItem> GetComparer(ItemSortBy sortBy, User user)
        {
            switch (sortBy)
            {
                case ItemSortBy.Album:
                    return new AlbumComparer();
                case ItemSortBy.AlbumArtist:
                    return new AlbumArtistComparer();
                case ItemSortBy.Artist:
                    return new ArtistComparer();
                case ItemSortBy.Random:
                    return new RandomComparer();
                case ItemSortBy.DateCreated:
                    return new DateCreatedComparer();
                case ItemSortBy.SortName:
                    return new SortNameComparer();
                case ItemSortBy.PremiereDate:
                    return new PremiereDateComparer();
                case ItemSortBy.DatePlayed:
                    return new DatePlayedComparer { User = user };
                default:
                    throw new ArgumentException();
            }
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
            switch (filter)
            {
                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = item.GetUserData(user, false);

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsRecentlyAdded:
                    return items.Where(item => item.IsRecentlyAdded(user));

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = item.GetUserData(user, false);

                        return userdata != null && userdata.PlaybackPositionTicks > 0;
                    });

                case ItemFilter.IsPlayed:
                    return items.Where(item =>
                    {
                        var userdata = item.GetUserData(user, false);

                        return userdata != null && userdata.PlayCount > 0;
                    });

                case ItemFilter.IsRecentlyPlayed:
                    return items.Where(item => item.IsRecentlyPlayed(user));

                case ItemFilter.IsUnplayed:
                    return items.Where(item =>
                    {
                        var userdata = item.GetUserData(user, false);

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

            if (imageType == ImageType.ChapterImage)
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
                items = items.Where(i => i.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase));
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
        /// Gets the sort order.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Nullable{SortOrder}.</returns>
        private SortOrder? GetSortOrder(GetItems request)
        {
            if (string.IsNullOrEmpty(request.SortOrder))
            {
                return null;
            }

            return (SortOrder)Enum.Parse(typeof(SortOrder), request.SortOrder, true);
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
        private IEnumerable<ItemSortBy> GetOrderBy(GetItems request)
        {
            var val = request.SortBy;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemSortBy[] { };
            }

            return val.Split(',').Select(v => (ItemSortBy)Enum.Parse(typeof(ItemSortBy), v, true));
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

    /// <summary>
    /// Class RandomComparer
    /// </summary>
    public class RandomComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return Guid.NewGuid().CompareTo(Guid.NewGuid());
        }
    }

    /// <summary>
    /// Class SortNameComparer
    /// </summary>
    public class SortNameComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(x.SortName, y.SortName, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    /// <summary>
    /// Class AlbumArtistComparer
    /// </summary>
    public class AlbumArtistComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private string GetValue(BaseItem x)
        {
            var audio = x as Audio;

            return audio == null ? string.Empty : audio.AlbumArtist;
        }
    }

    /// <summary>
    /// Class AlbumComparer
    /// </summary>
    public class AlbumComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private string GetValue(BaseItem x)
        {
            var audio = x as Audio;

            return audio == null ? string.Empty : audio.Album;
        }
    }

    /// <summary>
    /// Class ArtistComparer
    /// </summary>
    public class ArtistComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private string GetValue(BaseItem x)
        {
            var audio = x as Audio;

            return audio == null ? string.Empty : audio.Artist;
        }
    }

    /// <summary>
    /// Class PremiereDateComparer
    /// </summary>
    public class PremiereDateComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetDate(x).CompareTo(GetDate(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private DateTime GetDate(BaseItem x)
        {
            if (x.PremiereDate.HasValue)
            {
                return x.PremiereDate.Value;
            }

            if (x.ProductionYear.HasValue)
            {
                return new DateTime(x.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return DateTime.MaxValue;
        }
    }

    /// <summary>
    /// Class DatePlayedComparer
    /// </summary>
    public class DatePlayedComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public User User { get; set; }

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetDate(x).CompareTo(GetDate(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private DateTime GetDate(BaseItem x)
        {
            var userdata = x.GetUserData(User, false);

            if (userdata != null && userdata.LastPlayedDate.HasValue)
            {
                return userdata.LastPlayedDate.Value;
            }

            return DateTime.MaxValue;
        }
    }
}
