using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Emby.Server.Implementations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <inheritdoc />
[JellyfinMigration("2025-04-20T04:00:00", nameof(RenameEnableGroupingIntoCollections), "E73B777D-CD5C-4E71-957A-B86B3660B7CF", Stage = Stages.JellyfinMigrationStageTypes.PreInitialisation)]
#pragma warning disable CS0618 // Type or member is obsolete
public class RenameEnableGroupingIntoCollections : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<RenameEnableGroupingIntoCollections> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenameEnableGroupingIntoCollections"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public RenameEnableGroupingIntoCollections(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<RenameEnableGroupingIntoCollections>();
    }

    /// <inheritdoc />
    public void Perform()
    {
        string path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "system.xml");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Configuration file not found: {Path}", path);
            return;
        }

        try
        {
            XDocument xmlDocument = XDocument.Load(path);
            var element = xmlDocument.Descendants("EnableGroupingIntoCollections").FirstOrDefault();
            if (element is not null)
            {
                element.Name = "EnableGroupingMoviesIntoCollections";
                _logger.LogInformation("The tag <EnableGroupingIntoCollections> was successfully renamed to <EnableGroupingMoviesIntoCollections>.");
                xmlDocument.Save(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the XML file: {Message}", ex.Message);
        }
    }
}
