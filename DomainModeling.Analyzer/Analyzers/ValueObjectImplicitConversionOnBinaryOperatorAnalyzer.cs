using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Prevents accidental equality/comparison operator usage between unrelated types, where implicit conversions inadvertently make the operation compile.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectImplicitConversionOnBinaryOperatorAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "ComparisonBetweenUnrelatedValueObjects",
		title: "Comparison between unrelated value objects",
		messageFormat: "Possible unintended '{0}' comparison between unrelated value objects {1} and {2}. Either compare value objects of the same type, implement a dedicated operator overload, or compare underlying values directly.",
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
			SyntaxKind.EqualsExpression,
			SyntaxKind.NotEqualsExpression,
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

		// Not if either operand is typeless (e.g. null)
		if (leftTypeInfo.Type is null || rightTypeInfo.Type is null)
			return;

		// If either operand was implicitly converted FROM some IValueObject to something else, then the comparison is ill-advised
		if (OperandWasImplicitlyConvertedFromSomeIValueObject(leftTypeInfo) || OperandWasImplicitlyConvertedFromSomeIValueObject(rightTypeInfo))
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptor,
				context.Node.GetLocation(),
				binaryExpression.OperatorToken.ValueText,
				leftTypeInfo.Type.IsNullable(out var nullableUnderlyingType) ? nullableUnderlyingType.Name + '?' : leftTypeInfo.Type.Name,
				rightTypeInfo.Type.IsNullable(out nullableUnderlyingType) ? nullableUnderlyingType.Name + '?' : rightTypeInfo.Type.Name);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool OperandWasImplicitlyConvertedFromSomeIValueObject(TypeInfo operandTypeInfo)
	{
		var from = operandTypeInfo.Type;
		var to = operandTypeInfo.ConvertedType;

		// If no type available or no implicit conversion took place, return false
		if (from is null || from.Equals(to, SymbolEqualityComparer.Default))
			return false;

		// Do not flag nullable lifting (where a nullable and a non-nullable are compared)
		// Note that it LOOKS as though the nullable is converted to non-nullable, but the opposite is true
		if (to.IsNullable(out var nullableUnderlyingType) && nullableUnderlyingType.Equals(from, SymbolEqualityComparer.Default))
			return false;

		// Dig through nullables
		if (from.IsNullable(out nullableUnderlyingType))
			from = nullableUnderlyingType;
		if (to.IsNullable(out nullableUnderlyingType))
			to = nullableUnderlyingType;

		// Backwards compatibility: If converting to ValueObject, then ignore, because the ValueObject base class implements ==(ValueObject, ValueObject)
		if (to is { Name: "ValueObject", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } })
			return false;

		var isConvertedFromSomeIValueObject = from.AllInterfaces.Any(interf =>
			interf is { Name: "IValueObject", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } });

		return isConvertedFromSomeIValueObject;
	}
}
