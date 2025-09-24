using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Enforces the use of DefinedEnum&lt;TEnum, TPrimitive&gt; over DefinedEnum&lt;TEnum&gt; in properties and fields.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefinedEnumMemberWithoutPrimitiveAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "DefinedEnumMemberMissingPrimitiveSpecification",
		title: "DefinedEnum member missing specification of primitive representation",
		messageFormat: "DefinedEnum member {0}.{1} must specify its primitive representation using the second generic type parameter",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
		context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
	}

	private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
	{
		var propertySyntax = (PropertyDeclarationSyntax)context.Node;
		var property = context.SemanticModel.GetDeclaredSymbol(propertySyntax);

		if (property is null)
			return;

		WarnAgainstMissingPrimitiveSpecification(context, property.Type, property.ContainingType, propertySyntax.Type);
	}

	private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
	{
		var fieldSyntax = (FieldDeclarationSyntax)context.Node;

		// Note that fields can be defined like this:
		// private int field1, field2, field3;
		if (fieldSyntax.Declaration.Variables.Count == 0) // Prevents a NullReferenceException when enumerating the variables
			return;
		foreach (var fieldVariableSyntax in fieldSyntax.Declaration.Variables)
		{
			if (context.SemanticModel.GetDeclaredSymbol(fieldVariableSyntax) is not IFieldSymbol field)
				continue;

			WarnAgainstMissingPrimitiveSpecification(context, field.Type, field.ContainingType, fieldSyntax.Declaration.Type);
		}
	}

	private static void WarnAgainstMissingPrimitiveSpecification(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol, ITypeSymbol containingType, SyntaxNode locationNode)
	{
		// Dig through nullable
		if (typeSymbol.IsNullable(out var nullableUnderlyingType))
			typeSymbol = nullableUnderlyingType;

		if (typeSymbol is not
			INamedTypeSymbol
			{
				IsGenericType: true, Arity: 1, Name: "DefinedEnum",
				ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } }
			})
			return;

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptor,
			locationNode.GetLocation(),
			containingType.Name,
			typeSymbol.Name);

		context.ReportDiagnostic(diagnostic);
	}
}
