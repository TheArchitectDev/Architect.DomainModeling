using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extensions on <see cref="TypeDeclarationSyntax"/>.
/// </summary>
internal static class TypeDeclarationSyntaxExtensions
{
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
				if (attribute.Name.TryGetNameOnly(out var name) && name.StartsWith(namePrefix))
					return true;

		return false;
	}

	/// <summary>
	/// <para>
	/// Returns whether the <see cref="TypeDeclarationSyntax"/> is directly annotated with an attribute whose name contains the given prefix.
	/// </para>
	/// <para>
	/// Prefixes are useful because a developer may type either "[Obsolete]" or "[ObsoleteAttribute]", and infixes are useful for custom subclasses.
	/// </para>
	/// </summary>
	public static bool HasAttributeWithInfix(this TypeDeclarationSyntax typeDeclarationSyntax, string nameInfix)
	{
		foreach (var attributeList in typeDeclarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
				if (attribute.Name.TryGetNameOnly(out var name) && name.Contains(nameInfix))
					return true;

		return false;
	}
}
