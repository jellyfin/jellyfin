using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Jellyfin.CodeAnalysis;

/// <summary>
/// Analyzer to detect sync disposal of async-created IAsyncDisposable objects.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDisposalPatternAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for sync disposal of async-created IAsyncDisposable objects.
    /// </summary>
    public static readonly DiagnosticDescriptor AsyncDisposableSyncDisposal = new(
        id: "JF0001",
        title: "Async-created IAsyncDisposable objects should use 'await using'",
        messageFormat: "Using 'using' with async-created IAsyncDisposable object '{0}'. Use 'await using' instead to prevent resource leaks.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Objects that implement IAsyncDisposable and are created using 'await' should be disposed using 'await using' to prevent resource leaks.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [AsyncDisposableSyncDisposal];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
    }

    private static void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
    {
        var usingStatement = (UsingStatementSyntax)context.Node;

        // Skip 'await using' statements
        if (usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
        {
            return;
        }

        // Check if there's a variable declaration
        if (usingStatement.Declaration?.Variables is null)
        {
            return;
        }

        foreach (var variable in usingStatement.Declaration.Variables)
        {
            if (variable.Initializer?.Value is AwaitExpressionSyntax awaitExpression)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(awaitExpression);
                var type = typeInfo.Type;

                if (type is not null && ImplementsIAsyncDisposable(type))
                {
                    var diagnostic = Diagnostic.Create(
                        AsyncDisposableSyncDisposal,
                        usingStatement.GetLocation(),
                        type.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool ImplementsIAsyncDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            string.Equals(i.Name, "IAsyncDisposable", StringComparison.Ordinal)
            && string.Equals(i.ContainingNamespace?.ToDisplayString(), "System", StringComparison.Ordinal));
    }
}
