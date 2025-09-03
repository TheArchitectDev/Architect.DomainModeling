using System.Collections.Immutable;
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

		this.IdentityGenerator.Generate(context, identities, valueWrappers);
		this.WrapperValueObjectGenerator.Generate(context, wrapperValueObjects, valueWrappers);
	}

	/// <summary>
	/// Utility method that recursively determines which formatting and parsing interfaces are supported, based on all known value wrappers.
	/// This allows even nested value wrappers to dig down into the deepest underlying type.
	/// </summary>
	internal static (bool isSpanFormattable, bool isSpanParsable, bool isUtf8SpanFormattable, bool isUtf8SpanParsable) GetFormattabilityAndParsabilityRecursively(
		ImmutableArray<BasicGeneratable> valueWrapperGeneratables,
		string typeName, string containingNamespace, string underlyingTypeFullyQualifiedName)
	{
		var isSpanFormattable = false;
		var isSpanParsable = false;
		var isUtf8SpanFormattable = false;
		var isUtf8SpanParsable = false;

		// Concatenate our namespace and name, so that we are comparable to further iterations
		Span<char> ownFullyQualifiedTypeName = stackalloc char[containingNamespace.Length + 1 + typeName.Length];
		ownFullyQualifiedTypeName = [.. containingNamespace, '.', .. typeName];

		// A generated type will honor the formattability/parsability of its underlying type
		// As such, for generated types, it is worth recursing into the underlying types to discover if formattability/parsability is available through the chain
		var nextTypeName = (ReadOnlySpan<char>)ownFullyQualifiedTypeName;
		bool hasUnderlyingGeneratedType;
		do
		{
			hasUnderlyingGeneratedType = false;
			foreach (ref readonly var item in valueWrapperGeneratables.AsSpan())
			{
				// Based on the fully qualified type name we are looking for, try to find the corresponding NameOnlyGeneratable
				if (nextTypeName.EndsWith(item.TypeName.AsSpan()) &&
					nextTypeName.StartsWith(item.ContainingNamespace.AsSpan()) &&
					item.ContainingNamespace.Length + 1 + item.TypeName.Length == nextTypeName.Length &&
					nextTypeName[item.ContainingNamespace.Length] == '.')
				{
					hasUnderlyingGeneratedType = true;
					nextTypeName = item.UnderlyingTypeFullyQualifiedName.AsSpan();
					isSpanFormattable |= item.IsSpanFormattable;
					isSpanParsable |= item.IsSpanParsable;
					isUtf8SpanFormattable |= item.IsUtf8SpanFormattable;
					isUtf8SpanParsable |= item.IsUtf8SpanParsable;
					break;
				}
			}
		} while (hasUnderlyingGeneratedType && (isSpanFormattable & isSpanParsable & isUtf8SpanFormattable & isUtf8SpanParsable) == false); // Possible & worth seeking deeper

		return (isSpanFormattable, isSpanParsable, isUtf8SpanFormattable, isUtf8SpanParsable);
	}

	internal readonly record struct BasicGeneratable
	{
		public string TypeName { get; }
		public string ContainingNamespace { get; }
		public string UnderlyingTypeFullyQualifiedName { get; }
		public bool IsSpanFormattable { get; }
		public bool IsSpanParsable { get; }
		public bool IsUtf8SpanFormattable { get; }
		public bool IsUtf8SpanParsable { get; }

		public BasicGeneratable(
			string typeName,
			string containingNamespace,
			string underlyingTypeFullyQualifiedName,
			bool isSpanFormattable,
			bool isSpanParsable,
			bool isUtf8SpanFormattable,
			bool isUtf8SpanParsable)
		{
			this.TypeName = typeName;
			this.ContainingNamespace = containingNamespace;
			this.UnderlyingTypeFullyQualifiedName = underlyingTypeFullyQualifiedName;
			this.IsSpanFormattable = isSpanFormattable;
			this.IsSpanParsable = isSpanParsable;
			this.IsUtf8SpanFormattable = isUtf8SpanFormattable;
			this.IsUtf8SpanParsable = isUtf8SpanParsable;
		}
	}
}
