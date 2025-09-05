using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An instance of <typeparamref name="TWrapper"/> wrapping a value of type <typeparamref name="TValue"/> in a property named Value.
/// </para>
/// <para>
/// Supports wrapping and unwrapping (which may hit validation logic) as well as trusted serializing and deserializing (which avoids logic).
/// </para>
/// </summary>
/// <typeparam name="TWrapper">The wrapper type.</typeparam>
/// <typeparam name="TValue">The type being wrapped.</typeparam>
public interface IValueWrapper<TWrapper, TValue>
	where TWrapper : IValueWrapper<TWrapper, TValue>
{
	// Note that a struct implementation can always return a null value
	TValue? Value { get; }

	/// <summary>
	/// Constructs a new <typeparamref name="TWrapper"/> around the given <typeparamref name="TValue"/>.
	/// </summary>
	abstract static TWrapper Create(
#nullable disable // We are used interchangeably between types with nullable vs. non-nullable values, so do not enforce either
		TValue value);
#nullable enable

	/// <summary>
	/// Attempts to construct a new <typeparamref name="TWrapper"/> around the given <typeparamref name="TValue"/>.
	/// </summary>
	virtual static bool TryCreate(
#nullable disable // We are used interchangeably between types with nullable vs. non-nullable values, so do not enforce either
		TValue value,
#nullable enable
		[MaybeNullWhen(false)] out TWrapper result)
	{
		try
		{
			result = TWrapper.Create(value);
			return true;
		}
		catch
		{
			result = default;
			return false;
		}
	}

	/// <summary>
	/// <para>
	/// Serializes the <typeparamref name="TWrapper"/> as a <typeparamref name="TValue"/>.
	/// </para>
	/// <para>
	/// This provides the basis for concrete serialization, such as to JSON or for a database provider.
	/// </para>
	/// <para>
	/// Domain model serialization is intended to work with trusted data and should skip validation and other logic.
	/// </para>
	/// </summary>
	TValue? Serialize();

	/// <summary>
	/// <para>
	/// Deserializes a <typeparamref name="TWrapper"/> from a <typeparamref name="TValue"/>.
	/// </para>
	/// <para>
	/// This provides the basis for concrete deserialization, such as from JSON or from a database provider.
	/// </para>
	/// <para>
	/// Domain model serialization is intended to work with trusted data and should skip validation and other logic.
	/// </para>
	/// </summary>
	abstract static TWrapper Deserialize(TValue value);
}

/// <summary>
/// <para>
/// An instance of <typeparamref name="TWrapper"/> wrapping a value of type <typeparamref name="TValue"/> in a property named Value.
/// </para>
/// <para>
/// This interface further marks <typeparamref name="TValue"/> as the direct underlying type.
/// A wrapper around another wrapper may implement <see cref="IValueWrapper{TWrapper, TValue}"/> repeatedly for various <typeparamref name="TValue"/>s, but should have only one <see cref="IDirectValueWrapper{TWrapper, TValue}"/>.
/// </para>
/// <para>
/// Supports wrapping and unwrapping (which may hit validation logic) as well as trusted serializing and deserializing (which avoids logic).
/// </para>
/// </summary>
public interface IDirectValueWrapper<TWrapper, TValue> : IValueWrapper<TWrapper, TValue>
	where TWrapper : IDirectValueWrapper<TWrapper, TValue>
{
}

/// <summary>
/// <para>
/// An instance of <typeparamref name="TWrapper"/> wrapping a value of type <typeparamref name="TValue"/> in a property named Value.
/// </para>
/// <para>
/// This interface further marks <typeparamref name="TValue"/> as the core (deepest) underlying type.
/// A wrapper around another wrapper may implement <see cref="IValueWrapper{TWrapper, TValue}"/> repeatedly for various <typeparamref name="TValue"/>s, but should have only one <see cref="ICoreValueWrapper{TWrapper, TValue}"/>.
/// </para>
/// <para>
/// Supports wrapping and unwrapping (which may hit validation logic) as well as trusted serializing and deserializing (which avoids logic).
/// </para>
/// </summary>
public interface ICoreValueWrapper<TWrapper, TValue> : IValueWrapper<TWrapper, TValue>
	where TWrapper : ICoreValueWrapper<TWrapper, TValue>
{
}
