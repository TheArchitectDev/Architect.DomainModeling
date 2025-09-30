using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Architect.DomainModeling.CodeFixProviders;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnvalidatedEnumMemberAssignmentCodeFixer))]
public sealed class UnvalidatedEnumMemberAssignmentCodeFixer : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["UnvalidatedEnumAssignmentToDomainobject"];

	public override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnosticIdConstant;

	public override FixAllProvider? GetFixAllProvider()
	{
		return WellKnownFixAllProviders.BatchFixer;
	}

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First(diagnostic => diagnostic.Id == FixableDiagnosticIdConstant[0]);
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null) return;

		var node = root.FindNode(diagnostic.Location.SourceSpan);
		if (node is not ExpressionSyntax unvalidatedValue)
			return;
		var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
		if (assignment is null)
			return;

		var asDefinedAction = CodeAction.Create(
			title: "Validate with .AsDefined() extension method",
			createChangedDocument: ct => ApplyFixAsync(context.Document, root, assignment, unvalidatedValue, "AsDefined", ct),
			equivalenceKey: "ValidateWithAsDefinedExtension");
		var asDefinedFlagsAction = CodeAction.Create(
			title: "Validate with .AsDefinedFlags() extension method",
			createChangedDocument: ct => ApplyFixAsync(context.Document, root, assignment, unvalidatedValue, "AsDefinedFlags", ct),
			equivalenceKey: "ValidateWithAsDefinedFlagsExtension");
		var asUnvalidatedAction = CodeAction.Create(
			title: "Condone with .AsUnvalidated() extension method",
			createChangedDocument: ct => ApplyFixAsync(context.Document, root, assignment, unvalidatedValue, "AsUnvalidated", ct),
			equivalenceKey: "CondoneWithAsUnvalidatedExtension");

		context.RegisterCodeFix(asDefinedAction, diagnostic);
		context.RegisterCodeFix(asDefinedFlagsAction, diagnostic);
		context.RegisterCodeFix(asUnvalidatedAction, diagnostic);
	}

	private static async Task<Document> ApplyFixAsync(
		Document document, SyntaxNode root, AssignmentExpressionSyntax assignment, ExpressionSyntax unvalidatedValue,
		string methodName, CancellationToken cancellationToken)
	{
		var chainableValue = unvalidatedValue.WithoutTrivia();

		// We need parentheses around certain expressions
		if (chainableValue is CastExpressionSyntax or BinaryExpressionSyntax or ConditionalExpressionSyntax or SwitchExpressionSyntax or AssignmentExpressionSyntax)
			chainableValue = SyntaxFactory.ParenthesizedExpression(chainableValue);

		// For the "default" literal, we need to change it to the default expression "default(T)"
		if (chainableValue is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } literal)
			chainableValue = await GetDefaultExpressionForDefaultLiteral(literal, assignment, document, cancellationToken).ConfigureAwait(false);

		var wrappedExpression =
			SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					kind: SyntaxKind.SimpleMemberAccessExpression,
					expression: chainableValue,
					name: SyntaxFactory.IdentifierName(methodName)));

		// Preserve trivia
		wrappedExpression = wrappedExpression.WithTriviaFrom(unvalidatedValue);

		// Ensure presence of the required using declaration
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel is not null && semanticModel.Compilation.GetTypeByMetadataName("Architect.DomainModeling.EnumExtensions") is { } enumExtensionsType)
		{
			var referenceId = DocumentationCommentId.CreateReferenceId(enumExtensionsType);
			var symbolIdAnnotation = new SyntaxAnnotation("SymbolId", referenceId);

			wrappedExpression = wrappedExpression.WithAdditionalAnnotations(
				Simplifier.AddImportsAnnotation, symbolIdAnnotation);
		}

		var newRoot = root.ReplaceNode(unvalidatedValue, wrappedExpression);
		return document.WithSyntaxRoot(newRoot);
	}

	private static async Task<ExpressionSyntax> GetDefaultExpressionForDefaultLiteral(LiteralExpressionSyntax literal, AssignmentExpressionSyntax assignment, Document document, CancellationToken cancellationToken)
	{
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel is null)
			return literal;

		var targetType = semanticModel.GetTypeInfo(assignment.Left, cancellationToken).Type;
		if (targetType is null)
			return literal;

		// Type is non-null by definition, or else we would not have gotten the warning
		var typeSyntax = SyntaxFactory.ParseTypeName(targetType.ToDisplayString());
		if (typeSyntax is null)
			return literal;

		return SyntaxFactory.DefaultExpression(typeSyntax).WithTriviaFrom(literal);
	}
}
