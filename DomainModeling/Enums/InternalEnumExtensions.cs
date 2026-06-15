using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Enums;

internal static class InternalEnumExtensions
{
	private static readonly ulong DefaultUndefinedValue = 191; // Greatest prime under 3/4 of Byte.MaxValue
	private static readonly ulong FallbackUndefinedValue = 49139; // Greatest prime under 3/4 of UInt16.MaxValue

	/// <summary>
	/// <para>
	/// Attempts to return one of a small set of predefined values if one is undefined for <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// Does not account for the <see cref="FlagsAttribute"/>.
	/// </para>
	/// </summary>
	public static bool TryGetUndefinedValueFast<TEnum>(out TEnum value)
		where TEnum : unmanaged, Enum
	{
		value = GetEnumValue<TEnum>(DefaultUndefinedValue);
		if (!Enum.IsDefined(value))
			return true;

		value = GetEnumValue<TEnum>(FallbackUndefinedValue);
		if (Unsafe.SizeOf<TEnum>() >= 2 && !Enum.IsDefined(value))
			return true;

		value = default;
		return false;
	}

	/// <summary>
	/// <para>
	/// Attempts to find an undefined value for <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// Does not account for the <see cref="FlagsAttribute"/>.
	/// </para>
	/// </summary>
	public static bool TryGetUndefinedValue<TEnum>(out TEnum value)
		where TEnum : unmanaged, Enum
	{
		if (TryGetUndefinedValueFast(out value))
			return true;

		var values = Enum.GetValues<TEnum>();
		Debug.Assert(values.Select(GetBinaryValue).Order().SequenceEqual(values.Select(GetBinaryValue)), "Enum.GetValues() was expected to return elements in binary order.");

		// If we do not end with the binary maximum, then use that
		var enumBinaryMax = ~0UL >> (64 - 8 * Unsafe.SizeOf<TEnum>()); // E.g. 64-0 bits for ulong/long, 64-32 for uint/int, and so on
		if (values.Length == 0 || values[^1].GetBinaryValue() < enumBinaryMax)
		{
			value = GetEnumValue<TEnum>(enumBinaryMax);
			return true;
		}

		// If we do not start with the default, then use that
		ulong previousValue;
		if ((previousValue = values[0].GetBinaryValue()) != 0UL)
		{
			value = default;
			return true;
		}

		foreach (var definedValue in values.Skip(1))
		{
			// If there is a gap between the current and previous item
			var currentValue = definedValue.GetBinaryValue();
			if (currentValue > previousValue + 1)
			{
				value = GetEnumValue<TEnum>(previousValue + 1);
				return true;
			}
			previousValue = currentValue;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Returns the numeric value of the given <paramref name="enumValue"/>.
	/// Unlike <see cref="GetBinaryValue"/>, this retains negative values for signed enum types.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 GetNumericValue<TEnum>(this TEnum enumValue)
		where TEnum : unmanaged, Enum
	{
		// Branches optimized away by JIT, as Type.GetEnumUnderlyingType() is [Intrinsic] and treated as a constant
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(byte)) return (Int128)Unsafe.As<TEnum, byte>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(sbyte)) return (Int128)Unsafe.As<TEnum, sbyte>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(ushort)) return (Int128)Unsafe.As<TEnum, ushort>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(short)) return (Int128)Unsafe.As<TEnum, short>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(uint)) return (Int128)Unsafe.As<TEnum, uint>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(int)) return (Int128)Unsafe.As<TEnum, int>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(ulong)) return (Int128)Unsafe.As<TEnum, ulong>(ref enumValue);
		if (typeof(TEnum).GetEnumUnderlyingType() == typeof(long)) return (Int128)Unsafe.As<TEnum, long>(ref enumValue);
		throw new UnreachableException();
	}

	/// <summary>
	/// <para>
	/// Returns the binary value of the given <paramref name="enumValue"/>, contained in a <see cref="UInt64"/>.
	/// </para>
	/// <para>
	/// This method is the inverse of <see cref="GetEnumValue"/>.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong GetBinaryValue<TEnum>(this TEnum enumValue)
		where TEnum : unmanaged, Enum
	{
		var result = 0UL;

		// TEnum could be shorter than 8 bytes
		// Little-endian will automatically write into the least significant bytes, since those are on the left for little-endian
		// For big-endian, they are on the right, so we need to look at the rightmost portion of the ulong, depending on size of TEnum
		// For example, a ushort TEnum will look at the rightmost 2 bytes of the ulong
		if (BitConverter.IsLittleEndian)
			Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref result), enumValue);
		else
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref Unsafe.As<ulong, byte>(ref result), sizeof(ulong) - Unsafe.SizeOf<TEnum>()), enumValue);

		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the <typeparamref name="TEnum"/> enum value of the given <paramref name="binaryValue"/>.
	/// </para>
	/// <para>
	/// This method is the inverse of <see cref="GetBinaryValue"/>.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TEnum GetEnumValue<TEnum>(ulong binaryValue)
		where TEnum : unmanaged, Enum
	{
		// TEnum could be shorter than 8 bytes
		// Little-endian will automatically write into the least significant bytes, since those are on the left for little-endian
		// For big-endian, they are on the right, so we need to look at the rightmost portion of the ulong, depending on size of TEnum
		// For example, a ushort TEnum will look at the rightmost 2 bytes of the ulong
		if (BitConverter.IsLittleEndian)
			return Unsafe.ReadUnaligned<TEnum>(ref Unsafe.As<ulong, byte>(ref binaryValue));
		else
			return Unsafe.ReadUnaligned<TEnum>(ref Unsafe.Add(ref Unsafe.As<ulong, byte>(ref binaryValue), sizeof(ulong) - Unsafe.SizeOf<TEnum>()));
	}
}
