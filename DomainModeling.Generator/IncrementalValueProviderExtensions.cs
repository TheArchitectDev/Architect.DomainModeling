using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extension methods on <see cref="IncrementalValueProvider{TValue}"/> and <see cref="IncrementalValuesProvider{TValues}"/>.
/// </summary>
internal static class IncrementalValueProviderExtensions
{
#nullable disable // LINQ-assisted null filtering is not yet detected by the compiler
	/// <summary>
	/// <para>
	/// Deduplicates partials, preventing duplicate source generation.
	/// </para>
	/// <para>
	/// Partials are deduplicated by calling Collect(), followed by scattering the ouput again using SelectMany(), but only over Distinct() <typeparamref name="T"/> elements.
	/// </para>
	/// <para>
	/// Since <typeparamref name="T"/> is a result of the transformation, which is based on the semantic model, <typeparamref name="T"/> should be identical for each partial of a type.
	/// </para>
	/// <para>
	/// For a correct result, <typeparamref name="T"/> must implement structural equality.
	/// </para>
	/// </summary>
	public static IncrementalValuesProvider<T> DeduplicatePartials<T>(this IncrementalValuesProvider<T> provider)
		where T : IEquatable<T>
	{
		var result = provider.Collect().SelectMany((tuples, ct) => tuples.Distinct());
		return result;
	}
#nullable enable
}
