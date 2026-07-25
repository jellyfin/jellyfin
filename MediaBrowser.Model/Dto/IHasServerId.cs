#nullable disable

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Interface for DTOs that reference the id of the server they originate from.
    /// </summary>
    public interface IHasServerId
    {
        /// <summary>
        /// Gets the server id.
        /// </summary>
        string ServerId { get; }
    }
}
