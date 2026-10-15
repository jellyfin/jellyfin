using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// The rule JF0002 reports the symbols of DatabaseBannedSymbols.txt in shared production code. It may only be
/// suppressed around the statements that need it, with a pragma that gives the reason and is restored in the same file.
/// The wiring of the rule, the custom analyzers and the list, lives in the root Directory.Build.props only, where a
/// Debug build also checks that every project the ban applies to is compiled with both. A disable of every warning
/// switches the rule off as well, so production code may not add one. The workflows and response files of the
/// repository may not switch the rule off either, and the ABI job, whose Debug build checks the rule in the CI, has to
/// stay a Debug build.
/// </summary>
public partial class DatabaseBanSuppressionTests
{
    private const string Id = "JF0002";

    private const string ListFileName = "DatabaseBannedSymbols.txt";

    private const string RootProps = "Directory.Build.props";

    private const string AbiWorkflow = ".github/workflows/ci-compat.yml";

    // JF0003 reports an entry of the list that bans nothing.
    private static readonly string[] _rules = [Id, "JF0003"];

    private static readonly string[] _configurationExtensions = [".editorconfig", ".globalconfig", ".ruleset", ".props", ".targets", ".csproj", ".proj", ".rsp"];

    // What only the root Directory.Build.props may name: the rules, the list, the check that the ban is in effect, and
    // settings that switch the rule off without naming it (every analyzer, every rule of its category, or the import
    // of Directory.Build.props itself).
    private static readonly string[] _wiring =
    [
        Id,
        "JF0003",
        "DatabaseBannedSymbols",
        "CheckDatabaseBanIsInEffect",
        "RunAnalyzers",
        "ImportDirectoryBuildProps",
        "dotnet_analyzer_diagnostic.severity",
        "dotnet_analyzer_diagnostic.category-Portability"
    ];

    // These disable every warning to the end of the file, for classes that only read old configuration files before the
    // server starts. They predate the rule; no other production file may add such a disable.
    private static readonly string[] _disablesOfEveryWarning =
    [
        "Jellyfin.Server/Migrations/PreStartupRoutines/CreateNetworkConfiguration.cs",
        "Jellyfin.Server/Migrations/PreStartupRoutines/MigrateEncodingOptions.cs",
        "Jellyfin.Server/Migrations/PreStartupRoutines/MigrateMusicBrainzTimeout.cs",
        "Jellyfin.Server/Migrations/PreStartupRoutines/MigrateNetworkConfiguration.cs"
    ];

