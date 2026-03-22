using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Enums;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.DomainModeling;

/// <summary>
/// Provides utilities and configuration to work with defined enum values, i.e. values that are defined for their enum type.
/// </summary>
public static class DefinedEnum
{
	/// <summary>
	/// <para>
	/// The factory used to produce an exception whenever <see cref="EnumExtensions.AsDefined{TEnum}(TEnum, String?)"/> or <see cref="EnumExtensions.AsDefinedFlags{TEnum}(TEnum, String?)"/> is called on an undefined value.
	/// </para>
	/// <para>
	/// Receives the enum type, the numeric value, and the optional error state passed during the validation.
	/// </para>
	/// </summary>
	public static Func<Type, Int128, string?, Exception?>? ExceptionFactoryForUndefinedInput { get; set; }

	/// <summary>
	/// <para>
	/// Throws the configured exception for enum value <paramref name="numericValue"/> of type <paramref name="enumType"/> being undefined for its type.
	/// </para>
	/// <para>
	/// This method can be used to produce the same effect as <see cref="EnumExtensions.AsDefined{TEnum}(TEnum, String?)"/> without calling it, such as from a mapper.
	/// </para>
	/// </summary>
	/// <param name="enumType">The enum's type.</param>
	/// <param name="numericValue">The enum's numeric value, such as (Int128)(int)HttpStatusCode.OK.</param>
	/// <param name="errorState">An optional error state to be passed to <see cref="ExceptionFactoryForUndefinedInput"/>.</param>
	[DoesNotReturn]
	public static void ThrowUndefinedInput(Type enumType, Int128 numericValue, string? errorState = null)
	{
		throw ExceptionFactoryForUndefinedInput?.Invoke(enumType, numericValue, errorState) ?? new ArgumentException($"Only recognized {enumType.Name} values are permitted.");
	}

	/// <summary>
	/// <para>
	/// Throws the configured exception for enum value <paramref name="value"/> being undefined for its type.
	/// </para>
	/// <para>
	/// This method can be used to produce the same effect as <see cref="EnumExtensions.AsDefined{TEnum}(TEnum, String?)"/> without calling it, such as from a mapper.
	/// </para>
	/// </summary>
	/// <param name="value">The enum's value.</param>
	/// <param name="errorState">An optional error state to be passed to <see cref="ExceptionFactoryForUndefinedInput"/>.</param>
	[DoesNotReturn]
	public static void ThrowUndefinedInput<TEnum>(TEnum value, string? errorState = null)
		where TEnum : unmanaged, Enum
	{
		ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue(), errorState);
	}

	/// <summary>
	/// <para>
	/// Contains an undefined value for <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// This type cannot be constructed for a type parameter where all values are defined, such as with a byte-backed enum that defines all its 256 possible numbers.
	/// </para>
	/// </summary>
	public static class UndefinedValues<TEnum>
		where TEnum : unmanaged, Enum
	{
		private static readonly bool IsFlags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);
		internal static readonly ulong AllFlags = Enum.GetValues<TEnum>().Aggregate(0UL, (current, next) => current | next.GetBinaryValue());

		public static TEnum UndefinedValue { get; } = ~AllFlags is var unusedBits && InternalEnumExtensions.GetEnumValue<TEnum>(unusedBits) is var value && value.GetBinaryValue() is not 0UL // Any bits unused?
			? value
			: !IsFlags && InternalEnumExtensions.TryGetUndefinedValue<TEnum>(out value) // With all bits used, for non-flags we can still look for an individual unused value, since values are not combined
			? value
			: throw new NotSupportedException($"Type {typeof(TEnum).Name} does not leave any possible values undefined (or flag bits unused).");
	}
}
