using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Social;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Social;
using ServiceStack;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Social
{
    [Route("/Social/Shares/{Id}", "GET", Summary = "Gets a share")]
    [Authenticated]
    public class GetSocialShareInfo : IReturn<SocialShareInfo>
    {
        [ApiMember(Name = "Id", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Social/Shares/Public/{Id}", "GET", Summary = "Gets a share")]
    public class GetPublicSocialShareInfo : IReturn<SocialShareInfo>
    {
        [ApiMember(Name = "Id", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Social/Shares/Public/{Id}/Image", "GET", Summary = "Gets a share")]
    public class GetShareImage
    {
        [ApiMember(Name = "Id", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Social/Shares", "POST", Summary = "Creates a share")]
    [Authenticated]
    public class CreateShare : IReturn<SocialShareInfo>
    {
        [ApiMember(Name = "ItemId", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemId { get; set; }

        [ApiMember(Name = "UserId", Description = "The user id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string UserId { get; set; }
    }

    [Route("/Social/Shares/{Id}", "DELETE", Summary = "Deletes a share")]
    [Authenticated]
    public class DeleteShare : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Social/Shares/Public/{Id}/Item", "GET", Summary = "Gets a share")]
    public class GetSharedLibraryItem
    {
        [ApiMember(Name = "Id", Description = "The id of the item", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    public class SharingService : BaseApiService
    {
        private readonly ISharingManager _sharingManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IDtoService _dtoService;

        public SharingService(ISharingManager sharingManager, IDlnaManager dlnaManager, ILibraryManager libraryManager, IDtoService dtoService)
        {
            _sharingManager = sharingManager;
            _dlnaManager = dlnaManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        public object Get(GetSocialShareInfo request)
        {
            var info = _sharingManager.GetShareInfo(request.Id);

            return ToOptimizedResult(info);
        }

        public object Get(GetSharedLibraryItem request)
        {
            var info = _sharingManager.GetShareInfo(request.Id);

            if (info.ExpirationDate <= DateTime.UtcNow)
            {
                throw new ResourceNotFoundException();
            }

            var item = _libraryManager.GetItemById(info.ItemId);

            var dto = _dtoService.GetBaseItemDto(item, new DtoOptions());

            return ToOptimizedResult(dto);
        }

        public object Get(GetPublicSocialShareInfo request)
        {
            var info = _sharingManager.GetShareInfo(request.Id);

            if (info.ExpirationDate <= DateTime.UtcNow)
            {
                throw new ResourceNotFoundException();
            }

            return ToOptimizedResult(info);
        }

        public async Task<object> Post(CreateShare request)
        {
            var info = await _sharingManager.CreateShare(request.ItemId, request.UserId).ConfigureAwait(false);

            return ToOptimizedResult(info);
        }

        public void Delete(DeleteShare request)
        {
            var task = _sharingManager.DeleteShare(request.Id);
            Task.WaitAll(task);
        }

        public async Task<object> Get(GetShareImage request)
        {
            var share = _sharingManager.GetShareInfo(request.Id);

            if (share == null)
            {
                throw new ResourceNotFoundException();
            }
            if (share.ExpirationDate <= DateTime.UtcNow)
            {
                throw new ResourceNotFoundException();
            }

            var item = _libraryManager.GetItemById(share.ItemId);

            var image = item.GetImageInfo(ImageType.Primary, 0);

            if (image != null)
            {
                if (image.IsLocalFile)
                {
                    return ToStaticFileResult(image.Path);
                }

                try
                {
                    // Don't fail the request over this
                    var updatedImage = await _libraryManager.ConvertImageToLocal(item, image, 0).ConfigureAwait(false);
                    return ToStaticFileResult(updatedImage.Path);
                }
                catch
                {
                    
                }
            }

            // Grab a dlna icon if nothing else is available
            using (var response = _dlnaManager.GetIcon("logo240.jpg"))
            {
                using (var ms = new MemoryStream())
                {
                    response.Stream.CopyTo(ms);

                    ms.Position = 0;
                    var bytes = ms.ToArray();
                    return ResultFactory.GetResult(bytes, "image/" + response.Format.ToString().ToLower());
                }
            }

        }
    }
}
