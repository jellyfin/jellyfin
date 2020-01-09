using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Attachments
{
    [Route("/Videos/{Id}/{MediaSourceId}/Attachments/{Index}", "GET", Summary = "Gets specified attachment.")]
    public class GetAttachment
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "MediaSourceId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "Index", Description = "The attachment stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }
    }

    public class AttachmentService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IAttachmentExtractor _attachmentExtractor;

        public AttachmentService(
            ILogger<AttachmentService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ILibraryManager libraryManager,
            IAttachmentExtractor attachmentExtractor)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _libraryManager = libraryManager;
            _attachmentExtractor = attachmentExtractor;
        }

        public async Task<object> Get(GetAttachment request)
        {
            var (attachment, attachmentStream) = await GetAttachment(request).ConfigureAwait(false);
            var mime = string.IsNullOrWhiteSpace(attachment.MimeType) ? "application/octet-stream" : attachment.MimeType;

            return ResultFactory.GetResult(Request, attachmentStream, mime);
        }

        private Task<(MediaAttachment, Stream)> GetAttachment(GetAttachment request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            return _attachmentExtractor.GetAttachment(item,
                request.MediaSourceId,
                request.Index,
                CancellationToken.None);
        }
    }
}
