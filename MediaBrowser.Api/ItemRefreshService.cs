using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using ServiceStack.ServiceHost;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Items/{Id}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for an item")]
    public class RefreshItem : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Recursive", Description = "Indicates if the refresh should occur recursively.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Recursive { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    public class ItemRefreshService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public ItemRefreshService(ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RefreshItem request)
        {
            var task = RefreshItem(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Refreshes the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        private async Task RefreshItem(RefreshItem request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager);

            var folder = item as Folder;

            try
            {
                await item.RefreshMetadata(CancellationToken.None, forceRefresh: request.Forced).ConfigureAwait(false);

                if (folder != null)
                {
                    await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, request.Recursive,
                                                request.Forced).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }
    }
}
