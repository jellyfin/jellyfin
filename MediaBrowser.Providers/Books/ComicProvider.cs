using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Providers.Books;

/// <summary>
/// Comic provider.
/// </summary>
public class ComicProvider : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
{
    private readonly IEnumerable<IComicProvider> _comicProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComicProvider"/> class.
    /// </summary>
    /// <param name="comicProviders">The list of comic providers.</param>
    public ComicProvider(IEnumerable<IComicProvider> comicProviders)
    {
        _comicProviders = comicProviders;
    }

    /// <inheritdoc />
    public string Name => "Comic Provider";

    /// <inheritdoc />
    public async Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        foreach (IComicProvider comicProvider in _comicProviders)
        {
            var metadata = await comicProvider.ReadMetadata(info, directoryService, cancellationToken).ConfigureAwait(false);

            if (metadata.HasMetadata)
            {
                return metadata;
            }
        }

        return new MetadataResult<Book> { HasMetadata = false };
    }

    /// <inheritdoc />
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        foreach (IComicProvider iComicFileProvider in _comicProviders)
        {
            var fileChanged = iComicFileProvider.HasItemChanged(item);

            if (fileChanged)
            {
                return fileChanged;
            }
        }

        return false;
    }
}
