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
	// #TODO: Remove outcommented!!! And simplify region names, if need regions at all.
	#region IParsable - Preferred

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

	#endregion

	#region ISpanParsable - Preferred

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

	#endregion

//	#region ISpanParsable - Without ISpanParsable underlying value

//	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
//		where TWrapper : ISpanParsable<TWrapper>, IValueWrapper<TWrapper, TValue>
//	{
//#pragma warning disable IDE0060 // Remove unused parameter -- Required for less-preferred overload resolution
//		[Obsolete("This type must manually implement ISpanParsable<T>, since the wrapped underlying type does not implement ISpanParsable<T>.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support span parsing.");
//		}

//		[Obsolete("ISpanParsable<T> was not properly implemented on this type.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public static TWrapper Parse(ReadOnlySpan<char> s, IFormatProvider? provider,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support span parsing.");
//		}
//#pragma warning restore IDE0060 // Remove unused parameter
//	}

//	#endregion

	#region IUtf8SpanParsable - Preferred

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

	#endregion

	//#region IUtf8SpanParsable - String

	//extension<TWrapper>(IValueWrapper<TWrapper, string> wrapper)
	//	where TWrapper : IUtf8SpanParsable<TWrapper>, IValueWrapper<TWrapper, string>
	//{
	//	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result)
	//	{
	//		return TWrapper.TryParse(utf8Text, provider, out result);
	//	}

	//	public static TWrapper Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	//	{
	//		return TWrapper.Parse(utf8Text, provider);
	//	}
	//}

	//#endregion

//	#region IUtf8SpanParsable - Without IUtf8SpanParsable underlying value

//	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
//		where TWrapper : IUtf8SpanParsable<TWrapper>, IValueWrapper<TWrapper, TValue>
//	{
//#pragma warning disable IDE0060 // Remove unused parameter -- Required for less-preferred overload resolution
//		[Obsolete("This type must manually implement IUtf8SpanParsable<T>, since the wrapped underlying type does not implement IUtf8SpanParsable<T>.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out TWrapper result,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support UTF-8 span parsing.");
//		}

//		[Obsolete("This type must manually implement IUtf8SpanParsable<T>, since the wrapped underlying type does not implement IUtf8SpanParsable<T>.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public static TWrapper Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support UTF-8 span parsing.");
//		}
//#pragma warning restore IDE0060 // Remove unused parameter
//	}

//	#endregion
}

#endif
