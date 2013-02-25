using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Library
{
    /// <summary>
    /// Class GetDefaultVirtualFolders
    /// </summary>
    [Route("/Library/VirtualFolders", "GET")]
    [Route("/Users/{UserId}/VirtualFolders", "GET")]
    public class GetVirtualFolders : IReturn<List<VirtualFolderInfo>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }
    }

    [Route("/Library/VirtualFolders/{Name}", "POST")]
    [Route("/Users/{UserId}/VirtualFolders/{Name}", "POST")]
    public class AddVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }
        
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    [Route("/Library/VirtualFolders/{Name}", "DELETE")]
    [Route("/Users/{UserId}/VirtualFolders/{Name}", "DELETE")]
    public class RemoveVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    [Route("/Library/VirtualFolders/{Name}/Name", "POST")]
    [Route("/Users/{UserId}/VirtualFolders/{Name}/Name", "POST")]
    public class RenameVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string NewName { get; set; }
    }

    [Route("/Library/VirtualFolders/{Name}/Paths", "POST")]
    [Route("/Users/{UserId}/VirtualFolders/{Name}/Paths", "POST")]
    public class AddMediaPath : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Path { get; set; }
    }
    
    [Route("/Library/VirtualFolders/{Name}/Paths", "DELETE")]
    [Route("/Users/{UserId}/VirtualFolders/{Name}/Paths", "DELETE")]
    public class RemoveMediaPath : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Path { get; set; }
    }

    /// <summary>
    /// Class LibraryStructureService
    /// </summary>
    public class LibraryStructureService : BaseRestService
    {
        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <exception cref="System.ArgumentNullException">appHost</exception>
        public LibraryStructureService(IServerApplicationPaths appPaths)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _appPaths = appPaths;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVirtualFolders request)
        {
            var kernel = (Kernel)Kernel;

            if (string.IsNullOrEmpty(request.UserId))
            {
                var result = kernel.LibraryManager.GetDefaultVirtualFolders().ToList();

                return ToOptimizedResult(result);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                var result = kernel.LibraryManager.GetVirtualFolders(user).ToList();

                return ToOptimizedResult(result);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AddVirtualFolder request)
        {
            var kernel = (Kernel)Kernel;
            
            if (string.IsNullOrEmpty(request.UserId))
            {
                LibraryHelpers.AddVirtualFolder(request.Name, null, _appPaths);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                LibraryHelpers.AddVirtualFolder(request.Name, user, _appPaths);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RenameVirtualFolder request)
        {
            var kernel = (Kernel)Kernel;

            if (string.IsNullOrEmpty(request.UserId))
            {
                LibraryHelpers.RenameVirtualFolder(request.Name, request.NewName, null, _appPaths);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                LibraryHelpers.RenameVirtualFolder(request.Name, request.NewName, user, _appPaths);
            }
        }
        
        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(RemoveVirtualFolder request)
        {
            var kernel = (Kernel)Kernel;

            if (string.IsNullOrEmpty(request.UserId))
            {
                LibraryHelpers.RemoveVirtualFolder(request.Name, null, _appPaths);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                LibraryHelpers.RemoveVirtualFolder(request.Name, user, _appPaths);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AddMediaPath request)
        {
            var kernel = (Kernel)Kernel;

            if (string.IsNullOrEmpty(request.UserId))
            {
                LibraryHelpers.AddMediaPath(request.Name, request.Path, null, _appPaths);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                LibraryHelpers.AddMediaPath(request.Name, request.Path, user, _appPaths);
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(RemoveMediaPath request)
        {
            var kernel = (Kernel)Kernel;

            if (string.IsNullOrEmpty(request.UserId))
            {
                LibraryHelpers.RemoveMediaPath(request.Name, request.Path, null, _appPaths);
            }
            else
            {
                var user = kernel.GetUserById(new Guid(request.UserId));

                LibraryHelpers.RemoveMediaPath(request.Name, request.Path, user, _appPaths);
            }
        }
    }
}
