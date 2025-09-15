using Architect.DomainModeling.Comparisons;

namespace Architect.DomainModeling;

// For backward compatibility, these methods still exist on ValueObject

public abstract partial class ValueObject
{
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
		return ValueObjectStringValidator.ContainsNonAlphanumericCharacters(text);
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
		return ValueObjectStringValidator.ContainsNonWordCharacters(text);
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
		return ValueObjectStringValidator.ContainsNonAsciiCharacters(text);
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
		return ValueObjectStringValidator.ContainsNonAsciiOrNonPrintableCharacters(text, flagNewLinesAndTabs);
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
		return ValueObjectStringValidator.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(text);
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
		return ValueObjectStringValidator.ContainsNonPrintableCharacters(text, flagNewLinesAndTabs);
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
		return ValueObjectStringValidator.ContainsNonPrintableCharactersOrDoubleQuotes(text, flagNewLinesAndTabs);
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
		return ValueObjectStringValidator.ContainsWhitespaceOrNonPrintableCharacters(text);
	}
}
