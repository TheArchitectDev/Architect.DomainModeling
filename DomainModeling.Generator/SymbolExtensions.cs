using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

internal static class SymbolExtensions
{
	/// <summary>
	/// Returns whether the given <see cref="ISymbol"/> (such as a method) has the given <paramref name="name"/>, either straight up or via explicit interface implementation.
	/// The latter requires specialized matching, which this method approximates.
	/// </summary>
	public static bool HasNameOrExplicitInterfaceImplementationName(this ISymbol symbol, string name)
	{
		var needle = name.AsSpan();
		var haystack = symbol.Name.AsSpan();

		var index = haystack.LastIndexOf(needle);

		return index switch
		{
			< 0 => false, // Name not found
			0 => haystack.Length == needle.Length, // Starts with name, so depends on whether is exact match
			_ => haystack[index - 1] == '.' && haystack.Length == index + needle.Length, // Contains name, so depends on whether name directly follows a dot and is suffix
		};
	}
}
