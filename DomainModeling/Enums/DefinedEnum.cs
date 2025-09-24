using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using Architect.DomainModeling.Conversions;
using Architect.DomainModeling.Enums;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.DomainModeling;

/// <summary>
/// Provides utilities and configuration for <see cref="DefinedEnum{TEnum}"/> and <see cref="DefinedEnum{TEnum, TPrimitive}"/>.
/// </summary>
public static class DefinedEnum
{
	/// <summary>
	/// <para>
	/// The factory used to produce an exception whenever a <see cref="DefinedEnum{TEnum}"/> or <see cref="DefinedEnum{TEnum, TPrimitive}"/> is attempted to be constructed based on null.
	/// </para>
	/// <para>
	/// Receives the enum type.
	/// </para>
	/// </summary>
	public static Func<Type, Exception?>? ExceptionFactoryForNullInput { get; set; }
	/// <summary>
	/// <para>
	/// The factory used to produce an exception whenever a <see cref="DefinedEnum{TEnum}"/> or <see cref="DefinedEnum{TEnum, TPrimitive}"/> is attempted to be constructed based on an undefined value for its type.
	/// </para>
	/// <para>
	/// For an enum with the <see cref="FlagsAttribute"/>, any combination of bits used in its defined values is permitted, i.e. this is only used whenever a value includes any undefined bit.
	/// </para>
	/// <para>
	/// Receives the enum type and the numeric value.
	/// </para>
	/// </summary>
	public static Func<Type, Int128, Exception?>? ExceptionFactoryForUndefinedInput { get; set; }

	/// <summary>
	/// Constructs a new <see cref="DefinedEnum{TEnum}"/>, with type inference.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DefinedEnum<TEnum> Create<TEnum>(TEnum enumValue)
		where TEnum : unmanaged, Enum
	{
		return new DefinedEnum<TEnum>(enumValue);
	}

	/// <summary>
	/// <para>
	/// Constructs a new <see cref="DefinedEnum{TEnum}"/>, with type inference.
	/// </para>
	/// <para>
	/// Accepts a nullable parameter, but throws for null values.
	/// For example, this is useful for a mandatory request input where omission must lead to rejection.
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DefinedEnum<TEnum> Create<TEnum>([DisallowNull] TEnum? enumValue)
		where TEnum : unmanaged, Enum
	{
		return new DefinedEnum<TEnum>(enumValue);
	}

	[DoesNotReturn]
	internal static object ThrowNullInput(Type enumType, string paramName)
	{
		throw ExceptionFactoryForNullInput?.Invoke(enumType) ?? new ArgumentNullException(paramName);
	}

	[DoesNotReturn]
	internal static void ThrowUndefinedInput(Type enumType, Int128 numericValue)
	{
		throw ExceptionFactoryForUndefinedInput?.Invoke(enumType, numericValue) ?? new ArgumentException($"Only recognized {enumType.Name} values are permitted.");
	}
}

