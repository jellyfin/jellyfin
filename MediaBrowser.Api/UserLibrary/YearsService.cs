using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetYears
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Years", "GET")]
    [Route("/Users/{UserId}/Items/Root/Years", "GET")]
    public class GetYears : GetItemsByName
    {
    }

    /// <summary>
    /// Class YearsService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class YearsService : BaseItemsByNameService<Year>
    {
        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYears request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<Tuple<string, Func<int>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.Where(i => i.ProductionYear != null).ToList();

            return itemsList
                .Select(i => i.ProductionYear.Value)
                .Distinct()
                .Select(year => new Tuple<string, Func<int>>(year.ToString(UsCulture), () => itemsList.Count(i => i.ProductionYear.HasValue && i.ProductionYear.Value == year)));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        protected override Task<Year> GetEntity(string name)
        {
            var kernel = (Kernel)Kernel;

            return kernel.LibraryManager.GetYear(int.Parse(name, UsCulture));
        }
    }
}
