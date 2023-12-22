namespace Architect.DomainModeling.Generator;

/// <summary>
/// Can be used to write JSON serialization source code.
/// </summary>
internal static class JsonSerializationGenerator
{
	public static string WriteJsonConverterAttribute(string modelTypeName)
	{
		return $"[System.Text.Json.Serialization.JsonConverter(typeof({modelTypeName}.JsonConverter))]";
	}

	public static string WriteNewtonsoftJsonConverterAttribute(string modelTypeName)
	{
		return $"[Newtonsoft.Json.JsonConverter(typeof({modelTypeName}.NewtonsoftJsonConverter))]";
	}

	public static string WriteJsonConverter(
		string modelTypeName, string underlyingTypeFullyQualifiedName,
		bool numericAsString)
	{
		var result = $@"
#if NET7_0_OR_GREATER
		private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<{modelTypeName}>
		{{
			public override {modelTypeName} Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				DomainObjectSerializer.Deserialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(reader.TokenType == System.Text.Json.JsonTokenType.String
					? reader.GetParsedString<{underlyingTypeFullyQualifiedName}>(System.Globalization.CultureInfo.InvariantCulture)
					: System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeFullyQualifiedName}>(ref reader, options));
					"
					: $@"
				DomainObjectSerializer.Deserialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeFullyQualifiedName}>(ref reader, options)!);
					")}
			
			public override void Write(System.Text.Json.Utf8JsonWriter writer, {modelTypeName} value, System.Text.Json.JsonSerializerOptions options) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				writer.WriteStringValue(DomainObjectSerializer.Serialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(value).Format(stackalloc char[64], ""0.#"", System.Globalization.CultureInfo.InvariantCulture));
					"
					: $@"
				System.Text.Json.JsonSerializer.Serialize(writer, DomainObjectSerializer.Serialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(value), options);
					")}
			
			public override {modelTypeName} ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
				DomainObjectSerializer.Deserialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(
					((System.Text.Json.Serialization.JsonConverter<{underlyingTypeFullyQualifiedName}>)options.GetConverter(typeof({underlyingTypeFullyQualifiedName}))).ReadAsPropertyName(ref reader, typeToConvert, options));

			public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, {modelTypeName} value, System.Text.Json.JsonSerializerOptions options) =>
				((System.Text.Json.Serialization.JsonConverter<{underlyingTypeFullyQualifiedName}>)options.GetConverter(typeof({underlyingTypeFullyQualifiedName}))).WriteAsPropertyName(
					writer,
					DomainObjectSerializer.Serialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(value)!, options);
		}}
#else
		private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<{modelTypeName}>
		{{
			public override {modelTypeName} Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				reader.TokenType == System.Text.Json.JsonTokenType.String
					? ({modelTypeName}){underlyingTypeFullyQualifiedName}.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
					: ({modelTypeName})System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeFullyQualifiedName}>(ref reader, options);
					"
					: $@"
				({modelTypeName})System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeFullyQualifiedName}>(ref reader, options)!;
					")}
			
			public override void Write(System.Text.Json.Utf8JsonWriter writer, {modelTypeName} value, System.Text.Json.JsonSerializerOptions options) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				writer.WriteStringValue(value.Value.ToString(""0.#"", System.Globalization.CultureInfo.InvariantCulture));
					"
					: $@"
				System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);
					")}
		}}
#endif
			";

		return result;
	}

	public static string WriteNewtonsoftJsonConverter(
		string modelTypeName, string underlyingTypeFullyQualifiedName,
		bool isStruct, bool numericAsString)
	{
		var result = $@"
#if NET7_0_OR_GREATER
		private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
		{{
			public override bool CanConvert(Type objectType) =>
				objectType == typeof({modelTypeName}){(isStruct ? $" || objectType == typeof({modelTypeName}?)" : "")};

			public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				reader.Value is null && objectType != typeof({modelTypeName}) // Null data for a nullable value type
					? ({modelTypeName}?)null
					: DomainObjectSerializer.Deserialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(reader.TokenType == Newtonsoft.Json.JsonToken.String
						? {underlyingTypeFullyQualifiedName}.Parse(serializer.Deserialize<string>(reader)!, System.Globalization.CultureInfo.InvariantCulture)
						: serializer.Deserialize<{underlyingTypeFullyQualifiedName}>(reader));
					"
					: $@"
				reader.Value is null && (!typeof({modelTypeName}).IsValueType || objectType != typeof({modelTypeName})) // Null data for a reference type or nullable value type
					? ({modelTypeName}?)null
					: DomainObjectSerializer.Deserialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(serializer.Deserialize<{underlyingTypeFullyQualifiedName}>(reader)!);
					")}

			public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				serializer.Serialize(writer, value is not {modelTypeName} instance ? (object?)null : DomainObjectSerializer.Serialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(instance).ToString(""0.#"", System.Globalization.CultureInfo.InvariantCulture));
					"
					: $@"
				serializer.Serialize(writer, value is not {modelTypeName} instance ? (object?)null : DomainObjectSerializer.Serialize<{modelTypeName}, {underlyingTypeFullyQualifiedName}>(instance));
					")}
		}}
#else
		private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
		{{
			public override bool CanConvert(Type objectType) =>
				objectType == typeof({modelTypeName}){(isStruct ? $" || objectType == typeof({modelTypeName}?)" : "")};

			public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				reader.Value is null && objectType != typeof({modelTypeName}) // Null data for a nullable value type
					? ({modelTypeName}?)null
					: reader.TokenType == Newtonsoft.Json.JsonToken.String
						? ({modelTypeName}){underlyingTypeFullyQualifiedName}.Parse(serializer.Deserialize<string>(reader)!, System.Globalization.CultureInfo.InvariantCulture)
						: ({modelTypeName})serializer.Deserialize<{underlyingTypeFullyQualifiedName}>(reader);
					"
					: $@"
				reader.Value is null && (!typeof({modelTypeName}).IsValueType || objectType != typeof({modelTypeName})) // Null data for a reference type or nullable value type
					? ({modelTypeName}?)null
					: ({modelTypeName})serializer.Deserialize<{underlyingTypeFullyQualifiedName}>(reader)!;
					")}

			public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>{(numericAsString
					? $@"
				// The longer numeric types are not JavaScript-safe, so treat them as strings
				serializer.Serialize(writer, value is not {modelTypeName} instance ? (object?)null : instance.Value.ToString(""0.#"", System.Globalization.CultureInfo.InvariantCulture));
					"
					: $@"
				serializer.Serialize(writer, value is not {modelTypeName} instance ? (object?)null : instance.Value);
					")}
		}}
#endif
";

		return result;
	}
}
