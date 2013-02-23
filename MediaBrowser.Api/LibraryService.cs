using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetPhyscialPaths
    /// </summary>
    [Route("/Library/PhysicalPaths", "GET")]
    public class GetPhyscialPaths : IReturn<List<string>>
    {
    }

    /// <summary>
    /// Class GetItemTypes
    /// </summary>
    [Route("/Library/ItemTypes", "GET")]
    public class GetItemTypes : IReturn<List<string>>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance has internet provider.
        /// </summary>
        /// <value><c>true</c> if this instance has internet provider; otherwise, <c>false</c>.</value>
        public bool HasInternetProvider { get; set; }
    }

    /// <summary>
    /// Class GetPerson
    /// </summary>
    [Route("/Library/Persons/{Name}", "GET")]
    public class GetPerson : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetStudio
    /// </summary>
    [Route("/Library/Studios/{Name}", "GET")]
    public class GetStudio : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetGenre
    /// </summary>
    [Route("/Library/Genres/{Name}", "GET")]
    public class GetGenre : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetYear
    /// </summary>
    [Route("/Library/Years/{Year}", "GET")]
    public class GetYear : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int Year { get; set; }
    }

    /// <summary>
    /// Class GetDefaultVirtualFolders
    /// </summary>
    [Route("/Library/DefaultVirtualFolders", "GET")]
    public class GetDefaultVirtualFolders : IReturn<List<VirtualFolderInfo>>
    {
    }

    /// <summary>
    /// Class LibraryService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class LibraryService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDefaultVirtualFolders request)
        {
            var kernel = (Kernel)Kernel;

            var result = kernel.LibraryManager.GetDefaultVirtualFolders().ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPerson request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetPerson(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));
            
            var result = new DtoBuilder(Logger).GetDtoBaseItem(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenre request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetGenre(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger).GetDtoBaseItem(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudio request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetStudio(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger).GetDtoBaseItem(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYear request)
        {
            var kernel = (Kernel)Kernel;

            var item = kernel.LibraryManager.GetYear(request.Year).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger).GetDtoBaseItem(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPhyscialPaths request)
        {
            var kernel = (Kernel)Kernel;

            var result = kernel.RootFolder.Children.SelectMany(c => c.ResolveArgs.PhysicalLocations).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemTypes request)
        {
            var kernel = (Kernel)Kernel;

            var allTypes = kernel.AllTypes.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(BaseItem)));

            if (request.HasInternetProvider)
            {
                allTypes = allTypes.Where(t =>
                {
                    if (t == typeof(UserRootFolder) || t == typeof(AggregateFolder) || t == typeof(Folder) || t == typeof(IndexFolder) || t == typeof(CollectionFolder) || t == typeof(Year))
                    {
                        return false;
                    }

                    if (t == typeof(User))
                    {
                        return false;
                    }

                    // For now it seems internet providers generally only deal with video subclasses
                    if (t == typeof(Video))
                    {
                        return false;
                    }

                    if (t.IsSubclassOf(typeof(BasePluginFolder)))
                    {
                        return false;
                    }

                    return true;
                });
            }

            return allTypes.Select(t => t.Name).OrderBy(s => s).ToList();
        }
    }
}
