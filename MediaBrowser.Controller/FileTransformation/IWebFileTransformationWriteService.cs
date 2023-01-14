using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.FileTransformation;

/// <summary>
/// Provides a method for transforming a file.
/// </summary>
/// <param name="path">The origin file path.</param>
/// <param name="contents">The contents of the file.</param>
public delegate void TransformFile(string path, Stream contents);

/// <summary>
/// Provides Plugins with the capability to transform Html, Javascript and Css files.
/// </summary>
public interface IWebFileTransformationWriteService
{
    /// <summary>
    /// Adds a new Transformation to the Pipeline.
    /// </summary>
    /// <param name="path">The requested file path.</param>
    /// <param name="transformation">The callback that will be invoked when the file is requested.</param>
    void AddTransformation(string path, TransformFile transformation);
}
