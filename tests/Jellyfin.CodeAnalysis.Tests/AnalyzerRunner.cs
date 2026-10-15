using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Jellyfin.CodeAnalysis.Tests;

/// <summary>
/// Compiles sources against the assemblies of the runtime and runs one analyzer on them.
/// </summary>
internal static class AnalyzerRunner
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> _runtime = new(() => References(onlyTheRuntime: true));

    private static readonly Lazy<ImmutableArray<MetadataReference>> _everyAssembly = new(() => References(onlyTheRuntime: false));

    /// <summary>
    /// Runs the analyzer on the sources, with the additional files given, and returns its diagnostics, suppressed ones
    /// included.
    /// </summary>
    /// <param name="analyzer">The analyzer.</param>
    /// <param name="sources">The sources, each compiled as a file of its own.</param>
    /// <param name="additionalFiles">The additional files, by path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The diagnostics of the analyzer.</returns>
    public static Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        string[] sources,
        (string Path, string Text)[] additionalFiles,
        CancellationToken cancellationToken)
        => RunAsync(analyzer, CreateCompilation(sources, everyAssembly: false, cancellationToken), additionalFiles, cancellationToken);

    /// <summary>
    /// Compiles the sources, and fails if they do not compile.
    /// </summary>
    /// <param name="sources">The sources, each compiled as a file of its own.</param>
    /// <param name="everyAssembly">Whether to reference every assembly of this test project, rather than only the runtime.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compilation.</returns>
    public static Compilation CreateCompilation(string[] sources, bool everyAssembly, CancellationToken cancellationToken)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, $"Source{index}.cs", cancellationToken: cancellationToken));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            trees,
            everyAssembly ? _everyAssembly.Value : _runtime.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        return compilation;
    }

    /// <summary>
    /// Runs the analyzer on the compilation, with the additional files given, and returns its diagnostics, suppressed
    /// ones included.
    /// </summary>
    /// <param name="analyzer">The analyzer.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="additionalFiles">The additional files, by path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The diagnostics of the analyzer.</returns>
    public static async Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        Compilation compilation,
        (string Path, string Text)[] additionalFiles,
        CancellationToken cancellationToken)
    {
        var options = new AnalyzerOptions(additionalFiles.Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Text)).ToImmutableArray());
        var withAnalyzers = compilation.WithAnalyzers(
            [analyzer],
            new CompilationWithAnalyzersOptions(options, onAnalyzerException: null, concurrentAnalysis: false, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: true));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ImmutableArray<MetadataReference> References(bool onlyTheRuntime)
    {
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0 && (!onlyTheRuntime || string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.Ordinal)))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = SourceText.From(text, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
