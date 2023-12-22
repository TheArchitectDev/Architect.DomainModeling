using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// Provides conversion-related extension methods on <see cref="Utf8JsonReader"/>.
/// </summary>
public static class Utf8JsonReaderExtensions
{
#if NET7_0_OR_GREATER
	/// <summary>
	/// Reads the next string JSON token from the source and parses it as <typeparamref name="T"/>, which must implement <see cref="ISpanParsable{TSelf}"/>.
	/// </summary>
	/// <param name="reader">A <see cref="Utf8JsonReader"/> that is ready to read a property name.</param>
	/// <param name="provider">An object that provides culture-specific formatting information about the input string.</param>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static T GetParsedString<T>(this Utf8JsonReader reader, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
		where T : ISpanParsable<T>
	{
		ReadOnlySpan<char> chars = stackalloc char[0];

		var maxCharLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
		if (maxCharLength > 2048) // Avoid oversized stack allocations
		{
			chars = reader.GetString().AsSpan();
		}
		else
		{
			Span<char> buffer = stackalloc char[(int)maxCharLength];
			var charCount = reader.CopyString(buffer);
			chars = buffer[..charCount];
		}

		var result = T.Parse(chars, provider);
		return result;
	}
#endif

#if NET8_0_OR_GREATER
	/// <summary>
	/// Reads the next string JSON token from the source and parses it as <typeparamref name="T"/>, which must implement <see cref="IUtf8SpanParsable{TSelf}"/>.
	/// </summary>
	/// <param name="reader">A <see cref="Utf8JsonReader"/> that is ready to read a property name.</param>
	/// <param name="provider">An object that provides culture-specific formatting information about the input string.</param>
	public static T GetParsedString<T>(this Utf8JsonReader reader, IFormatProvider? provider)
		where T : IUtf8SpanParsable<T>
	{
		ReadOnlySpan<byte> chars = reader.HasValueSequence
			? stackalloc byte[0]
			: reader.ValueSpan;

		if (reader.HasValueSequence)
		{
			if (reader.ValueSequence.Length > 2048) // Avoid oversized stack allocations
			{
				chars = reader.ValueSequence.ToArray();
			}
			else
			{
				Span<byte> buffer = stackalloc byte[(int)reader.ValueSequence.Length];
				reader.ValueSequence.CopyTo(buffer);
				chars = buffer;
			}
		}

		var result = T.Parse(chars, provider);
		return result;
	}
#endif
}
