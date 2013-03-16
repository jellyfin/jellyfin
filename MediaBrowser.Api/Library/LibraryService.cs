using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    /// <summary>
    /// Class GetPhyscialPaths
    /// </summary>
    [Route("/Library/PhysicalPaths", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a list of physical paths from virtual folders")]
    public class GetPhyscialPaths : IReturn<List<string>>
    {
    }

    /// <summary>
    /// Class GetItemTypes
    /// </summary>
    [Route("/Library/ItemTypes", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a list of BaseItem types")]
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
    /// Class GetPerson
    /// </summary>
    [Route("/Persons/{Name}", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a person, by name")]
    public class GetPerson : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The person name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetStudio
    /// </summary>
    [Route("/Studios/{Name}", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a studio, by name")]
    public class GetStudio : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The studio name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetGenre
    /// </summary>
    [Route("/Genres/{Name}", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a genre, by name")]
    public class GetGenre : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetYear
    /// </summary>
    [Route("/Years/{Year}", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets a year")]
    public class GetYear : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        [ApiMember(Name = "Year", Description = "The year", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Year { get; set; }
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
        public object Get(GetPerson request)
        {
            var item = _libraryManager.GetPerson(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger, _libraryManager).GetBaseItemDto(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenre request)
        {
            var item = _libraryManager.GetGenre(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger, _libraryManager).GetBaseItemDto(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudio request)
        {
            var item = _libraryManager.GetStudio(request.Name).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger, _libraryManager).GetBaseItemDto(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYear request)
        {
            var item = _libraryManager.GetYear(request.Year).Result;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            var result = new DtoBuilder(Logger, _libraryManager).GetBaseItemDto(item, fields.ToList()).Result;

            return ToOptimizedResult(result);
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
