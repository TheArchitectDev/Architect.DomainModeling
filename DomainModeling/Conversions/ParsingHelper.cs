using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// <para>
/// Delegates to *Parsable interfaces depending on their presence on a given type parameter.
/// Uses overload resolution to avoid a compiler error where the interface is missing.
/// </para>
/// <para>
/// This type is intended for use by source-generated code, to avoid compiler errors in situations where the presence of the required interfaces is extremely likely but cannot be guaranteed.
/// </para>
/// </summary>
public static class ParsingHelper
{
#if NET7_0_OR_GREATER

	/// <summary>
	/// This overload throws because <see cref="IParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static bool TryParse<T>([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out T? result,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support parsing.");
	}

	/// <summary>
	/// Delegates to <see cref="IParsable{TSelf}.TryParse"/>.
	/// </summary>
	public static bool TryParse<T>([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out T? result)
		where T : IParsable<T>
	{
		return T.TryParse(s, provider, out result);
	}

	/// <summary>
	/// This overload throws because <see cref="IParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static T Parse<T>(string s, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support parsing.");
	}

	/// <summary>
	/// Delegates to <see cref="IParsable{TSelf}.Parse"/>.
	/// </summary>
	public static T Parse<T>(string s, IFormatProvider? provider)
		where T : IParsable<T>
	{
		return T.Parse(s, provider);
	}

	/// <summary>
	/// This overload throws because <see cref="ISpanParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static bool TryParse<T>(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out T? result,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support span parsing.");
	}

	/// <summary>
	/// Delegates to <see cref="ISpanParsable{TSelf}.TryParse"/>.
	/// </summary>
	public static bool TryParse<T>(ReadOnlySpan<char> s, IFormatProvider? provider, [NotNullWhen(true)] out T? result)
		where T : ISpanParsable<T>
	{
		return T.TryParse(s, provider, out result);
	}

	/// <summary>
	/// This overload throws because <see cref="ISpanParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static T Parse<T>(ReadOnlySpan<char> s, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support span parsing.");
	}

	/// <summary>
	/// Delegates to <see cref="ISpanParsable{TSelf}.Parse"/>.
	/// </summary>
	public static T Parse<T>(ReadOnlySpan<char> s, IFormatProvider? provider)
		where T : ISpanParsable<T>
	{
		return T.Parse(s, provider);
	}

#endif

#if NET8_0_OR_GREATER

#pragma warning disable IDE0060 // Remove unused parameter -- Required to let generated code make use of overload resolution
	/// <summary>
	/// <para>
	/// For strings, this overload tries to parse a span of UTF-8 characters into a string.
	/// </para>
	/// <para>
	/// For other types, this overload throws because <see cref="IUtf8SpanParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </para>
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static bool TryParse<T>(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [NotNullWhen(true)] out T? result,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		if (typeof(T) == typeof(string))
		{
			if (!Utf8.IsValid(utf8Text))
			{
				result = default;
				return false;
			}

			result = (T)(object)Encoding.UTF8.GetString(utf8Text);
			return true;
		}

		throw new NotSupportedException($"Type {typeof(T).Name} does not support UTF-8 span parsing.");
	}
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// Delegates to <see cref="IUtf8SpanParsable{TSelf}.TryParse"/>.
	/// </summary>
	public static bool TryParse<T>(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [NotNullWhen(true)] out T? result)
		where T : IUtf8SpanParsable<T>
	{
		return T.TryParse(utf8Text, provider, out result);
	}

#pragma warning disable IDE0060 // Remove unused parameter -- Required to let generated code make use of overload resolution
	/// <summary>
	/// <para>
	/// For strings, this overload parses a span of UTF-8 characters into a string.
	/// </para>
	/// <para>
	/// For other types, this overload throws because <see cref="IUtf8SpanParsable{TSelf}"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </para>
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static T Parse<T>(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		if (typeof(T) == typeof(string))
			return (T)(object)Encoding.UTF8.GetString(utf8Text);

		throw new NotSupportedException($"Type {typeof(T).Name} does not support UTF-8 span parsing.");
	}
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// Delegates to <see cref="IUtf8SpanParsable{TSelf}.Parse"/>.
	/// </summary>
	public static T Parse<T>(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
		where T : IUtf8SpanParsable<T>
	{
		return T.Parse(utf8Text, provider);
	}

#endif
}
