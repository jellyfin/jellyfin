using MediaBrowser.Common;
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
            var result = _libraryManager.RootFolder.Children
                .SelectMany(c => c.PhysicalLocations)
                .ToList();

            return ToOptimizedSerializedResultUsingCache(result);
        }
    }
}
