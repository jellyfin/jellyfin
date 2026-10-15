using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Jellyfin.CodeAnalysis.Tests;

/// <summary>
/// The list of the repository, against the real EF Core and provider assemblies, as the build gives them to the rule.
/// </summary>
public class DatabaseBannedSymbolsListTests
{
    [Fact]
    public async Task EveryEntry_ResolvesWhereItsProviderIsReferenced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (path, text) = ListOfTheRepository();
        var entries = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            .Select(line => line.Split(';')[0])
            .ToList();
        var compilation = AnalyzerRunner.CreateCompilation(["public class Code { }"], everyAssembly: true, cancellationToken);

        var unresolved = entries.Where(id => DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation).IsEmpty).ToList();

        // SQL Server is the one provider that this project does not reference.
        Assert.Equal(25, entries.Count);
        Assert.Equal(entries.Where(id => id.Contains(".SqlServer", StringComparison.Ordinal)), unresolved);
        Assert.Equal(2, unresolved.Count);
        Assert.Empty(await AnalyzerRunner.RunAsync(new DatabaseBannedSymbolsAnalyzer(), compilation, [(path, text)], cancellationToken));
    }

    [Fact]
    public async Task UseOfEachKindOfEntry_IsReported()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var compilation = AnalyzerRunner.CreateCompilation(
            [
                """
                using Microsoft.EntityFrameworkCore;
                using Microsoft.EntityFrameworkCore.Infrastructure;
                using Microsoft.EntityFrameworkCore.Migrations;

                public class Entity
                {
                    public int Id { get; set; }
                }

                public class Code
                {
                    public void Run(DatabaseFacade database, DbSet<Entity> set, MigrationBuilder migrationBuilder)
                    {
                        database.ExecuteSqlRaw("DELETE FROM x");
                        set.FromSqlRaw("SELECT 1");
                        database.GetDbConnection();
                        _ = database.ProviderName;
                        _ = migrationBuilder.ActiveProvider;
                        database.IsSqlite();
                        database.IsNpgsql();
                        migrationBuilder.IsMySql();
                    }
                }
                """
            ],
            everyAssembly: true,
            cancellationToken);

        var diagnostics = await AnalyzerRunner.RunAsync(new DatabaseBannedSymbolsAnalyzer(), compilation, [ListOfTheRepository()], cancellationToken);

        Assert.Equal(
            ["ActiveProvider", "ExecuteSqlRaw", "FromSqlRaw", "GetDbConnection", "IsMySql", "IsNpgsql", "IsSqlite", "ProviderName"],
            diagnostics.Select(diagnostic => diagnostic.Location.SourceTree!.GetText(cancellationToken).ToString(diagnostic.Location.SourceSpan)).Order(StringComparer.Ordinal));
        Assert.All(diagnostics, diagnostic => Assert.Equal("JF0002", diagnostic.Id));
    }

    private static (string Path, string Text) ListOfTheRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, DatabaseBannedSymbolsAnalyzer.FileName)))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine(directory.FullName, DatabaseBannedSymbolsAnalyzer.FileName);
        return (path, File.ReadAllText(path));
    }
}
