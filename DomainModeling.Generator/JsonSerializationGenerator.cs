namespace Architect.DomainModeling.Generator;

/// <summary>
/// Can be used to write JSON serialization source code.
/// </summary>
internal static class JsonSerializationGenerator
{
	public static string WriteJsonConverterAttribute(string modelTypeName, string underlyingTypeFullyQualifiedName,
		bool numericAsString = false)
	{
		return $"[System.Text.Json.Serialization.JsonConverter(typeof({(numericAsString ? "LargeNumber" : "")}WrapperJsonConverter<{modelTypeName}, {underlyingTypeFullyQualifiedName}>))]";
	}

	public static string WriteNewtonsoftJsonConverterAttribute(string modelTypeName, string underlyingTypeFullyQualifiedName,
		bool numericAsString = false)
	{
		return $"[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft{(numericAsString ? "LargeNumber" : "")}WrapperJsonConverter<{modelTypeName}, {underlyingTypeFullyQualifiedName}>))]";
	}
}