/// <summary>
/// <para>
/// An enum of type <typeparamref name="TEnum"/> whose value is one of the defined values for the enum type.
/// </para>
/// <para>
/// If <typeparamref name="TEnum"/> has the <see cref="FlagsAttribute"/>, any combination of bits used in its defined values is permitted.
/// </para>
/// <para>
/// This type additionally improves the performance of certain operations, such as <see cref="ToString()"/>.
/// </para>
/// <para>
/// Values can be created with type inference using <see cref="DefinedEnum.Create{TEnum}(TEnum?)"/>, or converted implicitly from a defined constant of <typeparamref name="TEnum"/>.
/// </para>
/// <para>
/// An analyzer prevents wrapper value object structs like this from skipping validation via the 'default' keyword.
/// </para>
/// </summary>
/// <typeparam name="TEnum">The type of the represented enum.</typeparam>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[DebuggerDisplay("{ToString()}")]
[WrapperValueObject<Int128>]
public readonly struct DefinedEnum<TEnum> :
	IWrapperValueObject<TEnum>,
	IEquatable<DefinedEnum<TEnum>>,
	IComparable<DefinedEnum<TEnum>>,
	ISpanFormattable,
	ISpanParsable<DefinedEnum<TEnum>>,
	IUtf8SpanFormattable,
	IUtf8SpanParsable<DefinedEnum<TEnum>>,
	ICoreValueWrapper<DefinedEnum<TEnum>, TEnum>
	where TEnum : unmanaged, Enum
{
	private static readonly FrozenDictionary<TEnum, string> DefinedValueNamePairs = Enum.GetValues<TEnum>()
		.Distinct() // Multiple members may be defined with the same numeric value, but both value and Enum.GetName(value) will be identical between duplicates
		.ToFrozenDictionary(value => value, value => Enum.GetName(value) ?? value.ToString());

	private static readonly bool IsFlags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);
	private static readonly ulong AllFlags = DefinedValueNamePairs.Aggregate(0UL, (current, next) => current | next.Key.GetBinaryValue());

	/// <summary>
	/// <para>
	/// Contains an arbitrary undefined value for enum type <typeparamref name="TEnum"/>.
	/// </para>
	/// <para>
	/// The value may change between compilations.
	/// </para>
	/// <para>
	/// The value is null in the rare case where <typeparamref name="TEnum"/> defines every possible numeric value for its underlying type, such as with a byte-backed enum that defines all 256 possible numbers.
	/// </para>
	/// </summary>
	public static readonly TEnum? UndefinedValue = ~AllFlags is var unusedBits && Unsafe.As<ulong, TEnum>(ref unusedBits) is var value && value.GetBinaryValue() != 0UL // Any bits unused?
		? value
		: !IsFlags && EnumExtensions.TryGetUndefinedValue(out value) // With all bits used, for non-flags we can still look for an individual unused value, since values are not combined
		? value
		: null;

	public TEnum Value
	{
		get => this._value;
		internal init => this._value = value;
	}
	private readonly TEnum _value;

	/// <summary>
	/// Throws the same exception as when a <see cref="DefinedEnum{TEnum}"/> was attempted to be constructed with an undefined input value.
	/// </summary>
	[DoesNotReturn]
	public static TEnum ThrowUndefinedInput(TEnum value = default)
	{
		DefinedEnum.ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue());
		return default;
	}

	public DefinedEnum(TEnum value)
	{
		if (IsFlags)
		{
			if ((AllFlags | value.GetBinaryValue()) != AllFlags)
				DefinedEnum.ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue());
		}
		else
		{
			if (!DefinedValueNamePairs.ContainsKey(value))
				DefinedEnum.ThrowUndefinedInput(typeof(TEnum), value.GetNumericValue());
		}

		this.Value = value;
	}

	/// <summary>
	/// Accepts a nullable parameter, but throws for null values.
	/// For example, this is useful for a mandatory request input where omission must lead to rejection.
	/// </summary>
	public DefinedEnum([DisallowNull] TEnum? value)
		: this(value ?? (TEnum)DefinedEnum.ThrowNullInput(typeof(TEnum), nameof(value)))
	{
	}

	/// <summary>
	/// <strong>Obsolete:</strong> This constructor exists for deserialization purposes only.
	/// </summary>
	[Obsolete("This constructor exists for deserialization purposes only.")]
	public DefinedEnum()
	{
	}

	public override string ToString()
	{
		return DefinedValueNamePairs.GetValueOrDefault(this.Value) ??
			this.Value.ToString(); // For combined flags, or in case someone created an undefined enum after all, such as using default(T)
	}

	public string ToString(string? format)
	{
		return format is null
			? this.ToString() // Efficient
			: this.Value.ToString(format);
	}

	public override int GetHashCode()
	{
		return this.Value.GetHashCode();
	}

	public override bool Equals(object? other)
	{
		return other is DefinedEnum<TEnum> otherValue && this.Equals(otherValue);
	}

	public bool Equals(DefinedEnum<TEnum> other)
	{
		return EqualityComparer<TEnum>.Default.Equals(this.Value, other.Value);
	}

	public int CompareTo(DefinedEnum<TEnum> other)
	{
		return Comparer<TEnum>.Default.Compare(this.Value, other.Value);
	}

	public static bool operator ==(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right.Value);
	public static bool operator !=(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => !(left == right);
	public static bool operator ==(DefinedEnum<TEnum> left, TEnum right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right);
	public static bool operator !=(DefinedEnum<TEnum> left, TEnum right) => !(left == right);
	public static bool operator ==(TEnum left, DefinedEnum<TEnum> right) => EqualityComparer<TEnum>.Default.Equals(left, right.Value);
	public static bool operator !=(TEnum left, DefinedEnum<TEnum> right) => !(left == right);

	public static bool operator >(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) > 0;
	public static bool operator <=(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => !(left > right);
	public static bool operator >(DefinedEnum<TEnum> left, TEnum right) => Comparer<TEnum>.Default.Compare(left.Value, right) > 0;
	public static bool operator <=(DefinedEnum<TEnum> left, TEnum right) => !(left > right);
	public static bool operator >(TEnum left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left, right.Value) > 0;
	public static bool operator <=(TEnum left, DefinedEnum<TEnum> right) => !(left > right);

	public static bool operator <(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) < 0;
	public static bool operator >=(DefinedEnum<TEnum> left, DefinedEnum<TEnum> right) => !(left < right);
	public static bool operator <(DefinedEnum<TEnum> left, TEnum right) => Comparer<TEnum>.Default.Compare(left.Value, right) < 0;
	public static bool operator >=(DefinedEnum<TEnum> left, TEnum right) => !(left < right);
	public static bool operator <(TEnum left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left, right.Value) < 0;
	public static bool operator >=(TEnum left, DefinedEnum<TEnum> right) => !(left < right);

	/// <summary>
	/// Constrained to defined constants by analyzer.
	/// </summary>
	public static implicit operator DefinedEnum<TEnum>(TEnum value) => new DefinedEnum<TEnum>(value);
	public static implicit operator TEnum(DefinedEnum<TEnum> instance) => instance.Value;

	/// <summary>
	/// Constrained to defined constants by analyzer.
	/// </summary>
	[return: NotNullIfNotNull(nameof(value))]
	public static implicit operator DefinedEnum<TEnum>?(TEnum? value) => value is { } actual ? new DefinedEnum<TEnum>(actual) : (DefinedEnum<TEnum>?)null;
	[return: NotNullIfNotNull(nameof(instance))]
	public static implicit operator TEnum?(DefinedEnum<TEnum>? instance) => instance?.Value;

	public static explicit operator Int128(DefinedEnum<TEnum> instance) => instance.Value.GetNumericValue();

	[return: NotNullIfNotNull(nameof(instance))]
	public static explicit operator Int128?(DefinedEnum<TEnum>? instance) => instance?.Value.GetNumericValue();

	public static implicit operator DefinedEnum<TEnum, string>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, string>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, byte>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, byte>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, sbyte>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, sbyte>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, ushort>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, ushort>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, short>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, short>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, uint>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, uint>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, int>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, int>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, ulong>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, ulong>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum, long>(DefinedEnum<TEnum> instance) => default(DefinedEnum<TEnum, long>) with { Value = instance.Value };

	public static implicit operator DefinedEnum<TEnum, string>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, string>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, byte>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, byte>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, sbyte>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, sbyte>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, ushort>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, ushort>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, short>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, short>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, uint>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, uint>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, int>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, int>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, ulong>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, ulong>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum, long>?(DefinedEnum<TEnum>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum, long>) with { Value = value } : null;

	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, string> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, byte> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, sbyte> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, ushort> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, short> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, uint> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, int> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, ulong> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };
	public static implicit operator DefinedEnum<TEnum>(DefinedEnum<TEnum, long> instance) => default(DefinedEnum<TEnum>) with { Value = instance.Value };

	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, string>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, byte>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, sbyte>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, ushort>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, short>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, uint>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, int>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, ulong>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;
	public static implicit operator DefinedEnum<TEnum>?(DefinedEnum<TEnum, long>? instance) => instance?.Value is { } value ? default(DefinedEnum<TEnum>) with { Value = value } : null;

	#region Wrapping & Serialization

	static DefinedEnum<TEnum> IValueWrapper<DefinedEnum<TEnum>, TEnum>.Create(TEnum value)
	{
		return new DefinedEnum<TEnum>(value);
	}

	TEnum IValueWrapper<DefinedEnum<TEnum>, TEnum>.Serialize()
	{
		return this.Value;
	}

	static DefinedEnum<TEnum> IValueWrapper<DefinedEnum<TEnum>, TEnum>.Deserialize(TEnum value)
	{
		return default(DefinedEnum<TEnum>) with { Value = value };
	}

	#endregion

	#region Formatting & Parsing

	/// <param name="formatProvider">Obsolete and ignored for enums.</param>
	public string ToString(string? format, IFormatProvider? formatProvider) =>
		this.ToString(format);

	/// <param name="provider">Obsolete and ignored for enums.</param>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		format.IsEmpty && this.ToString() is { } value // Efficient
			? (charsWritten = value.TryCopyTo(destination) ? value.Length : 0) != 0
			: Enum.TryFormat(this.Value, destination, out charsWritten, format);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum> result) =>
		Enum.TryParse<TEnum>(s, ignoreCase: true, out var value)
			? (result = (DefinedEnum<TEnum>)value) is var _
			: !((result = default) is var _);

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum> result) =>
		Enum.TryParse<TEnum>(s, ignoreCase: true, out var value)
			? (result = (DefinedEnum<TEnum>)value) is var _
			: !((result = default) is var _);

	public static DefinedEnum<TEnum> Parse(string s, IFormatProvider? provider) =>
		(DefinedEnum<TEnum>)Enum.Parse<TEnum>(s, ignoreCase: true);

	public static DefinedEnum<TEnum> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		(DefinedEnum<TEnum>)Enum.Parse<TEnum>(s, ignoreCase: true);

	/// <param name="provider">Obsolete and ignored for enums.</param>
	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		format.IsEmpty && this.ToString() is { } value // Efficient
			? Utf8.FromUtf16(value, utf8Destination, charsRead: out _, bytesWritten: out bytesWritten) == OperationStatus.Done
			: Utf8.FromUtf16(this.Format(stackalloc char[64], format, provider), utf8Destination, charsRead: out _, bytesWritten: out bytesWritten) == OperationStatus.Done; // Delegates to char overload

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum> result) =>
		utf8Text.Length <= 2048 && stackalloc char[utf8Text.Length] is var chars && Utf8.ToUtf16(utf8Text, chars, bytesRead: out _, charsWritten: out var charsWritten) == OperationStatus.Done
			? TryParse(chars[..charsWritten], provider, out result)
			: !((result = default) is var _);

	public static DefinedEnum<TEnum> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
		utf8Text.Length <= 2048 && stackalloc char[utf8Text.Length] is var chars && Utf8.ToUtf16(utf8Text, chars, bytesRead: out _, charsWritten: out var charsWritten) == OperationStatus.Done
			? Parse(chars[..charsWritten], provider)
			: Parse(Encoding.UTF8.GetString(utf8Text), provider); // Throws the appropriate exception

	#endregion
}

