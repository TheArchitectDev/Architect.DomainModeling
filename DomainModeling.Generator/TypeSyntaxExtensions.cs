using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extensions on <see cref="TypeSyntax"/>.
/// </summary>
internal static class TypeSyntaxExtensions
{
	/// <summary>
	/// Returns the given <see cref="TypeSyntax"/>'s name, or null if no name can be obtained.
	/// </summary>
	public static string? GetNameOrDefault(this TypeSyntax typeSyntax)
	{
		var result = typeSyntax switch
		{
			SimpleNameSyntax simple => simple.Identifier.ValueText, // SimpleNameSyntax, GenericNameSyntax
			QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
			_ => null!,
		};
		return result;
	}

	/// <summary>
	/// <para>
	/// Attempts to extract only the name (e.g. "Uri") from any type or name syntax (e.g. "System.Uri").
	/// </para>
	/// <para>
	/// Excludes namespaces, generic type arguments, etc.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetNameOnly(this TypeSyntax input, out string name)
	{
		name = input switch
		{
			SimpleNameSyntax simple => simple.Identifier.ValueText, // SimpleNameSyntax, GenericNameSyntax
			QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
			_ => null!,
		};

		return name is not null;
	}
}
