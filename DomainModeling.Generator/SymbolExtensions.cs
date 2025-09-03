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

		if (index < 0)
			return false;

		if (index == 0)
			return true;

		var nameFollowsDot = haystack[index - 1] == '.';

		return nameFollowsDot;
	}
}
