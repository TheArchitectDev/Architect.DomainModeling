using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// <para>
/// Exposes wrapping and unwrapping methods for <see cref="IValueWrapper{TWrapper, TValue}"/> instances.
/// </para>
/// <para>
/// Wrapping and unwrapping is the normal process of constructing value wrappers around values and extracting those values again. It may hit validation logic.
/// </para>
/// </summary>
public static class ValueWrapperUnwrapper
{
	#region Wrap

	/// <summary>
	/// Wraps a <typeparamref name="TValue"/> in a <typeparamref name="TWrapper"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: NotNullIfNotNull(nameof(value))]
	public static TWrapper? Wrap<TWrapper, TValue>(TValue? value)
		where TWrapper : IValueWrapper<TWrapper, TValue>
	{
		return value is null
			? default
			: TWrapper.Create(value);
	}

	#endregion

	#region Unwrap

	/// <summary>
	/// Unwraps the <typeparamref name="TValue"/> from a <typeparamref name="TWrapper"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TValue? Unwrap<TWrapper, TValue>(
		TWrapper? instance)
		where TWrapper : IValueWrapper<TWrapper, TValue>
	{
		return instance is null
			? default
			: instance.Value;
	}

	#endregion
}
