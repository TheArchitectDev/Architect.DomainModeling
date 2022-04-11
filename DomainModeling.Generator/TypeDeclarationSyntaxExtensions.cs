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
		/// Returns whether the <see cref="TypeDeclarationSyntax"/> is a non-nested partial type.
		/// </summary>
		public static bool IsNonNestedPartial(this TypeDeclarationSyntax typeDeclarationSyntax)
		{
			var result = typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) && typeDeclarationSyntax.Parent is NamespaceDeclarationSyntax;
			return result;
		}
	}
}