/// <summary>
/// <para>
/// An enum of type <typeparamref name="TEnum"/> whose value is one of the defined values for the enum type.
/// </para>
/// <para>
/// If <typeparamref name="TEnum"/> has the <see cref="FlagsAttribute"/>, any combination of bits used in its defined values is permitted.
/// </para>
/// <para>
/// The second type parameter indicates whether the enum should be represented as <see langword="string"/> or numerically in contexts where the enum itself cannot be used.
/// Examples include JSON serialization and Entity Framework mapping.
/// </para>
/// <para>
/// This type additionally improves the performance of certain operations, such as <see cref="ToString()"/>.
/// </para>
/// <para>
/// Values can be created with type inference using <see cref="DefinedEnum.Create{TEnum}(TEnum?)"/>, or converted implicitly from a defined constant of <typeparamref name="TEnum"/>.
/// They can also be received as parameters of type <see cref="DefinedEnum{TEnum}"/>, without the need to repeat the type parameter for the primitive.
/// </para>
/// <para>
/// An analyzer prevents wrapper value object structs like this from skipping validation via the 'default' keyword.
/// </para>
/// </summary>
/// <typeparam name="TEnum">The type of the represented enum.</typeparam>
/// <typeparam name="TPrimitive">The underlying representation, either <see langword="string"/> or the enum's exact underlying integral type.</typeparam>
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
	Justification = "We want serialization configured irrespectve of whether the developer uses trimming.")]
