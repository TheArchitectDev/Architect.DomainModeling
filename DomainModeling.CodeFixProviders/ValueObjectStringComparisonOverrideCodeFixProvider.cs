using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.CodeFixProviders;

/// <summary>
/// Provides a code fix for the case where a ValueObject overrides StringComparison but no longer has a base class and now needs to provide a non-override method.
/// Helps migrate to 4.0.0.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectStringComparisonOverrideCodeFixProvider))]
public sealed class ValueObjectStringComparisonOverrideCodeFixProvider : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["CS0115"]; // No suitable method found to override

	public override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnosticIdConstant;

	public override FixAllProvider? GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First(diagnostic => diagnostic.Id == FixableDiagnosticIdConstant[0]);
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		// A property named StringComparison that is protected
		// The override keyword is already implied by the diagnostic
		if (root.FindNode(diagnostic.Location.SourceSpan) is not PropertyDeclarationSyntax { Identifier.Text: "StringComparison", Parent: TypeDeclarationSyntax type, } property ||
			!property.Modifiers.Any(token => token.IsKind(SyntaxKind.ProtectedKeyword)))
			return;

		var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (semanticModel.GetDeclaredSymbol(type, context.CancellationToken) is not { } typeSymbol)
			return;

		// With WrapperValueObject or ValueObject attribute
		if (!typeSymbol.GetAttributes().Any(attr => attr is
			{
				AttributeClass.Name: "WrapperValueObjectAttribute" or "ValueObjectAttribute",
				AttributeClass.ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } }
			}))
			return;

		var fix = CodeAction.Create(
			title: "Change to private, non-override method",
			createChangedDocument: ct => ConvertProtectedOverrideMethodToPrivateNonOverride(property, context.Document, root),
			"ChangeStringComparisonToPrivateNonOverride");
		context.RegisterCodeFix(fix, diagnostic);
	}

	private static Task<Document> ConvertProtectedOverrideMethodToPrivateNonOverride(
		PropertyDeclarationSyntax property,
		Document document,
		SyntaxNode root)
	{
		var protectedModifier = property.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.ProtectedKeyword));
		var overrideModifier = property.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.OverrideKeyword));

		var privateModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTriviaFrom(protectedModifier);

		var modifiers = property.Modifiers
			.Except([protectedModifier, overrideModifier,])
			.Concat([privateModifier]);

		var updatedProperty = property.WithModifiers(SyntaxFactory.TokenList(modifiers));

		var updatedRoot = root.ReplaceNode(property, updatedProperty);
		return Task.FromResult(document.WithSyntaxRoot(updatedRoot));
	}
}
