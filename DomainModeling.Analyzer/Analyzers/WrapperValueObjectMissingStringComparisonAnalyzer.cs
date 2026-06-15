using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

// This is a separate analyzer because diagnostics directly from source generators appear less reliably, and this is an important diagnostic

/// <summary>
/// Enforces a StringComparison property on annotated, partial WrapperValueObject types with string members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrapperValueObjectMissingStringComparisonAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "WrapperValueObjectGeneratorMissingStringComparison",
		title: "WrapperValueObject has string members but no StringComparison property",
		messageFormat: "WrapperValueObject {0} has string members but no StringComparison property to know how to compare them. Implement 'private StringComparison StringComparison => ...",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
			SyntaxKind.RecordDeclaration,
			SyntaxKind.RecordStructDeclaration);
	}

	private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
	{
		var tds = (TypeDeclarationSyntax)context.Node;

		// Only partial
		if (!tds.Modifiers.Any(SyntaxKind.PartialKeyword))
			return;

		var semanticModel = context.SemanticModel;
		var type = semanticModel.GetDeclaredSymbol(tds, context.CancellationToken);

		if (type is null)
			return;

		// Only with WrapperValueObjectAttribute<string>
		if (!type.GetAttributes().Any(attr =>
			attr.AttributeClass is { Arity: 1, Name: "WrapperValueObjectAttribute", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } }, } attributeClass &&
			attributeClass.TypeArguments[0].SpecialType == SpecialType.System_String))
			return;

		// Only without StringComparison property (hand-written)
		if (type.GetMembers("StringComparison").Any(member => member is IPropertySymbol { IsImplicitlyDeclared: false } prop &&
			prop.DeclaringSyntaxReferences.Length > 0 && prop.DeclaringSyntaxReferences[0].SyntaxTree.FilePath?.EndsWith(".g.cs") == false))
			return;

		var diagnostic = Diagnostic.Create(
			DiagnosticDescriptor,
			tds.Identifier.GetLocation(),
			type.Name);

		context.ReportDiagnostic(diagnostic);
	}
}
