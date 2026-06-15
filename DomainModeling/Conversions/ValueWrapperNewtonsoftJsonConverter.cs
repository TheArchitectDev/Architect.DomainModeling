using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Architect.DomainModeling.Conversions;

/// <summary>
/// A generic Newtonsoft JSON converter for wrapper types, which serializes like the wrapped value itself.
/// </summary>
public sealed class ValueWrapperNewtonsoftJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TValue>
	: Newtonsoft.Json.JsonConverter
	where TWrapper : IValueWrapper<TWrapper, TValue>
{
	private static readonly Type? NullableWrapperType = typeof(TWrapper).IsValueType
		? typeof(Nullable<>).MakeGenericType(typeof(TWrapper))
		: null;

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(TWrapper) ||
			(typeof(TWrapper).IsValueType && objectType == NullableWrapperType!);
	}

	public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
	{
		if (reader.Value is null && (!typeof(TWrapper).IsValueType || objectType != typeof(TWrapper))) // Null data for a reference type or nullable value type
			return null;

		var value = serializer.Deserialize<TValue>(reader)!;
		return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
	}

	public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
	{
		var underlyingValue = value is not TWrapper instance
			? (object?)null
			: DomainObjectSerializer.Serialize<TWrapper, TValue>(instance);
		serializer.Serialize(writer, underlyingValue);
	}
}

/// <summary>
/// A generic System.Text JSON converter for wrapper types around numerics, which serializes like the wrapped value itself.
/// This variant is intended for numeric types whose larger values risk truncation in languages such as JavaScript.
/// It serializes to and from string.
/// </summary>
public sealed class LargeNumberValueWrapperNewtonsoftJsonConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TValue>
	: Newtonsoft.Json.JsonConverter
	where TWrapper : IValueWrapper<TWrapper, TValue>
	where TValue : INumber<TValue>, ISpanParsable<TValue>, ISpanFormattable
{
	private static readonly Type? NullableWrapperType = typeof(TWrapper).IsValueType
		? typeof(Nullable<>).MakeGenericType(typeof(TWrapper))
		: null;

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(TWrapper) ||
			(typeof(TWrapper).IsValueType && objectType == NullableWrapperType!);
	}

	public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
	{
		if (reader.Value is null && (!typeof(TWrapper).IsValueType || objectType != typeof(TWrapper))) // Null data for a reference type or nullable value type
			return null;

		// The longer numeric types are not JavaScript-safe, so treat them as strings
		if (reader.TokenType == Newtonsoft.Json.JsonToken.String)
		{
			var stringValue = serializer.Deserialize<string>(reader)!;
			var value = TValue.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
			return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
		}
		else
		{
			var value = serializer.Deserialize<TValue>(reader)!;
			return DomainObjectSerializer.Deserialize<TWrapper, TValue>(value);
		}
	}

	public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
	{
		// The longer numeric types are not JavaScript-safe, so treat them as strings
		//serializer.Serialize(writer, value is not TWrapper instance ? (object?)null : instance.Value.ToString(""0.#"", System.Globalization.CultureInfo.InvariantCulture));

		if (value is not TWrapper instance)
		{
			serializer.Serialize(writer, null);
			return;
		}

		var underlyingValue = DomainObjectSerializer.Serialize<TWrapper, TValue>(instance)!;
		var stringValue = underlyingValue.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
		serializer.Serialize(writer, stringValue);
	}
}
