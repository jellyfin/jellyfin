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
        var numLinked = LinkUnresolvedCredits(cancellationToken);

        // Person kinds only: an Artist credit belongs to a MusicArtist, refreshed by the music library.
        var people = _libraryManager.GetPeopleNames(new InternalPeopleQuery(
            [],
            [nameof(PersonKind.Artist), nameof(PersonKind.AlbumArtist)]));

        var numComplete = 0;
        var numCreated = 0;

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
                    // A credited name without an item is invisible everywhere, so create it here.
                    item = _libraryManager.GetOrCreatePerson(person);
                    if (item is null)
                    {
                        _logger.LogWarning("Failed to get or create person: {Name}", person);
                        continue;
                    }

                    numCreated++;
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
            _libraryManager.DeleteItemsUnsafeFast(item, true);
            subProgress.Report(100f / deadEntities.Count * (i++ * 100));
        }

        progress.Report(100);

        _logger.LogInformation("People validation complete, created {Created} missing people and linked {Linked} credits", numCreated, numLinked);
    }

    // Every kind, not just the Person ones refreshed below: a credit with no item is invisible
    // everywhere, and this is the only repair that does not need a full rescan.
    private int LinkUnresolvedCredits(CancellationToken cancellationToken)
    {
        var unlinked = _libraryManager.GetUnlinkedCredits();
        if (unlinked.Count == 0)
        {
            return 0;
        }

        _logger.LogDebug("Found {Amount} credits with no item", unlinked.Count);

        var numLinked = 0;
        foreach (var credit in unlinked)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = _libraryManager.GetOrCreateCreditItem(credit.Name, credit.Type);
                if (item is null)
                {
                    _logger.LogWarning("Failed to get or create the item for {Kind} credit {Name}", credit.Type, credit.Name);
                    continue;
                }

                numLinked += _libraryManager.LinkCreditsToItem(credit.Name, credit.Type, item.Id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking {Kind} credit {Name}", credit.Type, credit.Name);
            }
        }

        return numLinked;
    }
}
