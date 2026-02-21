using System;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Entities;

/// <summary>
/// Event arguments for when library options are updated.
/// </summary>
public class LibraryOptionsUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryOptionsUpdatedEventArgs"/> class.
    /// </summary>
    /// <param name="libraryPath">The path of the library whose options were updated.</param>
    /// <param name="libraryOptions">The updated library options.</param>
    public LibraryOptionsUpdatedEventArgs(string libraryPath, LibraryOptions libraryOptions)
    {
        LibraryPath = libraryPath;
        LibraryOptions = libraryOptions;
    }

    /// <summary>
    /// Gets the path of the library whose options were updated.
    /// </summary>
    public string LibraryPath { get; }

    /// <summary>
    /// Gets the updated library options.
    /// </summary>
    public LibraryOptions LibraryOptions { get; }
}
