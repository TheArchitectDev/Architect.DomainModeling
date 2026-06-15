using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// A generic System.Text JSON converter for wrapper types, which serializes like the wrapped value itself.
/// </summary>
[UnconditionalSuppressMessage(
	"Trimming", "IL2046:All interface implementations and method overrides must have annotations matching the interface or overridden virtual method 'RequiresUnreferencedCodeAttribute' annotations",
	Justification = "Unlike our base, we prefer to annotate the methods instead of the type, to avoid a warning merely because source-generated code includes a JSON converter."
)]
public sealed class ValueWrapperJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TValue>
	: System.Text.Json.Serialization.JsonConverter<TWrapper>
	where TWrapper : IValueWrapper<TWrapper, TValue>
{
	private const string RequiresUnreferencedCodeMessage = "Serialization requires unreferenced code.";
	private const string RequiresDynamicCodeMessage = "Serialization requires dynamic code.";

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		var value = System.Text.Json.JsonSerializer.Deserialize<TValue>(ref reader, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void Write(System.Text.Json.Utf8JsonWriter writer, TWrapper value, System.Text.Json.JsonSerializerOptions options)
	{
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TValue>(value);
		System.Text.Json.JsonSerializer.Serialize(writer, serializedValue, options);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		var value = ((System.Text.Json.Serialization.JsonConverter<TValue>)options.GetConverter(typeof(TValue))).ReadAsPropertyName(ref reader, typeToConvert, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, TWrapper value, System.Text.Json.JsonSerializerOptions options)
	{
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TValue>(value)!;
		((System.Text.Json.Serialization.JsonConverter<TValue>)options.GetConverter(typeof(TValue))).WriteAsPropertyName(
			writer,
			serializedValue,
			options);
	}
}

/// <summary>
/// A generic System.Text JSON converter for wrapper types around numerics, which serializes like the wrapped value itself.
/// This variant is intended for numeric types whose larger values risk truncation in languages such as JavaScript.
/// It serializes to and from string.
/// </summary>
[UnconditionalSuppressMessage(
	"Trimming", "IL2046:All interface implementations and method overrides must have annotations matching the interface or overridden virtual method 'RequiresUnreferencedCodeAttribute' annotations",
	Justification = "Unlike our base, we prefer to annotate the methods instead of the type, to avoid a warning merely because source-generated code includes a JSON converter."
)]
public sealed class LargeNumberValueWrapperJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TValue>
	: System.Text.Json.Serialization.JsonConverter<TWrapper>
	where TWrapper : IValueWrapper<TWrapper, TValue>
	where TValue : INumber<TValue>, ISpanParsable<TValue>, ISpanFormattable
{
	private const string RequiresUnreferencedCodeMessage = "Serialization requires unreferenced code.";
	private const string RequiresDynamicCodeMessage = "Serialization requires dynamic code.";

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		// The longer numeric types are not JavaScript-safe, so treat them as strings
		var value = reader.TokenType == System.Text.Json.JsonTokenType.String
			? reader.GetParsedString<TValue>(System.Globalization.CultureInfo.InvariantCulture)
			: System.Text.Json.JsonSerializer.Deserialize<TValue>(ref reader, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void Write(System.Text.Json.Utf8JsonWriter writer, TWrapper value, System.Text.Json.JsonSerializerOptions options)
	{
		// The longer numeric types are not JavaScript-safe, so treat them as strings
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TValue>(value)!;
		writer.WriteStringValue(serializedValue.Format(stackalloc char[64], "0.#", System.Globalization.CultureInfo.InvariantCulture));
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		var value = ((System.Text.Json.Serialization.JsonConverter<TValue>)options.GetConverter(typeof(TValue))).ReadAsPropertyName(ref reader, typeToConvert, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, TWrapper value, System.Text.Json.JsonSerializerOptions options)
	{
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TValue>(value)!;
		((System.Text.Json.Serialization.JsonConverter<TValue>)options.GetConverter(typeof(TValue))).WriteAsPropertyName(
			writer,
			serializedValue,
			options);
	}
}
