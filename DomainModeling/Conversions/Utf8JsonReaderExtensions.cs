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
	public static T GetParsedString<T>(this Utf8JsonReader reader, IFormatProvider? provider)
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
}
