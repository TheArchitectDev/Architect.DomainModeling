using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Analyzer;

internal static class AnalyzerTypeSymbolExtensions
{
	public static bool IsNullable(this ITypeSymbol? potentialNullable, out ITypeSymbol nullableUnderlyingType)
	{
		if (potentialNullable is not INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedTypeSymbol)
		{
			nullableUnderlyingType = null!;
			return false;
		}

		nullableUnderlyingType = namedTypeSymbol.TypeArguments[0];
		return true;
	}
}
