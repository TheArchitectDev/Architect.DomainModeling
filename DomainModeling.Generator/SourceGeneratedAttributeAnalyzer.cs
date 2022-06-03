using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// An analyzer used to report diagnostics if the [SourceGenerated] attribute is being ignored because the annotated type has not fulfilled any generator's requirements.
	/// </summary>
	[Generator]
	public class SourceGeneratedAttributeAnalyzer : SourceGenerator
	{
		public override void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
				.Where(generatable => generatable is not null);

			context.RegisterSourceOutput(provider, ReportDiagnostics!);
		}

		private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
		{
			// Type
			if (node is not TypeDeclarationSyntax tds)
				return false;

			// With SourceGenerated attribute
			if (!tds.HasAttributeWithPrefix(Constants.SourceGeneratedAttributeShortName))
				return false;

			return true;
		}

		private static Analyzable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var model = context.SemanticModel;
			var tds = (TypeDeclarationSyntax)context.Node;
			var type = model.GetDeclaredSymbol(tds);

			if (type is null)
				return null;

			if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
				return null;

			var hasMissingPartialKeyword = !tds.Modifiers.Any(SyntaxKind.PartialKeyword);

			string? expectedTypeName = null;
			if (type.IsOrImplementsInterface(type => type.Arity == 1 && type.IsType(Constants.IdentityInterfaceTypeName, Constants.DomainModelingNamespace), out _))
				expectedTypeName = tds is StructDeclarationSyntax ? null : "struct"; // Expect a struct
			else if (type.IsOrInheritsClass(type => type.Arity == 1 && type.IsType(Constants.WrapperValueObjectTypeName, Constants.DomainModelingNamespace), out _))
				expectedTypeName = tds is ClassDeclarationSyntax ? null : "class"; // Expect a class
			else if (type.IsOrInheritsClass(type => type.Arity == 0 && type.IsType(Constants.ValueObjectTypeName, Constants.DomainModelingNamespace), out _))
				expectedTypeName = tds is ClassDeclarationSyntax ? null : "class"; // Expect a class
			else if (type.IsOrInheritsClass(type => type.Arity == 2 && type.IsType(Constants.DummyBuilderTypeName, Constants.DomainModelingNamespace), out _))
				expectedTypeName = tds is ClassDeclarationSyntax ? null : "class"; // Expect a class
			else
				expectedTypeName = "*"; // No suitable inheritance found for source generation

			var result = new Analyzable()
			{
				HasMissingPartialKeyword = hasMissingPartialKeyword,
				ExpectedTypeName = expectedTypeName,
			};

			result.SetAssociatedData(type);

			return result;
		}

		private static void ReportDiagnostics(SourceProductionContext context, Analyzable analyzable)
		{
			var type = analyzable.GetAssociatedData<INamedTypeSymbol>();

			if (analyzable.HasMissingPartialKeyword)
				context.ReportDiagnostic("NonPartialSourceGeneratedType", "Missing partial keyword",
					"The type was not source-generated because one of its declarations is not marked as partial. To get source generation, add the partial keyword.", DiagnosticSeverity.Warning, type);

			if (analyzable.ExpectedTypeName == "*")
				context.ReportDiagnostic("UnusedSourceGeneratedAttribute", "Unexpected inheritance",
					"The type marked as source-generated has no base class or interface for which a source generator is defined.", DiagnosticSeverity.Warning, type);
			else if (analyzable.ExpectedTypeName is not null)
				context.ReportDiagnostic("UnusedSourceGeneratedAttribute", "Unexpected type",
					$"The type was not source-generated because it is not a {analyzable.ExpectedTypeName}. To get source generation, use a {analyzable.ExpectedTypeName} instead.", DiagnosticSeverity.Warning, type);
		}

		private sealed record Analyzable : IGeneratable
		{
			public bool HasMissingPartialKeyword { get; set; }
			public string? ExpectedTypeName { get; set; }
		}
	}
}
