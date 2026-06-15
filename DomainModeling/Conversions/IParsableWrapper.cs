using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// Provides default implementations for <see cref="IParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface IParsableWrapper<TWrapper, TValue> : IParsable<TWrapper>
	where TWrapper : IParsableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : IParsable<TValue>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IParsable<TWrapper>.TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		result = default;
		var success = TValue.TryParse(s, provider, out var value) && TWrapper.TryCreate(value, out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper IParsable<TWrapper>.Parse(string s, IFormatProvider? provider)
	{
		var value = TValue.Parse(s, provider);
		var result = TWrapper.Create(value);
		return result;
	}
}

/// <summary>
/// Provides default implementations for <see cref="ISpanParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface ISpanParsableWrapper<TWrapper, TValue> : IParsableWrapper<TWrapper, TValue>, ISpanParsable<TWrapper>
	where TWrapper : ISpanParsableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : ISpanParsable<TValue>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool ISpanParsable<TWrapper>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		result = default;
		var success = TValue.TryParse(s, provider, out var value) && TWrapper.TryCreate(value, out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper ISpanParsable<TWrapper>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		var value = TValue.Parse(s, provider);
		var result = TWrapper.Create(value);
		return result;
	}
}

/// <summary>
/// Provides default implementations for <see cref="IUtf8SpanParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface IUtf8SpanParsableWrapper<TWrapper, TValue> : IUtf8SpanParsable<TWrapper>
	where TWrapper : IUtf8SpanParsableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : IUtf8SpanParsable<TValue>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IUtf8SpanParsable<TWrapper>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		result = default;
		var success = TValue.TryParse(utf8Text, provider, out var value) && TWrapper.TryCreate(value, out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper IUtf8SpanParsable<TWrapper>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		var value = TValue.Parse(utf8Text, provider);
		var result = TWrapper.Create(value);
		return result;
	}
}

#region Strings

/// <summary>
/// Provides default implementations for <see cref="IParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface IParsableStringWrapper<TWrapper> : IParsable<TWrapper>
	where TWrapper : IParsableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IParsable<TWrapper>.TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		result = default;
		var success = s is not null && TWrapper.TryCreate(s, out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper IParsable<TWrapper>.Parse(string s, IFormatProvider? provider)
	{
		var result = TWrapper.Create(s);
		return result;
	}
}

/// <summary>
/// Provides default implementations for <see cref="ISpanParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface ISpanParsableStringWrapper<TWrapper> : IParsableStringWrapper<TWrapper>, ISpanParsable<TWrapper>
	where TWrapper : ISpanParsableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool ISpanParsable<TWrapper>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		var value = s.ToString();
		var success = TWrapper.TryCreate(value, out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper ISpanParsable<TWrapper>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		var value = s.ToString();
		var result = TWrapper.Create(value);
		return result;
	}
}

/// <summary>
/// Provides default implementations for <see cref="IUtf8SpanParsable{TSelf}"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface IUtf8SpanParsableStringWrapper<TWrapper> : IUtf8SpanParsable<TWrapper>
	where TWrapper : IUtf8SpanParsableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IUtf8SpanParsable<TWrapper>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	{
		result = default;
		var success = Utf8.IsValid(utf8Text) && TWrapper.TryCreate(Encoding.UTF8.GetString(utf8Text), out result);
		return success;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TWrapper IUtf8SpanParsable<TWrapper>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		var value = Encoding.UTF8.GetString(utf8Text);
		var result = TWrapper.Create(value);
		return result;
	}
}

#endregion
