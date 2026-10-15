using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// The rule JF0002 ignores an entry of DatabaseBannedSymbols.txt that names no symbol, as the banned API analyzer does,
/// so a misspelt entry bans nothing without a word. Every entry has to name a public method or property that exists in
/// an assembly this project loads. Only an entry whose assembly this project does not reference at all is taken from
/// a pinned list instead, which was checked against the documentation file of that package.
/// </summary>
public class DatabaseBannedSymbolsTests
{
    private const string FileName = "DatabaseBannedSymbols.txt";

    private static readonly string[] _assemblies =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.EntityFrameworkCore.Relational",
        "Microsoft.EntityFrameworkCore.Sqlite",
        "Npgsql.EntityFrameworkCore.PostgreSQL",
        "Microting.EntityFrameworkCore.MySql",
        "Microsoft.EntityFrameworkCore.SqlServer"
    ];

    // The entries for the providers of engines a project may not reference, by assembly. They were checked against
    // the documentation files of Microting.EntityFrameworkCore.MySql 10.0.12 and Microsoft.EntityFrameworkCore.SqlServer
    // 10.0.12, and are only taken from here where their assembly does not load.
    private static readonly (string Assembly, string Entry)[] _pinned =
    [
        ("Microting.EntityFrameworkCore.MySql", "M:Microsoft.EntityFrameworkCore.MySqlDatabaseFacadeExtensions.IsMySql(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade)"),
        ("Microting.EntityFrameworkCore.MySql", "M:Microsoft.EntityFrameworkCore.Migrations.MySqlMigrationBuilderExtensions.IsMySql(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder)"),
        ("Microsoft.EntityFrameworkCore.SqlServer", "M:Microsoft.EntityFrameworkCore.SqlServerDatabaseFacadeExtensions.IsSqlServer(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade)"),
        ("Microsoft.EntityFrameworkCore.SqlServer", "M:Microsoft.EntityFrameworkCore.Migrations.SqlServerMigrationBuilderExtensions.IsSqlServer(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder)")
    ];

    [Fact]
    public void EveryEntry_NamesAMemberThatExists()
    {
        var entries = ReadEntries();
        var loaded = _assemblies.Select(name => (Name: name, Assembly: TryLoad(name))).ToList();
        var types = loaded
            .Select(assembly => assembly.Assembly)
            .OfType<Assembly>()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .ToList();
        var members = types
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(DocumentationId)
            .Concat(types
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Select(property => $"P:{TypeId(property.DeclaringType!)}.{property.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        var notLoaded = loaded.Where(assembly => assembly.Assembly is null).Select(assembly => assembly.Name).ToHashSet(StringComparer.Ordinal);
        var pinned = _pinned.Where(entry => notLoaded.Contains(entry.Assembly)).Select(entry => entry.Entry).ToHashSet(StringComparer.Ordinal);

        var unknown = entries.Where(entry => !members.Contains(entry) && !pinned.Contains(entry)).ToList();
        var stale = _pinned.Select(entry => entry.Entry).Where(entry => !entries.Contains(entry)).ToList();

        Assert.NotEmpty(entries);
        Assert.True(unknown.Count == 0, $"{FileName} names no such member:{Environment.NewLine}{string.Join(Environment.NewLine, unknown)}");
        Assert.True(stale.Count == 0, $"{FileName} no longer has:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    [Fact]
    public void DocumentationId_OfAKnownMethod_IsTheOneTheCompilerWrites()
    {
        var method = typeof(Enumerable).GetMethods().Single(m => m.Name == nameof(Enumerable.ToDictionary)
            && m.GetGenericArguments().Length == 3
            && m.GetParameters().Length == 3
            && m.GetParameters()[2].ParameterType.IsGenericType);

        Assert.Equal(
            "M:System.Linq.Enumerable.ToDictionary``3(System.Collections.Generic.IEnumerable{``0},System.Func{``0,``1},System.Func{``0,``2})",
            DocumentationId(method));
    }

    private static List<string> ReadEntries()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, FileName)))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllLines(Path.Combine(directory.FullName, FileName))
            .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            .Select(line => line.Split(';')[0])
            .ToList();
    }

    private static Assembly? TryLoad(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static string DocumentationId(MethodInfo method)
    {
        var arity = method.IsGenericMethod ? $"``{method.GetGenericArguments().Length}" : string.Empty;
        var parameters = method.GetParameters();
        var list = parameters.Length == 0 ? string.Empty : $"({string.Join(",", parameters.Select(parameter => TypeId(parameter.ParameterType)))})";
        return $"M:{TypeId(method.DeclaringType!)}.{method.Name}{arity}{list}";
    }

    private static string TypeId(Type type)
    {
        if (type.IsGenericMethodParameter)
        {
            return $"``{type.GenericParameterPosition}";
        }

        if (type.IsGenericTypeParameter)
        {
            return $"`{type.GenericParameterPosition}";
        }

        if (type.IsArray)
        {
            return $"{TypeId(type.GetElementType()!)}[]";
        }

        if (type.IsByRef)
        {
            return $"{TypeId(type.GetElementType()!)}@";
        }

        var name = type.IsNested ? $"{TypeId(type.DeclaringType!)}.{type.Name}" : $"{type.Namespace}.{type.Name}";
        if (!type.IsGenericType)
        {
            return name;
        }

        var tick = name.LastIndexOf('`');
        var arguments = string.Join(",", type.GetGenericArguments().Select(TypeId));
        return $"{(tick < 0 ? name : name[..tick])}{{{arguments}}}";
    }
}
