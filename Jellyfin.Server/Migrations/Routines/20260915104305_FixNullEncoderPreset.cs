using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Server.Migrations.Stages;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Replaces the nullable encoder preset written by older versions with the default preset.
/// </summary>
[JellyfinMigration("2026-09-15T10:43:05", nameof(FixNullEncoderPreset), Stage = JellyfinMigrationStageTypes.PreInitialisation)]
internal class FixNullEncoderPreset : IAsyncMigrationRoutine
{
    private static readonly XNamespace _xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<FixNullEncoderPreset> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixNullEncoderPreset"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public FixNullEncoderPreset(IApplicationPaths applicationPaths, ILogger<FixNullEncoderPreset> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "encoding.xml");
        if (!File.Exists(path))
        {
            return Task.CompletedTask;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            _logger.LogWarning(ex, "Could not parse encoding configuration; skipping null encoder preset migration");
            return Task.CompletedTask;
        }

        var encoderPreset = document.Root?.Element(nameof(EncodingOptions.EncoderPreset));
        if (encoderPreset is null)
        {
            return Task.CompletedTask;
        }

        var nilAttribute = encoderPreset.Attribute(_xsiNamespace + "nil");
        if (nilAttribute is null || !string.Equals(nilAttribute.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        nilAttribute.Remove();
        encoderPreset.Value = nameof(EncoderPreset.auto);
        document.Save(path, SaveOptions.DisableFormatting);
        _logger.LogInformation("Replaced null encoder preset with auto in encoding configuration");
        return Task.CompletedTask;
    }
}
