using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Jellyfin.CodeAnalysis;

/// <summary>
/// Analyzer to detect raw SQL and checks for one database engine in shared production code.
/// </summary>
/// <remarks>
/// <para>
/// The banned symbols are read from the additional file <see cref="FileName"/>, in the format of the banned API
/// analyzer: one documentation id per line, optionally followed by a semicolon and a message, and comment lines that
/// start with <c>//</c>. A project without that additional file is not checked, so the build decides which projects
/// the ban applies to. An entry whose type the compilation has, but which names no member of it, is reported as JF0003
/// at its line of the file, because it bans nothing.
/// </para>
/// <para>
/// The banned API analyzer reports every file of banned symbols under the one id RS0030, so a disable of RS0030 that is
/// there for another banned API would hide this ban as well. This rule has an id of its own for that reason.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DatabaseBannedSymbolsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The name of the additional file that lists the banned symbols.
    /// </summary>
    public const string FileName = "DatabaseBannedSymbols.txt";

    /// <summary>
    /// Diagnostic descriptor for the use of a symbol that ties shared code to one database engine.
    /// </summary>
    public static readonly DiagnosticDescriptor DatabaseSpecificSymbol = new(
        id: "JF0002",
        title: "Raw SQL and checks for one database engine belong in the database providers",
        messageFormat: "'{0}' is banned in shared production code{1}",
        category: "Portability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Shared production code that runs raw SQL, or asks which database engine it runs on, works on that engine only. Use LINQ, or a member of IJellyfinDatabaseProvider that the providers implement.");

    /// <summary>
    /// Diagnostic descriptor for an entry of the list that names no member of a type the compilation has.
    /// </summary>
    public static readonly DiagnosticDescriptor UnknownMember = new(
        id: "JF0003",
        title: "An entry of the database ban names no member of its type",
        messageFormat: "'{0}' names no member of '{1}', so it bans nothing",
        category: "Portability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An entry of " + FileName + " whose type the compilation has must name a member of that type, with the parameter list of one overload. Otherwise the entry is misspelt, or the member has changed.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DatabaseSpecificSymbol, UnknownMember];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var banned = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        var unknown = new List<Diagnostic>();
        ReadBannedSymbols(context.Options.AdditionalFiles, context.Compilation, banned, unknown, context.CancellationToken);
        if (unknown.Count > 0)
        {
            context.RegisterCompilationEndAction(endContext => unknown.ForEach(endContext.ReportDiagnostic));
        }

        if (banned.Count == 0)
        {
            return;
        }

        context.RegisterOperationAction(
            operationContext => AnalyzeOperation(operationContext, banned),
            OperationKind.Invocation,
            OperationKind.MethodReference,
            OperationKind.PropertyReference,
            OperationKind.FieldReference,
            OperationKind.EventReference,
            OperationKind.ObjectCreation,
            OperationKind.Binary,
            OperationKind.Unary);
    }

    // An entry that resolves to nothing bans nothing. That is expected where the compilation does not have the type of
    // the member, because a project need not reference every provider; where it has the type, the entry is wrong.
    private static void ReadBannedSymbols(
        ImmutableArray<AdditionalText> files,
        Compilation compilation,
        Dictionary<ISymbol, string> banned,
        List<Diagnostic> unknown,
        CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            if (!string.Equals(Path.GetFileName(file.Path), FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = file.GetText(cancellationToken);
            if (text is null)
            {
                continue;
            }

            foreach (var line in text.Lines)
            {
                var entry = line.ToString().Trim();
                if (entry.Length == 0 || entry.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = entry.IndexOf(';');
                var id = separator < 0 ? entry : entry.Substring(0, separator).Trim();
                var message = separator < 0 ? string.Empty : entry.Substring(separator + 1).Trim();
                var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation);
                foreach (var symbol in symbols)
                {
                    banned[symbol] = message;
                }

                if (symbols.IsEmpty
                    && ContainingTypeId(id) is { } typeId
                    && DocumentationCommentId.GetFirstSymbolForDeclarationId(typeId, compilation) is not null)
                {
                    unknown.Add(Diagnostic.Create(
                        UnknownMember,
                        Location.Create(file.Path, line.Span, text.Lines.GetLinePositionSpan(line.Span)),
                        id,
                        typeId.Substring(2)));
                }
            }
        }
    }

    // The id of the type that declares the member an id names: "M:N.T`1.M``1(N.P)" names a member of "T:N.T`1". A
    // nested type is separated from its outer type by a dot, as a type from its namespace, and the lookup resolves both.
    private static string? ContainingTypeId(string id)
    {
        if (id.Length < 3 || id[1] != ':' || "MPFE".IndexOf(id[0]) < 0)
        {
            return null;
        }

        var parameters = id.IndexOf('(');
        var name = parameters < 0 ? id.Substring(2) : id.Substring(2, parameters - 2);
        var dot = name.LastIndexOf('.');
        return dot <= 0 ? null : "T:" + name.Substring(0, dot);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, Dictionary<ISymbol, string> banned)
    {
        ISymbol? symbol = context.Operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IMethodReferenceOperation reference => reference.Method,
            IPropertyReferenceOperation reference => reference.Property,
            IFieldReferenceOperation reference => reference.Field,
            IEventReferenceOperation reference => reference.Event,
            IObjectCreationOperation creation => creation.Constructor,
            IBinaryOperation binary => binary.OperatorMethod,
            IUnaryOperation unary => unary.OperatorMethod,
            _ => null
        };

        if (symbol is null || !TryGetMessage(symbol, banned, out var message))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DatabaseSpecificSymbol,
            GetLocation(context.Operation),
            symbol.ToDisplayString(),
            message.Length == 0 ? string.Empty : ": " + message));
    }

    // A call of an extension method or of a generic method refers to a reduced or constructed form of the method, and a
    // member of a generic type to a member of the constructed type; the file names their definitions.
    private static bool TryGetMessage(ISymbol symbol, Dictionary<ISymbol, string> banned, out string message)
    {
        var current = symbol;
        while (true)
        {
            if (banned.TryGetValue(current, out message!))
            {
                return true;
            }

            var next = current is IMethodSymbol { ReducedFrom: { } reducedFrom } ? reducedFrom : current.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(next, current))
            {
                message = string.Empty;
                return false;
            }

            current = next;
        }
    }

    private static Location GetLocation(IOperation operation)
    {
        return operation.Syntax switch
        {
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } => access.Name.GetLocation(),
            InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding } => binding.Name.GetLocation(),
            MemberAccessExpressionSyntax access => access.Name.GetLocation(),
            MemberBindingExpressionSyntax binding => binding.Name.GetLocation(),
            _ => operation.Syntax.GetLocation()
        };
    }
}
