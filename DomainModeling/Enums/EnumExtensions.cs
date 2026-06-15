using Architect.DomainModeling.Enums;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.DomainModeling;

/// <summary>
/// Provides enum extensions for domain modeling.
/// </summary>
public static class EnumExtensions
{
	/// <summary>
	/// <para>
	/// Validates that <paramref name="value"/> is a defined <typeparamref name="TEnum"/> value, throwing otherwise.
	/// </para>
	/// <para>
	/// The potential exception can be globally configured using <see cref="DefinedEnum.ExceptionFactoryForUndefinedInput"/>.
	/// </para>
	/// </summary>
	public static TEnum AsDefined<TEnum>(this TEnum value, string? errorState = null)
		where TEnum : unmanaged, Enum
	{
		if (!Enum.IsDefined(value))
			DefinedEnum.ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue(), errorState);

		return value;
	}

	/// <summary>
	/// <para>
	/// Validates that <paramref name="value"/> is either null or a defined <typeparamref name="TEnum"/> value, throwing otherwise.
	/// </para>
	/// <para>
	/// The potential exception can be globally configured using <see cref="DefinedEnum.ExceptionFactoryForUndefinedInput"/>.
	/// </para>
	/// </summary>
	public static TEnum? AsDefined<TEnum>(this TEnum? value, string? errorState = null)
		where TEnum : unmanaged, Enum
	{
		return value is TEnum actual ? AsDefined(actual, errorState) : null;
	}

	/// <summary>
	/// <para>
	/// Validates that <paramref name="value"/> is a valid combination of bits used in the defined values for <typeparamref name="TEnum"/>, throwing otherwise.
	/// </para>
	/// <para>
	/// The potential exception can be globally configured using <see cref="DefinedEnum.ExceptionFactoryForUndefinedInput"/>.
	/// </para>
	/// </summary>
	public static TEnum AsDefinedFlags<TEnum>(this TEnum value, string? errorState = null)
		where TEnum : unmanaged, Enum
	{
		if ((DefinedEnum.UndefinedValues<TEnum>.AllFlags | value.GetBinaryValue()) != DefinedEnum.UndefinedValues<TEnum>.AllFlags)
			DefinedEnum.ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue(), errorState);

		return value;
	}

	/// <summary>
	/// <para>
	/// Validates that <paramref name="value"/> is either null or a valid combination of bits used in the defined values for <typeparamref name="TEnum"/>, throwing otherwise.
	/// </para>
	/// <para>
	/// The potential exception can be globally configured using <see cref="DefinedEnum.ExceptionFactoryForUndefinedInput"/>.
	/// </para>
	/// </summary>
	public static TEnum? AsDefinedFlags<TEnum>(this TEnum? value, string? errorState = null)
		where TEnum : unmanaged, Enum
	{
		return value is TEnum actual ? AsDefinedFlags(actual, errorState) : null;
	}

	/// <summary>
	/// Clarifies the deliberate intent to assign <paramref name="value"/> <em>without</em> validating that it is a defined <typeparamref name="TEnum"/> value.
	/// </summary>
	public static TEnum AsUnvalidated<TEnum>(this TEnum value)
		where TEnum : unmanaged, Enum
	{
		return value;
	}

	/// <summary>
	/// Clarifies the deliberate intent to assign <paramref name="value"/> <em>without</em> validating that it is (null or) a defined <typeparamref name="TEnum"/> value.
	/// </summary>
	public static TEnum? AsUnvalidated<TEnum>(this TEnum? value)
		where TEnum : unmanaged, Enum
	{
		return value is TEnum actual ? AsUnvalidated(actual) : null;
	}
}
