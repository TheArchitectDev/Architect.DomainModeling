using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Combined source generator that delegates to the various concrete generators of value wrappers, so that they may have knowledge of each other.
/// </summary>
[Generator]
public class ValueWrapperGenerator : IIncrementalGenerator
{
	private IdentityGenerator IdentityGenerator { get; } = new IdentityGenerator();
	private WrapperValueObjectGenerator WrapperValueObjectGenerator { get; } = new WrapperValueObjectGenerator();

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		this.IdentityGenerator.InitializeBasicProvider(context, out var identityProvider);
		this.WrapperValueObjectGenerator.InitializeBasicProvider(context, out var wrapperValueObjectProvider);

		var identities = identityProvider.Collect();
		var wrapperValueObjects = wrapperValueObjectProvider.Collect();

		var valueWrappers = identities.Combine(wrapperValueObjects)
			.Select((tuple, ct) => tuple.Left.AddRange(tuple.Right));

		this.IdentityGenerator.Generate(context, valueWrappers);
		this.WrapperValueObjectGenerator.Generate(context, valueWrappers);
	}

	internal static string GetCoreTypeFullyQualifiedName(
		ImmutableArray<BasicGeneratable> valueWrappers,
		string typeName, string containingNamespace)
	{
		// Concatenate our namespace and name, so that we are similar to further iterations
		Span<char> initialFullyQualifiedTypeName = stackalloc char[containingNamespace.Length + 1 + typeName.Length];
		initialFullyQualifiedTypeName = [.. containingNamespace, '.', .. typeName];

		var result = (string?)null;

		var nextTypeName = (ReadOnlySpan<char>)initialFullyQualifiedTypeName;
		bool couldDigDeeper;
		do
		{
			couldDigDeeper = false;
			foreach (ref readonly var item in valueWrappers.AsSpan())
			{
				// Based on the fully qualified type name we are looking for, try to find the corresponding generatable
				if (item.ContainingNamespace.Length + 1 + item.TypeName.Length == nextTypeName.Length &&
					nextTypeName.EndsWith(item.TypeName.AsSpan()) &&
					nextTypeName.StartsWith(item.ContainingNamespace.AsSpan()) &&
					nextTypeName[item.ContainingNamespace.Length] == '.')
				{
					couldDigDeeper = true;
					result = item.CustomCoreTypeFullyQualifiedName ?? item.UnderlyingTypeFullyQualifiedName;
					nextTypeName = result.AsSpan();
					break;
				}
			}
		} while (couldDigDeeper);

		return result ?? initialFullyQualifiedTypeName.ToString();
	}

	// ATTENTION: This method cannot be combined with the other recursive one, because this one's results are affected by intermediate items, not just the deepest item
	/// <summary>
	/// Utility method that recursively determines which formatting and parsing interfaces are supported, based on all known value wrappers.
	/// This allows even nested value wrappers to dig down into the deepest underlying type.
	/// </summary>
	internal static (bool isSpanFormattable, bool isSpanParsable, bool isUtf8SpanFormattable, bool isUtf8SpanParsable) GetFormattabilityAndParsabilityRecursively(
		ImmutableArray<BasicGeneratable> valueWrappers,
		string typeName, string containingNamespace)
	{
		var isSpanFormattable = false;
		var isSpanParsable = false;
		var isUtf8SpanFormattable = false;
		var isUtf8SpanParsable = false;

		// Concatenate our namespace and name, so that we are similar to further iterations
		Span<char> initialFullyQualifiedTypeName = stackalloc char[containingNamespace.Length + 1 + typeName.Length];
		initialFullyQualifiedTypeName = [.. containingNamespace, '.', .. typeName];

		// A generated type will honor the formattability/parsability of its underlying type
		// As such, for generated types, it is worth recursing into the underlying types to discover if formattability/parsability is available through the chain
		var nextTypeName = (ReadOnlySpan<char>)initialFullyQualifiedTypeName;
		bool couldDigDeeper;
		do
		{
			couldDigDeeper = false;
			foreach (ref readonly var item in valueWrappers.AsSpan())
			{
				// Based on the fully qualified type name we are looking for, try to find the corresponding generatable
				if (item.ContainingNamespace.Length + 1 + item.TypeName.Length == nextTypeName.Length &&
					nextTypeName.EndsWith(item.TypeName.AsSpan()) &&
					nextTypeName.StartsWith(item.ContainingNamespace.AsSpan()) &&
					nextTypeName[item.ContainingNamespace.Length] == '.')
				{
					couldDigDeeper = true;
					nextTypeName = item.UnderlyingTypeFullyQualifiedName.AsSpan();
					isSpanFormattable |= item.IsSpanFormattable;
					isSpanParsable |= item.IsSpanParsable;
					isUtf8SpanFormattable |= item.IsUtf8SpanFormattable;
					isUtf8SpanParsable |= item.IsUtf8SpanParsable;
					break;
				}
			}
		} while (couldDigDeeper && (isSpanFormattable & isSpanParsable & isUtf8SpanFormattable & isUtf8SpanParsable) == false); // Possible & worth seeking deeper

		return (isSpanFormattable, isSpanParsable, isUtf8SpanFormattable, isUtf8SpanParsable);
	}

	[StructLayout(LayoutKind.Auto)]
	internal readonly record struct BasicGeneratable
	{
		public bool IsIdentity { get; }
		public string TypeName { get; }
		public string ContainingNamespace { get; }
		public string UnderlyingTypeFullyQualifiedName { get; }
		/// <summary>
		/// Set only if manually chosen by the developer.
		/// Helps implement wrappers around unofficial wrapper type, such as a WrapperValueObject&lt;Uri&gt; that pretends its core type is string.
		/// </summary>
		public string? CustomCoreTypeFullyQualifiedName { get; }
		public bool IsSpanFormattable { get; }
		public bool IsSpanParsable { get; }
		public bool IsUtf8SpanFormattable { get; }
		public bool IsUtf8SpanParsable { get; }

		public BasicGeneratable(
			bool isIdentity,
			string typeName,
			string containingNamespace,
			string underlyingTypeFullyQualifiedName,
			string? customCoreTypeFullyQualifiedName,
			bool isSpanFormattable,
			bool isSpanParsable,
			bool isUtf8SpanFormattable,
			bool isUtf8SpanParsable)
		{
			this.IsIdentity = isIdentity;
			this.TypeName = typeName;
			this.ContainingNamespace = containingNamespace;
			this.UnderlyingTypeFullyQualifiedName = underlyingTypeFullyQualifiedName;
			this.CustomCoreTypeFullyQualifiedName = customCoreTypeFullyQualifiedName;
			this.IsSpanFormattable = isSpanFormattable;
			this.IsSpanParsable = isSpanParsable;
			this.IsUtf8SpanFormattable = isUtf8SpanFormattable;
			this.IsUtf8SpanParsable = isUtf8SpanParsable;
		}
	}
}
