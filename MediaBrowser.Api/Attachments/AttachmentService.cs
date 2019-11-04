using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

namespace MediaBrowser.Api.Attachments
{
    [Route("/Videos/{Id}/{MediaSourceId}/Attachments/{Index}/{Filename}", "GET", Summary = "Gets specified attachment.")]
    public class GetAttachment
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "MediaSourceId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "Index", Description = "The attachment stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }

        [ApiMember(Name = "Filename", Description = "The attachment filename", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Filename { get; set; }
    }

    public class AttachmentService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IAttachmentExtractor _attachmentExtractor;

        public AttachmentService(ILibraryManager libraryManager, IAttachmentExtractor attachmentExtractor)
        {
            _libraryManager = libraryManager;
            _attachmentExtractor = attachmentExtractor;
        }

        public async Task<object> Get(GetAttachment request)
        {
            var item = (Video)_libraryManager.GetItemById(request.Id);
            var (attachment, attachmentStream) = await GetAttachment(request).ConfigureAwait(false);
            var mime = string.IsNullOrWhiteSpace(attachment.MIMEType) ? "application/octet-stream" : attachment.MIMEType;

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
