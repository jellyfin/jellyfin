using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class PeopleValidator.
/// </summary>
public class PeopleValidator
{
    /// <summary>
    /// The _library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// The _logger.
    /// </summary>
    private readonly ILogger _logger;

    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleValidator" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">The file system.</param>
    public PeopleValidator(ILibraryManager libraryManager, ILogger logger, IFileSystem fileSystem)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Validates the people.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="progress">The progress.</param>
    /// <returns>Task.</returns>
    public async Task ValidatePeople(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var people = _libraryManager.GetPeopleNames(new InternalPeopleQuery());

        var numComplete = 0;

        var numPeople = people.Count;

        IProgress<double> subProgress = new Progress<double>((val) => progress.Report(val / 2));

        _logger.LogDebug("Will refresh {Amount} people", numPeople);

        foreach (var person in people)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = _libraryManager.GetPerson(person);
                if (item is null)
                {
                    _logger.LogWarning("Failed to get person: {Name}", person);
                    continue;
                }

                var options = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ImageRefreshMode = MetadataRefreshMode.ValidationOnly,
                    MetadataRefreshMode = MetadataRefreshMode.ValidationOnly
                };

                await item.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating IBN entry {Person}", person);
            }

            // Update progress
            numComplete++;
            double percent = numComplete;
            percent /= numPeople;

            subProgress.Report(100 * percent);
        }

        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            IsDeadPerson = true,
            IsLocked = false
        });

        subProgress = new Progress<double>((val) => progress.Report((val / 2) + 50));

        var i = 0;
        foreach (var item in deadEntities.Chunk(500))
        {
            _libraryManager.DeleteItemsUnsafeFast(item);
            subProgress.Report(100f / deadEntities.Count * (i++ * 100));
        }

        progress.Report(100);

        _logger.LogInformation("People validation complete");
    }
}
