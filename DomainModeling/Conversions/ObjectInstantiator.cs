using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// Instantiates objects of arbitrary types.
/// </summary>
internal static class ObjectInstantiator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>
{
	private static readonly Func<T> ConstructionFunction;

	static ObjectInstantiator()
	{
		if (typeof(T).IsValueType)
		{
			ConstructionFunction = () => default!;
		}
		else if (typeof(T).IsAbstract || typeof(T).IsInterface || typeof(T).IsGenericTypeDefinition)
		{
			ConstructionFunction = () => throw new NotSupportedException("Uninitialized instantiation of abstract, interface, or unbound generic types is not supported.");
		}
		else if (typeof(T) == typeof(string) || typeof(T).IsArray)
		{
			ConstructionFunction = () => throw new NotSupportedException("Uninitialized instantiation of arrays and strings is not supported.");
		}
		else if (typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, Array.Empty<Type>(), modifiers: null) is ConstructorInfo ctor)
		{
#if NET8_0_OR_GREATER
			var invoker = ConstructorInvoker.Create(ctor);
			ConstructionFunction = () => (T)invoker.Invoke();
#else
			ConstructionFunction = () => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
#endif
		}
		else
		{
			ConstructionFunction = () => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
		}
	}

	/// <summary>
	/// <para>
	/// Instantiates an instance of type <typeparamref name="T"/>, using its default constructor if one is available, or by producing an uninitialized object otherwise.
	/// </para>
	/// <para>
	/// Throws a <see cref="NotSupportedException"/> for arrays, strings, and unbound generic types.
	/// </para>
	/// </summary>
	public static T Instantiate()
	{
		return ConstructionFunction();
	}
}
