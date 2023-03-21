using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

[Generator]
public class WrapperValueObjectGenerator : SourceGenerator
{
	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
			.Where(generatable => generatable is not null)
			.DeduplicatePartials();

		context.RegisterSourceOutput(provider, GenerateSource!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Partial subclass
		if (node is not ClassDeclarationSyntax cds || !cds.Modifiers.Any(SyntaxKind.PartialKeyword) || cds.BaseList is null)
			return false;

		foreach (var baseType in cds.BaseList.Types)
		{
			// Consider any type with SOME 1-param generic "WrapperValueObject" inheritance/implementation
			if (baseType.Type.HasArityAndName(1, Constants.WrapperValueObjectTypeName))
				return true;
		}

		return false;
	}

	private static Generatable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var result = new Generatable();

		var model = context.SemanticModel;
		var type = model.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node);

		if (type is null)
			return null;

		// Only with the attribute
		if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
			return null;

		result.SetAssociatedData(type);
		result.IsWrapperValueObject = type.BaseType?.IsType(Constants.WrapperValueObjectTypeName, Constants.DomainModelingNamespace) == true;
		result.IsAbstract = type.IsAbstract;
		result.IsGeneric = type.IsGenericType;
		result.IsNested = type.IsNested();

		result.TypeName = type.Name; // Will be non-generic if we pass the conditions to proceed with generation
		result.ContainingNamespace = type.ContainingNamespace.ToString();

		var underlyingType = type.BaseType?.TypeArguments[0] ?? type;
		result.UnderlyingTypeName = underlyingType.ToString();

		// IComparable is implemented on-demand, if the type implements IComparable against itself and the underlying type is self-comparable
		result.IsComparable = type.AllInterfaces.Any(interf => interf.IsType("IComparable", "System", generic: true) && interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default));
		result.IsComparable = result.IsComparable && underlyingType.IsComparable(seeThroughNullable: true);

		var members = type.GetMembers();

		var existingComponents = WrapperValueObjectTypeComponents.None;

		existingComponents |= WrapperValueObjectTypeComponents.Value.If(members.Any(member => member.Name == "Value"));

		existingComponents |= WrapperValueObjectTypeComponents.Constructor.If(type.Constructors.Any(ctor =>
			!ctor.IsStatic && ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.ToStringOverride.If(members.Any(member =>
			member.Name == nameof(ToString) && member is IMethodSymbol method && method.Parameters.Length == 0));

		existingComponents |= WrapperValueObjectTypeComponents.GetHashCodeOverride.If(members.Any(member =>
			member.Name == nameof(GetHashCode) && member is IMethodSymbol method && method.Parameters.Length == 0));

		existingComponents |= WrapperValueObjectTypeComponents.EqualsOverride.If(members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.IsType<object>()));

		existingComponents |= WrapperValueObjectTypeComponents.EqualsMethod.If(members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.CompareToMethod.If(members.Any(member =>
			member.Name == nameof(IComparable.CompareTo) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.EqualsOperator.If(members.Any(member =>
			member.Name == "op_Equality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.NotEqualsOperator.If(members.Any(member =>
			member.Name == "op_Inequality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.GreaterThanOperator.If(members.Any(member =>
			member.Name == "op_GreaterThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.LessThanOperator.If(members.Any(member =>
			member.Name == "op_LessThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
			member.Name == "op_GreaterThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.LessEqualsOperator.If(members.Any(member =>
			member.Name == "op_LessThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.ConvertToOperator.If(members.Any(member =>
			(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.ConvertFromOperator.If(members.Any(member =>
			(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default) &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		// Consider having a reference-typed underlying type as already having the operator (though actually it does not apply at all)
		existingComponents |= WrapperValueObjectTypeComponents.NullableConvertToOperator.If(!underlyingType.IsValueType || members.Any(member =>
			(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[0].Type.IsType(nameof(Nullable<int>), "System") && method.Parameters[0].Type.HasSingleGenericTypeArgument(underlyingType)));

		// Consider having a reference-typed underlying type as already having the operator (though actually it does not apply at all)
		existingComponents |= WrapperValueObjectTypeComponents.NullableConvertFromOperator.If(!underlyingType.IsValueType || members.Any(member =>
			(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.ReturnType.IsType(nameof(Nullable<int>), "System") && method.ReturnType.HasSingleGenericTypeArgument(underlyingType) &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsType("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

		existingComponents |= WrapperValueObjectTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft.Json") == true));

		existingComponents |= WrapperValueObjectTypeComponents.StringComparison.If(members.Any(member =>
			member.Name == "StringComparison" && member.IsOverride));

		result.ExistingComponents = existingComponents;

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var type = generatable.GetAssociatedData<INamedTypeSymbol>();

		// Only with the intended inheritance
		if (!generatable.IsWrapperValueObject)
		{
			context.ReportDiagnostic("ValueObjectGeneratorUnexpectedInheritance", "Unexpected base class",
				"The type marked as source-generated has an unexpected base class. Did you mean ValueObject<T>?", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-abstract
		if (generatable.IsAbstract)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorAbstractType", "Source-generated abstract type",
				"The type was not source-generated because it is abstract.", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-generic
		if (generatable.IsGeneric)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorGenericType", "Source-generated generic type",
				"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-nested
		if (generatable.IsNested)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorNestedType", "Source-generated nested type",
				"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, type);
			return;
		}

		var typeName = generatable.TypeName;
		var containingNamespace = generatable.ContainingNamespace;
		var underlyingType = type.BaseType!.TypeArguments[0];
		var underlyingTypeName = generatable.UnderlyingTypeName;
		var isComparable = generatable.IsComparable;
		var isToStringNullable = underlyingType.IsToStringNullable();
		var existingComponents = generatable.ExistingComponents;

		string? propertyNameParseStatement = null;
		if (type.IsOrImplementsInterface(interf => interf.Name == "ISpanParsable" && interf.ContainingNamespace.HasFullName("System") && interf.Arity == 1 && interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default), out _))
			propertyNameParseStatement = $"return reader.GetParsedString<{typeName}>(System.Globalization.CultureInfo.InvariantCulture);";
		else if (underlyingType.IsType<string>())
			propertyNameParseStatement = $"return new {typeName}(reader.GetString()!);";
		else if (!underlyingType.IsGeneric() && underlyingType.IsOrImplementsInterface(interf => interf.Name == "ISpanParsable" && interf.ContainingNamespace.HasFullName("System") && interf.Arity == 1 && interf.TypeArguments[0].Equals(underlyingType, SymbolEqualityComparer.Default), out _))
			propertyNameParseStatement = $"return new {typeName}(reader.GetParsedString<{underlyingType.ContainingNamespace}.{underlyingType.Name}>(System.Globalization.CultureInfo.InvariantCulture));";

		var propertyNameFormatStatement = "writer.WritePropertyName(value.ToString());";
		if (type.IsOrImplementsInterface(interf => interf.Name == "ISpanFormattable" && interf.ContainingNamespace.HasFullName("System") && interf.Arity == 0, out _))
			propertyNameFormatStatement = $"writer.WritePropertyName(value.Format(stackalloc char[64], default, System.Globalization.CultureInfo.InvariantCulture));";
		else if (underlyingType.IsType<string>())
			propertyNameFormatStatement = "writer.WritePropertyName(value.Value);";
		else if (!underlyingType.IsGeneric() && underlyingType.IsOrImplementsInterface(interf => interf.Name == "ISpanFormattable" && interf.ContainingNamespace.HasFullName("System") && interf.Arity == 0, out _))
			propertyNameFormatStatement = $"writer.WritePropertyName(value.Value.Format(stackalloc char[64], default, System.Globalization.CultureInfo.InvariantCulture));";

		var readAndWriteAsPropertyNameMethods = propertyNameParseStatement is null || propertyNameFormatStatement is null
			? ""
			: $@"
#if NET7_0_OR_GREATER
			public override {typeName} ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
			{{
				{propertyNameParseStatement}
			}}

			public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, {typeName} value, System.Text.Json.JsonSerializerOptions options)
			{{
				{propertyNameFormatStatement}
			}}
#endif
";

		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {Constants.DomainModelingNamespace};
using {Constants.DomainModelingNamespace}.Conversions;

#nullable enable

namespace {containingNamespace}
{{
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
	[System.Text.Json.Serialization.JsonConverter(typeof({typeName}.JsonConverter))]
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
	[Newtonsoft.Json.JsonConverter(typeof({typeName}.NewtonsoftJsonConverter))]
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}

	/* Generated */ {type.DeclaredAccessibility.ToCodeString()} sealed partial class {typeName} : IEquatable<{typeName}>{(isComparable ? "" : "/*")}, IComparable<{typeName}>{(isComparable ? "" : "*/")}
	{{
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.StringComparison) ? "/*" : "")}
		{(underlyingType.IsType<string>() ? "" : @"protected sealed override StringComparison StringComparison => throw new NotSupportedException(""This operation applies to string-based value objects only."");")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.StringComparison) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "/*" : "")}
		public {underlyingTypeName} Value {{ get; }}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "/*" : "")}
		public {typeName}({underlyingTypeName} value)
		{{
			this.Value = value{(underlyingType.IsValueType ? "" : " ?? throw new ArgumentNullException(nameof(value))")};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "/*" : "")}
		public sealed override string{(isToStringNullable ? "?" : "")} ToString()
		{{
			{(underlyingType.CreateStringExpression("Value").Contains('?') ? "// Null-safety protects instances from FormatterServices.GetUninitializedObject()" : "")}
			return {underlyingType.CreateStringExpression("Value")};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public sealed override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			// Null-safety protects instances from FormatterServices.GetUninitializedObject()
			return {underlyingType.CreateHashCodeExpression("Value", "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))")};
#pragma warning restore RS1024 // Compare symbols correctly
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GetHashCodeOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOverride) ? "/*" : "")}
		public sealed override bool Equals(object? other)
		{{
			return other is {typeName} otherValue && this.Equals(otherValue);
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsMethod) ? "/*" : "")}
		public bool Equals({typeName}? other)
		{{
			return other is null
				? false
				: {underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)")};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsMethod) ? " */" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) ? "/*" : "")}
		{(isComparable ? "" : "/*")}
		public int CompareTo({typeName}? other)
		{{
			return other is null
				? +1
				: {underlyingType.CreateComparisonExpression("Value", "String.Compare(this.{0}, other.{0}, this.StringComparison)")};
		}}
		{(isComparable ? "" : "*/")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOperator) ? "/*" : "")}
		public static bool operator ==({typeName}? left, {typeName}? right) => left is null ? right is null : left.Equals(right);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NotEqualsOperator) ? "/*" : "")}
		public static bool operator !=({typeName}? left, {typeName}? right) => !(left == right);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NotEqualsOperator) ? "*/" : "")}

		{(isComparable ? "" : "/*")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterThanOperator) ? "/*" : "")}
		public static bool operator >({typeName}? left, {typeName}? right) => left is null ? false : left.CompareTo(right) > 0;
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessThanOperator) ? "/*" : "")}
		public static bool operator <({typeName}? left, {typeName}? right) => left is null ? right is not null : left.CompareTo(right) < 0;
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterEqualsOperator) ? "/*" : "")}
		public static bool operator >=({typeName}? left, {typeName}? right) => !(left < right);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterEqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessEqualsOperator) ? "/*" : "")}
		public static bool operator <=({typeName}? left, {typeName}? right) => !(left > right);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessEqualsOperator) ? "*/" : "")}
		{(isComparable ? "" : "*/")}

		{(underlyingType.TypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "/*" : "")}
		{(underlyingType.IsValueType ? "" : @"[return: NotNullIfNotNull(""value"")]")}
		public static explicit operator {typeName}{(underlyingType.IsValueType ? "" : "?")}({underlyingTypeName}{(underlyingType.IsValueType ? "" : "?")} value) => {(underlyingType.IsValueType ? "" : "value is null ? null : ")}new {typeName}(value);
		{(underlyingType.TypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "*/" : "")}

		{(underlyingType.TypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType ? "" : @"[return: NotNullIfNotNull(""instance"")]")}
		public static implicit operator {underlyingTypeName}{(underlyingType.IsValueType ? "" : "?")}({typeName}{(underlyingType.IsValueType ? "" : "?")} instance) => instance{(underlyingType.IsValueType ? "" : "?")}.Value;
		{(underlyingType.TypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "*/" : "")}

		{(underlyingType.IsNullable() || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "/*" : "")}
		{(underlyingType.IsValueType ? @"[return: NotNullIfNotNull(""value"")]" : "")}
		{(underlyingType.IsValueType ? $"public static explicit operator {typeName}?({underlyingTypeName}? value) => value is null ? null : new {typeName}(value.Value);" : "")}
		{(underlyingType.IsNullable() || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "*/" : "")}

		{(underlyingType.IsNullable() || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType ? @"[return: NotNullIfNotNull(""instance"")]" : "")}
		{(underlyingType.IsValueType ? $"public static implicit operator {underlyingTypeName}?({typeName}? instance) => instance?.Value;" : "")}
		{(underlyingType.IsNullable() || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
		private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<{typeName}>
		{{
			public override {typeName} Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
			{{
				return ({typeName})System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeName}>(ref reader, options)!;
			}}

			public override void Write(System.Text.Json.Utf8JsonWriter writer, {typeName} value, System.Text.Json.JsonSerializerOptions options)
			{{
				System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);
			}}

			{readAndWriteAsPropertyNameMethods}
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
		private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
		{{
			public override bool CanConvert(Type objectType)
			{{
				return objectType == typeof({typeName});
			}}

			public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
			{{
				if (value is null)
					serializer.Serialize(writer, null);
				else
					serializer.Serialize(writer, (({typeName})value).Value);
			}}

			public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
			{{
				return ({typeName}?)serializer.Deserialize<{underlyingTypeName}?>(reader);
			}}
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}
	}}
}}
";

		AddSource(context, source, typeName, containingNamespace);
	}

	[Flags]
	private enum WrapperValueObjectTypeComponents : ulong
	{
		None = 0,

		Value = 1 << 0,
		Constructor = 1 << 1,
		ToStringOverride = 1 << 2,
		GetHashCodeOverride = 1 << 3,
		EqualsOverride = 1 << 4,
		EqualsMethod = 1 << 5,
		CompareToMethod = 1 << 6,
		EqualsOperator = 1 << 7,
		NotEqualsOperator = 1 << 8,
		GreaterThanOperator = 1 << 9,
		LessThanOperator = 1 << 10,
		GreaterEqualsOperator = 1 << 11,
		LessEqualsOperator = 1 << 12,
		ConvertToOperator = 1 << 13,
		ConvertFromOperator = 1 << 14,
		NullableConvertToOperator = 1 << 15,
		NullableConvertFromOperator = 1 << 16,
		NewtonsoftJsonConverter = 1 << 17,
		SystemTextJsonConverter = 1 << 18,
		StringComparison = 1 << 19,
	}

	private sealed record Generatable : IGeneratable
	{
		public bool IsWrapperValueObject { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsGeneric { get; set; }
		public bool IsNested { get; set; }
		public bool IsComparable { get; set; }
		public string TypeName { get; set; } = null!;
		public string ContainingNamespace { get; set; } = null!;
		public string UnderlyingTypeName { get; set; } = null!;
		public WrapperValueObjectTypeComponents ExistingComponents { get; set; }
	}
}
