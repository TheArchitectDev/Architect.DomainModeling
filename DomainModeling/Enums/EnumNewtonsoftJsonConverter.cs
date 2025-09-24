using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Conversions;
using Newtonsoft.Json;

namespace Architect.DomainModeling.Enums;

/// <summary>
/// A factory to produce a generic Newtonsoft JSON converter for enum wrapper types, which serializes like the wrapped value itself.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal sealed class EnumNewtonsoftJsonConverterFactory : JsonConverter
{
	private static readonly ConcurrentDictionary<Type, JsonConverter> ConvertersPerType = new(concurrencyLevel: 1, capacity: 11);

	public override bool CanConvert(Type objectType)
	{
		return objectType.IsConstructedGenericType && objectType.GetGenericTypeDefinition() == typeof(DefinedEnum<,>);
	}

	[UnconditionalSuppressMessage(
		"Trimming", "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined",
		Justification = "Both the converted and converter types are marked with DynamicallyAccessedMemberTypes.All."
	)]
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var nullableUnderlyingType = Nullable.GetUnderlyingType(objectType);
		if (nullableUnderlyingType is not null)
		{
			if (reader.Value is null)
				return null;
			objectType = nullableUnderlyingType;
		}

		return ConvertersPerType.GetOrAdd(objectType, type => (JsonConverter)Activator.CreateInstance(typeof(EnumNewtonsoftJsonConverter<,>).MakeGenericType(type, type.GenericTypeArguments[1]))!)
			.ReadJson(reader, objectType, existingValue, serializer);
	}

	[UnconditionalSuppressMessage(
		"Trimming", "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined",
		Justification = "Both the converted and converter types are marked with DynamicallyAccessedMemberTypes.All."
	)]
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value?.GetType() is { } type)
			ConvertersPerType.GetOrAdd(type, type => (JsonConverter)Activator.CreateInstance(typeof(EnumNewtonsoftJsonConverter<,>).MakeGenericType(type, type.GenericTypeArguments[1]))!)
				.WriteJson(writer, value, serializer);
		else
			serializer.Serialize(writer, null);
	}
}

/// <summary>
/// A generic Newtonsoft JSON converter for enum wrapper types, which serializes like the wrapped value itself.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal sealed class EnumNewtonsoftJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TPrimitive>
	: JsonConverter<TWrapper>
	where TWrapper : IValueWrapper<TWrapper, TPrimitive>, ISpanParsable<TWrapper>
{
	public override TWrapper? ReadJson(JsonReader reader, Type objectType, TWrapper? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var value = serializer.Deserialize<TPrimitive>(reader);
		return DomainObjectSerializer.Deserialize<TWrapper, TPrimitive>(value);
	}

	public override void WriteJson(JsonWriter writer, TWrapper? value, JsonSerializer serializer)
	{
		var underlyingValue = value is not TWrapper instance
			? (object?)null
			: DomainObjectSerializer.Serialize<TWrapper, TPrimitive>(instance);
		serializer.Serialize(writer, underlyingValue);
	}
}
