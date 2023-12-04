using System.Threading.Tasks;
using Jellyfin.Data.Entities.Libraries;

namespace Jellyfin.Server.Implementations.Library.Interfaces;

/// <summary>
/// The Genre Manager.
/// </summary>
public interface IGenreManager
{
    /// <summary>
    /// Add a new genre.
    /// </summary>
    /// <param name="genreName">The genre name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Genre> AddGenre(string genreName);
}
