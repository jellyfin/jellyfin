#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Model.Services
{
    public interface IRequiresRequestStream
    {
        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        Stream RequestStream { get; set; }
    }
}
