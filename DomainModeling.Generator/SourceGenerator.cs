using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// The base class for <see cref="ISourceGenerator"/>s implemented in this package.
/// </summary>
public abstract class SourceGenerator : IIncrementalGenerator
{
	/// <summary>
	/// <para>
	/// Helps avoid errors caused by duplicate type names.
	/// </para>
	/// <para>
	/// Stores stable string hash codes concatenated with separators: one for each namespace encountered for a { generator, type } pair.
	/// </para>
	/// </summary>
	private static ConcurrentDictionary<string, string> NamespacesByGeneratorAndTypeName { get; } = new ConcurrentDictionary<string, string>();

	static SourceGenerator()
	{
#if DEBUG
		// Uncomment the following to debug the source generators
		//if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
#endif
	}

	// Note: If we ever want to know the .NET version being compiled for, one way could be Execute()'s GeneratorExecutionContext.Compilation.ReferencedAssemblyNames.FirstOrDefault(name => name.Name == "System.Runtime")?.Version.Major

	protected static void AddSource(SourceProductionContext context, string sourceText, string typeName, string containingNamespace,
		[CallerFilePath] string? callerFilePath = null)
	{
		var sourceName = $"{typeName}.g.cs";

		// When type names collide, add a stable hash code based on the namespace
		// Note that directly including namespaces in the file name would create hard-to-read file names and risk oversized paths
		var uniqueKey = callerFilePath is null
			? typeName
			: $"{Path.GetFileNameWithoutExtension(callerFilePath)}:{typeName}";
		var stableNamespaceHashCode = containingNamespace.GetStableStringHashCode32();
		var hashCodesForTypeName = NamespacesByGeneratorAndTypeName.AddOrUpdate(uniqueKey, addValue: stableNamespaceHashCode, (key, namespaceHashCodeConcatenation) => namespaceHashCodeConcatenation.Contains(stableNamespaceHashCode)
			? namespaceHashCodeConcatenation
			: $"{namespaceHashCodeConcatenation}-{stableNamespaceHashCode}");
		if (!hashCodesForTypeName.StartsWith(stableNamespaceHashCode)) // Not the first to want this name
			sourceName = $"{typeName}-{stableNamespaceHashCode}.g.cs";

		sourceText = sourceText.NormalizeWhitespace();

		context.AddSource(sourceName, SourceText.From(sourceText, Encoding.UTF8));
	}

	public abstract void Initialize(IncrementalGeneratorInitializationContext context);
}
