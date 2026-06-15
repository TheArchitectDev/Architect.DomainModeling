using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// Provides default implementations for <see cref="IFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface IFormattableWrapper<TWrapper, TValue> : IFormattable
	where TWrapper : IFormattableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : IFormattable?
{
	/// <summary>
	/// Beware: <see cref="IFormattable"/>.<see cref="IFormattable.ToString"/> promises a non-null result, but not all cases can correctly fulfill that promise.
	/// Specifically, a wrapper containing a null value can provide no correct answer here other than null.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		// This is tricky if the underlying value is null, such as when a struct wraps a reference type and it is spawned with the "default" keyword
		// TryFormat() does not have an issue: it is correct to write 0 chars when there is nothing to write
		// ToString() does have an issue: it is incorrect to represent nothing as any string other than null

		// The problem originates from the interface: IFormattable.ToString() returning a non-nullable string is a false promise, as not every scenario can fulfill this with a correct answer
		// Either this was an oversight by the .NET team, or they made a trade-off: a very occasional incorrectness of the nullability in exchange for simplicity for the vast majority of cases
		// Either way, the most correct and accurate resolution is to return null after all (thus acknowledging the oversight or trade-off)

		var value = ((TWrapper)this).Value;
		var result = value?.ToString(format, formatProvider);
		return result!;
	}
}

/// <summary>
/// Provides default implementations for <see cref="ISpanFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface ISpanFormattableWrapper<TWrapper, TValue> : IFormattableWrapper<TWrapper, TValue>, ISpanFormattable
	where TWrapper : ISpanFormattableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : ISpanFormattable?
{
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		var value = ((TWrapper)this).Value;
		if (value is null)
		{
			charsWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}
		var result = value.TryFormat(destination, out charsWritten, format, provider);
		return result;
	}
}

/// <summary>
/// Provides default implementations for <see cref="IUtf8SpanFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </summary>
public interface IUtf8SpanFormattableWrapper<TWrapper, TValue> : IUtf8SpanFormattable
	where TWrapper : IUtf8SpanFormattableWrapper<TWrapper, TValue>, IValueWrapper<TWrapper, TValue>
	where TValue : IUtf8SpanFormattable?
{
	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		var value = ((TWrapper)this).Value;
		if (value is null)
		{
			bytesWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}
		var result = value.TryFormat(utf8Destination, out bytesWritten, format, provider);
		return result;
	}
}

#region Strings

/// <summary>
/// Provides default implementations for <see cref="IFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface IFormattableStringWrapper<TWrapper> : IFormattable
	where TWrapper : IFormattableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
	{
		var value = ((TWrapper)this).Value;
		return value!;
	}
}

/// <summary>
/// Provides default implementations for <see cref="ISpanFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface ISpanFormattableStringWrapper<TWrapper> : IFormattableStringWrapper<TWrapper>, ISpanFormattable
	where TWrapper : ISpanFormattableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		var value = ((TWrapper)this).Value;

		charsWritten = 0;

		if (value is null)
			return true; // We succeeded at doing all we must - false is only for insufficient space

		if (value.Length > destination.Length)
			return false;

		value.AsSpan().CopyTo(destination);
		charsWritten = value.Length;
		return true;
	}
}

/// <summary>
/// Provides default implementations for <see cref="IUtf8SpanFormattable"/> for an <see cref="IValueWrapper{TWrapper, TValue}"/> wrapping a <see cref="String"/>.
/// </summary>
public interface IUtf8SpanFormattableStringWrapper<TWrapper> : IUtf8SpanFormattable
	where TWrapper : IUtf8SpanFormattableStringWrapper<TWrapper>, IValueWrapper<TWrapper, string>
{
	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		var value = ((TWrapper)this).Value;

		if (value is null)
		{
			bytesWritten = 0;
			return true; // We succeeded at doing all we must - false is only for insufficient space
		}

		var success = Utf8.FromUtf16(value, utf8Destination, charsRead: out _, bytesWritten: out bytesWritten) == System.Buffers.OperationStatus.Done;
		return success;
	}
}

#endregion
