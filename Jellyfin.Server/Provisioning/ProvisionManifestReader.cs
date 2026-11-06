using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;

namespace Jellyfin.Server.Provisioning;

/// <summary>
/// Reads and validates the file passed to <c>--provision-file</c>.
/// </summary>
public static class ProvisionManifestReader
{
    /// <remarks>
    /// Derived from <see cref="JsonDefaults"/> so enums and GUIDs are spelled the way they are in
    /// the API models, then loosened for a file a human maintains: comments, trailing commas and
    /// either casing of the property names are all accepted.
    /// </remarks>
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonDefaults.Options)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Reads the manifest at the given path.
    /// </summary>
    /// <param name="path">Path to the manifest file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="FileNotFoundException">No file exists at <paramref name="path"/>.</exception>
    /// <exception cref="JsonException">The file is not valid JSON or is missing a required member.</exception>
    /// <exception cref="InvalidOperationException">The file parsed but describes an unusable server.</exception>
    public static async Task<ProvisionManifest> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No provision file exists at '{path}'.", path);
        }

        ProvisionManifest? manifest;
        var stream = File.OpenRead(path);
        await using (stream.ConfigureAwait(false))
        {
            manifest = await JsonSerializer.DeserializeAsync<ProvisionManifest>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false);
        }

        if (manifest is null)
        {
            throw new InvalidOperationException($"The provision file '{path}' is empty.");
        }

        Validate(manifest);
        return manifest;
    }

    private static void Validate(ProvisionManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Administrator.Name))
        {
            throw new InvalidOperationException("Administrator.Name must not be empty.");
        }

        if (string.IsNullOrEmpty(manifest.Administrator.Password))
        {
            throw new InvalidOperationException("Administrator.Password must not be empty.");
        }

        for (var i = 0; i < manifest.Libraries.Count; i++)
        {
            var library = manifest.Libraries[i];
            if (string.IsNullOrWhiteSpace(library.Name))
            {
                throw new InvalidOperationException($"Libraries[{i}].Name must not be empty.");
            }

            if (library.Paths.Count == 0)
            {
                throw new InvalidOperationException($"Library '{library.Name}' must declare at least one path.");
            }

            foreach (var libraryPath in library.Paths)
            {
                if (string.IsNullOrWhiteSpace(libraryPath))
                {
                    throw new InvalidOperationException($"Library '{library.Name}' declares an empty path.");
                }
            }
        }
    }
}
