using MediaBrowser.Api.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.LiveTv
{
    [Route("/LiveTv/Channels/{Id}/Images/{Type}", "POST")]
    [Route("/LiveTv/Channels/{Id}/Images/{Type}/{Index}", "POST")]
    [Api(Description = "Posts an item image")]
    public class PostChannelImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    [Route("/LiveTv/Channels/{Id}/Images/{Type}", "DELETE")]
    [Route("/LiveTv/Channels/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Api(Description = "Deletes an item image")]
    public class DeleteChannelImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }
    [Route("/LiveTv/Channels/{Id}/Images/{Type}", "GET")]
    [Route("/LiveTv/Channels/{Id}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an item image")]
    public class GetChannelImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Recordings/{Id}/Images/{Type}", "GET")]
    [Route("/LiveTv/Recordings/{Id}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an item image")]
    public class GetRecordingImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Programs/{Id}/Images/{Type}", "GET")]
    [Route("/LiveTv/Programs/{Id}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an item image")]
    public class GetProgramImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Program Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Channels/{Id}/Images", "GET")]
    [Api(Description = "Gets information about an item's images")]
    public class GetChannelImageInfos : IReturn<List<ImageInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
    
    public class LiveTvImageService : BaseApiService
    {
        private readonly ILiveTvManager _liveTv;

        private readonly IUserManager _userManager;

        private readonly ILibraryManager _libraryManager;

        private readonly IApplicationPaths _appPaths;

        private readonly IProviderManager _providerManager;

        private readonly IItemRepository _itemRepo;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        
        public LiveTvImageService(ILiveTvManager liveTv, IUserManager userManager, ILibraryManager libraryManager, IApplicationPaths appPaths, IProviderManager providerManager, IItemRepository itemRepo, IDtoService dtoService, IImageProcessor imageProcessor)
        {
            _liveTv = liveTv;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _appPaths = appPaths;
            _providerManager = providerManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
        }

        public object Get(GetChannelImageInfos request)
        {
            var item = _liveTv.GetInternalChannel(request.Id);

            var result = GetImageService().GetItemImageInfos(item);

            return ToOptimizedResult(result);
        }

        public object Get(GetChannelImage request)
        {
            var item = _liveTv.GetInternalChannel(request.Id);

            return GetImageService().GetImage(request, item);
        }

        public object Get(GetRecordingImage request)
        {
            var item = _liveTv.GetInternalRecording(request.Id, CancellationToken.None).Result;

            return GetImageService().GetImage(request, item);
        }

        public void Post(PostChannelImage request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = pathInfo.GetArgumentValue<string>(2);

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(4), true);

            var item = _liveTv.GetInternalChannel(id);

            var task = GetImageService().PostImage(item, request.RequestStream, request.Type, Request.ContentType);

            Task.WaitAll(task);
        }

        public void Delete(DeleteChannelImage request)
        {
            var item = _liveTv.GetInternalChannel(request.Id);

            var task = item.DeleteImage(request.Type, request.Index);

            Task.WaitAll(task);
        }

        private ImageService GetImageService()
        {
            return new ImageService(_userManager, _libraryManager, _appPaths, _providerManager, _itemRepo, _dtoService,
                _imageProcessor);
        }
    }
}
