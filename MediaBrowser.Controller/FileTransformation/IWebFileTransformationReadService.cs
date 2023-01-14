using System.IO;

namespace MediaBrowser.Controller.FileTransformation;

/// <summary>
/// Provides access to the Transformations setup by <see cref="IWebFileTransformationWriteService"/>.
/// </summary>
public interface IWebFileTransformationReadService
{
    /// <summary>
    /// Checks if a given paths file needs transformation.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the file needs transformation, otherwise false.</returns>
    bool NeedsTransformation(string path);

    /// <summary>
    /// Runs the Transformation pipeline on the Stream.
    /// </summary>
    /// <param name="path">The origin path where the file originates from.</param>
    /// <param name="stream">The writable stream of that files contents.</param>
    void RunTransformation(string path, Stream stream);
}
