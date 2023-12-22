#if NET7_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Architect.DomainModeling.Conversions;

public static class DomainObjectSerializer
{
	private static readonly MethodInfo GenericDeserializeMethod = typeof(DomainObjectSerializer).GetMethods().Single(method =>
		method.Name == nameof(Deserialize) && method.GetParameters() is []);
	private static readonly MethodInfo GenericDeserializeFromValueMethod = typeof(DomainObjectSerializer).GetMethods().Single(method =>
		method.Name == nameof(Deserialize) && method.GetParameters().Length == 1);
	private static readonly MethodInfo GenericSerializeMethod = typeof(DomainObjectSerializer).GetMethods().Single(method =>
		method.Name == nameof(Serialize) && method.GetParameters().Length == 1);

	#region Deserialize empty

	/// <summary>
	/// Deserializes an empty, uninitialized instance of type <typeparamref name="TModel"/>.
	/// </summary>
	public static TModel Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel>()
		where TModel : IDomainObject
	{
		if (typeof(TModel).IsValueType)
			return default!;

		return ObjectInstantiator<TModel>.Instantiate();
	}

	/// <summary>
	/// <para>
	/// Creates an expression of a call to <see cref="Deserialize{TModel}()"/>.
	/// </para>
	/// <para>
	/// When evaluated, the expression deserializes an empty, uninitialized instance of the <paramref name="modelType"/>.
	/// </para>
	/// </summary>
	public static Expression CreateDeserializeExpression([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type modelType)
	{
		var method = GenericDeserializeMethod.MakeGenericMethod(modelType);
		var result = Expression.Call(method);
		return result;
	}

	/// <summary>
	/// <para>
	/// Creates a lambda expression that calls <see cref="Deserialize{TModel}()"/>.
	/// </para>
	/// <para>
	/// The result deserializes an empty, uninitialized instance of type <typeparamref name="TModel"/>.
	/// </para>
	/// <para>
	/// To obtain a delegate, call <see cref="Expression{TDelegate}.Compile()"/> on the result.
	/// </para>
	/// </summary>
	public static Expression<Func<TModel>> CreateDeserializeExpression<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel>()
		where TModel : IDomainObject
	{
		var call = CreateDeserializeExpression(typeof(TModel));
		var lambda = Expression.Lambda<Func<TModel>>(call);
		return lambda;
	}

	#endregion

	#region Deserialize from value

	/// <summary>
	/// Deserializes a <typeparamref name="TModel"/> from a <typeparamref name="TUnderlying"/>.
	/// </summary>
	[return: NotNullIfNotNull(nameof(value))]
	public static TModel? Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TUnderlying>(
		TUnderlying? value)
		where TModel : ISerializableDomainObject<TModel, TUnderlying>
	{
		return value is null
			? default
			: TModel.Deserialize(value);
	}

	/// <summary>
	/// <para>
	/// Creates an expression of a call to <see cref="Deserialize{TModel, TUnderlying}(TUnderlying)"/>.
	/// </para>
	/// <para>
	/// When evaluated, the result deserializes an instance of the <paramref name="modelType"/> from a given instance of the <paramref name="underlyingType"/>.
	/// </para>
	/// </summary>
	public static Expression CreateDeserializeExpression([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type modelType, Type underlyingType)
	{
		var result = CreateDeserializeExpressionCore(modelType, underlyingType, out _);
		return result;
	}

	/// <summary>
	/// <para>
	/// Creates a lambda expression that calls <see cref="Deserialize{TModel, TUnderlying}(TUnderlying)"/>.
	/// </para>
	/// <para>
	/// The result deserializes a <typeparamref name="TModel"/> from a given <typeparamref name="TUnderlying"/>.
	/// </para>
	/// <para>
	/// To obtain a delegate, call <see cref="Expression{TDelegate}.Compile()"/> on the result.
	/// </para>
	/// </summary>
	public static Expression<Func<TUnderlying, TModel>> CreateDeserializeExpression<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TUnderlying>()
		where TModel : ISerializableDomainObject<TModel, TUnderlying>
	{
		var call = CreateDeserializeExpressionCore(typeof(TModel), typeof(TUnderlying), out var parameter);
		var lambda = Expression.Lambda<Func<TUnderlying, TModel>>(call, parameter);
		return lambda;
	}

	private static MethodCallExpression CreateDeserializeExpressionCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type modelType, Type underlyingType,
		out ParameterExpression parameter)
	{
		var method = GenericDeserializeFromValueMethod.MakeGenericMethod(modelType, underlyingType);
		parameter = Expression.Parameter(underlyingType, "value");
		var result = Expression.Call(method, parameter);
		return result;
	}

	#endregion

	#region Serialize

	/// <summary>
	/// Serializes a <typeparamref name="TModel"/> as a <typeparamref name="TUnderlying"/>.
	/// </summary>
	public static TUnderlying? Serialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TUnderlying>(
		TModel? instance)
		where TModel : ISerializableDomainObject<TModel, TUnderlying>
	{
		return instance is null
			? default
			: instance.Serialize();
	}

	/// <summary>
	/// <para>
	/// Creates an expression of a call to <see cref="Serialize{TModel, TUnderlying}(TModel)"/>.
	/// </para>
	/// <para>
	/// When evaluated, the result serializes a given instance of the <paramref name="modelType"/> as an instance of the <paramref name="underlyingType"/>.
	/// </para>
	/// </summary>
	public static Expression CreateSerializeExpression([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type modelType, Type underlyingType)
	{
		var result = CreateSerializeExpressionCore(modelType, underlyingType, out _);
		return result;
	}

	/// <summary>
	/// <para>
	/// Creates a lambda expression that calls <see cref="Serialize{TModel, TUnderlying}(TModel)"/>.
	/// </para>
	/// <para>
	/// The result serializes a given <typeparamref name="TModel"/> as a <typeparamref name="TUnderlying"/>.
	/// </para>
	/// <para>
	/// To obtain a delegate, call <see cref="Expression{TDelegate}.Compile()"/> on the result.
	/// </para>
	/// </summary>
	public static Expression<Func<TModel, TUnderlying>> CreateSerializeExpression<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TUnderlying>()
		where TModel : ISerializableDomainObject<TModel, TUnderlying>
	{
		var call = CreateSerializeExpressionCore(typeof(TModel), typeof(TUnderlying), out var parameter);
		var lambda = Expression.Lambda<Func<TModel, TUnderlying>>(call, parameter);
		return lambda;
	}

	private static MethodCallExpression CreateSerializeExpressionCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type modelType, Type underlyingType,
		out ParameterExpression parameter)
	{
		var method = GenericSerializeMethod.MakeGenericMethod(modelType, underlyingType);
		parameter = Expression.Parameter(modelType, "instance");
		var result = Expression.Call(method, parameter);
		return result;
	}

	#endregion
}

#endif
