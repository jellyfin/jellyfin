using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Morestachio;
using Morestachio.Framework.IO.SingleStream;
using Morestachio.Rendering;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Compiles and renders the startup UI Morestachio template.
/// Shared by the live <see cref="SetupServer"/> and the standalone startup UI preview tool so both
/// exercise the exact same template and formatters.
/// </summary>
public sealed class StartupUiRenderer
{
    private readonly IRenderer _renderer;

    private StartupUiRenderer(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Compiles the startup UI template located at <paramref name="templatePath"/>.
    /// </summary>
    /// <param name="templatePath">The full path to the <c>index.mstemplate.html</c> template.</param>
    /// <returns>A ready to use <see cref="StartupUiRenderer"/>.</returns>
    public static async Task<StartupUiRenderer> CreateAsync(string templatePath)
    {
        var fileTemplate = await File.ReadAllTextAsync(templatePath).ConfigureAwait(false);
        var renderer = (await ParserOptionsBuilder.New()
            .WithTemplate(fileTemplate)
            .WithFormatter(
            (Version version, int arg) =>
            {
                // version type does not for some stupid reason implement IFormattable which morestachio relies on for ToString support therefor we need to do it manually.
                return version.ToString(arg);
            },
            "ToString")
            .WithFormatter(
                (StartupLogTopic logEntry, IEnumerable<StartupLogTopic> children) =>
                {
                    if (children.Any())
                    {
                        var maxLevel = logEntry.LogLevel;
                        var stack = new Stack<StartupLogTopic>(children);

                        while (maxLevel != LogLevel.Error && stack.Count > 0 && (logEntry = stack.Pop()) is not null) // error is the highest inherted error level.
                        {
                            maxLevel = maxLevel < logEntry.LogLevel ? logEntry.LogLevel : maxLevel;
                            foreach (var child in logEntry.Children)
                            {
                                stack.Push(child);
                            }
                        }

                        return maxLevel;
                    }

                    return logEntry.LogLevel;
                },
                "FormatLogLevel")
            .WithFormatter(
                (LogLevel logLevel) =>
                {
                    switch (logLevel)
                    {
                        case LogLevel.Trace:
                        case LogLevel.Debug:
                        case LogLevel.None:
                            return "success";
                        case LogLevel.Information:
                            return "info";
                        case LogLevel.Warning:
                            return "warn";
                        case LogLevel.Error:
                            return "danger";
                        case LogLevel.Critical:
                            return "danger-strong";
                    }

                    return string.Empty;
                },
                "ToString")
            .BuildAndParseAsync()
            .ConfigureAwait(false))
            .CreateCompiledRenderer();

        return new StartupUiRenderer(renderer);
    }

    /// <summary>
    /// Renders the template with the provided model into the target stream.
    /// </summary>
    /// <param name="model">The values made available to the template.</param>
    /// <param name="output">The stream the rendered HTML is written to.</param>
    /// <returns>A Task.</returns>
    public Task RenderAsync(IDictionary<string, object> model, Stream output)
    {
        return _renderer.RenderAsync(
            model,
            new ByteCounterStream(output, IODefaults.FileStreamBufferSize, true, _renderer.ParserOptions));
    }
}
