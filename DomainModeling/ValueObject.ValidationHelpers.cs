using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Architect.DomainModeling;

public abstract partial class ValueObject
{
	// Note: Most methods in this class expect to reach their final return statement, so they optimize for that case with logical instead of conditional operators, to reduce branching

	/// <summary>
	/// A vector filled completely with the ASCII null character's value (0).
	/// </summary>
	private static readonly Vector<ushort> AsciiNullValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)0U, Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the ' ' (space) character's value (32).
	/// </summary>
	private static readonly Vector<ushort> SpaceValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)' ', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the ASCII zero digit character's value (48).
	/// </summary>
	private static readonly Vector<ushort> ZeroDigitValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)'0', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the ASCII nine digit character's value (57).
	/// </summary>
	private static readonly Vector<ushort> NineDigitValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)'9', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the '_' character's value (95).
	/// </summary>
	private static readonly Vector<ushort> UnderscoreValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)'_', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the 'a' character's value (97).
	/// </summary>
	private static readonly Vector<ushort> LowercaseAValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)'a', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the 'z' character's value (122).
	/// </summary>
	private static readonly Vector<ushort> LowercaseZValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)'z', Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with the greatest ASCII character's value (127).
	/// </summary>
	private static readonly Vector<ushort> MaxAsciiValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)SByte.MaxValue, Vector<ushort>.Count).ToArray())[0];
	/// <summary>
	/// A vector filled completely with a character that, when binary OR'ed with an ASCII letter, results in the corresponding lowercase letter.
	/// </summary>
	private static readonly Vector<ushort> ToLowercaseAsciiValueVector = MemoryMarshal.Cast<ushort, Vector<ushort>>(Enumerable.Repeat((ushort)0b100000U, Vector<ushort>.Count).ToArray())[0];

	/// <summary>
	/// <para>
	/// This method detects non-alphanumeric characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of ASCII letters/digits.
	/// </para>
	/// </summary>
	protected static bool ContainsNonAlphanumericCharacters(ReadOnlySpan<char> text)
	{
		var remainder = text.Length;

		// Attempt to process most of the input with SIMD
		if (Vector.IsHardwareAccelerated)
		{
			remainder %= Vector<ushort>.Count;

			var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

			foreach (var vector in vectors)
			{
				var lowercaseVector = Vector.BitwiseOr(vector, ToLowercaseAsciiValueVector);

				// Flagged (true) if any non-zero
				if (Vector.GreaterThanAny(
					// Non-alphanumeric (i.e. outside of alphanumeric range)
					Vector.BitwiseAnd(
						// Outside range 0-9
						Vector.BitwiseOr(
							Vector.LessThan(vector, ZeroDigitValueVector),
							Vector.GreaterThan(vector, NineDigitValueVector)),
						// Outside range [a-zA-Z]
						Vector.BitwiseOr(
							Vector.LessThan(lowercaseVector, LowercaseAValueVector),
							Vector.GreaterThan(lowercaseVector, LowercaseZValueVector))),
					AsciiNullValueVector))
				{
					return true;
				}
			}
		}

		for (var i = text.Length - remainder; i < text.Length; i++)
		{
			uint chr = text[i];

			if (CharIsOutsideRange(chr, '0', '9') & // Not 0-9
				CharIsOutsideRange(chr | 0b100000U, 'a', 'z')) // Not A-Z in any casing (setting the 6th bit changes uppercase into lowercase)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// <para>
	/// This method detects non-word characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of [0-9A-Za-z_], i.e. ASCII letters/digits/underscores.
	/// </para>
	/// </summary>
	protected static bool ContainsNonWordCharacters(ReadOnlySpan<char> text)
	{
		var remainder = text.Length;

		// Attempt to process most of the input with SIMD
		if (Vector.IsHardwareAccelerated)
		{
			remainder %= Vector<ushort>.Count;

			var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

			foreach (var vector in vectors)
			{
				var lowercaseVector = Vector.BitwiseOr(vector, ToLowercaseAsciiValueVector);

				// Flagged (true) if any non-zero
				if (Vector.GreaterThanAny(
					// Xor results in zero (not flagged) for underscores (non-alphanumeric=1, underscore=1) and alphanumerics (non-alphanumeric=0, underscore=0)
					// Xor results in one (flagged) otherwise
					Vector.Xor(
						// Non-alphanumeric (i.e. outside of alphanumeric range)
						Vector.BitwiseAnd(
							// Outside range 0-9
							Vector.BitwiseOr(
								Vector.LessThan(vector, ZeroDigitValueVector),
								Vector.GreaterThan(vector, NineDigitValueVector)),
							// Outside range [a-zA-Z]
							Vector.BitwiseOr(
								Vector.LessThan(lowercaseVector, LowercaseAValueVector),
								Vector.GreaterThan(lowercaseVector, LowercaseZValueVector))),
						// An underscore
						Vector.Equals(vector, UnderscoreValueVector)),
					AsciiNullValueVector))
				{
					return true;
				}
			}
		}

		for (var i = text.Length - remainder; i < text.Length; i++)
		{
			uint chr = text[i];

			if (CharIsOutsideRange(chr, '0', '9') & // Not 0-9
				CharIsOutsideRange(chr | 0b100000U, 'a', 'z') & // Not A-Z in any casing (setting the 6th bit changes uppercase into lowercase)
				chr != '_') // Not the underscore
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// <para>
	/// This method detects non-ASCII characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of ASCII characters.
	/// </para>
	/// </summary>
	protected static bool ContainsNonAsciiCharacters(ReadOnlySpan<char> text)
	{
		var remainder = text.Length;

		// Attempt to process most of the input with SIMD
		if (Vector.IsHardwareAccelerated)
		{
			remainder %= Vector<ushort>.Count;

			var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

			foreach (var vector in vectors)
				if (Vector.GreaterThanAny(vector, MaxAsciiValueVector))
					return true;
		}

		// Process the remainder char-by-char
		const uint maxAsciiChar = (uint)SByte.MaxValue;
		foreach (var chr in text[^remainder..])
			if (chr > maxAsciiChar)
				return true;

		return false;
	}

	/// <summary>
	/// <para>
	/// This method detects non-printable characters and non-ASCII characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of printable ASCII characters.
	/// </para>
	/// </summary>
	/// <param name="flagNewLinesAndTabs">Pass true (default) to flag \r, \n, and \t as non-printable characters. Pass false to overlook them.</param>
	protected static bool ContainsNonAsciiOrNonPrintableCharacters(ReadOnlySpan<char> text, bool flagNewLinesAndTabs = true)
	{
		// ASCII chars below ' ' (32) are control characters
		// ASCII char SByte.MaxValue (127) is a control character
		// Characters above SByte.MaxValue (127) are non-ASCII

		if (!flagNewLinesAndTabs)
			return EvaluateOverlookingNewLinesAndTabs(text);

		var remainder = text.Length;

		// Attempt to process most of the input with SIMD
		if (Vector.IsHardwareAccelerated)
		{
			remainder %= Vector<ushort>.Count;

			var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

			foreach (var vector in vectors)
				if (Vector.LessThanAny(vector, SpaceValueVector) | Vector.GreaterThanOrEqualAny(vector, MaxAsciiValueVector))
					return true;
		}

		// Process the remainder char-by-char
		const uint minChar = ' ';
		const uint maxChar = (uint)SByte.MaxValue - 1U;
		foreach (var chr in text[^remainder..])
			if (CharIsOutsideRange(chr, minChar, maxChar))
				return true;

		return false;

		// Local function that performs the work while overlooking \r, \n, and \t characters
		static bool EvaluateOverlookingNewLinesAndTabs(ReadOnlySpan<char> text)
		{
			var remainder = text.Length;

			// Attempt to process most of the input with SIMD
			if (Vector.IsHardwareAccelerated)
			{
				remainder %= Vector<ushort>.Count;

				var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

				foreach (var vector in vectors)
				{
					// If the vector contains any non-ASCII or non-printable characters
					if (Vector.LessThanAny(vector, SpaceValueVector) | Vector.GreaterThanOrEqualAny(vector, MaxAsciiValueVector)) // Usually false, so short-circuit
					{
						for (var i = 0; i < Vector<ushort>.Count; i++)
						{
							uint chr = vector[i];

							if (CharIsOutsideRange(chr, minChar, maxChar) && // Usually false, so short-circuit
								(CharIsOutsideRange(chr, '\t', '\n') & chr != '\r'))
								return true;
						}
					}
				}
			}

			// Process the remainder char-by-char
			for (var i = text.Length - remainder; i < text.Length; i++)
			{
				uint chr = text[i];

				if (CharIsOutsideRange(chr, minChar, maxChar) && // Usually false, so short-circuit
					(CharIsOutsideRange(chr, '\t', '\n') & chr != '\r'))
					return true;
			}

			return false;
		}
	}

	/// <summary>
	/// <para>
	/// This method detects non-printable characters, whitespace characters, and non-ASCII characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of printable ASCII characters that are not whitespace.
	/// </para>
	/// </summary>
	protected static bool ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(ReadOnlySpan<char> text)
	{
		// Characters above SByte.MaxValue (127) are non-ASCII
		// ASCII char SByte.MaxValue (127) is a control character
		// ASCII chars ' ' (32) and below are all the other control chars and all whitespace chars

		var remainder = text.Length;

		// Attempt to process most of the input with SIMD
		if (Vector.IsHardwareAccelerated)
		{
			remainder %= Vector<ushort>.Count;

			var vectors = MemoryMarshal.Cast<char, Vector<ushort>>(text[..^remainder]);

			foreach (var vector in vectors)
				if (Vector.LessThanOrEqualAny(vector, SpaceValueVector) | Vector.GreaterThanOrEqualAny(vector, MaxAsciiValueVector))
					return true;
		}

		// Process the remainder char-by-char
		const uint minChar = ' ' + 1U;
		const uint maxChar = (uint)SByte.MaxValue - 1U;
		foreach (var chr in text[^remainder..])
			if (CharIsOutsideRange(chr, minChar, maxChar))
				return true;

		return false;
	}

	/// <summary>
	/// <para>
	/// This method detects non-printable characters, such as control characters.
	/// It does <em>not</em> detect whitespace characters, even if they are zero-width.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of printable characters.
	/// </para>
	/// <para>
	/// </para>
	/// <para>
	/// A parameter controls whether this method flags newline and tab characters, allowing single-line vs. multiline input to be validated.
	/// </para>
	/// </summary>
	/// <param name="flagNewLinesAndTabs">Pass true to flag \r, \n, and \t as non-printable characters. Pass false to overlook them.</param>
	protected static bool ContainsNonPrintableCharacters(ReadOnlySpan<char> text, bool flagNewLinesAndTabs)
	{
		return flagNewLinesAndTabs
			? EvaluateIncludingNewLinesAndTabs(text)
			: EvaluateOverlookingNewLinesAndTabs(text);

		// Local function that performs the work while including \r, \n, and \t characters
		static bool EvaluateIncludingNewLinesAndTabs(ReadOnlySpan<char> text)
		{
			foreach (var chr in text)
			{
				var category = Char.GetUnicodeCategory(chr);

				if (category == UnicodeCategory.Control | category == UnicodeCategory.PrivateUse | category == UnicodeCategory.OtherNotAssigned)
					return true;
			}

			return false;
		}

		// Local function that performs the work while overlooking \r, \n, and \t characters
		static bool EvaluateOverlookingNewLinesAndTabs(ReadOnlySpan<char> text)
		{
			foreach (var chr in text)
			{
				var category = Char.GetUnicodeCategory(chr);

				if (category == UnicodeCategory.Control | category == UnicodeCategory.PrivateUse | category == UnicodeCategory.OtherNotAssigned)
				{
					if (chr == '\r' | chr == '\n' | chr == '\t') continue; // Exempt
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// <para>
	/// This method detects double quotes (") and non-printable characters, such as control characters.
	/// It does <em>not</em> detect whitespace characters, even if they are zero-width.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of printable characters that are not double quotes (").
	/// </para>
	/// <para>
	/// </para>
	/// <para>
	/// A parameter controls whether this method flags newline and tab characters, allowing single-line vs. multiline input to be validated.
	/// </para>
	/// </summary>
	/// <param name="flagNewLinesAndTabs">Pass true to flag \r, \n, and \t as non-printable characters. Pass false to overlook them.</param>
	protected static bool ContainsNonPrintableCharactersOrDoubleQuotes(ReadOnlySpan<char> text, bool flagNewLinesAndTabs)
	{
		return flagNewLinesAndTabs
			? EvaluateIncludingNewLinesAndTabs(text)
			: EvaluateOverlookingNewLinesAndTabs(text);

		// Local function that performs the work while including \r, \n, and \t characters
		static bool EvaluateIncludingNewLinesAndTabs(ReadOnlySpan<char> text)
		{
			foreach (var chr in text)
			{
				var category = Char.GetUnicodeCategory(chr);

				if (category == UnicodeCategory.Control | category == UnicodeCategory.PrivateUse | category == UnicodeCategory.OtherNotAssigned | chr == '"')
					return true;
			}

			return false;
		}

		// Local function that performs the work while overlooking \r, \n, and \t characters
		static bool EvaluateOverlookingNewLinesAndTabs(ReadOnlySpan<char> text)
		{
			foreach (var chr in text)
			{
				var category = Char.GetUnicodeCategory(chr);

				if (category == UnicodeCategory.Control | category == UnicodeCategory.PrivateUse | category == UnicodeCategory.OtherNotAssigned | chr == '"')
				{
					if (chr == '\r' | chr == '\n' | chr == '\t') continue; // Exempt
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// <para>
	/// This method detects whitespace characters and non-printable characters.
	/// </para>
	/// <para>
	/// It returns true, unless the given <paramref name="text"/> consists exclusively of printable characters that are not whitespace.
	/// </para>
	/// </summary>
	protected static bool ContainsWhitespaceOrNonPrintableCharacters(ReadOnlySpan<char> text)
	{
		// https://referencesource.microsoft.com/#mscorlib/system/globalization/charunicodeinfo.cs,9c0ae0026fafada0
		// 11=SpaceSeparator
		// 12=LineSeparator
		// 13=ParagraphSeparator
		// 14=Control
		const uint minValue = (uint)UnicodeCategory.SpaceSeparator;
		const uint maxValue = (uint)UnicodeCategory.Control;

		foreach (var chr in text)
		{
			var category = Char.GetUnicodeCategory(chr);

			if (ValueIsInRange((uint)category, minValue, maxValue) | category == UnicodeCategory.PrivateUse | category == UnicodeCategory.OtherNotAssigned)
				return true;
		}

		return false;
	}

	/// <summary>
	/// <para>
	/// Returns whether the given character is <em>outside</em> of the given range of values.
	/// Values equal to the minimum or maximum are considered to be inside the range.
	/// </para>
	/// <para>
	/// This method uses only a single comparison.
	/// </para>
	/// </summary>
	/// <param name="chr">The character to compare.</param>
	/// <param name="minValue">The minimum value considered inside the range.</param>
	/// <param name="maxValue">The maximum value considered inside the range.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool CharIsOutsideRange(uint chr, uint minValue, uint maxValue)
	{
		// The implementation is optimized to minimize the number of comparisons
		// By using uints, a negative value becomes a very large value
		// Then, by subtracting the range's min char (e.g. 'a'), only chars INSIDE the range have values 0 through (max-min), e.g. 0 through 25 (for 'a' through 'z')
		// To then check if the value is outside of the range, we can simply check if it is greater
		// See also https://referencesource.microsoft.com/#mscorlib/system/string.cs,289

		return chr - minValue > (maxValue - minValue);
	}

	/// <summary>
	/// <para>
	/// Returns whether the given value is <em>inside</em> of the given range of values.
	/// Values equal to the minimum or maximum are considered to be inside the range.
	/// </para>
	/// <para>
	/// This method uses only a single comparison.
	/// </para>
	/// </summary>
	/// <param name="value">The value to compare.</param>
	/// <param name="minValue">The minimum value considered inside the range.</param>
	/// <param name="maxValue">The maximum value considered inside the range.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ValueIsInRange(uint value, uint minValue, uint maxValue)
	{
		// The implementation is optimized to minimize the number of comparisons
		// By using uints, a negative value becomes a very large value
		// Then, by subtracting the range's min char (e.g. 'a'), only chars INSIDE the range have values 0 through (max-min), e.g. 0 through 25 (for 'a' through 'z')
		// To then check if the value is outside of the range, we can simply check if it is greater
		// See also https://referencesource.microsoft.com/#mscorlib/system/string.cs,289

		return value - minValue <= (maxValue - minValue);
	}
}
