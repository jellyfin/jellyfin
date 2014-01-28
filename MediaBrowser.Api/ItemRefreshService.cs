using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using ServiceStack;
using System;
using System.Linq;
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

    [Route("/Artists/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for an artist")]
    public class RefreshArtist : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/Genres/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for a genre")]
    public class RefreshGenre : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/MusicGenres/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for a music genre")]
    public class RefreshMusicGenre : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/GameGenres/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for a game genre")]
    public class RefreshGameGenre : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/Persons/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for a person")]
    public class RefreshPerson : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    [Route("/Studios/{Name}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for a studio")]
    public class RefreshStudio : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    public class ItemRefreshService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;

        public ItemRefreshService(ILibraryManager libraryManager, IDtoService dtoService)
        {
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        public void Post(RefreshArtist request)
        {
            var task = RefreshArtist(request);

            Task.WaitAll(task);
        }

        private async Task RefreshArtist(RefreshArtist request)
        {
            var item = GetArtist(request.Name, _libraryManager);

            var cancellationToken = CancellationToken.None;

            var albums = _libraryManager.RootFolder
                                        .RecursiveChildren
                                        .OfType<MusicAlbum>()
                                        .Where(i => i.HasArtist(item.Name))
                                        .ToList();

            var musicArtists = albums
                .Select(i => i.Parent)
                .OfType<MusicArtist>()
                .ToList();

            var musicArtistRefreshTasks = musicArtists.Select(i => i.ValidateChildren(new Progress<double>(), cancellationToken, true, request.Forced));

            await Task.WhenAll(musicArtistRefreshTasks).ConfigureAwait(false);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(RefreshGenre request)
        {
            var task = RefreshGenre(request);

            Task.WaitAll(task);
        }

        private async Task RefreshGenre(RefreshGenre request)
        {
            var item = GetGenre(request.Name, _libraryManager);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(RefreshMusicGenre request)
        {
            var task = RefreshMusicGenre(request);

            Task.WaitAll(task);
        }

        private async Task RefreshMusicGenre(RefreshMusicGenre request)
        {
            var item = GetMusicGenre(request.Name, _libraryManager);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(RefreshGameGenre request)
        {
            var task = RefreshGameGenre(request);

            Task.WaitAll(task);
        }

        private async Task RefreshGameGenre(RefreshGameGenre request)
        {
            var item = GetGameGenre(request.Name, _libraryManager);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(RefreshPerson request)
        {
            var task = RefreshPerson(request);

            Task.WaitAll(task);
        }

        private async Task RefreshPerson(RefreshPerson request)
        {
            var item = GetPerson(request.Name, _libraryManager);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(RefreshStudio request)
        {
            var task = RefreshStudio(request);

            Task.WaitAll(task);
        }

        private async Task RefreshStudio(RefreshStudio request)
        {
            var item = GetStudio(request.Name, _libraryManager);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
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
            var item = _dtoService.GetItemByDtoId(request.Id);

            try
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);

                if (item.IsFolder)
                {
                    // Collection folders don't validate their children so we'll have to simulate that here
                    var collectionFolder = item as CollectionFolder;

                    if (collectionFolder != null)
                    {
                        await RefreshCollectionFolderChildren(request, collectionFolder).ConfigureAwait(false);
                    }
                    else
                    {
                        var folder = (Folder)item;

                        await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, request.Recursive, request.Forced).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        /// <summary>
        /// Refreshes the collection folder children.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="collectionFolder">The collection folder.</param>
        /// <returns>Task.</returns>
        private async Task RefreshCollectionFolderChildren(RefreshItem request, CollectionFolder collectionFolder)
        {
            foreach (var child in collectionFolder.Children.ToList())
            {
                await child.RefreshMetadata(new MetadataRefreshOptions
                {
                    ReplaceAllMetadata = request.Forced,

                }, CancellationToken.None).ConfigureAwait(false);

                if (child.IsFolder)
                {
                    var folder = (Folder)child;

                    await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, request.Recursive, request.Forced).ConfigureAwait(false);
                }
            }
        }
    }
}
