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
		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<TypeDeclarationSyntax> TypesWithoutPartialKeyword { get; } = new List<TypeDeclarationSyntax>(64);
			/// <summary>
			/// Also contains the expected type.
			/// </summary>
			public List<(TypeDeclarationSyntax, Type)> TypesWithIncorrectKind { get; } = new List<(TypeDeclarationSyntax, Type)>();
			public List<TypeDeclarationSyntax> TypesWithUnusableInheritance { get; } = new List<TypeDeclarationSyntax>();

			public void OnVisitSyntaxNode(SyntaxNode node)
			{
				if (node is TypeDeclarationSyntax tds)
				{
					if (!tds.Modifiers.Any(SyntaxKind.PartialKeyword))
					{
						this.TypesWithoutPartialKeyword.Add(tds);
						return;
					}

					Type? expectedKind = null;

					var baseTypes = tds.BaseList?.Types ?? new SeparatedSyntaxList<BaseTypeSyntax>();
					for (var i = 0; i < baseTypes.Count; i++)
					{
						var baseType = baseTypes[i];

						if (baseType.Type is not SimpleNameSyntax simpleName) continue;

						if (simpleName.Arity == 1 && simpleName.Identifier.ValueText == Constants.IdentityInterfaceTypeName)
							if (tds is StructDeclarationSyntax) return; // Valid
							else expectedKind = typeof(StructDeclarationSyntax);

						if (tds is ClassDeclarationSyntax && simpleName.Arity == 1 && simpleName.Identifier.ValueText == Constants.WrapperValueObjectTypeName)
							if (tds is ClassDeclarationSyntax) return; // Valid
							else expectedKind = typeof(ClassDeclarationSyntax);

						if (tds is ClassDeclarationSyntax && simpleName.Arity == 0 && simpleName.Identifier.ValueText == Constants.ValueObjectTypeName)
							if (tds is ClassDeclarationSyntax) return; // Valid
							else expectedKind = typeof(ClassDeclarationSyntax);

						if (tds is ClassDeclarationSyntax && simpleName.Arity == 2 && simpleName.Identifier.ValueText == Constants.DummyBuilderTypeName)
							if (tds is ClassDeclarationSyntax) return; // Valid
							else expectedKind = typeof(ClassDeclarationSyntax);
					}

					if (expectedKind is null)
						this.TypesWithUnusableInheritance.Add(tds);
					else if (tds.GetType() != expectedKind)
						this.TypesWithIncorrectKind.Add((tds, typeof(StructDeclarationSyntax)));
				}
			}
		}

		public override void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public override void Execute(GeneratorExecutionContext context)
		{
			// Work only with our own syntax receiver
			if (context.SyntaxReceiver is not SyntaxReceiver receiver)
				return;

			foreach (var tds in receiver.TypesWithoutPartialKeyword)
			{
				var model = context.Compilation.GetSemanticModel(tds.SyntaxTree);
				var type = model.GetDeclaredSymbol(tds)!;

				if (type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
					context.ReportDiagnostic("NonPartialSourceGeneratedType", "Missing partial keyword",
						"The type was not source-generated because it is not marked as partial. To get source generation, add the partial keyword.", DiagnosticSeverity.Warning, type);
			}

			foreach (var (tds, expectedType) in receiver.TypesWithIncorrectKind)
			{
				var expectedTypeName = expectedType == typeof(StructDeclarationSyntax)
					? "struct"
					: expectedType == typeof(ClassDeclarationSyntax)
					? "class"
					: null;

				if (expectedType is null) return; // No message defined for this expected kind

				var model = context.Compilation.GetSemanticModel(tds.SyntaxTree);
				var type = model.GetDeclaredSymbol(tds)!;

				if (type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
					context.ReportDiagnostic("UnusedSourceGeneratedAttribute", "Unexpected type",
						$"The type was not source-generated because it is not a {expectedTypeName}. To get source generation, use a {expectedTypeName} instead.", DiagnosticSeverity.Warning, type);
			}

			foreach (var tds in receiver.TypesWithUnusableInheritance)
			{
				var model = context.Compilation.GetSemanticModel(tds.SyntaxTree);
				var type = model.GetDeclaredSymbol(tds)!;

				if (type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
					context.ReportDiagnostic("UnusedSourceGeneratedAttribute", "Unexpected inheritance",
						"The type marked as source-generated has no base class or interface for which a source generator is defined.", DiagnosticSeverity.Warning, type);
			}
		}
	}
}