    [Fact]
    public void ProductionCode_OnlyDisablesTheRuleWithAScopedPragmaThatGivesAReason()
    {
        var root = RepositoryRoot();
        var problems = new List<string>();
        var disablesOfEveryWarning = new List<string>();
        foreach (var file in Files(root, ProductionSource))
        {
            var lines = File.ReadAllLines(file);
            int? open = null;
            for (var i = 0; i < lines.Length; i++)
            {
                var where = $"{RepositoryPath(root, file)}:{i + 1}";
                var match = PragmaWarning().Match(lines[i]);
                if (!match.Success)
                {
                    // A suppression attribute, or a constant that one could use.
                    problems.AddRange(_rules.Where(rule => lines[i].Contains(rule, StringComparison.OrdinalIgnoreCase)).Select(rule => $"{where}: names {rule} outside a #pragma warning line"));
                    continue;
                }

                var restore = string.Equals(match.Groups["action"].Value, "restore", StringComparison.Ordinal);
                var ids = match.Groups["ids"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (ids.Length == 0)
                {
                    if (restore)
                    {
                        open = null;
                    }
                    else
                    {
                        disablesOfEveryWarning.Add(RepositoryPath(root, file));
                    }

                    continue;
                }

                if (!ids.Contains(Id, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (restore)
                {
                    open = null;
                    continue;
                }

                if (open is not null)
                {
                    problems.Add($"{where}: disables {Id} again before it is restored");
                }

                if (string.IsNullOrWhiteSpace(match.Groups["reason"].Value))
                {
                    problems.Add($"{where}: disables {Id} without a reason");
                }

                open = i;
            }

            if (open is not null)
            {
                problems.Add($"{RepositoryPath(root, file)}:{open + 1}: disables {Id} without restoring it");
            }
        }

        problems.AddRange(disablesOfEveryWarning.GroupBy(file => file).Where(group => group.Count() > 1 || !_disablesOfEveryWarning.Contains(group.Key)).Select(group => $"{group.Key}: disables every warning, {Id} included"));
        problems.AddRange(_disablesOfEveryWarning.Except(disablesOfEveryWarning).Select(file => $"{file}: no longer disables every warning; remove it from the list of this test"));
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void FilesThatDisableEveryWarning_UseNoBannedMember()
    {
        var root = RepositoryRoot();
        var names = File.ReadAllLines(Path.Combine(root, ListFileName))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            .Select(MemberName)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var problems = _disablesOfEveryWarning
            .SelectMany(file => names
                .Where(name => File.ReadAllText(Path.Combine(root, file)).Contains(name, StringComparison.Ordinal))
                .Select(name => $"{file}: names {name}, which its disable of every warning would hide from {Id}"))
            .ToList();

        Assert.NotEmpty(names);
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void OnlyTheRootDirectoryBuildProps_WiresTheRule()
    {
        var root = RepositoryRoot();
        var problems = new List<string>();
        foreach (var file in Files(root, path => _configurationExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))))
        {
            var path = RepositoryPath(root, file);
            var text = File.ReadAllText(file);
            if (!string.Equals(path, RootProps, StringComparison.Ordinal))
            {
                problems.AddRange(_wiring.Where(name => text.Contains(name, StringComparison.OrdinalIgnoreCase)).Select(name => $"{path}: names {name}"));
            }

            // The custom analyzers are referenced by the root Directory.Build.props, and by their own tests.
            var analyzerTests = path.StartsWith("tests/Jellyfin.CodeAnalysis.Tests/", StringComparison.Ordinal) && path.EndsWith(".csproj", StringComparison.Ordinal);
            if (!string.Equals(path, RootProps, StringComparison.Ordinal) && !analyzerTests && text.Contains("Jellyfin.CodeAnalysis.csproj", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"{path}: names Jellyfin.CodeAnalysis.csproj");
            }

            // A nested Directory.Build.props or Directory.Build.targets that does not import its parent leaves the root out.
            var name = Path.GetFileName(file);
            if (!string.Equals(path, RootProps, StringComparison.Ordinal)
                && (string.Equals(name, "Directory.Build.props", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Directory.Build.targets", StringComparison.OrdinalIgnoreCase))
                && !Regex.IsMatch(text, @"GetPathOfFileAbove\(\s*'" + Regex.Escape(name) + "'", RegexOptions.IgnoreCase))
            {
                problems.Add($"{path}: does not import the {name} above it");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void WorkflowsAndResponseFiles_DoNotSwitchTheRuleOff()
    {
        var root = RepositoryRoot();
        var workflows = Path.Combine(root, ".github", "workflows");
        var files = Directory.EnumerateFiles(workflows)
            .Where(file => file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Concat(Files(root, path => path.EndsWith(".rsp", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // A property set on the command line, as -p:, /p: or --property:, names the property, so the names cover those too.
        var problems = files
            .Select(file => (Path: RepositoryPath(root, file), Text: File.ReadAllText(file)))
            .SelectMany(file => new[] { "ImportDirectoryBuildProps", "RunAnalyzers" }
                .Where(name => file.Text.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Select(name => $"{file.Path}: names {name}")
                .Concat(SwitchesTheRulesOff().Matches(file.Text).Select(match => $"{file.Path}: {match.Value.Trim()}")))
            .ToList();

        Assert.NotEmpty(files);
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // The Debug build of the ABI job is where the CI checks the rule, so it must stay a Debug build.
    [Fact]
    public void AbiHeadJob_BuildsTheServerInDebug()
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot(), AbiWorkflow));
        var start = Array.FindIndex(lines, line => string.Equals(line.TrimEnd(), "  abi-head:", StringComparison.Ordinal));
        Assert.True(start >= 0, $"{AbiWorkflow} has no job abi-head");
        var end = Array.FindIndex(lines, start + 1, line => JobStart().IsMatch(line));
        var job = lines[(start + 1)..(end < 0 ? lines.Length : end)];

        var builds = job.Where(line => line.Contains("dotnet build", StringComparison.Ordinal)).ToList();
        var build = Assert.Single(builds);
        Assert.Contains("dotnet build Jellyfin.Server", build, StringComparison.Ordinal);
        Assert.False(BuildArgumentThatChangesTheConfiguration().IsMatch(build), $"{AbiWorkflow}: abi-head builds with {build.Trim()}");
        Assert.DoesNotContain(job, line => line.Contains("Configuration", StringComparison.OrdinalIgnoreCase));
    }

    // "M:N.T.ExecuteSqlRaw(N.P)" and "M:N.T.SqlQueryRaw``1(N.P)" name ExecuteSqlRaw and SqlQueryRaw.
    private static string MemberName(string entry)
    {
        var id = entry.Split(';')[0];
        var parameters = id.IndexOf('(', StringComparison.Ordinal);
        var name = parameters < 0 ? id : id[..parameters];
        name = name[(name.LastIndexOf('.') + 1)..];
        var arity = name.IndexOf('`', StringComparison.Ordinal);
        return arity < 0 ? name : name[..arity];
    }

    // Production code is what the rule applies to; the analyzer that defines the rule names it too.
    private static bool ProductionSource(string relativePath)
        => relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            && !relativePath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase)
            && !relativePath.StartsWith("src/Jellyfin.CodeAnalysis/", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> Files(string root, Func<string, bool> include)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(child);
                if (!name.StartsWith('.') && name is not "bin" and not "obj" and not "node_modules")
                {
                    pending.Push(child);
                }
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (include(RepositoryPath(root, file)))
                {
                    yield return file;
                }
            }
        }
    }

    private static string RepositoryPath(string root, string file) => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ListFileName)))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    [GeneratedRegex(@"^\s*#\s*pragma\s+warning\s+(?<action>disable|restore)\b(?<ids>[^/]*)(//(?<reason>.*))?$")]
    private static partial Regex PragmaWarning();

    [GeneratedRegex(@"(NoWarn|WarningsNotAsErrors)[^\n]*JF000[23]", RegexOptions.IgnoreCase)]
    private static partial Regex SwitchesTheRulesOff();

    [GeneratedRegex(@"^  \S")]
    private static partial Regex JobStart();

    [GeneratedRegex(@"(^|\s)(-c\b|--configuration\b|-p:|/p:|-property:|--property:|/property:)", RegexOptions.IgnoreCase)]
    private static partial Regex BuildArgumentThatChangesTheConfiguration();
}
