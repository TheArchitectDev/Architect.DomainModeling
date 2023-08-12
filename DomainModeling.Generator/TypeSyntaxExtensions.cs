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
		int actualArity;
		string actualUnqualifiedName;

		if (typeSyntax is SimpleNameSyntax simpleName)
		{
			actualArity = simpleName.Arity;
			actualUnqualifiedName = simpleName.Identifier.ValueText;
		}
		else if (typeSyntax is QualifiedNameSyntax qualifiedName)
		{
			actualArity = qualifiedName.Arity;
			actualUnqualifiedName = qualifiedName.Right.Identifier.ValueText;
		}
		else if (typeSyntax is AliasQualifiedNameSyntax aliasQualifiedName)
		{
			actualArity = aliasQualifiedName.Arity;
			actualUnqualifiedName = aliasQualifiedName.Name.Identifier.ValueText;
		}
		else
		{
			return false;
		}

		return (arity is null || actualArity == arity) && actualUnqualifiedName == unqualifiedName;
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
