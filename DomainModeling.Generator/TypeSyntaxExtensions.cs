using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extensions on <see cref="TypeSyntax"/>.
/// </summary>
internal static class TypeSyntaxExtensions
{
	/// <summary>
	/// Returns whether the given <see cref="TypeSyntax"/> has the given arity (type parameter count) and (unqualified) name.
	/// </summary>
	/// <param name="arity">Pass null to accept any arity.</param>
	public static bool HasArityAndName(this TypeSyntax typeSyntax, int? arity, string unqualifiedName)
	{
		return TryGetArityAndUnqualifiedName(typeSyntax, out var actualArity, out var actualUnqualifiedName) &&
			(arity is null || actualArity == arity) &&
			actualUnqualifiedName == unqualifiedName;
	}

	/// <summary>
	/// Returns whether the given <see cref="TypeSyntax"/> has the given arity (type parameter count) and (unqualified) name suffix.
	/// </summary>
	/// <param name="arity">Pass null to accept any arity.</param>
	public static bool HasArityAndNameSuffix(this TypeSyntax typeSyntax, int? arity, string unqualifiedName)
	{
		return TryGetArityAndUnqualifiedName(typeSyntax, out var actualArity, out var actualUnqualifiedName) &&
			(arity is null || actualArity == arity) &&
			actualUnqualifiedName.EndsWith(unqualifiedName);
	}

	private static bool TryGetArityAndUnqualifiedName(TypeSyntax typeSyntax, out int arity, out string unqualifiedName)
	{
		if (typeSyntax is SimpleNameSyntax simpleName)
		{
			arity = simpleName.Arity;
			unqualifiedName = simpleName.Identifier.ValueText;
		}
		else if (typeSyntax is QualifiedNameSyntax qualifiedName)
		{
			arity = qualifiedName.Arity;
			unqualifiedName = qualifiedName.Right.Identifier.ValueText;
		}
		else if (typeSyntax is AliasQualifiedNameSyntax aliasQualifiedName)
		{
			arity = aliasQualifiedName.Arity;
			unqualifiedName = aliasQualifiedName.Name.Identifier.ValueText;
		}
		else
		{
			arity = -1;
			unqualifiedName = null!;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Returns the given <see cref="TypeSyntax"/>'s name, or null if no name can be obtained.
	/// </summary>
	public static string? GetNameOrDefault(this TypeSyntax typeSyntax)
	{
		return typeSyntax switch
		{
			SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
			QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
			_ => null,
		};
	}
}
