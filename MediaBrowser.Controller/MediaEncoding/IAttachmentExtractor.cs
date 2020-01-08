using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IAttachmentExtractor
    {
        Task<(MediaAttachment attachment, Stream stream)> GetAttachment(
            BaseItem item,
            string mediaSourceId,
            int attachmentStreamIndex,
            CancellationToken cancellationToken);
    }
}
