using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// In [Wrapper]ValueObjects, this analyzer warns against the use of property/field initializers in classes without a default constructor (such as when a primary constructor is used).
/// Without a default constructor, deserialization uses GetUninitializedObject(), which skips property/field initializers, likely to the developer's surprise.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectFieldInitializerWithoutDefaultCtorAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "ValueObjectFieldInitializerWithoutDefaultConstructor",
		title: "ValueObject has field initializers but no default constructor",
		messageFormat: "Field initializer on value object with no default constructor. Lack of a default constructor forces deserialization to use GetUninitializedObject(), which skips field initializers. Consider a calculated property (=> syntax) to avoid field initializers.",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

		context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
	}

	private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
	{
		var field = (FieldDeclarationSyntax)context.Node;
		var semanticModel = context.SemanticModel;

		if (!IsMemberOfRelevantType(field, semanticModel, context.CancellationToken))
			return;

		// A field declaration can actually define multiple fields at once: private int One, Two = 1, 2;
		foreach (var fieldVariable in field.Declaration.Variables)
		{
			if (fieldVariable.Initializer?.Value is not { } initializer)
				continue;

			// When using a primary constructor, then we use field initializers to assign its parameters
			// Such initializers are fine
			// It is only OTHER (non-parameterized) initalizers that are likely to cause confusion
			if (InitializerReferencesAnyConstructorParameter(initializer, semanticModel, context.CancellationToken))
				return;

			context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, fieldVariable.Initializer.GetLocation()));
		}
	}

	private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
	{
		var property = (PropertyDeclarationSyntax)context.Node;
		var semanticModel = context.SemanticModel;

		if (!IsMemberOfRelevantType(property, semanticModel, context.CancellationToken))
			return;

		if (property.Initializer?.Value is not { } initializer)
			return;

		// When using a primary constructor, then we use field initializers to assign its parameters
		// Such initializers are fine
		// It is only OTHER (non-parameterized) initalizers that are likely to cause confusion
		if (InitializerReferencesAnyConstructorParameter(initializer, semanticModel, context.CancellationToken))
			return;

		context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, property.Initializer.GetLocation()));
	}

	private static bool IsMemberOfRelevantType(MemberDeclarationSyntax member, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		if (member.Parent is not TypeDeclarationSyntax tds)
			return false;

		if (semanticModel.GetDeclaredSymbol(tds, cancellationToken) is not { } type)
			return false;

		// Only in reference types with the [Wrapper]ValueObjectAttributes
		if (type.IsValueType || !type.GetAttributes().Any(attr => attr.AttributeClass is
			{
				Name: "ValueObjectAttribute" or "WrapperValueObjectAttribute",
				ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } },
			}))
			return false;

		// Only without a default ctor
		if (type.InstanceConstructors.Any(ctor => ctor.Parameters.Length == 0))
			return false;

		return true;
	}

	private static bool InitializerReferencesAnyConstructorParameter(ExpressionSyntax initializer, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		// The initializer might reference constructor parameters
		// However, it might also DECLARE parameters and then reference them, which is irrelevant to us
		// To see the distinction, first observe which parameters are declared INSIDE the initializer
		var parametersDeclaredInsideInitializer = new HashSet<IParameterSymbol>(SymbolEqualityComparer.Default);
		foreach (var parameterSyntax in initializer.DescendantNodesAndSelf().OfType<ParameterSyntax>())
			if (semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is { } parameterSymbol)
				parametersDeclaredInsideInitializer.Add(parameterSymbol);

		foreach (var id in initializer.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
		{
			var symbol = semanticModel.GetSymbolInfo(id, cancellationToken).Symbol;
			if (symbol is IParameterSymbol parameterSymbol && !parametersDeclaredInsideInitializer.Contains(parameterSymbol))
			{
				// Parameter originates outside initializer
				// This initializer uses primary ctor params
				return true;
			}
		}
		return false;
	}
}
