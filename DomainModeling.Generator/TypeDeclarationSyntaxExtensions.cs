using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// Provides extensions on <see cref="TypeDeclarationSyntax"/>.
	/// </summary>
	internal static class TypeDeclarationSyntaxExtensions
	{
		/// <summary>
		/// Returns whether the <see cref="TypeDeclarationSyntax"/> is a nested type.
		/// </summary>
		public static bool IsNested(this TypeDeclarationSyntax typeDeclarationSyntax)
		{
			var result = typeDeclarationSyntax.Parent is not NamespaceDeclarationSyntax && typeDeclarationSyntax.Parent is not FileScopedNamespaceDeclarationSyntax;
			return result;
		}

		/// <summary>
		/// Returns whether the <see cref="TypeDeclarationSyntax"/> has any attributes.
		/// </summary>
		public static bool HasAttributes(this TypeDeclarationSyntax typeDeclarationSyntax)
		{
			var result = typeDeclarationSyntax.AttributeLists.Count > 0;
			return result;
		}

		/// <summary>
		/// Returns whether the <see cref="TypeDeclarationSyntax"/> is directly annotated with an attribute whose name starts with the given prefix.
		/// </summary>
		public static bool HasAttributeWithPrefix(this TypeDeclarationSyntax typeDeclarationSyntax, string namePrefix)
		{
			for (var i = 0; i < typeDeclarationSyntax.AttributeLists.Count; i++)
				for (var j = 0; j < typeDeclarationSyntax.AttributeLists[i].Attributes.Count; j++)
					if (typeDeclarationSyntax.AttributeLists[i].Attributes[j].Name is IdentifierNameSyntax identifier && identifier.Identifier.ValueText.StartsWith(namePrefix))
						return true;

			return false;
		}
	}
}
