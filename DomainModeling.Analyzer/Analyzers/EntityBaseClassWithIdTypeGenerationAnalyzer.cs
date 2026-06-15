using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Encourages migrating from Entity&lt;TId, TPrimitive&gt; to EntityAttribute&lt;TId, TIdUnderlying&gt;.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityBaseClassWithIdTypeGenerationAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "EntityBaseClassWithIdTypeGeneration";

	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "EntityBaseClassWithIdTypeGeneration",
		title: "Used entity base class instead of attribute to initiate ID type source generation",
		messageFormat: "Entity<TId, TIdPrimitive> is deprecated in favor of the [Entity<TId, TIdUnderlying>] attribute. Use the extended attribute and remove TIdPrimitive from the base class.",
		category: "Design",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
	}

	private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.Node;

		// Get the first base type (the actual base class rather than interfaces)
		if (classDeclaration.BaseList is not { Types: { Count: > 0 } baseTypes } || baseTypes[0] is not { } baseType)
			return;

		var typeInfo = context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken);
		if (typeInfo.Type is not INamedTypeSymbol baseTypeSymbol)
			return;

		while (baseTypeSymbol is not null)
		{
			if (baseTypeSymbol is { Arity: 2, Name: "Entity", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } } })
				break;

			baseTypeSymbol = baseTypeSymbol.BaseType!;
		}

		// If Entity<TId, TIdPrimitive>
		if (baseTypeSymbol is null)
			return;

		var diagnostic = Diagnostic.Create(DiagnosticDescriptor, baseType.GetLocation());
		context.ReportDiagnostic(diagnostic);
	}
}
