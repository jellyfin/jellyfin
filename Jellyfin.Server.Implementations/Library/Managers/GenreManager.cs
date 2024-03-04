using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Server.Implementations.Library.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Library.Managers;

/// <inheritdoc />
public class GenreManager : IGenreManager
{
    private readonly IDbContextFactory<LibraryDbContext> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenreManager"/> class.
    /// </summary>
    /// <param name="provider">The LibraryDb context.</param>
    public GenreManager(IDbContextFactory<LibraryDbContext> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<Genre> AddGenre(string genreName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(genreName);

        var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
        Genre? genre;
        await using (dbContext.ConfigureAwait(false))
        {
            genre = await dbContext.Genres.FirstOrDefaultAsync(e => e.Name == genreName).ConfigureAwait(false);
            if (genre == null)
            {
                genre = new Genre(genreName);
                await dbContext.Genres.AddAsync(genre).ConfigureAwait(false);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        return genre;
    }
}
