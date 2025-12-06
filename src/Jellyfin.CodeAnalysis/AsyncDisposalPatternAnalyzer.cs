using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Jellyfin.CodeAnalysis;

/// <summary>
/// Analyzer to detect ConfigureAwait(false) usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncDisposalPatternAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for ConfigureAwait(false) usage.
    /// </summary>
    public static readonly DiagnosticDescriptor ConfigureAwaitFalseRule = new(
        id: "JF0001",
        title: "Avoid ConfigureAwait(false)",
        messageFormat: "Avoid using ConfigureAwait(false) here since it is configured as default",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ConfigureAwait(false) should be omited as it is configured as false by default.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [ConfigureAwaitFalseRule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // We only care about invocations, e.g. something.ConfigureAwait(false)
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // We want something like: <expression>.ConfigureAwait(false)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Method name must be "ConfigureAwait"
        if (!string.Equals(memberAccess.Name.Identifier.Text, "ConfigureAwait", StringComparison.Ordinal))
        {
            return;
        }

        // Must have exactly one argument
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 1)
        {
            return;
        }

        // Argument must be the literal 'false'
        if (arguments[0].Expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            return;
        }

        // At this point we have .ConfigureAwait(false) – report a warning.
        var diagnostic = Diagnostic.Create(
            ConfigureAwaitFalseRule,
            invocation.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}
