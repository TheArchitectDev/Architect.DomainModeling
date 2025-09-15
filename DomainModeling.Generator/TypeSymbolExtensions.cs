using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extensions on <see cref="ITypeSymbol"/>.
/// </summary>
internal static class TypeSymbolExtensions
{
	private const string ComparisonsNamespace = "Architect.DomainModeling.Comparisons";

	/// <summary>
	/// Returns the full CLR metadata name of the <see cref="INamedTypeSymbol"/>, e.g. "Namespace.Type+NestedGenericType`1".
	/// </summary>
	public static string GetFullMetadataName(this INamedTypeSymbol namedTypeSymbol)
	{
		// Recurse until we have a non-nested type
		if (namedTypeSymbol.IsNested())
			return $"{GetFullMetadataName(namedTypeSymbol.ContainingType)}+{namedTypeSymbol.MetadataName}";

		// Beware that types may exist in the global namespace
		return namedTypeSymbol.ContainingNamespace is INamespaceSymbol { IsGlobalNamespace: false } ns
			? $"{ns.ToDisplayString()}.{namedTypeSymbol.MetadataName}"
			: namedTypeSymbol.MetadataName;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true };
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName, int arity)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true } &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity;
		return result;
	}

	/// <param name="intermediateNamespace">A single intermediate namespace component, e.g. "Collections", but <em>not</em> "Collections.Generic".</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName, string intermediateNamespace)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } ns && ns.Name == intermediateNamespace;
		return result;
	}

	/// <param name="intermediateNamespace">A single intermediate namespace component, e.g. "Collections", but <em>not</em> "Collections.Generic".</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName, string intermediateNamespace, int arity)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } ns && ns.Name == intermediateNamespace &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity;
		return result;
	}

	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName, string intermediateNamespace1, string intermediateNamespace2)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					}
				} ns1
			} ns2 &&
			ns1.Name == intermediateNamespace1 &&
			ns2.Name == intermediateNamespace2;
		return result;
	}

	public static bool IsSystemType(this ITypeSymbol typeSymbol, string typeName, string intermediateNamespace1, string intermediateNamespace2, int arity)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					}
				} ns1
			} ns2 &&
			ns1.Name == intermediateNamespace1 &&
			ns2.Name == intermediateNamespace2;
		return result;
	}

	/// <param name="namespaceComponent1">A single namespace component, e.g. "Architect", but <em>not</em> "Architect.DomainModeling".</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { ContainingNamespace.IsGlobalNamespace: true } ns1 && ns1.Name == namespaceComponent1;
		return result;
	}

	/// <param name="namespaceComponent1">A single namespace component, e.g. "Architect", but <em>not</em> "Architect.DomainModeling".</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1, int arity)
	{
		var result = typeSymbol.Name == typeName && typeSymbol.ContainingNamespace is { ContainingNamespace.IsGlobalNamespace: true } ns1 && ns1.Name == namespaceComponent1 &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity;
		return result;
	}

	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1, string namespaceComponent2)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace.IsGlobalNamespace: true,
				} ns1
			} ns2 &&
			ns1.Name == namespaceComponent1 &&
			ns2.Name == namespaceComponent2;
		return result;
	}

	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1, string namespaceComponent2, int arity)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace.IsGlobalNamespace: true,
				} ns1
			} ns2 &&
			ns1.Name == namespaceComponent1 &&
			ns2.Name == namespaceComponent2;
		return result;
	}

	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1, string namespaceComponent2, string namespaceComponent3)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace:
					{
						ContainingNamespace.IsGlobalNamespace: true,
					} ns1
				} ns2
			} ns3 &&
			ns1.Name == namespaceComponent1 &&
			ns2.Name == namespaceComponent2 &&
			ns3.Name == namespaceComponent3;
		return result;
	}

	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string namespaceComponent1, string namespaceComponent2, string namespaceComponent3, int arity)
	{
		var result =
			typeSymbol.Name == typeName &&
			typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity &&
			typeSymbol.ContainingNamespace is
			{
				ContainingNamespace:
				{
					ContainingNamespace:
					{
						ContainingNamespace.IsGlobalNamespace: true,
					} ns1
				} ns2
			} ns3 &&
			ns1.Name == namespaceComponent1 &&
			ns2.Name == namespaceComponent2 &&
			ns3.Name == namespaceComponent3;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> has the given <paramref name="typeName"/> and <paramref name="containingNamespace"/>.
	/// </summary>
	public static bool IsTypeWithNamespace(this ITypeSymbol typeSymbol, string typeName, string containingNamespace, int? arity = null)
	{
		return IsTypeWithNamespace(typeSymbol, typeName.AsSpan(), containingNamespace.AsSpan(), arity);
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> has the given <paramref name="typeName"/> and <paramref name="containingNamespace"/>.
	/// </summary>
	/// <param name="generic">If not null, the being-generic of the type must match this value.</param>
	private static bool IsTypeWithNamespace(this ITypeSymbol typeSymbol, ReadOnlySpan<char> typeName, ReadOnlySpan<char> containingNamespace, int? arity = null)
	{
		var backtickIndex = typeName.IndexOf('`');
		if (backtickIndex >= 0)
			typeName = typeName.Slice(0, backtickIndex);

		var result = typeSymbol.Name.AsSpan().Equals(typeName, StringComparison.Ordinal) &&
			typeSymbol.ContainingNamespace.HasFullName(containingNamespace);

		if (result && arity is not null)
			result = typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity == arity;

		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is of type <typeparamref name="T"/>.
	/// </summary>
	public static bool IsType<T>(this ITypeSymbol typeSymbol)
	{
		return typeSymbol.IsType(typeof(T));
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is of the given type.
	/// </summary>
	public static bool IsType(this ITypeSymbol typeSymbol, Type type)
	{
		if (type.IsGenericTypeDefinition) ThrowOpenGenericTypeException();

		if (!IsTypeWithNamespace(typeSymbol, type.Name, type.Namespace)) return false;

		return !type.IsGenericType || HasGenericTypeArguments(typeSymbol, type);

		// Local function that throws for open generic types
		static void ThrowOpenGenericTypeException()
		{
			throw new NotSupportedException("This method does not support open generic types.");
		}

		// Local function that returns whether the input types have matching generic type arguments
		static bool HasGenericTypeArguments(ITypeSymbol typeSymbol, Type type)
		{
			if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return false;

			var requiredTypeArgs = type.GenericTypeArguments;
			var actualTypeArgs = namedTypeSymbol.TypeArguments;

			if (requiredTypeArgs.Length != actualTypeArgs.Length) return false;

			for (var i = 0; i < requiredTypeArgs.Length; i++)
				if (!actualTypeArgs[i].IsType(requiredTypeArgs[i]))
					return false;

			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSpanOfSpecialType(this ITypeSymbol typeSymbol, SpecialType specialType)
	{
		var result = typeSymbol.IsSystemType("Span") && typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments[0].SpecialType == specialType;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsReadOnlySpanOfSpecialType(this ITypeSymbol typeSymbol, SpecialType specialType)
	{
		var result = typeSymbol.IsSystemType("ReadOnlySpan") && typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments[0].SpecialType == specialType;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is or inherits from a certain class, as determined by the given <paramref name="predicate"/>.
	/// </summary>
	public static bool IsOrInheritsClass(this ITypeSymbol typeSymbol, Func<INamedTypeSymbol, bool> predicate, out INamedTypeSymbol targetType)
	{
		if (typeSymbol is INamedTypeSymbol namedTypeSymbol && predicate(namedTypeSymbol))
		{
			targetType = namedTypeSymbol;
			return true;
		}

		var baseType = typeSymbol.BaseType;

		while (baseType is not null)
		{
			// End of inheritance chain
			if (baseType.SpecialType == SpecialType.System_Object)
				break;

			if (predicate(baseType))
			{
				targetType = baseType;
				return true;
			}

			baseType = baseType.BaseType;
		}

		targetType = null!;
		return false;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is or implements a certain interface, as determined by the given <paramref name="predicate"/>.
	/// </summary>
	public static bool IsOrImplementsInterface(this ITypeSymbol typeSymbol, Func<INamedTypeSymbol, bool> predicate, out INamedTypeSymbol targetType)
	{
		if (typeSymbol is INamedTypeSymbol namedTypeSymbol && predicate(namedTypeSymbol))
		{
			targetType = namedTypeSymbol;
			return true;
		}

		foreach (var interf in typeSymbol.AllInterfaces)
		{
			if (predicate(interf))
			{
				targetType = interf;
				return true;
			}
		}

		targetType = null!;
		return false;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> represents one of the 8 primitive integral types, such as <see cref="Int32"/> or <see cref="UInt64"/>.
	/// </summary>
	/// <param name="seeThroughNullable">Whether to return true for a <see cref="Nullable{T}"/> of a matching underlying type.</param>
	public static bool IsPrimitiveIntegral(this ITypeSymbol typeSymbol, bool seeThroughNullable)
	{
		if (seeThroughNullable && typeSymbol.IsNullable(out var underlyingType))
			typeSymbol = underlyingType;

		var specialType = typeSymbol.SpecialType;
		var result = specialType >= SpecialType.System_SByte && specialType <= SpecialType.System_UInt64;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a nested type.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNested(this ITypeSymbol typeSymbol)
	{
		var result = typeSymbol.ContainingType is not null;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a generic type.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsGeneric(this ITypeSymbol typeSymbol)
	{
		var result = typeSymbol is INamedTypeSymbol { IsGenericType: true };
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a generic type with the given number of type parameters.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsGeneric(this ITypeSymbol typeSymbol, int arity)
	{
		var result = typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && namedTypeSymbol.Arity == arity;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a generic type with the given number of type parameters.
	/// Outputs the type arguments on true.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsGeneric(this ITypeSymbol typeSymbol, int arity, out ImmutableArray<ITypeSymbol> typeArguments)
	{
		if (typeSymbol is not INamedTypeSymbol { IsGenericType: true } namedTypeSymbol || namedTypeSymbol.Arity != arity)
		{
			typeArguments = default;
			return false;
		}

		typeArguments = namedTypeSymbol.TypeArguments;
		return true;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a <see cref="Nullable{T}"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullable(this ITypeSymbol typeSymbol)
	{
		return typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a <see cref="Nullable{T}"/>, outputting the underlying type if so.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullable(this ITypeSymbol typeSymbol, out ITypeSymbol underlyingType)
	{
		if (typeSymbol is INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } namedTypeSymbol)
		{
			underlyingType = namedTypeSymbol.TypeArguments[0];
			return true;
		}

		underlyingType = null!;
		return false;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a <see cref="Nullable{T}"/>, where T matches <paramref name="underlyingType"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullableOf(this ITypeSymbol typeSymbol, ITypeSymbol underlyingType)
	{
		var result = IsNullable(typeSymbol, out var comparand) && underlyingType.Equals(comparand, SymbolEqualityComparer.Default);
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns whether the <see cref="ITypeSymbol"/> implements any <see cref="IComparable"/> or <see cref="IComparable{T}"/> interface.
	/// </para>
	/// <para>
	/// This method can optionally see through <see cref="Nullable{T}"/> (which does not implement the necessary interface) to the underlying type.
	/// Beware that nullables <em>cannot</em> simply be compared with left.CompareTo(right).
	/// </para>
	/// </summary>
	/// <param name="seeThroughNullable">Whether to return true for a <see cref="Nullable{T}"/> of a matching underlying type.</param>
	public static bool IsComparable(this ITypeSymbol typeSymbol, bool seeThroughNullable)
	{
		if (seeThroughNullable && typeSymbol.IsNullable(out var underlyingType))
			typeSymbol = underlyingType;

		var result = typeSymbol.AllInterfaces.Any(interf => interf.IsSystemType("IComparable"));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is or implements <see cref="IEnumerable{T}"/> and a most specific such interface can be identified.
	/// For example, if <see cref="IEnumerable{T}"/> is implemented for multiple types but <see cref="IList{T}"/> is implemented for only one, there is a clear winner.
	/// </summary>
	public static bool IsSpecificGenericEnumerable(this ITypeSymbol typeSymbol, out INamedTypeSymbol? elementType)
	{
		elementType = default;

		if (typeSymbol is IArrayTypeSymbol { Rank: 1, ElementType: INamedTypeSymbol arrayElementType }) // Single-dimensional, non-nested array
		{
			elementType = arrayElementType;
			return true;
		}

		var interfaces = typeSymbol.AllInterfaces;

		Span<SpecialType> specialTypes = stackalloc SpecialType[1 + interfaces.Length];

		// Put the SpecialType of each interface in the corresponding slot
		for (var i = 0; i < interfaces.Length; i++)
			specialTypes[i] = interfaces[i].ConstructedFrom.SpecialType;

		// Put the type itself in the additional slot at the end
		specialTypes[specialTypes.Length - 1] = typeSymbol is INamedTypeSymbol
			? typeSymbol.SpecialType
			: SpecialType.None;

		var indexOfMostSpecificCollectionInterface =
			GetIndexOfSoleSpecialTypeMatch(specialTypes, SpecialType.System_Collections_Generic_IList_T) ??
			GetIndexOfSoleSpecialTypeMatch(specialTypes, SpecialType.System_Collections_Generic_ICollection_T) ??
			GetIndexOfSoleSpecialTypeMatch(specialTypes, SpecialType.System_Collections_Generic_IReadOnlyList_T) ??
			GetIndexOfSoleSpecialTypeMatch(specialTypes, SpecialType.System_Collections_Generic_IReadOnlyCollection_T) ??
			GetIndexOfSoleSpecialTypeMatch(specialTypes, SpecialType.System_Collections_Generic_IEnumerable_T);

		if (indexOfMostSpecificCollectionInterface is null)
			return false;

		elementType = indexOfMostSpecificCollectionInterface == specialTypes.Length - 1 // The input type, rather than one of its interfaces
			? ((INamedTypeSymbol)typeSymbol).TypeArguments[0] as INamedTypeSymbol
			: interfaces[indexOfMostSpecificCollectionInterface.Value].TypeArguments[0] as INamedTypeSymbol;

		return true;

		// Local function that returns the index of the single matching special type, or null if there is not exactly one match
		static int? GetIndexOfSoleSpecialTypeMatch(ReadOnlySpan<SpecialType> specialTypes, SpecialType specialType)
		{
			var match = (int?)null;
			for (var i = 0; i < specialTypes.Length; i++)
			{
				if (specialTypes[i] != specialType)
					continue;

				// Multiple matches
				if (match != null)
					return null;

				match = i;
			}
			return match;
		}
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> or a base type has an override of <see cref="Object.Equals(Object)"/> more specific than <see cref="Object"/>'s implementation.
	/// </summary>
	public static bool HasEqualsOverride(this ITypeSymbol typeSymbol)
	{
		// Technically this could match an overridden "new" Equals defined by a base type, but that is a nonsense scenario
		var result = typeSymbol.GetMembers(nameof(Object.Equals)).OfType<IMethodSymbol>().Any(method =>
			method.IsOverride && !method.IsStatic && method.Arity == 0 && method.Parameters.Length == 1 && method.Parameters[0].Type.SpecialType == SpecialType.System_Object);

		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static AttributeData? GetAttribute<TAttribute>(this ITypeSymbol typeSymbol)
	{
		var result = typeSymbol.GetAttribute(attribute => attribute.IsType<TAttribute>());
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static AttributeData? GetAttribute(this ITypeSymbol typeSymbol, string typeName, string containingNamespace, int? arity = null)
	{
		var result = typeSymbol.GetAttribute(attribute => (arity is null || attribute.Arity == arity) && attribute.IsTypeWithNamespace(typeName, containingNamespace));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static AttributeData? GetAttribute(this ITypeSymbol typeSymbol, Func<INamedTypeSymbol, bool> predicate)
	{
		var result = typeSymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass is not null && predicate(attribute.AttributeClass));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> defines a conversion to the specified type.
	/// </summary>
	public static bool HasConversionTo(this ITypeSymbol typeSymbol, SpecialType specialType)
	{
		var result = typeSymbol.SpecialType != specialType && typeSymbol.GetMembers().Any(member =>
			member is IMethodSymbol { Name: WellKnownMemberNames.ExplicitConversionName or WellKnownMemberNames.ImplicitConversionName, DeclaredAccessibility: Accessibility.Public, } method &&
			method.ReturnType.SpecialType == specialType);
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> defines a conversion from the specified type.
	/// </summary>
	public static bool HasConversionFrom(this ITypeSymbol typeSymbol, SpecialType specialType)
	{
		var result = typeSymbol.SpecialType != specialType && typeSymbol.GetMembers().Any(member =>
			member is IMethodSymbol { Name: WellKnownMemberNames.ExplicitConversionName or WellKnownMemberNames.ImplicitConversionName, DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1, } method &&
			method.Parameters[0].Type.SpecialType == specialType);
		return result;
	}

	/// <summary>
	/// Enumerates the primitive types (string, int, bool, etc.) from which the given <see cref="ITypeSymbol"/> is convertible.
	/// </summary>
	/// <param name="skipForSpecialTypes">If true, if the given type is itself a special type, this method yields nothing.</param>
	public static IEnumerable<(SpecialType, Type)> EnumerateAvailableConversionsFromPrimitives(this ITypeSymbol typeSymbol, bool skipForSpecialTypes)
	{
		if (skipForSpecialTypes && typeSymbol.SpecialType != SpecialType.None)
			yield break;

		if (typeSymbol.HasConversionFrom(SpecialType.System_String)) yield return (SpecialType.System_String, typeof(string));

		if (typeSymbol.HasConversionFrom(SpecialType.System_Boolean)) yield return (SpecialType.System_Boolean, typeof(bool));

		if (typeSymbol.HasConversionFrom(SpecialType.System_Byte)) yield return (SpecialType.System_Byte, typeof(byte));
		if (typeSymbol.HasConversionFrom(SpecialType.System_SByte)) yield return (SpecialType.System_SByte, typeof(sbyte));
		if (typeSymbol.HasConversionFrom(SpecialType.System_UInt16)) yield return (SpecialType.System_UInt16, typeof(ushort));
		if (typeSymbol.HasConversionFrom(SpecialType.System_Int16)) yield return (SpecialType.System_Int16, typeof(short));
		if (typeSymbol.HasConversionFrom(SpecialType.System_UInt32)) yield return (SpecialType.System_UInt32, typeof(uint));
		if (typeSymbol.HasConversionFrom(SpecialType.System_Int32)) yield return (SpecialType.System_Int32, typeof(int));
		if (typeSymbol.HasConversionFrom(SpecialType.System_UInt64)) yield return (SpecialType.System_UInt64, typeof(ulong));
		if (typeSymbol.HasConversionFrom(SpecialType.System_Int64)) yield return (SpecialType.System_Int64, typeof(long));
	}

	/// <summary>
	/// Returns the code for a ToString() expression of "this.Value".
	/// </summary>
	/// <param name="stringVariant">The expression to use for strings.</param>
	public static string CreateValueToStringExpression(this ITypeSymbol typeSymbol, string stringVariant = "this.Value")
	{
		return typeSymbol switch
		{
			{ SpecialType: SpecialType.System_String } => stringVariant,
			{ IsValueType: true } and not INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } => "this.Value.ToString()",
			_ => "this.Value?.ToString()", // Null-safety can be especially relevant for instances created with RuntimeHelpers.GetUninitializedObject()
		};
	}

	/// <summary>
	/// Returns the code for a ToString() expression of the given <paramref name="memberName"/> of "this".
	/// </summary>
	/// <param name="memberName">The member name. For example, "Value" leads to a string of "this.Value".</param>
	/// <param name="stringVariant">The expression to use for strings. Any {0} is replaced by the member name.</param>
	public static string CreateToStringExpression(this ITypeSymbol typeSymbol, string memberName, string stringVariant = "this.{0}")
	{
		return typeSymbol switch
		{
			{ IsValueType: true } and not INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } => $"this.{memberName}.ToString()",
			{ SpecialType: SpecialType.System_String } => String.Format(stringVariant, memberName),
			_ => $"this.{memberName}?.ToString()", // Null-safety can be especially relevant for instances created with RuntimeHelpers.GetUninitializedObject()
		};
	}

	/// <summary>
	/// Returns whether the sensible code for <see cref="Object.ToString"/> might return null for the given type, according to its annotations or lack thereof.
	/// </summary>
	public static bool IsToStringNullable(this ITypeSymbol typeSymbol)
	{
		if (typeSymbol.IsNullable()) return true;

		var nullableAnnotation = typeSymbol.SpecialType == SpecialType.System_String
			? typeSymbol.NullableAnnotation
			: typeSymbol.GetMembers(nameof(Object.ToString)).OfType<IMethodSymbol>().SingleOrDefault(method => !method.IsGenericMethod && method.Parameters.Length == 0)?.ReturnType.NullableAnnotation
				?? NullableAnnotation.None; // Could inspect base members, but that is going a bit far

		return nullableAnnotation != NullableAnnotation.NotAnnotated;
	}

	/// <summary>
	/// Returns the code for a hash code expression of the given <paramref name="memberName"/> of "this".
	/// </summary>
	/// <param name="memberName">The member name. For example, "Value" leads to a hash code of "this.Value".</param>
	/// <param name="stringVariant">The expression to use for strings. Any {0} is replaced by the member name.</param>
	public static string CreateHashCodeExpression(this ITypeSymbol typeSymbol, string memberName, string stringVariant = "(this.{0} is null ? 0 : String.GetHashCode(this.{0}))")
	{
		// DO NOT REORDER

		if (typeSymbol.SpecialType == SpecialType.System_String) return String.Format(stringVariant, memberName);

		var typeOrNullableUnderlying = typeSymbol.IsNullable(out var nullableUnderlyingType)
			? nullableUnderlyingType
			: typeSymbol;

		if (typeOrNullableUnderlying.IsSystemType("Memory", arity: 1)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";
		if (typeOrNullableUnderlying.IsSystemType("ReadOnlyMemory", arity: 1)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";

		// Special-case certain specific collections, provided that they have no custom equality
		if (!typeSymbol.HasEqualsOverride())
		{
			if (typeSymbol.IsSystemType("Dictionary", "Collections", "Generic", arity: 2)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(this.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("IDictionary", "Collections", "Generic", arity: 2), out var interf)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(({interf})this.{memberName})"; // Disambiguate
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("IReadOnlyDictionary", "Collections", "Generic", arity: 2), out _)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(this.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("ILookup", "Linq", arity: 2), out _)) return $"{ComparisonsNamespace}.LookupComparer.GetLookupHashCode(this.{memberName})";
		}

		// Special-case collections, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if ((!typeOrNullableUnderlying.HasEqualsOverride() || typeOrNullableUnderlying.IsOrImplementsInterface(type => type.IsSystemType("IStructuralEquatable", "Collections", arity: 0), out _)) &&
			typeOrNullableUnderlying.IsSpecificGenericEnumerable(out var elementType))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode<{elementType}>(this.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode(this.{memberName})";
		}

		if (typeSymbol.IsValueType && nullableUnderlyingType is null) return $"this.{memberName}.GetHashCode()";
		return $"(this.{memberName}?.GetHashCode() ?? 0)";
	}

	/// <summary>
	/// Returns the code for an equality expression on the given <paramref name="memberName"/> between "this" and "other".
	/// </summary>
	/// <param name="memberName">The member name. For example, "Value" leads to an equality check between "this.Value" and "other.Value".</param>
	/// <param name="stringVariant">The expression to use for strings. Any {0} is replaced by the member name.</param>
	public static string CreateEqualityExpression(this ITypeSymbol typeSymbol, string memberName, string stringVariant = "String.Equals(this.{0}, other.{0})")
	{
		// DO NOT REORDER

		// Not yet source-generated
		if (typeSymbol.TypeKind == TypeKind.Error) return $"{ComparisonsNamespace}.InferredTypeDefaultComparer.Equals(this.{memberName}, other.{memberName})";

		if (typeSymbol.SpecialType == SpecialType.System_String) return String.Format(stringVariant, memberName);

		if (typeSymbol.IsSystemType("Memory", arity: 1)) return $"MemoryExtensions.SequenceEqual(this.{memberName}.Span, other.{memberName}.Span)";
		if (typeSymbol.IsSystemType("ReadOnlyMemory", arity: 1)) return $"MemoryExtensions.SequenceEqual(this.{memberName}.Span, other.{memberName}.Span)";
		if (typeSymbol.IsNullable(out var underlyingType) && underlyingType.IsSystemType("Memory", arity: 1)) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : MemoryExtensions.SequenceEqual(this.{memberName}.Value.Span, other.{memberName}.Value.Span))";
		if (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsSystemType("ReadOnlyMemory", arity: 1)) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : MemoryExtensions.SequenceEqual(this.{memberName}.Value.Span, other.{memberName}.Value.Span))";

		// Special-case certain specific collections, provided that they have no custom equality
		if (!typeSymbol.HasEqualsOverride())
		{
			if (typeSymbol.IsSystemType("Dictionary", "Collections", "Generic", arity: 2))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("IDictionary", "Collections", "Generic", arity: 2), out var interf))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("IReadOnlyDictionary", "Collections", "Generic", arity: 2), out interf))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsSystemType("ILookup", "Linq", arity: 2), out interf))
				return $"{ComparisonsNamespace}.LookupComparer.LookupEquals(this.{memberName}, other.{memberName})";
		}

		var typeOrNullableUnderlying = typeSymbol.IsNullable(out var nullableUnderlyingType)
			? nullableUnderlyingType
			: typeSymbol;

		// Special-case collections, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if ((!typeOrNullableUnderlying.HasEqualsOverride() || typeOrNullableUnderlying.IsOrImplementsInterface(type => type.IsSystemType("IStructuralEquatable", "Collections", arity: 0), out _)) &&
			typeOrNullableUnderlying.IsSpecificGenericEnumerable(out var elementType))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals<{elementType}>(this.{memberName}, other.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals(this.{memberName}, other.{memberName})";
		}

		if (nullableUnderlyingType is not null) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : this.{memberName}.Value.Equals(other.{memberName}.Value))";
		if (typeSymbol.IsValueType) return $"this.{memberName}.Equals(other.{memberName})";
		return $"(this.{memberName}?.Equals(other.{memberName}) ?? other.{memberName} is null)";
	}

	/// <summary>
	/// Returns the code for a comparison expression on the given <paramref name="memberName"/> between "this" and "other".
	/// </summary>
	/// <param name="memberName">The member name. For example, "Value" leads to a comparison between "this.Value" and "other.Value".</param>
	/// <param name="stringVariant">The expression to use for strings. Any {0} is replaced by the member name.</param>
	public static string CreateComparisonExpression(this ITypeSymbol typeSymbol, string memberName, string stringVariant = "String.Compare(this.{0}, other.{0}, StringComparison.Ordinal)")
	{
		// DO NOT REORDER

		// Not yet source-generated
		if (typeSymbol.TypeKind == TypeKind.Error) return $"{ComparisonsNamespace}.InferredTypeDefaultComparer.Compare(this.{memberName}, other.{memberName})";

		// Collections have not been implemented, as we do not generate CompareTo() if any data member is not IComparable (as is the case for collections)

		if (typeSymbol.SpecialType == SpecialType.System_String) return String.Format(stringVariant, memberName);
		if (typeSymbol.IsNullable()) return $"(this.{memberName} is null || other.{memberName} is null ? -(this.{memberName} is null).CompareTo(other.{memberName} is null) : this.{memberName}.Value.CompareTo(other.{memberName}.Value))";
		if (typeSymbol.IsValueType) return $"this.{memberName}.CompareTo(other.{memberName})";
		return $"(this.{memberName} is null || other.{memberName} is null ? -(this.{memberName} is null).CompareTo(other.{memberName} is null) : this.{memberName}.CompareTo(other.{memberName}))";
	}

	/// <summary>
	/// Returns the code for an expression that instantiates a dummy instance of the specified type.
	/// </summary>
	/// <param name="symbolName">The name of the member/parameter/... to instantiate an instance for. May be used as the dummy string value if applicable.</param>
	public static string CreateDummyInstantiationExpression(this ITypeSymbol typeSymbol, string symbolName)
	{
		return typeSymbol.CreateDummyInstantiationExpression(symbolName, [], _ => null!);
	}

	/// <summary>
	/// Returns the code for an expression that instantiates a dummy instance of the specified type.
	/// </summary>
	/// <param name="symbolName">The name of the member/parameter/... to instantiate an instance for. May be used as the dummy string value if applicable.</param>
	/// <param name="customizedTypes">Encountered types that match one of these are instead instantiated by the expression resulting from <paramref name="createCustomTypeExpression"/>.</param>
	/// <param name="createCustomTypeExpression">Returns an instantiation expression for a given <see cref="ITypeSymbol"/> that is present in <paramref name="customizedTypes"/>.</param>
	public static string CreateDummyInstantiationExpression(this ITypeSymbol typeSymbol, string symbolName,
		IEnumerable<ITypeSymbol> customizedTypes, Func<ITypeSymbol, string> createCustomTypeExpression)
	{
		return CreateDummyInstantiationExpression(typeSymbol, symbolName, customizedTypes, createCustomTypeExpression,
			seenTypeSymbols: new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
	}

	private static string CreateDummyInstantiationExpression(this ITypeSymbol typeSymbol, string symbolName,
		IEnumerable<ITypeSymbol> customizedTypes, Func<ITypeSymbol, string> createCustomTypeExpression,
		HashSet<ITypeSymbol> seenTypeSymbols)
	{
		if (typeSymbol.IsNullable(out var nullabeUnderlyingType))
			typeSymbol = nullabeUnderlyingType;

		// Avoid stack overflow due to recursion
		if (!seenTypeSymbols.Add(typeSymbol)) return typeSymbol.IsReferenceType ? "null" : $"default({typeSymbol})";

		try
		{
			customizedTypes = customizedTypes as IReadOnlyCollection<ITypeSymbol> ?? customizedTypes.ToList();

			if (customizedTypes.Any(type => type.Equals(typeSymbol, SymbolEqualityComparer.Default))) return createCustomTypeExpression(typeSymbol);

			// Special-case wrapper value objects to use the param name rather than the type name (e.g. "FirstName" and "LastName" instead of "ProperName" and "ProperName")
			// As a bonus, this also handles constructors generated by this very package (which are not visible to us)
			if ((typeSymbol.GetAttribute("WrapperValueObjectAttribute", "Architect.DomainModeling", arity: 1) ??
				typeSymbol.GetAttribute("IdentityValueObjectAttribute", "Architect.DomainModeling", arity: 1))
				is AttributeData wrapperAttribute)
			{
				return $"new {typeSymbol.WithNullableAnnotation(NullableAnnotation.None)}({wrapperAttribute.AttributeClass!.TypeArguments[0].CreateDummyInstantiationExpression(symbolName, customizedTypes, createCustomTypeExpression, seenTypeSymbols)})";
			}

			if (typeSymbol.SpecialType == SpecialType.System_String) return $@"""{symbolName.ToTitleCase()}""";
			if (typeSymbol.SpecialType == SpecialType.System_Char) return "'1'";
			if (typeSymbol.SpecialType == SpecialType.System_Decimal) return "1m";
			if (typeSymbol.SpecialType == SpecialType.System_DateTime) return "new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc)";
			if (typeSymbol.IsSystemType("DateTimeOffset")) return "new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc)";
			if (typeSymbol.IsSystemType("DateOnly")) return "new DateOnly(2000, 01, 01)";
			if (typeSymbol.IsSystemType("TimeOnly")) return "new TimeOnly(01, 00, 00)";
			if (typeSymbol.TypeKind == TypeKind.Enum) return typeSymbol.GetMembers().OfType<IFieldSymbol>().Any() ? $"{typeSymbol}.{typeSymbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault()!.Name}" : $"default({typeSymbol})";
			if (typeSymbol.TypeKind == TypeKind.Array) return $"new[] {{ {((IArrayTypeSymbol)typeSymbol).ElementType.CreateDummyInstantiationExpression($"{symbolName}Element", customizedTypes, createCustomTypeExpression, seenTypeSymbols)} }}";
			if (typeSymbol.IsPrimitiveIntegral(seeThroughNullable: false) || typeSymbol.IsSystemType("UInt128") || typeSymbol.IsSystemType("Int128") || typeSymbol.IsSystemType("BigInteger", "Numerics")) return $"({typeSymbol})1";
			if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return typeSymbol.IsReferenceType ? "null" : $"default({typeSymbol})";

			var suitableCtor = namedTypeSymbol.Constructors
				.Where(ctor => ctor.Parameters.Length > 0)
				.OrderByDescending(ctor => ctor.DeclaredAccessibility) // Most accessible first
				.ThenBy(ctor => ctor.Parameters.Length) // Shortest first (the most basic non-default option)
				.FirstOrDefault();

			if (suitableCtor is null) return typeSymbol.IsReferenceType ? "null" : $"default({typeSymbol})";

			// TODO Enhancement: We could use an object initializer if there are accessible setters

			// For objects taking a parameter named "value", instead prefer the name of the outer constructor's parameter
			var parameters = String.Join(", ", suitableCtor.Parameters.Select(param => param.Type.CreateDummyInstantiationExpression(param.Name == "value" ? symbolName : param.Name, customizedTypes, createCustomTypeExpression, seenTypeSymbols)));
			return $"new {typeSymbol.WithNullableAnnotation(NullableAnnotation.None)}({parameters})";
		}
		finally
		{
			seenTypeSymbols.Remove(typeSymbol);
		}
	}
}
