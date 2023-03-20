namespace Architect.DomainModeling.Conversions;

/// <summary>
/// Provides formatting-related extension methods on formattable types.
/// </summary>
public static class FormattingExtensions
{
#if NET7_0_OR_GREATER
	/// <summary>
	/// <para>
	/// Formats the <paramref name="value"/> into the provided <paramref name="buffer"/>, returning the segment that was written to.
	/// </para>
	/// <para>
	/// If there is not enough space in the <paramref name="buffer"/>, instead a new string is allocated and returned as a span.
	/// </para>
	/// </summary>
	/// <param name="value">The value to format.</param>
	/// <param name="buffer">The buffer to attempt to format into. For best performance, it is advisable to use a stack-allocated buffer that is expected to be large enough, e.g. 64 chars.</param>
	/// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format.</param>
	/// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
	public static ReadOnlySpan<char> Format<T>(this T value, Span<char> buffer, ReadOnlySpan<char> format, IFormatProvider? provider)
		where T : notnull, ISpanFormattable
	{
		if (!value.TryFormat(buffer, out var charCount, format, provider))
			return value.ToString().AsSpan();

		return buffer[..charCount];
	}
#endif
}
