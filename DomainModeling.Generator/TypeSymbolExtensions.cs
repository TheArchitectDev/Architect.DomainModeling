using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Provides extensions on <see cref="ITypeSymbol"/>.
/// </summary>
internal static class TypeSymbolExtensions
{
	private const string ComparisonsNamespace = "Architect.DomainModeling.Comparisons";

	private static IReadOnlyCollection<string> ConversionOperatorNames { get; } = new[]
	{
		"op_Implicit", "op_Explicit",
	};

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

		if (!IsType(typeSymbol, type.Name, type.Namespace)) return false;

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

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> has the given <paramref name="fullTypeName"/>.
	/// </summary>
	/// <param name="fullTypeName">The type name including the namespace, e.g. System.Object.</param>
	public static bool IsType(this ITypeSymbol typeSymbol, string fullTypeName, bool? generic = null)
	{
		var fullTypeNameSpan = fullTypeName.AsSpan();

		var lastDotIndex = fullTypeNameSpan.LastIndexOf('.');

		if (lastDotIndex < 1) return false;

		var typeName = fullTypeNameSpan.Slice(1 + lastDotIndex);
		var containingNamespace = fullTypeNameSpan.Slice(0, lastDotIndex);

		return IsType(typeSymbol, typeName, containingNamespace, generic);
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> has the given <paramref name="typeName"/> and <paramref name="containingNamespace"/>.
	/// </summary>
	public static bool IsType(this ITypeSymbol typeSymbol, string typeName, string containingNamespace, bool? generic = null)
	{
		return IsType(typeSymbol, typeName.AsSpan(), containingNamespace.AsSpan(), generic);
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> has the given <paramref name="typeName"/> and <paramref name="containingNamespace"/>.
	/// </summary>
	/// <param name="generic">If not null, the being-generic of the type must match this value.</param>
	private static bool IsType(this ITypeSymbol typeSymbol, ReadOnlySpan<char> typeName, ReadOnlySpan<char> containingNamespace, bool? generic = null)
	{
		var result = typeSymbol.Name.AsSpan().Equals(typeName, StringComparison.Ordinal) &&
			typeSymbol.ContainingNamespace.HasFullName(containingNamespace);

		if (result && generic is not null)
			result = typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType == generic.Value;

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
			if (baseType.IsType<object>())
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
	/// Returns whether the <see cref="ITypeSymbol"/> is a constructed generic type with a single type argument matching the <paramref name="requiredTypeArgument"/>.
	/// </summary>
	public static bool HasSingleGenericTypeArgument(this ITypeSymbol typeSymbol, ITypeSymbol requiredTypeArgument)
	{
		return typeSymbol is INamedTypeSymbol namedTypeSymbol &&
			namedTypeSymbol.TypeArguments.Length == 1 &&
			namedTypeSymbol.TypeArguments[0].Equals(requiredTypeArgument, SymbolEqualityComparer.Default);
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> represents an integral type, such as <see cref="Int32"/> or <see cref="UInt64"/>.
	/// </summary>
	/// <param name="seeThroughNullable">Whether to return true for a <see cref="Nullable{T}"/> of a matching underlying type.</param>
	/// <param name="includeDecimal">Whether to consider <see cref="Decimal"/> as an integral type.</param>
	public static bool IsIntegral(this ITypeSymbol typeSymbol, bool seeThroughNullable, bool includeDecimal = false)
	{
		if (typeSymbol.IsNullable(out var underlyingType) && seeThroughNullable)
			typeSymbol = underlyingType;

		var result = typeSymbol.IsType<byte>() ||
			typeSymbol.IsType<sbyte>() ||
			typeSymbol.IsType<ushort>() ||
			typeSymbol.IsType<short>() ||
			typeSymbol.IsType<uint>() ||
			typeSymbol.IsType<int>() ||
			typeSymbol.IsType<ulong>() ||
			typeSymbol.IsType<long>() ||
			(includeDecimal && typeSymbol.IsType<decimal>());

		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a nested type.
	/// </summary>
	public static bool IsNested(this ITypeSymbol typeSymbol)
	{
		var result = typeSymbol.ContainingType is not null;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a generic type with the given number of type parameters.
	/// </summary>
	public static bool IsGeneric(this ITypeSymbol typeSymbol, int typeParameterCount)
	{
		if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return false;

		var result = namedTypeSymbol.IsGenericType && namedTypeSymbol.Arity == typeParameterCount;
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a generic type with the given number of type parameters.
	/// Outputs the type arguments on true.
	/// </summary>
	public static bool IsGeneric(this ITypeSymbol typeSymbol, int typeParameterCount, out ImmutableArray<ITypeSymbol> typeArguments)
	{
		typeArguments = default;

		if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return false;

		if (!IsGeneric(typeSymbol, typeParameterCount)) return false;

		typeArguments = namedTypeSymbol.TypeArguments;
		return true;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a <see cref="Nullable{T}"/>.
	/// </summary>
	public static bool IsNullable(this ITypeSymbol typeSymbol)
	{
		return typeSymbol.IsNullable(out _);
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is a <see cref="Nullable{T}"/>, outputting the underlying type if so.
	/// </summary>
	public static bool IsNullable(this ITypeSymbol typeSymbol, out ITypeSymbol underlyingType)
	{
		if (typeSymbol is INamedTypeSymbol namedTypeSymbol && typeSymbol.IsType("System.Nullable", generic: true))
		{
			underlyingType = namedTypeSymbol.TypeArguments[0];
			return true;
		}

		underlyingType = null!;
		return false;
	}

	/// <summary>
	/// Returns whether the given <see cref="ITypeSymbol"/> implements <see cref="IEquatable{T}"/> against itself.
	/// </summary>
	public static bool IsSelfEquatable(this ITypeSymbol typeSymbol)
	{
		return typeSymbol.IsOrImplementsInterface(interf => interf.IsType("IEquatable", "System", generic: true) && interf.HasSingleGenericTypeArgument(typeSymbol), out _);
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

		var result = typeSymbol.AllInterfaces.Any(interf => interf.IsType("System.IComparable"));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is or implements <see cref="System.Collections.IEnumerable"/>.
	/// If so, this method outputs the element type of the most <em>concrete</em> <see cref="IEnumerable{T}"/> type it implements, if any.
	/// </summary>
	public static bool IsEnumerable(this ITypeSymbol typeSymbol, out INamedTypeSymbol? elementType)
	{
		elementType = null;

		if (!typeSymbol.IsOrImplementsInterface(type => type.IsType("IEnumerable", "System.Collections", generic: false), out var nonGenericEnumerableInterface))
			return false;

		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IList", "System.Collections.Generic", generic: true), out var interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IReadOnlyList", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("ISet", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IReadOnlySet", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("ICollection", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IReadOnlyCollection", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}
		if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IEnumerable", "System.Collections.Generic", generic: true), out interf))
		{
			elementType = ((INamedTypeSymbol)interf).TypeArguments[0] as INamedTypeSymbol;
			return true;
		}

		return true;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> or a base type has an override of <see cref="Object.Equals(Object)"/> more specific than <see cref="Object"/>'s implementation.
	/// </summary>
	public static bool HasEqualsOverride(this ITypeSymbol typeSymbol, bool falseForStructs = false)
	{
		// Technically this could match an overridden "new" Equals defined by a base type, but that is a nonsense scenario
		var result = typeSymbol.GetMembers(nameof(Object.Equals)).OfType<IMethodSymbol>().Any(method => method.IsOverride && !method.IsStatic &&
			method.Arity == 0 && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<object>());

		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static bool HasAttribute<TAttribute>(this ITypeSymbol typeSymbol)
	{
		var result = typeSymbol.HasAttribute(attribute => attribute.IsType<TAttribute>());
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static bool HasAttribute(this ITypeSymbol typeSymbol, string typeName, string containingNamespace)
	{
		var result = typeSymbol.HasAttribute(attribute => attribute.IsType(typeName, containingNamespace));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> is annotated with the specified attribute.
	/// </summary>
	public static bool HasAttribute(this ITypeSymbol typeSymbol, Func<INamedTypeSymbol, bool> predicate)
	{
		var result = typeSymbol.GetAttributes().Any(attribute => attribute.AttributeClass is not null && predicate(attribute.AttributeClass));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> defines a conversion to the specified type.
	/// </summary>
	public static bool HasConversionTo(this ITypeSymbol typeSymbol, string typeName, string containingNamespace)
	{
		var result = !typeSymbol.IsType(typeName, containingNamespace) && typeSymbol.GetMembers().Any(member =>
			member is IMethodSymbol method && ConversionOperatorNames.Contains(method.Name) && member.DeclaredAccessibility == Accessibility.Public &&
			method.ReturnType.IsType(typeName, containingNamespace));
		return result;
	}

	/// <summary>
	/// Returns whether the <see cref="ITypeSymbol"/> defines a conversion from the specified type.
	/// </summary>
	public static bool HasConversionFrom(this ITypeSymbol typeSymbol, string typeName, string containingNamespace)
	{
		var result = !typeSymbol.IsType(typeName, containingNamespace) && typeSymbol.GetMembers().Any(member =>
			member is IMethodSymbol method && ConversionOperatorNames.Contains(method.Name) && member.DeclaredAccessibility == Accessibility.Public &&
			method.Parameters.Length == 1 && method.Parameters[0].Type.IsType(typeName, containingNamespace));
		return result;
	}

	/// <summary>
	/// Enumerates the primitive types (string, int, bool, etc.) from which the given <see cref="ITypeSymbol"/> is convertible.
	/// </summary>
	/// <param name="skipForSystemTypes">If true, if the given type is directly under the System namespace, this method yields nothing.</param>
	public static IEnumerable<Type> GetAvailableConversionsFromPrimitives(this ITypeSymbol typeSymbol, bool skipForSystemTypes)
	{
		if (skipForSystemTypes && typeSymbol.ContainingNamespace.Name == "System" && (typeSymbol.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace ?? true))
			yield break;

		if (typeSymbol.HasConversionFrom("String", "System")) yield return typeof(string);

		if (typeSymbol.HasConversionFrom("Boolean", "System")) yield return typeof(bool);

		if (typeSymbol.HasConversionFrom("Byte", "System")) yield return typeof(byte);
		if (typeSymbol.HasConversionFrom("SByte", "System")) yield return typeof(sbyte);
		if (typeSymbol.HasConversionFrom("UInt16", "System")) yield return typeof(ushort);
		if (typeSymbol.HasConversionFrom("Int16", "System")) yield return typeof(short);
		if (typeSymbol.HasConversionFrom("UInt32", "System")) yield return typeof(uint);
		if (typeSymbol.HasConversionFrom("Int32", "System")) yield return typeof(int);
		if (typeSymbol.HasConversionFrom("UInt64", "System")) yield return typeof(ulong);
		if (typeSymbol.HasConversionFrom("Int64", "System")) yield return typeof(long);
	}

	/// <summary>
	/// Returns the code for a string expression of the given <paramref name="memberName"/> of "this".
	/// </summary>
	/// <param name="memberName">The member name. For example, "Value" leads to a string of "this.Value".</param>
	/// <param name="stringVariant">The expression to use for strings. Any {0} is replaced by the member name.</param>
	public static string CreateStringExpression(this ITypeSymbol typeSymbol, string memberName, string stringVariant = "this.{0}")
	{
		if (typeSymbol.IsValueType && !typeSymbol.IsNullable()) return $"this.{memberName}.ToString()";
		if (typeSymbol.IsType<string>()) return String.Format(stringVariant, memberName);
		return $"this.{memberName}?.ToString()";
	}

	/// <summary>
	/// Returns whether the sensible code for <see cref="Object.ToString"/> might return null for the given type, according to its annotations or lack thereof.
	/// </summary>
	public static bool IsToStringNullable(this ITypeSymbol typeSymbol)
	{
		if (typeSymbol.IsNullable()) return true;

		var nullableAnnotation = typeSymbol.IsType<string>()
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

		if (typeSymbol.IsType<string>()) return String.Format(stringVariant, memberName);

		if (typeSymbol.IsType("Memory", "System", generic: true)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";
		if (typeSymbol.IsType("ReadOnlyMemory", "System", generic: true)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";
		if (typeSymbol.IsNullable(out var underlyingType) && underlyingType.IsType("Memory", "System", generic: true)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";
		if (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType("ReadOnlyMemory", "System", generic: true)) return $"{ComparisonsNamespace}.EnumerableComparer.GetMemoryHashCode(this.{memberName})";

		// Special-case certain specific collections, provided that they have no custom equality
		if (!typeSymbol.HasEqualsOverride())
		{
			if (typeSymbol.IsType("Dictionary", "System.Collections.Generic", generic: true)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(this.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IDictionary", "System.Collections.Generic", generic: true), out var interf)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(({interf})this.{memberName})"; // Disambiguate
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IReadOnlyDictionary", "System.Collections.Generic", generic: true), out _)) return $"{ComparisonsNamespace}.DictionaryComparer.GetDictionaryHashCode(this.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("ILookup", "System.Linq", generic: true), out _)) return $"{ComparisonsNamespace}.LookupComparer.GetLookupHashCode(this.{memberName})";
		}

		// Special-case collections, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if (typeSymbol.IsEnumerable(out var elementType) &&
			(!typeSymbol.HasEqualsOverride() || typeSymbol.IsOrImplementsInterface(type => type.IsType("IStructuralEquatable", "System.Collections", generic: false), out _)))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode<{elementType}>(this.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode(this.{memberName})";
		}

		// Special-case collections wrapped in nullable, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsEnumerable(out elementType) &&
			(!underlyingType.HasEqualsOverride() || underlyingType.IsOrImplementsInterface(type => type.IsType("IStructuralEquatable", "System.Collections", generic: false), out _)))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode<{elementType}>(this.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.GetEnumerableHashCode(this.{memberName})";
		}

		if (typeSymbol.IsValueType && !typeSymbol.IsNullable()) return $"this.{memberName}.GetHashCode()";
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
		if (typeSymbol.TypeKind == TypeKind.Error) return $"Equals(this.{memberName}, other.{memberName})";

		if (typeSymbol.IsType<string>()) return String.Format(stringVariant, memberName);

		if (typeSymbol.IsType("Memory", "System", generic: true)) return $"MemoryExtensions.SequenceEqual(this.{memberName}.Span, other.{memberName}.Span)";
		if (typeSymbol.IsType("ReadOnlyMemory", "System", generic: true)) return $"MemoryExtensions.SequenceEqual(this.{memberName}.Span, other.{memberName}.Span)";
		if (typeSymbol.IsNullable(out var underlyingType) && underlyingType.IsType("Memory", "System", generic: true)) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : MemoryExtensions.SequenceEqual(this.{memberName}.Value.Span, other.{memberName}.Value.Span))";
		if (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType("ReadOnlyMemory", "System", generic: true)) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : MemoryExtensions.SequenceEqual(this.{memberName}.Value.Span, other.{memberName}.Value.Span))";

		// Special-case certain specific collections, provided that they have no custom equality
		if (!typeSymbol.HasEqualsOverride())
		{
			if (typeSymbol.IsType("Dictionary", "System.Collections.Generic", generic: true))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IDictionary", "System.Collections.Generic", generic: true), out var interf))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("IReadOnlyDictionary", "System.Collections.Generic", generic: true), out interf))
				return $"{ComparisonsNamespace}.DictionaryComparer.DictionaryEquals(this.{memberName}, other.{memberName})";
			if (typeSymbol.IsOrImplementsInterface(type => type.IsType("ILookup", "System.Linq", generic: true), out interf))
				return $"{ComparisonsNamespace}.LookupComparer.LookupEquals(this.{memberName}, other.{memberName})";
		}

		// Special-case collections, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if (typeSymbol.IsEnumerable(out var elementType) &&
			(!typeSymbol.HasEqualsOverride() || typeSymbol.IsOrImplementsInterface(type => type.IsType("IStructuralEquatable", "System.Collections", generic: false), out _)))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals<{elementType}>(this.{memberName}, other.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals(this.{memberName}, other.{memberName})";
		}

		// Special-case collections wrapped in nullable, provided that they either (A) have no custom equality or (B) implement IStructuralEquatable (where the latter tend to override regular Equals() with explicit reference equality)
		if (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsEnumerable(out elementType) &&
			(!underlyingType.HasEqualsOverride() || underlyingType.IsOrImplementsInterface(type => type.IsType("IStructuralEquatable", "System.Collections", generic: false), out _)))
		{
			if (elementType is not null) return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals<{elementType}>(this.{memberName}, other.{memberName})";
			else return $"{ComparisonsNamespace}.EnumerableComparer.EnumerableEquals(this.{memberName}, other.{memberName})";
		}

		if (typeSymbol.IsNullable()) return $"(this.{memberName} is null || other.{memberName} is null ? this.{memberName} is null & other.{memberName} is null : this.{memberName}.Value.Equals(other.{memberName}.Value))";
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
		if (typeSymbol.TypeKind == TypeKind.Error) return $"Compare(this.{memberName}, other.{memberName})";

		// Collections have not been implemented, as we do not generate CompareTo() if any data member is not IComparable (as is the case for collections)

		if (typeSymbol.IsType<string>()) return String.Format(stringVariant, memberName);
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
		return typeSymbol.CreateDummyInstantiationExpression(symbolName, Array.Empty<ITypeSymbol>(), _ => null!);
	}

	/// <summary>
	/// Returns the code for an expression that instantiates a dummy instance of the specified type.
	/// </summary>
	/// <param name="symbolName">The name of the member/parameter/... to instantiate an instance for. May be used as the dummy string value if applicable.</param>
	/// <param name="customizedTypes">Encountered types that match one of these are instead instantiated by the expression resulting from <paramref name="createCustomTypeExpression"/>.</param>
	/// <param name="createCustomTypeExpression">Returns an instantiation expression for a given <see cref="ITypeSymbol"/>, which is present in <paramref name="customizedTypes"/>.</param>
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

			// Use constructors being generated by this very package (which are not revealed to us)
			{
				if (typeSymbol.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
				{
					if (typeSymbol.IsOrInheritsClass(type => type.IsType(Constants.WrapperValueObjectTypeName, Constants.DomainModelingNamespace), out var wrapperValueObjectType))
						return $"new {typeSymbol.WithNullableAnnotation(NullableAnnotation.None)}({((INamedTypeSymbol)wrapperValueObjectType).TypeArguments[0].CreateDummyInstantiationExpression(typeSymbol.Name, customizedTypes, createCustomTypeExpression, seenTypeSymbols)})";
					if (typeSymbol.IsOrImplementsInterface(interf => interf.IsType(Constants.IdentityInterfaceTypeName, Constants.DomainModelingNamespace), out var wrapperValueObjectInterface))
						return $"new {typeSymbol.WithNullableAnnotation(NullableAnnotation.None)}({((INamedTypeSymbol)wrapperValueObjectInterface).TypeArguments[0].CreateDummyInstantiationExpression(typeSymbol.Name, customizedTypes, createCustomTypeExpression, seenTypeSymbols)})";
				}
			}

			if (typeSymbol.IsType<string>()) return $@"""{symbolName.ToTitleCase()}""";
			if (typeSymbol.IsType<decimal>() || (typeSymbol.IsNullable(out var underlyingType) && underlyingType.IsType<decimal>())) return $"1m";
			if (typeSymbol.IsType<DateTime>() || (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType<DateTime>())) return $"new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc)";
			if (typeSymbol.IsType<DateTimeOffset>() || (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType<DateTimeOffset>())) return $"new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc)";
			if (typeSymbol.IsType("DateOnly", "System") || (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType("DateOnly", "System"))) return $"new DateOnly(2000, 01, 01)";
			if (typeSymbol.IsType("TimeOnly", "System") || (typeSymbol.IsNullable(out underlyingType) && underlyingType.IsType("TimeOnly", "System"))) return $"new TimeOnly(01, 00, 00)";
			if (typeSymbol.TypeKind == TypeKind.Enum) return typeSymbol.GetMembers().OfType<IFieldSymbol>().Any() ? $"{typeSymbol}.{typeSymbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault()!.Name}" : $"default({typeSymbol})";
			if (typeSymbol.TypeKind == TypeKind.Array) return $"new[] {{ {((IArrayTypeSymbol)typeSymbol).ElementType.CreateDummyInstantiationExpression($"{symbolName}Element", customizedTypes, createCustomTypeExpression, seenTypeSymbols)} }}";
			if (typeSymbol.IsIntegral(seeThroughNullable: true, includeDecimal: true)) return $"({typeSymbol})1";
			if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return typeSymbol.IsReferenceType ? "null" : $"default({typeSymbol})";

			var suitableCtor = namedTypeSymbol.Constructors
				.Where(ctor => ctor.Parameters.Length > 0)
				.OrderByDescending(ctor => ctor.DeclaredAccessibility) // Most accessible first
				.ThenBy(ctor => ctor.Parameters.Length) // Shortest first (the most basic non-default option)
				.FirstOrDefault();

			if (suitableCtor is null) return typeSymbol.IsReferenceType ? "null" : $"default({typeSymbol})";

			// TODO Enhancement: We could use an object initializer if there are accessible setters

			var parameters = String.Join(", ", suitableCtor.Parameters.Select(param => param.Type.CreateDummyInstantiationExpression(param.Name == "value" ? param.ContainingType.Name : param.Name, customizedTypes, createCustomTypeExpression, seenTypeSymbols)));
			return $"new {typeSymbol.WithNullableAnnotation(NullableAnnotation.None)}({parameters})";
		}
		finally
		{
			seenTypeSymbols.Remove(typeSymbol);
		}
	}
}
