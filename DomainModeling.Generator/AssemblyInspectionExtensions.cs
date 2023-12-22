using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extension methods that help inspect assemblies.
/// </summary>
public static class AssemblyInspectionExtensions
{
	/// <summary>
	/// Enumerates the given <see cref="IAssemblySymbol"/> and all of its referenced <see cref="IAssemblySymbol"/> instances, recursively.
	/// Does not deduplicate.
	/// </summary>
	/// <param name="predicate">A predicate that can filter out assemblies and prevent further recursion into them.</param>
	public static IEnumerable<IAssemblySymbol> EnumerateAssembliesRecursively(this IAssemblySymbol assemblySymbol, Func<IAssemblySymbol, bool>? predicate = null)
	{
		if (predicate is not null && !predicate(assemblySymbol))
			yield break;

		yield return assemblySymbol;

		foreach (var module in assemblySymbol.Modules)
			foreach (var assembly in module.ReferencedAssemblySymbols)
				foreach (var nestedAssembly in EnumerateAssembliesRecursively(assembly, predicate))
					yield return nestedAssembly;
	}

	/// <summary>
	/// Enumerates all non-nested types in the given <see cref="INamespaceSymbol"/>, recursively.
	/// </summary>
	public static IEnumerable<INamedTypeSymbol> EnumerateNonNestedTypes(this INamespaceSymbol namespaceSymbol)
	{
		foreach (var type in namespaceSymbol.GetTypeMembers())
			yield return type;

		foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
			foreach (var type in EnumerateNonNestedTypes(childNamespace))
				yield return type;
	}
}
