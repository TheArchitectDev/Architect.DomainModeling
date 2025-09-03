using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// An instance of <typeparamref name="TWrapper"/> wrapping a value of type <typeparamref name="TValue"/> in a property named Value.
/// </summary>
/// <typeparam name="TWrapper">The wrapper type.</typeparam>
/// <typeparam name="TValue">The type being wrapped.</typeparam>
public interface IValueWrapper<TWrapper, TValue>
	where TWrapper : IValueWrapper<TWrapper, TValue>
{
	// Note that a struct implementation can always return a null value
	TValue? Value { get; }

	abstract static TWrapper Create(
#nullable disable // We are used interchangeably between types with nullable vs. non-nullable values, so do not enforce either
		TValue value);
#nullable enable

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
}
