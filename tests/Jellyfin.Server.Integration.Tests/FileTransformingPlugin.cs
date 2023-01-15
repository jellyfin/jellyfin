using System;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.FileTransformation;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Server.Integration.Tests;

#pragma warning disable CS1591
public class FileTransformingPlugin : BasePlugin<BasePluginConfiguration>
{
    private readonly IWebFileTransformationWriteService _fileTransformationWriteService;

    public FileTransformingPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        IWebFileTransformationWriteService fileTransformationWriteService) : base(applicationPaths, xmlSerializer)
    {
        _fileTransformationWriteService = fileTransformationWriteService;
        _fileTransformationWriteService.AddTransformation("index.html", TransformIndexHtml);
    }

    public override string Name => "File Transformation Plugin test.";

    public override Guid Id => new Guid("649C546C-D66F-458D-80FF-0FE5FEFEEDA8");

    private void TransformIndexHtml(string path, Stream contents)
    {
        using var textReader = new StreamReader(contents, null, true, -1, true);
        var text = textReader.ReadToEnd();
        var regex = Regex.Replace(text, "(<html>)", "<<$1>>");
        contents.Seek(0, SeekOrigin.Begin);

        using var textWriter = new StreamWriter(contents, null, -1, true);
        textWriter.Write(regex);
    }
}
