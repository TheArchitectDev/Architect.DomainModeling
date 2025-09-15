using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.CodeFixProviders;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingStringComparisonCodeFixProvider))]
public sealed class MissingStringComparisonCodeFixProvider : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["ValueObjectGeneratorMissingStringComparison", "WrapperValueObjectGeneratorMissingStringComparison"];

	public override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnosticIdConstant;

	public override FixAllProvider? GetFixAllProvider()
	{
		return null;
	}

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First(diagnostic => diagnostic.Id == FixableDiagnosticIdConstant[0] || diagnostic.Id == FixableDiagnosticIdConstant[1]);
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		if (root is null)
			return;

		var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
		var tds = token.Parent?.AncestorsAndSelf()
			.OfType<TypeDeclarationSyntax>()
			.FirstOrDefault();

		if (tds is null)
			return;

		var ordinalFix = CodeAction.Create(
			title: "Implement StringComparison { get; } with StringComparison.Ordinal",
			createChangedDocument: ct => AddStringComparisonMemberAsync(context.Document, root, tds, stringComparisonExpression: "StringComparison.Ordinal", ct),
			equivalenceKey: "ImplementStringComparisonOrdinalGetter");
		context.RegisterCodeFix(ordinalFix, context.Diagnostics.First());

		var ordinalIgnoreCaseFix = CodeAction.Create(
			title: "Implement StringComparison { get; } with StringComparison.OrdinalIgnoreCase",
			createChangedDocument: ct => AddStringComparisonMemberAsync(context.Document, root, tds, stringComparisonExpression: "StringComparison.OrdinalIgnoreCase", ct),
			equivalenceKey: "ImplementStringComparisonOrdinalIgnoreCaseGetter");
		context.RegisterCodeFix(ordinalIgnoreCaseFix, context.Diagnostics.First());
	}

	private static Task<Document> AddStringComparisonMemberAsync(
		Document document,
		SyntaxNode root,
		TypeDeclarationSyntax tds,
		string stringComparisonExpression,
		CancellationToken _)
	{
		var newlineTrivia = GetNewlineTrivia(tds);

		var property = SyntaxFactory.PropertyDeclaration(
				SyntaxFactory.ParseTypeName("StringComparison"),
				SyntaxFactory.Identifier("StringComparison"))
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
			.WithExpressionBody(
				SyntaxFactory.ArrowExpressionClause(
					SyntaxFactory.ParseExpression(stringComparisonExpression)))
			.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
			.WithLeadingTrivia(newlineTrivia)
			.WithTrailingTrivia(newlineTrivia)
			.WithTrailingTrivia(newlineTrivia);

		var updatedTds = tds.WithMembers(tds.Members.Insert(0, property));
		var updatedRoot = root.ReplaceNode(tds, updatedTds);
		return Task.FromResult(document.WithSyntaxRoot(updatedRoot));
	}

	private static SyntaxTrivia GetNewlineTrivia(SyntaxNode node)
	{
		var allTrivia = node.DescendantTrivia(descendIntoTrivia: true);

		var (nCount, rnCount) = (0, 0);

		foreach (var trivia in allTrivia)
		{
			if (!trivia.IsKind(SyntaxKind.EndOfLineTrivia))
				continue;

			var length = trivia.Span.Length;
			var lengthIsOne = length == 1;
			var lengthIsTwo = length == 2;
			nCount += Unsafe.As<bool, int>(ref lengthIsOne);
			rnCount += Unsafe.As<bool, int>(ref lengthIsTwo);
		}

		return rnCount > nCount
			? SyntaxFactory.ElasticCarriageReturnLineFeed
			: SyntaxFactory.ElasticLineFeed;
	}
}
