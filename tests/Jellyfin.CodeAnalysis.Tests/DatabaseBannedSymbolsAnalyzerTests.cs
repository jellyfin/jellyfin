using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Jellyfin.CodeAnalysis.Tests;

public class DatabaseBannedSymbolsAnalyzerTests
{
    // Stand-ins for the EF Core members, with their real names, so that the ids below are those of the real list.
    private const string EfCore =
        """
        namespace Microsoft.EntityFrameworkCore.Infrastructure
        {
            public class DatabaseFacade
            {
                public string? ProviderName => null;
            }
        }

        namespace Microsoft.EntityFrameworkCore
        {
            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public static class RelationalDatabaseFacadeExtensions
            {
                public static int ExecuteSqlRaw(this DatabaseFacade databaseFacade, string sql, params object[] parameters) => 0;

                public static int ExecuteSqlRaw(this DatabaseFacade databaseFacade, string sql, IEnumerable<object> parameters) => 0;

                public static IQueryable<TResult> SqlQueryRaw<TResult>(this DatabaseFacade databaseFacade, string sql, params object[] parameters) => Enumerable.Empty<TResult>().AsQueryable();
            }
        }

        namespace Jellyfin.Stubs
        {
            public class Outer
            {
                public class Inner<T>
                {
                    public void Run()
                    {
                    }
                }
            }
        }
        """;

    private const string List =
        """
        // Raw SQL and checks for one database engine.

        M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,System.String,System.Object[]);Raw SQL is written for one engine
        M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SqlQueryRaw``1(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,System.String,System.Object[]);Raw SQL is written for one engine
        P:Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.ProviderName
        """;

    private static readonly (string Path, string Text)[] _list = [("/repo/" + DatabaseBannedSymbolsAnalyzer.FileName, List)];

