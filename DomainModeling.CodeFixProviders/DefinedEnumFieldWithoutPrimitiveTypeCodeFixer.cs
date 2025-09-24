using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.CodeFixProviders;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DefinedEnumFieldWithoutPrimitiveTypeCodeFixer))]
public sealed class DefinedEnumFieldWithoutPrimitiveTypeCodeFixer : CodeFixProvider
{
	private static readonly ImmutableArray<string> FixableDiagnosticIdConstant = ["DefinedEnumMemberMissingPrimitiveSpecification"];

	public override ImmutableArray<string> FixableDiagnosticIds => FixableDiagnosticIdConstant;

	public override FixAllProvider? GetFixAllProvider()
	{
		return null;
	}

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First(diagnostic => diagnostic.Id == FixableDiagnosticIdConstant[0]);
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null) return;

		var diagnosticSpan = diagnostic.Location.SourceSpan;
		var node = root.FindNode(diagnosticSpan);

		var memberTypeSyntax = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>()?.Type ??
			node.FirstAncestorOrSelf<FieldDeclarationSyntax>()?.Declaration.Type;
		if (memberTypeSyntax is null) return;

		var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (semanticModel is null) return;

		var typeInfo = semanticModel.GetTypeInfo(memberTypeSyntax, context.CancellationToken);
		if (typeInfo.Type is not INamedTypeSymbol namedType) return;

		// Dig through nullable
		var genericSyntaxToReplace = memberTypeSyntax as GenericNameSyntax;
		if (namedType is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullableType && nullableType.TypeArguments[0] is INamedTypeSymbol nullableUnderlyingType &&
			memberTypeSyntax is NullableTypeSyntax { ElementType: GenericNameSyntax nestedGenericSyntax })
		{
			namedType = nullableUnderlyingType;
			genericSyntaxToReplace = nestedGenericSyntax;
		}
		if (genericSyntaxToReplace is null) return;

		if (namedType.ConstructedFrom is not { Name: "DefinedEnum", Arity: 1, ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } })
			return;

		if (namedType.TypeArguments[0] is not INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType })
			return;

		var stringAction = CodeAction.Create(
			title: "Add type parameter to represent as string",
			createChangedDocument: cancellationToken => CreateChangedDocumentAsync(context, root, genericSyntaxToReplace, memberTypeSyntax, enumUnderlyingType: null),
			equivalenceKey: "AddStringTypeParameter");

		var numericAction = CodeAction.Create(
			title: "Add type parameter to represent numerically",
			createChangedDocument: cancellationToken => CreateChangedDocumentAsync(context, root, genericSyntaxToReplace, memberTypeSyntax, enumUnderlyingType),
			equivalenceKey: "AddNumericTypeParameter");

		context.RegisterCodeFix(stringAction, diagnostic);
		context.RegisterCodeFix(numericAction, diagnostic);
	}

	private static Task<Document> CreateChangedDocumentAsync(CodeFixContext context, SyntaxNode root,
		GenericNameSyntax genericSyntaxToReplace, TypeSyntax memberTypeSyntax, INamedTypeSymbol? enumUnderlyingType)
	{
		var underlyingTypeSyntax = enumUnderlyingType is not { SpecialType: { } specialType }
			? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
			: specialType switch
			{
				SpecialType.System_Byte => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword)),
				SpecialType.System_SByte => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword)),
				SpecialType.System_Int16 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword)),
				SpecialType.System_UInt16 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword)),
				SpecialType.System_Int32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
				SpecialType.System_UInt32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
				SpecialType.System_Int64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)),
				SpecialType.System_UInt64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword)),
				_ => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))
			};

		var enhancedGenericType = genericSyntaxToReplace.WithTypeArgumentList(
			genericSyntaxToReplace.TypeArgumentList.WithArguments(
				genericSyntaxToReplace.TypeArgumentList.Arguments.Add(underlyingTypeSyntax)));
		var enhancedMemberType = memberTypeSyntax is NullableTypeSyntax
			? (TypeSyntax)SyntaxFactory.NullableType(enhancedGenericType).WithTriviaFrom(memberTypeSyntax)
			: enhancedGenericType.WithTriviaFrom(memberTypeSyntax);
		var newRoot = root.ReplaceNode(memberTypeSyntax, enhancedMemberType);
		return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
	}
}
