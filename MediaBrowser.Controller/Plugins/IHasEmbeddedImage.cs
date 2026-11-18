namespace MediaBrowser.Controller.Plugins;

/// <summary>
/// Marker interface for integrated/bundled plugins that ship their plugin image as an embedded
/// resource inside the plugin assembly rather than as a file on disk.
/// </summary>
/// <remarks>
/// This interface is intended for plugins compiled into the server. External plugins should
/// continue to declare their image via the <c>imagePath</c> field in <c>meta.json</c>.
/// </remarks>
public interface IHasEmbeddedImage
{
    /// <summary>
    /// Gets the name of the embedded resource in this plugin's assembly to serve as the plugin image.
    /// </summary>
    string ImageResourceName { get; }
}
