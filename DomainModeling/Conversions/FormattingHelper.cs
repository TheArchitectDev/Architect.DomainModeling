using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// <para>
/// Delegates to *Formattable interfaces depending on their presence on a given type parameter.
/// Uses overload resolution to avoid a compiler error where the interface is missing.
/// </para>
/// <para>
/// This type is intended for use by source-generated code, to avoid compiler errors in situations where the presence of the required interfaces is extremely likely but cannot be guaranteed.
/// </para>
/// </summary>
//#if NET10_0_OR_GREATER
//[Obsolete("New default interface implementations and extension members alleviate the need for this helper.")]
//#endif
public static class FormattingHelper
{
	/// <summary>
	/// This overload throws because <see cref="IFormattable"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static string ToString<T>(T? instance,
		string? format, IFormatProvider? formatProvider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support formatting.");
	}

	/// <summary>
	/// Delegates to <see cref="IFormattable.ToString"/>.
	/// </summary>
	[return: NotNullIfNotNull(nameof(instance))]
	public static string ToString<T>(T? instance,
		string? format, IFormatProvider? formatProvider)
		where T : IFormattable
	{
		// We exist to help fulfill IFormattable.ToString()
		// We imitate its false promise that the string will be non-null if there is an instance

		// This is tricky if the underlying value is null, such as when a struct wraps a reference type and it is spawned with the "default" keyword
		// TryFormat() does not have an issue: it is correct to write 0 chars when there is nothing to write
		// ToString() does have an issue: it is incorrect to represent nothing as any string other than null

		// The problem originates from the interface: IFormattable.ToString() returning a non-nullable string is a false promise, as not every scenario can fulfill this with a correct answer
		// Either this was an oversight by the .NET team, or they made a trade-off: a very occasional incorrectness of the nullability in exchange for simplicity for the vast majority of cases
		// Either way, the most correct and accurate resolution is to return null after all (thus acknowledging the oversight or trade-off)

		return instance?.ToString(format, formatProvider)!;
	}

#pragma warning disable IDE0060 // Remove unused parameter -- Required to let generated code make use of overload resolution
	/// <summary>
	/// <para>
	/// Returns the input string.
	/// </para>
	/// <para>
	/// This overload exists to avoid a special case for strings, which do not implement <see cref="IFormattable"/>.
	/// </para>
	/// </summary>
	/// <param name="format">Ignored.</param>
	/// <param name="formatProvider">Ignored.</param>
	[return: NotNullIfNotNull(nameof(instance))]
	public static string? ToString(string? instance,
		string? format, IFormatProvider? formatProvider)
	{
		return instance;
	}
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// This overload throws because <see cref="ISpanFormattable"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static bool TryFormat<T>(T? instance,
		Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support span formatting.");
	}

	/// <summary>
	/// Delegates to <see cref="ISpanFormattable.TryFormat"/>.
	/// </summary>
	public static bool TryFormat<T>(T? instance,
		Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		where T : ISpanFormattable
	{
		if (instance is null)
		{
			charsWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}

		return instance.TryFormat(destination, out charsWritten, format, provider);
	}

#pragma warning disable IDE0060 // Remove unused parameter -- Required to let generated code make use of overload resolution
	/// <summary>
	/// <para>
	/// Tries to write the string into the provided span of characters.
	/// </para>
	/// <para>
	/// This overload exists to avoid a special case for strings, which do not implement <see cref="ISpanFormattable"/>.
	/// </para>
	/// </summary>
	/// <param name="format">Ignored.</param>
	/// <param name="provider">Ignored.</param>
	public static bool TryFormat(string? instance,
		Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		charsWritten = 0;

		if (instance is null)
			return true; // We succeeded at doing all we must - false is only for insufficient space

		if (instance.Length > destination.Length)
			return false;

		instance.AsSpan().CopyTo(destination);
		charsWritten = instance.Length;
		return true;
	}
#pragma warning restore IDE0060 // Remove unused parameter

	/// <summary>
	/// This overload throws because <see cref="IUtf8SpanFormattable"/> is unavailable.
	/// Implement the interface to have overload resolution pick the functional overload.
	/// </summary>
	/// <param name="callerLineNumber">Used only for overload resolution.</param>
	public static bool TryFormat<T>(T? instance,
		Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider,
		[CallerLineNumber] int callerLineNumber = -1)
	{
		throw new NotSupportedException($"Type {typeof(T).Name} does not support UTF-8 span formatting.");
	}

	/// <summary>
	/// Delegates to <see cref="IUtf8SpanFormattable.TryFormat"/>.
	/// </summary>
	public static bool TryFormat<T>(T? instance,
		Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		where T : IUtf8SpanFormattable
	{
		if (instance is null)
		{
			bytesWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}

		return instance.TryFormat(utf8Destination, out bytesWritten, format, provider);
	}

#pragma warning disable IDE0060 // Remove unused parameter -- Required to let generated code make use of overload resolution
	/// <summary>
	/// <para>
	/// Tries to write the string into the provided span of bytes.
	/// </para>
	/// <para>
	/// This overload exists to avoid a special case for strings, which do not implement <see cref="IUtf8SpanFormattable"/>.
	/// </para>
	/// </summary>
	/// <param name="format">Ignored.</param>
	/// <param name="provider">Ignored.</param>
	public static bool TryFormat(string instance,
		Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		if (instance is null)
		{
			bytesWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}

		return Utf8.FromUtf16(instance, utf8Destination, charsRead: out _, bytesWritten: out bytesWritten) == System.Buffers.OperationStatus.Done;
	}
#pragma warning restore IDE0060 // Remove unused parameter
}
