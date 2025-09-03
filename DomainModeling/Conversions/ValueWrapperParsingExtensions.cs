#if NET10_0_OR_GREATER

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Architect.DomainModeling.Conversions;

/// <summary>
/// <para>
/// Provides parsing methods on types marked with <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </para>
/// <para>
/// <see cref="IParsableWrapper{TWrapper, TValue}"/> &amp; co provide default interface implementations that alleviate the need to implement parse methods manually for value wrappers.
/// However, that only works using explicit interface implementations, which can only be accessed through the interface or via generics.
/// </para>
/// <para>
/// These extensions provide access to the default interface implementations directly from the wrapper type.
/// </para>
/// </summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression -- Suppression below is falsely flagged as unnecessary
#pragma warning disable CA1050 // Declare types in namespaces -- Lives in global namespace for visibility of extensions, on highly specific types
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArchitectDomainModelingValueWrapperParsingExtensions
{
	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : IParsable<TWrapper>, IValueWrapper<TWrapper, TValue>
	{
		[OverloadResolutionPriority(-1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryParse(string s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
		{
			return TWrapper.TryParse(s, provider, out result);
		}

		[OverloadResolutionPriority(-1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TWrapper Parse(string s, IFormatProvider? provider)
		{
			return TWrapper.Parse(s, provider);
		}
	}

	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : ISpanParsable<TWrapper>, IValueWrapper<TWrapper, TValue>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
		{
			return TWrapper.TryParse(s, provider, out result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TWrapper Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
		{
			return TWrapper.Parse(s, provider);
		}
	}

	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : IUtf8SpanParsable<TWrapper>, IValueWrapper<TWrapper, TValue>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
		{
			return TWrapper.TryParse(utf8Text, provider, out result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TWrapper Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
		{
			return TWrapper.Parse(utf8Text, provider);
		}
	}
}

#endif
