using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Library
{
    [Route("/Videos/{Id}/Subtitles/{Index}", "GET", Summary = "Gets an external subtitle file")]
    public class GetSubtitle
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Index", Description = "The subtitle stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }
    }

    [Route("/Videos/{Id}/Subtitles/{Index}", "DELETE", Summary = "Deletes an external subtitle file")]
    public class DeleteSubtitle
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }

        [ApiMember(Name = "Index", Description = "The subtitle stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "DELETE")]
        public int Index { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/{Language}", "GET")]
    public class SearchRemoteSubtitles : IReturn<List<RemoteSubtitleInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Language", Description = "Language", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Language { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/Providers", "GET")]
    public class GetSubtitleProviders : IReturn<List<SubtitleProviderInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/{SubtitleId}", "POST")]
    public class DownloadRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "SubtitleId", Description = "SubtitleId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SubtitleId { get; set; }
    }

    [Route("/Providers/Subtitles/Subtitles/{Id}", "GET")]
    public class GetRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    public class SubtitleService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IItemRepository _itemRepo;

        public SubtitleService(ILibraryManager libraryManager, ISubtitleManager subtitleManager, IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _subtitleManager = subtitleManager;
            _itemRepo = itemRepo;
        }

        public object Get(SearchRemoteSubtitles request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            var response = _subtitleManager.SearchSubtitles(video, request.Language, CancellationToken.None).Result;

            return ToOptimizedResult(response);
        }
        public object Get(GetSubtitle request)
        {
            var subtitleStream = _itemRepo.GetMediaStreams(new MediaStreamQuery
            {

                Index = request.Index,
                ItemId = new Guid(request.Id),
                Type = MediaStreamType.Subtitle

            }).FirstOrDefault();

            if (subtitleStream == null)
            {
                throw new ResourceNotFoundException();
            }

            return ToStaticFileResult(subtitleStream.Path);
        }

        public void Delete(DeleteSubtitle request)
        {
            var task = _subtitleManager.DeleteSubtitles(request.Id, request.Index);

            Task.WaitAll(task);
        }

        public object Get(GetSubtitleProviders request)
        {
            var result = _subtitleManager.GetProviders(request.Id);

            return ToOptimizedResult(result);
        }

        public object Get(GetRemoteSubtitles request)
        {
            var result = _subtitleManager.GetRemoteSubtitles(request.Id, CancellationToken.None).Result;

            return ResultFactory.GetResult(result.Stream, MimeTypes.GetMimeType("file." + result.Format));
        }

        public void Post(DownloadRemoteSubtitles request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            Task.Run(async () =>
            {
                try
                {
                    await _subtitleManager.DownloadSubtitles(video, request.SubtitleId, CancellationToken.None)
                        .ConfigureAwait(false);

                    await video.RefreshMetadata(new MetadataRefreshOptions(), CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error downloading subtitles", ex);
                }

            });
        }
    }
}
