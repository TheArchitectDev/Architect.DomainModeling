using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.CodeFixProviders;

/// <summary>
/// Provides a code fix for migrating from Entity&lt;TId, TPrimitive&gt; to EntityAttribute&lt;TId, TIdUnderlying&gt;.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EntityBaseClassWithIdTypeGenerationCodeFixProvider))]
public sealed class EntityBaseClassWithIdTypeGenerationCodeFixProvider : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["EntityBaseClassWithIdTypeGeneration"];

	public sealed override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnosticIdConstant;

	public sealed override FixAllProvider GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First(diagnostic => diagnostic.Id == FixableDiagnosticIdConstant[0]);
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		if (root.FindNode(diagnostic.Location.SourceSpan) is not BaseTypeSyntax baseTypeSyntax)
			return;

		var tds = baseTypeSyntax.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
		if (tds is null)
			return;

		// Do not offer the fix for abstract types
		if (tds.Modifiers.Any(SyntaxKind.AbstractKeyword))
			return;

		// Do not offer the fix if the inheritance is indirect
		if (baseTypeSyntax.Type is not GenericNameSyntax { Arity: 2, TypeArgumentList.Arguments.Count: 2, } entityBaseTypeSyntax)
			return;
		var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (semanticModel.GetTypeInfo(baseTypeSyntax.Type, context.CancellationToken).Type is not
			INamedTypeSymbol { Arity: 2, Name: "Entity", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } } } entityBaseType)
			return;

		var action = CodeAction.Create(
			title: "Move type parameter for ID's underlying type into EntityAttribute",
			createChangedDocument: ct => ConvertToAttributeAsync(context.Document, root, tds, entityBaseTypeSyntax),
			equivalenceKey: "MoveIdTypeGenerationFromBaseToAttribute");
		context.RegisterCodeFix(action, diagnostic);
	}

	private static Task<Document> ConvertToAttributeAsync(
		Document document,
		SyntaxNode root,
		TypeDeclarationSyntax tds,
		GenericNameSyntax baseTypeSyntax)
	{
		var idTypeToGenerate = baseTypeSyntax.TypeArgumentList.Arguments[0];
		var idUnderlyingType = baseTypeSyntax.TypeArgumentList.Arguments[1];

		// Create Entity<TId, TIdUnderlying> attribute
		var attributeArguments = SyntaxFactory.SeparatedList([idTypeToGenerate, idUnderlyingType,]);
		var entityAttribute =
			SyntaxFactory.Attribute(
				SyntaxFactory.GenericName(SyntaxFactory.Identifier("Entity"))
					.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(attributeArguments)));

		// Check if Entity attribute already exists
		var existingEntityAttribute = tds.AttributeLists
			.SelectMany(list => list.Attributes)
			.FirstOrDefault(attribute => attribute.Name is IdentifierNameSyntax { Identifier.Text: "Entity" or "EntityAttribute" } or QualifiedNameSyntax { Right.Identifier.Text: "Entity" or "EntityAttribute" });

		// Replace or add the Entity attribute
		TypeDeclarationSyntax newTds;
		if (existingEntityAttribute is { Parent: AttributeListSyntax oldAttributeList })
		{
			var newAttributes = oldAttributeList.Attributes.Replace(existingEntityAttribute, entityAttribute);
			var newAttributeList = oldAttributeList
				.WithAttributes(newAttributes)
				.WithLeadingTrivia(oldAttributeList.GetLeadingTrivia())
				.WithTrailingTrivia(oldAttributeList.GetTrailingTrivia());
			newTds = tds.ReplaceNode(oldAttributeList, newAttributeList);
		}
		else
		{
			// This requires some gymnastics to keep the trivia intact

			// No existing attributes - move the type's leading trivia onto the new attribute
			if (tds.AttributeLists.Count == 0)
			{
				var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(entityAttribute))
					.WithLeadingTrivia(tds.GetLeadingTrivia());
				var newAttributeLists = tds.AttributeLists.Add(attributeList);
				newTds = tds
					.WithoutLeadingTrivia()
					.WithAttributeLists(newAttributeLists);
			}
			// Existing attributes - carefully preserve keep the leading trivia on the first attribute
			else
			{
				var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(entityAttribute));
				var newAttributeLists = tds.AttributeLists
					.Replace(tds.AttributeLists[0], tds.AttributeLists[0].WithLeadingTrivia(tds.AttributeLists[0].GetLeadingTrivia()))
					.Add(attributeList);
				newTds = tds.WithAttributeLists(newAttributeLists);
			}
		}

		// Replace the base type
		if (newTds.BaseList is { Types: { Count: > 0 } baseTypes } && baseTypes[0].Type is GenericNameSyntax { Arity: 2, TypeArgumentList.Arguments: { Count: 2 } typeArgs, } genericBaseType)
		{
			var newTypeArgs = SyntaxFactory.TypeArgumentList(
				SyntaxFactory.SingletonSeparatedList(typeArgs[0]));
			var newGenericBaseType = genericBaseType.WithTypeArgumentList(newTypeArgs);
			var newBaseType = baseTypes[0]
				.WithType(newGenericBaseType)
				.WithLeadingTrivia(baseTypes[0].GetLeadingTrivia())
				.WithTrailingTrivia(baseTypes[0].GetTrailingTrivia());
			newTds = newTds.ReplaceNode(baseTypes[0], newBaseType);
		}

		var newRoot = root.ReplaceNode(tds, newTds);
		return Task.FromResult(document.WithSyntaxRoot(newRoot));
	}
}
