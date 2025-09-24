using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Enums;

internal static class EnumExtensions
{
	private static readonly byte DefaultUndefinedValue = 191; // Greatest prime under 3/4 of Byte.MaxValue
	private static readonly ushort FallbackUndefinedValue = 49139; // Greatest prime under 3/4 of UInt16.MaxValue

	/// <summary>
	/// <para>
	/// Attempts to return one of a small set of predefined values if one is undefined for <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// Does not accounts for the <see cref="FlagsAttribute"/>.
	/// </para>
	/// </summary>
	public static bool TryGetUndefinedValueFast<TEnum>(out TEnum value)
		where TEnum : unmanaged, Enum
	{
		var defaultUndefined = Unsafe.As<byte, TEnum>(ref Unsafe.AsRef(in DefaultUndefinedValue));
		if (!Enum.IsDefined(defaultUndefined))
		{
			value = defaultUndefined;
			return true;
		}

		var fallbackUndefined = Unsafe.As<ushort, TEnum>(ref Unsafe.AsRef(in FallbackUndefinedValue));
		if (Unsafe.SizeOf<TEnum>() >= 2 && !Enum.IsDefined(fallbackUndefined))
		{
			value = fallbackUndefined;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// <para>
	/// Attempts to find an undefined value for <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// Does not accounts for the <see cref="FlagsAttribute"/>.
	/// </para>
	/// </summary>
	public static bool TryGetUndefinedValue<TEnum>(out TEnum value)
		where TEnum : unmanaged, Enum
	{
		if (TryGetUndefinedValueFast(out value))
			return true;

		var values = Enum.GetValues<TEnum>();
		System.Diagnostics.Debug.Assert(values.Select(GetBinaryValue).Order().SequenceEqual(values.Select(GetBinaryValue)), "Enum.GetValues() was expected to return elements in binary order.");

		// If we do not end with the binary maximum, then use that
		var enumBinaryMax = ~0UL >> (64 - 8 * Unsafe.SizeOf<TEnum>()); // E.g. 64-0 bits for ulong/long, 64-32 for uint/int, and so on
		if (values.Length == 0 || values[^1].GetBinaryValue() < enumBinaryMax)
		{
			value = Unsafe.As<ulong, TEnum>(ref enumBinaryMax);
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
				previousValue++;
				value = Unsafe.As<ulong, TEnum>(ref previousValue);
				return true;
			}
			previousValue = currentValue;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Returns the numeric value of the given <paramref name="enumValue"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 GetNumericValue<T>(this T enumValue)
		where T : unmanaged, Enum
	{
		// Optimized by JIT, as Type.GetTypeCode(T) is treated as a constant
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.Byte => (Int128)Unsafe.As<T, byte>(ref enumValue),
			TypeCode.SByte => (Int128)Unsafe.As<T, sbyte>(ref enumValue),
			TypeCode.Int16 => (Int128)Unsafe.As<T, short>(ref enumValue),
			TypeCode.UInt16 => (Int128)Unsafe.As<T, ushort>(ref enumValue),
			TypeCode.Int32 => (Int128)Unsafe.As<T, int>(ref enumValue),
			TypeCode.UInt32 => (Int128)Unsafe.As<T, uint>(ref enumValue),
			TypeCode.Int64 => (Int128)Unsafe.As<T, long>(ref enumValue),
			TypeCode.UInt64 => (Int128)Unsafe.As<T, ulong>(ref enumValue),
			_ => default,
		};
	}

	/// <summary>
	/// <para>
	/// Returns the binary value of the given <paramref name="enumValue"/>, contained in a <see cref="UInt64"/>.
	/// </para>
	/// <para>
	/// The original value's bytes can be retrieved by doing a cast or <see cref="Unsafe.As{TFrom, TTo}"/> to the original enum or underlying type.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong GetBinaryValue<T>(this T enumValue)
		where T : unmanaged, Enum
	{
		var result = 0UL;

		// Since the actual value may be smaller than ulong's 8 bytes, we must align to the least significant byte
		// This way, casting the ulong back to the original type gets back the exact original bytes
		// On little-endian, that means aligning to the left of the bytes
		// On big-endian, that means aligning to the right of the bytes
		if (BitConverter.IsLittleEndian)
			Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref result), enumValue);
		else
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref Unsafe.As<ulong, byte>(ref result), sizeof(ulong) - Unsafe.SizeOf<T>()), enumValue);

		return result;
	}
}
