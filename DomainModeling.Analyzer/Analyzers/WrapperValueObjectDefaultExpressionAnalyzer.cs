using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Prevents the use of default expressions and literals on struct WrapperValueObject types, so that validation cannot be circumvented.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrapperValueObjectDefaultExpressionAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "WrapperValueObjectDefaultExpression",
		title: "Default expression instantiating unvalidated value object",
		messageFormat: "A 'default' expression would create an unvalidated instance of value object {0}. Use a parameterized constructor, or use IsDefault() to merely compare.",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterSyntaxNodeAction(AnalyzeDefaultExpressionOrLiteral,
			SyntaxKind.DefaultExpression,
			SyntaxKind.DefaultLiteralExpression);
	}

	private static void AnalyzeDefaultExpressionOrLiteral(SyntaxNodeAnalysisContext context)
	{
		var defaultExpressionOrLiteral = (ExpressionSyntax)context.Node;

		var typeInfo = context.SemanticModel.GetTypeInfo(defaultExpressionOrLiteral, context.CancellationToken);

		if (typeInfo.Type is not { } type)
			return;

		// Only for structs
		if (!type.IsValueType)
			return;

		// Only with WrapperValueObjectAttribute<TValue>
		if (!type.GetAttributes().Any(attr =>
			attr.AttributeClass is { Arity: 1, Name: "WrapperValueObjectAttribute", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } }, }))
			return;

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptor,
			context.Node.GetLocation(),
			type.Name);

		context.ReportDiagnostic(diagnostic);
	}
}
