using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Prevents the use of comparison operators with nullables, where lifting causes nulls to be handled without being treated as less than any other value.
/// This avoids a counterintuitive and likely unintended result for comparisons between null and non-null.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectLiftingOnComparisonOperatorAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "CounterintuitiveNullHandlingOnLiftedValueObjectComparison",
		title: "Comparisons between null and non-null might produce unintended results",
		messageFormat: "'Lifted' comparisons do not treat null as less than other values, which may lead to unexpected results. Handle nulls explicitly, or use Comparer<T>.Default.Compare() to treat null as smaller than other values.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(
			AnalyzeBinaryExpression,
			SyntaxKind.LessThanExpression,
			SyntaxKind.LessThanOrEqualExpression,
			SyntaxKind.GreaterThanExpression,
			SyntaxKind.GreaterThanOrEqualExpression);
	}

	private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not BinaryExpressionSyntax binaryExpression)
			return;

		var semanticModel = context.SemanticModel;
		var cancellationToken = context.CancellationToken;

		var leftTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Left, cancellationToken);
		var rightTypeInfo = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);

		// If either operand is a nullable of some IValueObject, then the comparison is ill-advised
		if (OperandIsSomeNullableIValueObject(leftTypeInfo) || OperandIsSomeNullableIValueObject(rightTypeInfo))
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptor,
				context.Node.GetLocation());

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool OperandIsSomeNullableIValueObject(TypeInfo operandTypeInfo)
	{
		var isSomeIValueObject = operandTypeInfo.Type is { } type && type.AllInterfaces.Any(interf =>
			interf is { Name: "IValueObject", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } });

		if (!isSomeIValueObject)
			return false;

		// Might be a struct being implicitly converted to a nullable struct
		// Note that, for nullables, it can LOOK as if non-nullables are compared, but that is not the case

		// Might also be any type (even a class) being implicitly converted to a nullable primitive, such as "int?" (which can LOOK like "int")

		var result = operandTypeInfo.ConvertedType.IsNullable(out _);
		return result;
	}
}