[System.Text.Json.Serialization.JsonConverter(typeof(EnumJsonConverterFactory))]
[Newtonsoft.Json.JsonConverter(typeof(EnumNewtonsoftJsonConverterFactory))]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[DebuggerDisplay("{ToString()}")]
[WrapperValueObject<Int128>]
public readonly struct DefinedEnum<TEnum, TPrimitive> :
	IWrapperValueObject<TEnum>,
	IEquatable<DefinedEnum<TEnum, TPrimitive>>, IEquatable<DefinedEnum<TEnum>>,
	IComparable<DefinedEnum<TEnum, TPrimitive>>, IComparable<DefinedEnum<TEnum>>,
	ISpanFormattable,
	ISpanParsable<DefinedEnum<TEnum, TPrimitive>>,
	IUtf8SpanFormattable,
	IUtf8SpanParsable<DefinedEnum<TEnum, TPrimitive>>,
	ICoreValueWrapper<DefinedEnum<TEnum, TPrimitive>, TPrimitive>
	where TEnum : unmanaged, Enum
	where TPrimitive : IEquatable<TPrimitive>, IComparable<TPrimitive>, ISpanParsable<TPrimitive>, IConvertible // string or TEnumUnderlying
{
	public TEnum Value
	{
		get => this._value;
		internal init => this._value = value;
	}
	private readonly TEnum _value;

	static DefinedEnum()
	{
		if (typeof(TPrimitive) != typeof(string) && typeof(TPrimitive) != Enum.GetUnderlyingType(typeof(TEnum)))
			throw new NotSupportedException($"{nameof(DefinedEnum)}<{nameof(TEnum)}, {nameof(TPrimitive)}> requires {nameof(TPrimitive)} to be either string or the enum's underlying integer type.");
	}

	public DefinedEnum(TEnum value)
	{
		this.Value = new DefinedEnum<TEnum>(value).Value;
	}

	/// <summary>
	/// Accepts a nullable parameter, but throws for null values.
	/// For example, this is useful for a mandatory request input where omission must lead to rejection.
	/// </summary>
	public DefinedEnum([DisallowNull] TEnum? value)
		: this(value ?? (TEnum)DefinedEnum.ThrowNullInput(typeof(TEnum), nameof(value)))
	{
	}

	/// <summary>
	/// <strong>Obsolete:</strong> This constructor exists for deserialization purposes only.
	/// </summary>
	[Obsolete("This constructor exists for deserialization purposes only.")]
	public DefinedEnum()
	{
	}

	public override string ToString()
	{
		return (default(DefinedEnum<TEnum>) with { Value = this.Value }).ToString();
	}

	public string ToString(string? format)
	{
		return (default(DefinedEnum<TEnum>) with { Value = this.Value }).ToString(format);
	}

	public override int GetHashCode()
	{
		return this.Value.GetHashCode();
	}

	public override bool Equals(object? other)
	{
		return (other is DefinedEnum<TEnum, TPrimitive> otherValue && this.Equals(otherValue)) ||
			(other is DefinedEnum<TEnum> genericOtherValue && this.Equals(genericOtherValue));
	}

	public bool Equals(DefinedEnum<TEnum, TPrimitive> other)
	{
		return EqualityComparer<TEnum>.Default.Equals(this.Value, other.Value);
	}

	public bool Equals(DefinedEnum<TEnum> other)
	{
		return EqualityComparer<TEnum>.Default.Equals(this.Value, other.Value);
	}

	public int CompareTo(DefinedEnum<TEnum, TPrimitive> other)
	{
		return Comparer<TEnum>.Default.Compare(this.Value, other.Value);
	}

	public int CompareTo(DefinedEnum<TEnum> other)
	{
		return Comparer<TEnum>.Default.Compare(this.Value, other.Value);
	}

	public static bool operator ==(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right.Value);
	public static bool operator !=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => !(left == right);
	public static bool operator ==(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right.Value);
	public static bool operator !=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => !(left == right);
	public static bool operator ==(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right.Value);
	public static bool operator !=(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => !(left == right);
	public static bool operator ==(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => EqualityComparer<TEnum>.Default.Equals(left.Value, right);
	public static bool operator !=(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => !(left == right);
	public static bool operator ==(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => EqualityComparer<TEnum>.Default.Equals(left, right.Value);
	public static bool operator !=(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => !(left == right);

	public static bool operator >(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) > 0;
	public static bool operator <=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => !(left > right);
	public static bool operator >(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) > 0;
	public static bool operator <=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => !(left > right);
	public static bool operator >(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) > 0;
	public static bool operator <=(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => !(left > right);
	public static bool operator >(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => Comparer<TEnum>.Default.Compare(left.Value, right) > 0;
	public static bool operator <=(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => !(left > right);
	public static bool operator >(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left, right.Value) > 0;
	public static bool operator <=(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => !(left > right);

	public static bool operator <(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) < 0;
	public static bool operator >=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum, TPrimitive> right) => !(left < right);
	public static bool operator <(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) < 0;
	public static bool operator >=(DefinedEnum<TEnum, TPrimitive> left, DefinedEnum<TEnum> right) => !(left < right);
	public static bool operator <(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left.Value, right.Value) < 0;
	public static bool operator >=(DefinedEnum<TEnum> left, DefinedEnum<TEnum, TPrimitive> right) => !(left < right);
	public static bool operator <(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => Comparer<TEnum>.Default.Compare(left.Value, right) < 0;
	public static bool operator >=(DefinedEnum<TEnum, TPrimitive> left, TEnum right) => !(left < right);
	public static bool operator <(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => Comparer<TEnum>.Default.Compare(left, right.Value) < 0;
	public static bool operator >=(TEnum left, DefinedEnum<TEnum, TPrimitive> right) => !(left < right);

	/// <summary>
	/// Constrained to defined constants by analyzer.
	/// </summary>
	public static implicit operator DefinedEnum<TEnum, TPrimitive>(TEnum value) => new DefinedEnum<TEnum, TPrimitive>(value);
	public static implicit operator TEnum(DefinedEnum<TEnum, TPrimitive> instance) => instance.Value;

	/// <summary>
	/// Constrained to defined constants by analyzer.
	/// </summary>
	[return: NotNullIfNotNull(nameof(value))]
	public static implicit operator DefinedEnum<TEnum, TPrimitive>?(TEnum? value) => value is { } actual ? new DefinedEnum<TEnum, TPrimitive>(actual) : (DefinedEnum<TEnum, TPrimitive>?)null;
	[return: NotNullIfNotNull(nameof(instance))]
	public static implicit operator TEnum?(DefinedEnum<TEnum, TPrimitive>? instance) => instance?.Value;

	public static explicit operator Int128(DefinedEnum<TEnum, TPrimitive> instance) => instance.Value.GetNumericValue();

	[return: NotNullIfNotNull(nameof(instance))]
	public static explicit operator Int128?(DefinedEnum<TEnum, TPrimitive>? instance) => instance?.Value.GetNumericValue();

	#region Wrapping & Serialization

	TPrimitive IValueWrapper<DefinedEnum<TEnum, TPrimitive>, TPrimitive>.Value => typeof(TPrimitive) == typeof(string)
		? (TPrimitive)(object)this.ToString() // Efficient
		: Unsafe.As<TEnum, TPrimitive>(ref Unsafe.AsRef(in this._value));

	static DefinedEnum<TEnum, TPrimitive> IValueWrapper<DefinedEnum<TEnum, TPrimitive>, TPrimitive>.Create(TPrimitive value)
	{
		return typeof(TPrimitive) == typeof(string)
			? Parse((string)(object)value, provider: null)
			: new DefinedEnum<TEnum, TPrimitive>(Unsafe.As<TPrimitive, TEnum>(ref value));
	}

	TPrimitive IValueWrapper<DefinedEnum<TEnum, TPrimitive>, TPrimitive>.Serialize()
	{
		return typeof(TPrimitive) == typeof(string)
			? (TPrimitive)(object)this.ToString() // Efficient
			: Unsafe.As<TEnum, TPrimitive>(ref Unsafe.AsRef(in this._value));
	}

	static DefinedEnum<TEnum, TPrimitive> IValueWrapper<DefinedEnum<TEnum, TPrimitive>, TPrimitive>.Deserialize(TPrimitive value)
	{
		return default(DefinedEnum<TEnum, TPrimitive>) with
		{
			Value = typeof(TPrimitive) == typeof(string)
				? TryParse((string)(object)value, provider: null, out var result) ? result : (DefinedEnum<TEnum>.UndefinedValue ?? default)
				: Unsafe.As<TPrimitive, TEnum>(ref value),
		};
	}

	#endregion

	#region Formatting & Parsing

	/// <param name="formatProvider">Obsolete and ignored for enums.</param>
	public string ToString(string? format, IFormatProvider? formatProvider) =>
		(default(DefinedEnum<TEnum>) with { Value = this.Value }).ToString(format, formatProvider);

	/// <param name="provider">Obsolete and ignored for enums.</param>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		(default(DefinedEnum<TEnum>) with { Value = this.Value }).TryFormat(destination, out charsWritten, format, provider);

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum, TPrimitive> result) =>
		DefinedEnum<TEnum>.TryParse(s, provider, out var intermediateResult)
			? (result = default(DefinedEnum<TEnum, TPrimitive>) with { Value = intermediateResult.Value }) is var _
			: !((result = default) is var _);

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum, TPrimitive> result) =>
		DefinedEnum<TEnum>.TryParse(s, provider, out var intermediateResult)
			? (result = default(DefinedEnum<TEnum, TPrimitive>) with { Value = intermediateResult.Value }) is var _
			: !((result = default) is var _);

	public static DefinedEnum<TEnum, TPrimitive> Parse(string s, IFormatProvider? provider) =>
		default(DefinedEnum<TEnum, TPrimitive>) with { Value = DefinedEnum<TEnum>.Parse(s, provider).Value };

	public static DefinedEnum<TEnum, TPrimitive> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
		default(DefinedEnum<TEnum, TPrimitive>) with { Value = DefinedEnum<TEnum>.Parse(s, provider).Value };

	/// <param name="provider">Obsolete and ignored for enums.</param>
	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
		(default(DefinedEnum<TEnum>) with { Value = this.Value }).TryFormat(utf8Destination, out bytesWritten, format, provider);

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out DefinedEnum<TEnum, TPrimitive> result) =>
		DefinedEnum<TEnum>.TryParse(utf8Text, provider, out var intermediateResult)
			? (result = default(DefinedEnum<TEnum, TPrimitive>) with { Value = intermediateResult.Value }) is var _
			: !((result = default) is var _);

	public static DefinedEnum<TEnum, TPrimitive> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
		default(DefinedEnum<TEnum, TPrimitive>) with { Value = DefinedEnum<TEnum>.Parse(utf8Text, provider).Value };

	#endregion
}
