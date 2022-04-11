using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// Provides extensions on <see cref="inamespacesymbol"/>.
	/// </summary>
	internal static class NamespaceSymbolExtensions
	{
		/// <summary>
		/// Returns whether the given <see cref="INamedTypeSymbol"/> is or resides in the System namespace.
		/// </summary>
		public static bool IsInSystemNamespace(this INamespaceSymbol namespaceSymbol)
		{
			while (namespaceSymbol.ContainingNamespace is not null)
				namespaceSymbol = namespaceSymbol.ContainingNamespace;

			return namespaceSymbol.Name == "System";
		}

		/// <summary>
		/// Returns whether the given <see cref="INamedTypeSymbol"/> has the given <paramref name="fullName"/>.
		/// </summary>
		public static bool HasFullName(this INamespaceSymbol namespaceSymbol, string fullName)
		{
			return namespaceSymbol.HasFullName(fullName.AsSpan());
		}

		/// <summary>
		/// Returns whether the given <see cref="INamedTypeSymbol"/> has the given <paramref name="fullName"/>.
		/// </summary>
		public static bool HasFullName(this INamespaceSymbol namespaceSymbol, ReadOnlySpan<char> fullName)
		{
			do
			{
				var length = namespaceSymbol.Name.Length;

				// If the last component's name mismatches
				// Or there is no 'start of string' or dot right before it
				// Then the names mismatch
				if (!fullName.EndsWith(namespaceSymbol.Name.AsSpan(), StringComparison.Ordinal) ||
					!(fullName.Length == length || fullName[fullName.Length - length - 1] == '.'))
				{
					return false;
				}

				fullName = fullName.Slice(0, fullName.Length - namespaceSymbol.Name.Length);
				if (fullName.Length > 0) fullName = fullName.Slice(0, fullName.Length - 1); // Slice the '.'

				namespaceSymbol = namespaceSymbol.ContainingNamespace;
			} while (namespaceSymbol.ContainingNamespace?.IsGlobalNamespace == false);

			return true;
		}
	}
}
