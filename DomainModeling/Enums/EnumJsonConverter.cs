using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Architect.DomainModeling.Conversions;

namespace Architect.DomainModeling.Enums;

/// <summary>
/// A factory to produce a generic System.Text JSON converter for enum wrapper types, which serializes like the wrapped value itself.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[UnconditionalSuppressMessage(
	"Trimming", "IL2046:All interface implementations and method overrides must have annotations matching the interface or overridden virtual method 'RequiresUnreferencedCodeAttribute' annotations",
	Justification = "Unlike our base, we prefer to annotate the methods instead of the type, to avoid a warning merely because source-generated code includes a JSON converter."
)]
internal sealed class EnumJsonConverterFactory : JsonConverterFactory
{
	private const string RequiresUnreferencedCodeMessage = "Serialization requires unreferenced code.";
	private const string RequiresDynamicCodeMessage = "Serialization requires dynamic code.";

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.IsConstructedGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DefinedEnum<,>);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[UnconditionalSuppressMessage(
		"Trimming", "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined",
		Justification = "Both the converted and converter types are marked with DynamicallyAccessedMemberTypes.All."
	)]
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		return Activator.CreateInstance(typeof(EnumJsonConverter<,>).MakeGenericType(typeToConvert, typeToConvert.GenericTypeArguments[1])) as JsonConverter;
	}
}

/// <summary>
/// A generic System.Text JSON converter for enum wrapper types, which serializes like the wrapped value itself.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[UnconditionalSuppressMessage(
	"Trimming", "IL2046:All interface implementations and method overrides must have annotations matching the interface or overridden virtual method 'RequiresUnreferencedCodeAttribute' annotations",
	Justification = "Unlike our base, we prefer to annotate the methods instead of the type, to avoid a warning merely because source-generated code includes a JSON converter."
)]
internal sealed class EnumJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TPrimitive>
	: JsonConverter<TWrapper>
	where TWrapper : IValueWrapper<TWrapper, TPrimitive>, ISpanParsable<TWrapper>
{
	private const string RequiresUnreferencedCodeMessage = "Serialization requires unreferenced code.";
	private const string RequiresDynamicCodeMessage = "Serialization requires dynamic code.";

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = JsonSerializer.Deserialize<TPrimitive>(ref reader, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TPrimitive>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
	{
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TPrimitive>(value);
		JsonSerializer.Serialize(writer, serializedValue, options);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override TWrapper ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = ((JsonConverter<TPrimitive>)options.GetConverter(typeof(TPrimitive))).ReadAsPropertyName(ref reader, typeToConvert, options)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TPrimitive>(value);
	}

	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	public override void WriteAsPropertyName(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
	{
		var serializedValue = DomainObjectSerializer.Serialize<TWrapper, TPrimitive>(value)!;
		((JsonConverter<TPrimitive>)options.GetConverter(typeof(TPrimitive))).WriteAsPropertyName(
			writer,
			serializedValue,
			options);
	}
}
