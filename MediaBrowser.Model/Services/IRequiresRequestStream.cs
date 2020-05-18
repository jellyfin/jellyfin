#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Model.Services
{
    public interface IRequiresRequestStream
    {
        /// <summary>
        /// Gets or sets the raw Http Request Input Stream.
        /// </summary>
        Stream RequestStream { get; set; }
    }
}
