using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// Provides extensions on <see cref="TypeSyntax"/>.
	/// </summary>
	internal static class TypeSyntaxExtensions
	{
		public static bool HasArityAndName(this TypeSyntax typeSyntax, int arity, string unqualifiedName)
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

			return actualArity == arity && actualUnqualifiedName == unqualifiedName;
		}
	}
}
