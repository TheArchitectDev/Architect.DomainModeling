using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

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
		var result = typeDeclarationSyntax.Parent is not BaseNamespaceDeclarationSyntax;
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
	/// <para>
	/// Returns whether the <see cref="TypeDeclarationSyntax"/> is directly annotated with an attribute whose name starts with the given prefix.
	/// </para>
	/// <para>
	/// Prefixes are useful because a developer may type either "[Obsolete]" or "[ObsoleteAttribute]".
	/// </para>
	/// </summary>
	public static bool HasAttributeWithPrefix(this TypeDeclarationSyntax typeDeclarationSyntax, string namePrefix)
	{
		foreach (var attributeList in typeDeclarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
				if ((attribute.Name is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText.StartsWith(namePrefix)) ||
					(attribute.Name	is GenericNameSyntax genericName && genericName.Identifier.ValueText.StartsWith(namePrefix)))
					return true;

		return false;
	}
}
