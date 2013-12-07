using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    /// <summary>
    /// Class GetPhyscialPaths
    /// </summary>
    [Route("/Library/PhysicalPaths", "GET")]
    [Api(Description = "Gets a list of physical paths from virtual folders")]
    public class GetPhyscialPaths : IReturn<List<string>>
    {
    }

    /// <summary>
    /// Class GetItemTypes
    /// </summary>
    [Route("/Library/ItemTypes", "GET")]
    [Api(Description = "Gets a list of BaseItem types")]
    public class GetItemTypes : IReturn<List<string>>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance has internet provider.
        /// </summary>
        /// <value><c>true</c> if this instance has internet provider; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "HasInternetProvider", Description = "Optional filter by item types that have internet providers. true/false", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool HasInternetProvider { get; set; }
    }

    /// <summary>
    /// Class LibraryService
    /// </summary>
    public class LibraryService : BaseApiService
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <exception cref="System.ArgumentNullException">appHost</exception>
        public LibraryService(IApplicationHost appHost, ILibraryManager libraryManager)
        {
            if (appHost == null)
            {
                throw new ArgumentNullException("appHost");
            }

            _appHost = appHost;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPhyscialPaths request)
        {
            var result = _libraryManager.RootFolder.Children.SelectMany(c => c.ResolveArgs.PhysicalLocations).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemTypes request)
        {
            var allTypes = _appHost.AllConcreteTypes.Where(t => t.IsSubclassOf(typeof(BaseItem)));

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
