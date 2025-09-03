#if NET10_0_OR_GREATER

// Note: In the .NET 10 preview, this type resulted in: warning AD0001: Analyzer 'ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer' threw an exception of type 'System.InvalidCastException' with message 'Unable to cast object of type 'Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.NonErrorNamedTypeSymbol' to type 'Microsoft.CodeAnalysis.IMethodSymbol'.'.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Architect.DomainModeling.Conversions;

/// <summary>
/// <para>
/// Provides formatting methods on types marked with <see cref="IValueWrapper{TWrapper, TValue}"/>.
/// </para>
/// <para>
/// <see cref="IFormattableWrapper{TWrapper, TValue}"/> &amp; co provide default interface implementations that alleviate the need to implement formatting methods manually for value wrappers.
/// However, that only works using explicit interface implementations, which can only be accessed through the interface or via generics.
/// </para>
/// <para>
/// These extensions provide access to the default interface implementations directly from the wrapper type.
/// </para>
/// </summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression -- Suppression below is falsely flagged as unnecessary
#pragma warning disable CA1050 // Declare types in namespaces -- Lives in global namespace for visibility of extensions, on highly specific types
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArchitectDomainModelingValueWrapperFormattingExtensions
{
	// #TODO: Remove outcommented!!! And simplify region names, if need regions at all.
	#region IFormattable - Preferred

	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : IFormattable, IValueWrapper<TWrapper, TValue>
	{
		/// <summary>
		/// Beware: <see cref="IFormattable"/>.<see cref="IFormattable.ToString"/> promises a non-null result, but not all cases can correctly fulfill that promise.
		/// Specifically, a wrapper containing a null value can provide no correct answer here other than null.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			return ((TWrapper)wrapper).ToString(format, formatProvider);
		}
	}

	#endregion
	
	//#region IFormattable - String

	//extension<TWrapper>(IValueWrapper<TWrapper, string> wrapper)
	//	where TWrapper : IFormattable, IValueWrapper<TWrapper, string>
	//{
	//	public string ToString(string? format, IFormatProvider? formatProvider)
	//	{
	//		return ((TWrapper)wrapper).ToString(format, formatProvider);
	//	}
	//}

	//#endregion

//	#region IFormattable - Without IFormattable underlying value

//	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
//		where TWrapper : ISpanFormattable, IValueWrapper<TWrapper, TValue>
//	{
//#pragma warning disable IDE0060 // Remove unused parameter -- Required for less-preferred overload resolution
//		[Obsolete("This type must manually implement IFormattable, since the wrapped underlying type does not implement IFormattable.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public string ToString(string? format, IFormatProvider? formatProvider,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support formatting.");
//		}
//#pragma warning restore IDE0060 // Remove unused parameter
//	}

//	#endregion
	
	#region ISpanFormattable - Preferred

	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : ISpanFormattable, IValueWrapper<TWrapper, TValue>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		{
			return ((TWrapper)wrapper).TryFormat(destination, out charsWritten, format, provider);
		}
	}

	#endregion

	//#region ISpanFormattable - String

	//extension<TWrapper>(IValueWrapper<TWrapper, string> wrapper)
	//	where TWrapper : ISpanFormattable, IValueWrapper<TWrapper, string>
	//{
	//	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	//	{
	//		return ((TWrapper)wrapper).TryFormat(destination, out charsWritten, format, provider);
	//	}
	//}

	//#endregion

//	#region ISpanFormattable - Without ISpanFormattable underlying value

//	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
//		where TWrapper : ISpanFormattable, IValueWrapper<TWrapper, TValue>
//	{
//#pragma warning disable IDE0060 // Remove unused parameter -- Required for less-preferred overload resolution
//		[Obsolete("This type must manually implement ISpanFormattable, since the wrapped underlying type does not implement ISpanFormattable.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support span formatting.");
//		}
//#pragma warning restore IDE0060 // Remove unused parameter
//	}

//	#endregion

	#region IUtf8SpanFormattable - Preferred

	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
		where TWrapper : IUtf8SpanFormattable, IValueWrapper<TWrapper, TValue>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
		{
			return ((TWrapper)wrapper).TryFormat(utf8Destination, out bytesWritten, format, provider);
		}
	}

	#endregion

	//#region IUtf8SpanFormattable - String

	//extension<TWrapper>(IValueWrapper<TWrapper, string> wrapper)
	//	where TWrapper : IUtf8SpanFormattable, IValueWrapper<TWrapper, string>
	//{
	//	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	//	{
	//		return ((TWrapper)wrapper).TryFormat(utf8Destination, out bytesWritten, format, provider);
	//	}
	//}

	//#endregion

//	#region IUtf8SpanFormattable - Without IUtf8SpanFormattable underlying value

//	extension<TWrapper, TValue>(IValueWrapper<TWrapper, TValue> wrapper)
//		where TWrapper : IUtf8SpanFormattable, IValueWrapper<TWrapper, TValue>
//	{
//#pragma warning disable IDE0060 // Remove unused parameter -- Required for less-preferred overload resolution
//		[Obsolete("This type must manually implement IUtf8SpanFormattable, since the wrapped underlying type does not implement IUtf8SpanFormattable.", error: true)]
//		[OverloadResolutionPriority(Int32.MinValue)]
//		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider,
//			[CallerLineNumber] int callerLineNumber = -1)
//		{
//			throw new NotSupportedException($"Type {typeof(TWrapper).Name} does not support UTF-8 span formatting.");
//		}
//#pragma warning restore IDE0060 // Remove unused parameter
//	}

//	#endregion
}

#endif
