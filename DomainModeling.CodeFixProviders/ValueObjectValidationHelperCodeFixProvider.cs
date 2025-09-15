using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Architect.DomainModeling.CodeFixProviders;

/// <summary>
/// Provides a code fix for the case where a ValueObject attempts to use the legacy protected static validation helpers but no longer has a base class and now needs to access a static helper class.
/// Helps migrate to 4.0.0.
/// </summary>
[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectStringComparisonOverrideCodeFixProvider))]
public sealed class ValueObjectValidationHelperCodeFixProvider : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["CS0103"]; // The name does not exist in the current context

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

		// A method with the name of one of the methods that used to be available
		if (root.FindNode(diagnostic.Location.SourceSpan) is not IdentifierNameSyntax
			{
				Parent: InvocationExpressionSyntax invocation,
				Identifier.Text:
					"ContainsNonAlphanumericCharacters" or
					"ContainsNonWordCharacters" or
					"ContainsNonAsciiCharacters" or
					"ContainsNonAsciiOrNonPrintableCharacters" or
					"ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters" or
					"ContainsNonPrintableCharacters" or
					"ContainsNonPrintableCharactersOrDoubleQuotes" or
					"ContainsWhitespaceOrNonPrintableCharacters"
			})
			return;

		var fix = CodeAction.Create(
			title: "Redirect call to ValueObjectStringValidator",
			createChangedDocument: ct => RedirectCallToValueObjectStringValidator(invocation, context.Document, ct),
			"RedirectCallToValueObjectStringValidator");
		context.RegisterCodeFix(fix, diagnostic);
	}

	private static async Task<Document> RedirectCallToValueObjectStringValidator(
		InvocationExpressionSyntax invocation,
		Document document,
		CancellationToken cancellationToken)
	{
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

		if (invocation.Expression is not IdentifierNameSyntax simpleName)
			return document;

		var qualifiedName = SyntaxFactory.ParseExpression($"ValueObjectStringValidator.{simpleName.Identifier.Text}")
			.WithTriviaFrom(simpleName);

		// Ensure presence of the required using declaration
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel is not null && semanticModel.Compilation.GetTypeByMetadataName("Architect.DomainModeling.Comparisons.ValueObjectStringValidator") is { } requiredType)
		{
			var referenceId = DocumentationCommentId.CreateReferenceId(requiredType);
			var symbolIdAnnotation = new SyntaxAnnotation("SymbolId", referenceId);

			qualifiedName = qualifiedName.WithAdditionalAnnotations(
				Simplifier.AddImportsAnnotation, symbolIdAnnotation);
		}

		var updatedInvocation = invocation.WithExpression(qualifiedName);
		editor.ReplaceNode(invocation, updatedInvocation);
		document = editor.GetChangedDocument();

		return document;
	}
}
