using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.Configuration;

/// <summary>
/// A writable options implementation that persists changes to a JSON file and triggers
/// an <see cref="IOptionsMonitor{TOptions}"/> reload so that all DI consumers observe
/// the updated value without restarting the server.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
public sealed class WritableOptions<T> : IWritableOptions<T>
    where T : class, new()
{
    private static readonly JsonSerializerOptions _writeOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private readonly IOptionsMonitor<T> _monitor;
    private readonly IConfigurationRoot _configRoot;
    private readonly string _sectionKey;
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="WritableOptions{T}"/> class.
    /// </summary>
    /// <param name="monitor">The options monitor providing the current value.</param>
    /// <param name="configRoot">The configuration root used to trigger a reload after writes.</param>
    /// <param name="sectionKey">The JSON top-level key under which <typeparamref name="T"/> is stored.</param>
    /// <param name="filePath">Full path of the JSON file that backs this options type.</param>
    public WritableOptions(
        IOptionsMonitor<T> monitor,
        IConfigurationRoot configRoot,
        string sectionKey,
        string filePath)
    {
        _monitor = monitor;
        _configRoot = configRoot;
        _sectionKey = sectionKey;
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public T Value => _monitor.CurrentValue;

    /// <inheritdoc/>
    public void Update(Action<T> applyChanges)
    {
        ArgumentNullException.ThrowIfNull(applyChanges);

        // Clone the current value so we don't mutate the shared snapshot.
        var json = JsonSerializer.Serialize(_monitor.CurrentValue);
        var updated = JsonSerializer.Deserialize<T>(json) ?? new T();

        applyChanges(updated);

        WriteToFile(updated);

        // Trigger IOptionsMonitor change notifications.
        _configRoot.Reload();
    }

    /// <summary>
    /// Writes <paramref name="value"/> into the section key of the backing JSON file, preserving
    /// any other top-level keys that may exist in the same file.
    /// </summary>
    /// <param name="value">The options value to persist.</param>
    internal void WriteToFile(T value)
    {
        JsonObject root;
        if (File.Exists(_filePath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(_filePath)) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        root[_sectionKey] = JsonSerializer.SerializeToNode(value);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_filePath, root.ToJsonString(_writeOptions));
    }
}