    [Fact]
    public void Descriptor_IsAnErrorWithAnIdOfItsOwn()
    {
        var descriptor = DatabaseBannedSymbolsAnalyzer.DatabaseSpecificSymbol;

        Assert.Equal("JF0002", descriptor.Id);
        Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public async Task ExtensionCall_OfABannedMethod_IsReportedAtItsNameWithTheMessageOfTheList()
    {
        var diagnostic = Assert.Single(await RunAsync(
            """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database) => database.ExecuteSqlRaw("DELETE FROM x");
            }
            """));

        Assert.Equal("JF0002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.False(diagnostic.IsSuppressed);
        Assert.Equal("ExecuteSqlRaw", SpanText(diagnostic));
        Assert.EndsWith("is banned in shared production code: Raw SQL is written for one engine", diagnostic.GetMessage(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaticCall_OfABannedMethod_IsReported()
    {
        var diagnostic = Assert.Single(await RunAsync(
            """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database) => RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(database, "DELETE FROM x");
            }
            """));

        Assert.Equal("ExecuteSqlRaw", SpanText(diagnostic));
    }

    [Fact]
    public async Task Call_OfAnOverloadThatIsNotListed_IsNotReported()
    {
        Assert.Empty(await RunAsync(
            """
            using System.Collections.Generic;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database) => database.ExecuteSqlRaw("DELETE FROM x", new List<object>());
            }
            """));
    }

    [Fact]
    public async Task Call_OfABannedGenericMethod_IsReported()
    {
        var diagnostic = Assert.Single(await RunAsync(
            """
            using System.Linq;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public IQueryable<int> Run(DatabaseFacade database) => database.SqlQueryRaw<int>("SELECT 1");
            }
            """));

        Assert.Equal("SqlQueryRaw<int>", SpanText(diagnostic));
    }

    [Fact]
    public async Task MethodGroup_OfABannedMethod_IsReported()
    {
        Assert.Single(await RunAsync(
            """
            using System;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public Func<string, object[], int> Run(DatabaseFacade database) => database.ExecuteSqlRaw;
            }
            """));
    }

    [Fact]
    public async Task BannedProperty_InsideAnExpressionTree_IsReportedWithoutAMessage()
    {
        var diagnostic = Assert.Single(await RunAsync(
            """
            using System;
            using System.Linq.Expressions;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public Expression<Func<DatabaseFacade, string?>> Run() => database => database.ProviderName;
            }
            """));

        Assert.Equal("ProviderName", SpanText(diagnostic));
        Assert.EndsWith("is banned in shared production code", diagnostic.GetMessage(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task BannedProperty_InGeneratedCode_IsReported()
    {
        Assert.Single(await RunAsync(
            """
            // <auto-generated />
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public string? Run(DatabaseFacade database) => database.ProviderName;
            }
            """));
    }

    [Fact]
    public async Task FileWideDisableOfTheBannedApiRule_DoesNotSilenceIt()
    {
        var diagnostic = Assert.Single(await RunAsync(
            """
            #pragma warning disable RS0030 // Do not use banned APIs
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database) => database.ExecuteSqlRaw("DELETE FROM x");
            }
            """));

        Assert.False(diagnostic.IsSuppressed);
    }

    [Fact]
    public async Task ScopedDisableOfItsId_SilencesOnlyTheCallsItCovers()
    {
        var diagnostics = await RunAsync(
            """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database)
                {
            #pragma warning disable JF0002 // The reason
                    var first = database.ExecuteSqlRaw("DELETE FROM x");
            #pragma warning restore JF0002
                    return first + database.ExecuteSqlRaw("DELETE FROM y");
                }
            }
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.Single(diagnostics, diagnostic => diagnostic.IsSuppressed && diagnostic.Location.GetLineSpan().StartLinePosition.Line == 8);
        Assert.Single(diagnostics, diagnostic => !diagnostic.IsSuppressed && diagnostic.Location.GetLineSpan().StartLinePosition.Line == 10);
    }

    [Fact]
    public async Task Project_WithoutTheList_IsNotChecked()
    {
        Assert.Empty(await RunAsync(
            """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public int Run(DatabaseFacade database) => database.ExecuteSqlRaw("DELETE FROM x");
            }
            """,
            [("/repo/BannedSymbols.txt", List), ("/repo/BannedSymbols.Database.txt", List)]));
    }

    [Fact]
    public async Task Nameof_OfABannedMember_IsReported()
    {
        Assert.Single(await RunAsync(
            """
            using Microsoft.EntityFrameworkCore.Infrastructure;

            public class Code
            {
                public string Run() => nameof(DatabaseFacade.ProviderName);
            }
            """));
    }

    [Fact]
    public async Task Entry_ThatNamesNoMemberOfATypeTheCompilationHas_IsReportedAtItsLineOfTheList()
    {
        const string Path = "/repo/" + DatabaseBannedSymbolsAnalyzer.FileName;
        var diagnostics = await RunAsync(
            "public class Code { }",
            [(Path,
                """
                // Misspelt entries.
                M:Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade,System.String);No such overload
                P:Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.ProviderNam
                """)]);

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal("JF0003", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(Path, diagnostic.Location.GetLineSpan().Path);
        });
        Assert.Equal([1, 2], diagnostics.Select(diagnostic => diagnostic.Location.GetLineSpan().StartLinePosition.Line).Order());
        Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture).EndsWith(
            "names no member of 'Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade', so it bans nothing",
            System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task Entry_OfANestedGenericType_IsMatchedToThatType()
    {
        var diagnostics = await RunAsync(
            """
            public class Code
            {
                public void Run() => new Jellyfin.Stubs.Outer.Inner<int>().Run();
            }
            """,
            [("/repo/" + DatabaseBannedSymbolsAnalyzer.FileName,
                """
                M:Jellyfin.Stubs.Outer.Inner`1.Run
                M:Jellyfin.Stubs.Outer.Inner`1.Rnu
                """)]);

        Assert.Equal(2, diagnostics.Length);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "JF0002" && SpanText(diagnostic) == "Run");
        var unknown = Assert.Single(diagnostics, diagnostic => diagnostic.Id == "JF0003");
        Assert.EndsWith("names no member of 'Jellyfin.Stubs.Outer.Inner`1', so it bans nothing", unknown.GetMessage(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task Entry_OfATypeTheCompilationLacks_IsNotReported()
    {
        Assert.Empty(await RunAsync(
            "public class Code { }",
            [("/repo/" + DatabaseBannedSymbolsAnalyzer.FileName,
                """
                M:Microsoft.EntityFrameworkCore.SqlServerDatabaseFacadeExtensions.IsSqlServer(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade)
                M:Microsoft.EntityFrameworkCore.Absent`1.Run``1(System.String)
                P:Microsoft.EntityFrameworkCore.Absent.Outer.Value
                """)]));
    }

    [Fact]
    public async Task List_WhoseEntriesAllNameMembers_ReportsNothingByItself()
    {
        Assert.Empty(await RunAsync("public class Code { }"));
    }

    private static string SpanText(Diagnostic diagnostic)
        => diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan);

    private static Task<ImmutableArray<Diagnostic>> RunAsync(string source, (string Path, string Text)[]? additionalFiles = null)
        => AnalyzerRunner.RunAsync(new DatabaseBannedSymbolsAnalyzer(), [EfCore, source], additionalFiles ?? _list, TestContext.Current.CancellationToken);
}
