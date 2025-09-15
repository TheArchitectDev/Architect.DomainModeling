using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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

	/// <summary>
	/// Returns the direct parent of the given wrapper's core type.
	/// For example, if the type is simply a direct wrapper, this method returns its own data, but otherwise, it returns whatever is the direct parent of the core type.
	/// </summary>
	internal static BasicGeneratable GetDirectParentOfCoreType(
		ImmutableArray<BasicGeneratable> valueWrappers,
		string typeName, string containingNamespace)
	{
		// Concatenate our namespace and name, so that we are similar to further iterations
		Span<char> initialFullyQualifiedTypeName = stackalloc char[containingNamespace.Length + 1 + typeName.Length];
		initialFullyQualifiedTypeName = [.. containingNamespace, '.', .. typeName];

		ref readonly var result = ref Unsafe.NullRef<BasicGeneratable>();

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
					result = ref item;
					nextTypeName = (item.CustomCoreTypeFullyQualifiedName ?? item.UnderlyingTypeFullyQualifiedName).AsSpan();
					break;
				}
			}
		} while (couldDigDeeper);

		return Unsafe.IsNullRef(ref Unsafe.AsRef(result))
			? default
			: result;
	}

	internal static string GetCoreTypeFullyQualifiedName(
		ImmutableArray<BasicGeneratable> valueWrappers,
		string typeName, string containingNamespace)
	{
		var directParentOfCoreType = GetDirectParentOfCoreType(valueWrappers, typeName, containingNamespace);
		return directParentOfCoreType.CustomCoreTypeFullyQualifiedName ?? directParentOfCoreType.UnderlyingTypeFullyQualifiedName;
	}

	// ATTENTION: This method cannot be combined with the other recursive one, because this one's results are affected by intermediate items, not just the deepest item
	/// <summary>
	/// Utility method that recursively determines which formatting and parsing interfaces are supported, based on all known value wrappers.
	/// This allows even nested value wrappers to dig down into the deepest underlying type.
	/// </summary>
	internal static (bool coreValueIsNonNull, bool isSpanFormattable, bool isSpanParsable, bool isUtf8SpanFormattable, bool isUtf8SpanParsable) GetFormattabilityAndParsabilityRecursively(
		ImmutableArray<BasicGeneratable> valueWrappers,
		string typeName, string containingNamespace)
	{
		var coreValueCouldBeNull = false;
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
					coreValueCouldBeNull |= item.CoreValueCouldBeNull;
					isSpanFormattable |= item.IsSpanFormattable;
					isSpanParsable |= item.IsSpanParsable;
					isUtf8SpanFormattable |= item.IsUtf8SpanFormattable;
					isUtf8SpanParsable |= item.IsUtf8SpanParsable;
					break;
				}
			}
		} while (couldDigDeeper && (isSpanFormattable & isSpanParsable & isUtf8SpanFormattable & isUtf8SpanParsable) == false); // Possible & worth seeking deeper

		return (!coreValueCouldBeNull, isSpanFormattable, isSpanParsable, isUtf8SpanFormattable, isUtf8SpanParsable);
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
		/// Helps implement wrappers around unofficial wrapper types, such as a WrapperValueObject&lt;Uri&gt; that pretends its core type is <see langword="string"/>.
		/// </summary>
		public string? CustomCoreTypeFullyQualifiedName { get; }
		public bool CoreTypeIsStruct { get; }
		/// <summary>
		/// A core Value property declared as non-null is a desirable property to propagate, such as to return a non-null value from a conversion operator.
		/// </summary>
		public bool CoreValueCouldBeNull { get; }
		public bool IsSpanFormattable { get; }
		public bool IsSpanParsable { get; }
		public bool IsUtf8SpanFormattable { get; }
		public bool IsUtf8SpanParsable { get; }

		public BasicGeneratable(
			bool isIdentity,
			string containingNamespace,
			ITypeSymbol wrapperType,
			ITypeSymbol underlyingType,
			ITypeSymbol? customCoreType)
		{
			var coreType = customCoreType ?? underlyingType;

			this.IsIdentity = isIdentity;
			this.TypeName = wrapperType.Name;
			this.ContainingNamespace = containingNamespace;
			this.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
			this.CustomCoreTypeFullyQualifiedName = customCoreType?.ToString();
			this.CoreTypeIsStruct = coreType.IsValueType;
			this.CoreValueCouldBeNull = !CoreValueIsReachedAsNonNull(wrapperType);
			this.IsSpanFormattable = underlyingType.SpecialType == SpecialType.System_String || underlyingType.AllInterfaces.Any(interf =>
				interf is { Name: "ISpanFormattable", ContainingNamespace.Name: "System", Arity: 0, });
			this.IsSpanParsable = underlyingType.SpecialType == SpecialType.System_String || underlyingType.AllInterfaces.Any(interf =>
				interf is { Name: "ISpanParsable", ContainingNamespace.Name: "System", Arity: 1, });
			this.IsUtf8SpanFormattable = underlyingType.SpecialType == SpecialType.System_String || underlyingType.AllInterfaces.Any(interf =>
				interf is { Name: "IUtf8SpanFormattable", ContainingNamespace.Name: "System", Arity: 0, });
			this.IsUtf8SpanParsable = underlyingType.SpecialType == SpecialType.System_String || underlyingType.AllInterfaces.Any(interf =>
				interf is { Name: "IUtf8SpanParsable", ContainingNamespace.Name: "System", Arity: 1, });
		}

		/// <summary>
		/// The developer may have implemented the Value as non-null.
		/// It is worthwhile to propagate this knowledge through nested types, such as to mark the conversion operator to the core type as non-null.
		/// </summary>
		private static bool CoreValueIsReachedAsNonNull(ITypeSymbol type)
		{
			// A manual ICoreValueWrapper.Value implementation is leading
			// In its absence, it is source-generated based on the regular Value property

			// We look only at the first ICoreValueWrapper interface, since we should only be using one of each
			var coreOrDirectValueWrapperInterface =
				type.AllInterfaces.FirstOrDefault(interf =>
					interf is { Arity: 2, Name: "ICoreValueWrapper", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } } } &&
					interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default))
				??
				type.AllInterfaces.FirstOrDefault(interf =>
					interf is { Arity: 2, Name: "IDirectValueWrapper", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true, } } } &&
					interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default));

			if (coreOrDirectValueWrapperInterface is null)
				return !type.GetMembers("Value").Any(member => member is IPropertySymbol { NullableAnnotation: not NullableAnnotation.NotAnnotated });

			// ICoreValueWrapper<,> implements IValueWrapper<,>, which declares the Value property
			var valueWrapperInterface = coreOrDirectValueWrapperInterface.Interfaces.Single(interf => interf.Name == "IValueWrapper");

			var explicitValueMember = type.GetMembers().FirstOrDefault(member =>
				member.Name.EndsWith(".Value") &&
				member is IPropertySymbol prop &&
				prop.ExplicitInterfaceImplementations.Any(prop => valueWrapperInterface.Equals(prop.ContainingType, SymbolEqualityComparer.Default)));

			return explicitValueMember is IPropertySymbol { NullableAnnotation: NullableAnnotation.NotAnnotated };
		}
	}
}
