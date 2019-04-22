using System.IO;

namespace Jellyfin.Model.Services
{
    public interface IRequiresRequestStream
    {
        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        Stream RequestStream { get; set; }
    }
}
