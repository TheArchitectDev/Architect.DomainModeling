using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Defines extensions on generic and specific enums.
/// </summary>
internal static class EnumExtensions
{
	/// <summary>
	/// Returns the source <see cref="Accessibility"/>, or <paramref name="minimumAccessibility"/> if the source was less than that.
	/// </summary>
	public static Accessibility AtLeast(this Accessibility accessibility, Accessibility minimumAccessibility)
	{
		return accessibility >= minimumAccessibility
			? accessibility
			: minimumAccessibility;
	}

	/// <summary>
	/// Returns the code representation of the given <paramref name="accessibility"/>, e.g. "protected internal".
	/// </summary>
	/// <param name="unspecified">The result to return for unspecified accessibility.</param>
	public static string ToCodeString(this Accessibility accessibility, string unspecified = "")
	{
		var result = accessibility switch
		{
			Accessibility.NotApplicable => unspecified,
			Accessibility.Private => "private",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.Protected => "protected",
			Accessibility.Internal => "internal",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.Public => "public",
			_ => throw new NotSupportedException($"Unsupported accessibility: {accessibility}."),
		};

		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the <paramref name="enumValue"/>, or a default value (all flags unset) if <paramref name="condition"/> is false.
	/// </para>
	/// <para>
	/// This method is intended to help easily modify enum flags conditionally.
	/// </para>
	/// <para>
	/// <code>// Set the SomeFlag if 1 == 2</code>
	/// <code>myEnum |= MyEnum.SomeFlag.If(1 == 2);</code>
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T If<T>(this T enumValue, bool condition)
		where T : unmanaged, Enum
	{
		// Branch-free implementation
		ReadOnlySpan<T> values = stackalloc T[] { default, enumValue, };
		var index = Unsafe.As<bool, byte>(ref condition);
		return values[index];
	}

	/// <summary>
	/// <para>
	/// Returns the <paramref name="enumValue"/>, or a default value (all flags unset) if <paramref name="condition"/> is true.
	/// </para>
	/// <para>
	/// This method is intended to help easily modify enum flags conditionally.
	/// </para>
	/// <para>
	/// <code>// Set the SomeFlag unless 1 == 2</code>
	/// <code>myEnum |= MyEnum.SomeFlag.Unless(1 == 2);</code>
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Unless<T>(this T enumValue, bool condition)
		where T : unmanaged, Enum
	{
		// Branch-free implementation
		ReadOnlySpan<T> values = stackalloc T[] { enumValue, default, };
		var index = Unsafe.As<bool, byte>(ref condition);
		return values[index];
	}

	/// <summary>
	/// Efficiently returns whether the <paramref name="subject"/> has the given <paramref name="flag"/>(s) set.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasFlags<T>(this T subject, T flag)
		where T : unmanaged, Enum
	{
		var numericSubject = GetNumericValue(subject);
		var numericFlag = GetNumericValue(flag);

		return (numericSubject & numericFlag) == numericFlag;
	}

	/// <summary>
	/// <para>
	/// Returns the numeric value of the given <paramref name="enumValue"/>.
	/// </para>
	/// <para>
	/// The resulting <see cref="UInt64"/> can be cast to the intended integral type, even if it is a signed type.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong GetNumericValue<T>(T enumValue)
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
