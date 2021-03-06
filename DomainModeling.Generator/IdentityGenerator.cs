using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

[Generator]
public class IdentityGenerator : SourceGenerator
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
		// Partial struct with some interface
		if (node is StructDeclarationSyntax sds && sds.Modifiers.Any(SyntaxKind.PartialKeyword) && sds.BaseList is not null)
		{
			// With SourceGenerated attribute
			if (sds.HasAttributeWithPrefix(Constants.SourceGeneratedAttributeShortName))
			{
				// Consider any type with SOME 1-param generic "IIdentity" inheritance/implementation
				foreach (var baseType in sds.BaseList.Types)
				{
					if (baseType.Type.HasArityAndName(1, Constants.IdentityInterfaceTypeName))
						return true;
				}
			}
		}

		// Concrete, non-generic class
		if (node is ClassDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword) && cds.Arity == 0 && cds.BaseList is not null)
		{
			// Consider any type with SOME 2-param generic "Entity" inheritance/implementation
			foreach (var baseType in cds.BaseList.Types)
			{
				if (baseType.Type.HasArityAndName(2, Constants.EntityTypeName))
					return true;
			}
		}

		return false;
	}

	private static Generatable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var result = new Generatable();

		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol(tds);

		if (type is null)
			return null;

		var isBasedOnEntity = type.IsOrInheritsClass(baseType => baseType.Name == Constants.EntityTypeName, out _);

		// Path A: An Entity subclass that might be an Entity<TId, TUnderlying> for which TId may have to be generated
		if (isBasedOnEntity)
		{
			// Only an actual Entity<TId, TUnderlying>
			if (!type.IsOrInheritsClass(baseType => baseType.Arity == 2 && baseType.IsType(Constants.EntityTypeName, Constants.DomainModelingNamespace), out var entityType))
				return null;

			var idType = entityType.TypeArguments[0];
			var underlyingType = entityType.TypeArguments[1];
			result.SetAssociatedData(new Tuple<INamedTypeSymbol?, ITypeSymbol, ITypeSymbol>(type, idType, underlyingType));
			result.EntityTypeName = type.Name;

			// The ID type exists if it is not of TypeKind.Error
			result.IdTypeExists = idType.TypeKind != TypeKind.Error;

			if (result.IdTypeExists)
				return result;

			result.ContainingNamespace = type.ContainingNamespace.ToString();
			result.IdTypeName = idType.Name;
			result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();

			// We do not support combining with a manual definition, so we honor the entity's accessibility
			// The entity could be a private nested type (for example), and a private non-nested ID type would have insufficient accessibility, so then we need at least "internal"
			result.Accessibility = type.DeclaredAccessibility.AtLeast(Accessibility.Internal);

			return result;
		}
		// Path B: An IIdentity struct for which a partial should be generated
		else
		{
			// Only with the attribute
			if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
				return null;

			var interf = type.Interfaces.SingleOrDefault(interf => interf.Arity == 1 && interf.ContainingNamespace.HasFullName(Constants.DomainModelingNamespace) && interf.IsGenericType && interf.Arity == 1);

			// Only an actual IIdentity<T>
			if (interf is null)
				return result;

			var underlyingType = interf.TypeArguments[0];

			result.IsIIdentity = true;
			result.IdTypeExists = true;
			result.SetAssociatedData(new Tuple<INamedTypeSymbol?, ITypeSymbol, ITypeSymbol>(null, type, underlyingType));
			result.ContainingNamespace = type.ContainingNamespace.ToString();
			result.IdTypeName = type.Name;
			result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
			result.Accessibility = type.DeclaredAccessibility;
			result.IsGeneric = type.IsGenericType;
			result.IsNested = type.IsNested();

			var members = type.GetMembers();

			var existingComponents = IdTypeComponents.None;

			existingComponents |= IdTypeComponents.Value.If(members.Any(member => member.Name == "Value"));

			existingComponents |= IdTypeComponents.Constructor.If(type.Constructors.Any(ctor =>
				!ctor.IsStatic && ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ToStringOverride.If(members.Any(member =>
				member.Name == nameof(ToString) && member is IMethodSymbol method && method.Parameters.Length == 0));

			existingComponents |= IdTypeComponents.GetHashCodeOverride.If(members.Any(member =>
				member.Name == nameof(GetHashCode) && member is IMethodSymbol method && method.Parameters.Length == 0));

			existingComponents |= IdTypeComponents.EqualsOverride.If(members.Any(member =>
				member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.IsType<object>()));

			existingComponents |= IdTypeComponents.EqualsMethod.If(members.Any(member =>
				member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.CompareToMethod.If(members.Any(member =>
				member.Name == nameof(IComparable.CompareTo) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.EqualsOperator.If(members.Any(member =>
				member.Name == "op_Equality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.NotEqualsOperator.If(members.Any(member =>
				member.Name == "op_Inequality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.GreaterThanOperator.If(members.Any(member =>
				member.Name == "op_GreaterThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.LessThanOperator.If(members.Any(member =>
				member.Name == "op_LessThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
				member.Name == "op_GreaterThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.LessEqualsOperator.If(members.Any(member =>
				member.Name == "op_LessThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ConvertToOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ConvertFromOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.NullableConvertToOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				method.ReturnType.IsType(nameof(Nullable<int>), "System") && method.ReturnType.HasSingleGenericTypeArgument(type) &&
				(underlyingType.IsReferenceType
					? method.Parameters[0].Type.IsType(underlyingType.Name, underlyingType.ContainingNamespace.Name)
					: method.Parameters[0].Type.IsType(nameof(Nullable<int>), "System") && method.Parameters[0].Type.HasSingleGenericTypeArgument(underlyingType))));

			existingComponents |= IdTypeComponents.NullableConvertFromOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
				(underlyingType.IsReferenceType
					? method.ReturnType.IsType(underlyingType.Name, underlyingType.ContainingNamespace.Name)
					: method.ReturnType.IsType(nameof(Nullable<int>), "System") && method.ReturnType.HasSingleGenericTypeArgument(underlyingType)) &&
				method.Parameters[0].Type.IsType(nameof(Nullable<int>), "System") && method.Parameters[0].Type.HasSingleGenericTypeArgument(type)));

			existingComponents |= IdTypeComponents.SerializableAttribute.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType<SerializableAttribute>() == true));

			existingComponents |= IdTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

			existingComponents |= IdTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft.Json") == true));

			existingComponents |= IdTypeComponents.StringComparison.If(members.Any(member =>
				member.Name == "StringComparison"));

			result.ExistingComponents = existingComponents;

			return result;
		}
	}

	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var typeTuple = generatable.GetAssociatedData<Tuple<INamedTypeSymbol?, ITypeSymbol, ITypeSymbol>>();
		var entityType = typeTuple.Item1;
		var idType = typeTuple.Item2;
		var underlyingType = typeTuple.Item3;
		var containingNamespace = generatable.ContainingNamespace;
		var idTypeName = generatable.IdTypeName;
		var underlyingTypeFullyQualifiedName = generatable.UnderlyingTypeFullyQualifiedName;
		var entityTypeName = generatable.EntityTypeName;

		var accessibility = generatable.Accessibility;
		var existingComponents = generatable.ExistingComponents;
		var hasSourceGeneratedAttribute = generatable.IdTypeExists;

		if (generatable.IdTypeExists)
		{
			// Entity<TId, TUnderlying> was needlessly used, with a preexisting TId
			if (entityTypeName is not null)
			{
				context.ReportDiagnostic("EntityIdentityTypeAlreadyExists", "Entity identity type already exists",
					"Base class Entity<TId, TIdPrimitive> is intended to generate source for TId, but TId refers to an existing type. To use an existing identity type, inherit from Entity<TId> instead.", DiagnosticSeverity.Warning, entityType);
				return;
			}

			// Only with the intended inheritance
			if (!generatable.IsIIdentity)
			{
				context.ReportDiagnostic("IdentityGeneratorUnexpectedInheritance", "Unexpected interface",
					"The type marked as source-generated has an unexpected base class or interface. Did you mean IIdentity<T>?", DiagnosticSeverity.Warning, idType);
				return;
			}
			// Only if non-generic
			if (generatable.IsGeneric)
			{
				context.ReportDiagnostic("IdentityGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, idType);
				return;
			}
			// Only if non-nested
			if (generatable.IsNested)
			{
				context.ReportDiagnostic("IdentityGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, idType);
				return;
			}
		}

		const string nullInputAnnotation = "[AllowNull] ";
		const string nullPropertyAnnotation = "[AllowNull, MaybeNull]";
		const string mayReturnNullAnnotation = "[return: MaybeNull]";

		var summary = entityTypeName is null ? null : $@"
	/// <summary>
	/// The identity type used for the <see cref=""{entityTypeName}""/> entity.
	/// </summary>";

		// Special case for strings, unless they are explicitly annotated as nullable
		// An ID wrapping a null string (such as a default instance) acts as if it contains an empty string instead
		// This allows strings to be used as a primitive without any null troubles
		// Conversions are carefree this way, and null inputs simply get converted to empty string equivalents, which tend not to match any valid ID
		var isNonNullString = underlyingType.IsType<string>() && underlyingType.NullableAnnotation != NullableAnnotation.Annotated;
		var nonNullStringSummary = !isNonNullString ? null : @"
		/// <summary>
		/// If the current object is a default instance or was constructed with a null value, this property produces an empty string.
		/// </summary>";

		// JavaScript (and arguably, by extent, JSON) have insufficient numeric capacity to properly hold the longer numeric types
		var underlyingTypeIsNumericUnsuitableForJson = underlyingType.IsType<decimal>() || underlyingType.IsType<ulong>() || underlyingType.IsType<long>() || underlyingType.IsType<System.Numerics.BigInteger>();
		var longNumericTypeComment = !underlyingTypeIsNumericUnsuitableForJson ? null : "// The longer numeric types are not JavaScript-safe, so treat them as strings";

		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {Constants.DomainModelingNamespace};

namespace {containingNamespace}
{{
	{summary}
	{(existingComponents.HasFlags(IdTypeComponents.SerializableAttribute) ? "/*" : "")}
	[Serializable]
	{(existingComponents.HasFlags(IdTypeComponents.SerializableAttribute) ? "*/" : "")}

	{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
	[System.Text.Json.Serialization.JsonConverter(typeof({idTypeName}.JsonConverter))]
	{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

	{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
	[Newtonsoft.Json.JsonConverter(typeof({idTypeName}.NewtonsoftJsonConverter))]
	{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}

	{(hasSourceGeneratedAttribute ? "" : "[SourceGenerated]")}
	{(entityTypeName is null ? "/* Generated */ " : "")}{accessibility.ToCodeString()} readonly{(entityTypeName is null ? " partial" : "")} struct {idTypeName} : {Constants.IdentityInterfaceTypeName}<{underlyingTypeFullyQualifiedName}>, IEquatable<{idTypeName}>, IComparable<{idTypeName}>
	{{
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "/*" : "")}
		{nonNullStringSummary}
		{(underlyingType.IsValueType ? "" : isNonNullString ? "[NotNull]" : nullPropertyAnnotation)}
		public {underlyingTypeFullyQualifiedName} Value {(isNonNullString ? @"=> this._value ?? """";" : "{ get; }")}
		{(isNonNullString ? "private readonly string _value;" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.Constructor) ? "/*" : "")}
		public {idTypeName}({(underlyingType.IsValueType ? "" : nullInputAnnotation)}{underlyingTypeFullyQualifiedName} value)
		{{
			{(isNonNullString ? @"this._value = value ?? """";" : "this.Value = value;")}
		}}
		{(existingComponents.HasFlags(IdTypeComponents.Constructor) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.StringComparison) ? "/*" : "")}
		{(underlyingType.IsType<string>()
		? @"private StringComparison StringComparison => StringComparison.Ordinal;"
		: "")}
		{(existingComponents.HasFlags(IdTypeComponents.StringComparison) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ToStringOverride) ? "/*" : "")}
		{(isNonNullString ? "" : "[return: MaybeNull]")}
		public override string ToString()
		{{
			return {underlyingType.CreateStringExpression("Value")};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			return {underlyingType.CreateHashCodeExpression("Value", stringVariant: "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))")};
#pragma warning restore RS1024 // Compare symbols correctly
		}}
		{(existingComponents.HasFlags(IdTypeComponents.GetHashCodeOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.EqualsOverride) ? "/*" : "")}
		public override bool Equals([AllowNull] object other)
		{{
			return other is {idTypeName} otherId && this.Equals(otherId);
		}}
		{(existingComponents.HasFlags(IdTypeComponents.EqualsOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.EqualsMethod) ? "/*" : "")}
		public bool Equals({idTypeName} other)
		{{
			return {underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)")};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.EqualsMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.CompareToMethod) ? "/*" : "")}
		public int CompareTo({idTypeName} other)
		{{
			return {underlyingType.CreateComparisonExpression("Value", stringVariant: "String.Compare(this.{0}, other.{0}, this.StringComparison)")};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.CompareToMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.EqualsOperator) ? "/*" : "")}
		public static bool operator ==({idTypeName} left, {idTypeName} right) => left.Equals(right);
		{(existingComponents.HasFlags(IdTypeComponents.EqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.NotEqualsOperator) ? "/*" : "")}
		public static bool operator !=({idTypeName} left, {idTypeName} right) => !(left == right);
		{(existingComponents.HasFlags(IdTypeComponents.NotEqualsOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.GreaterThanOperator) ? "/*" : "")}
		public static bool operator >({idTypeName} left, {idTypeName} right) => left.CompareTo(right) > 0;
		{(existingComponents.HasFlags(IdTypeComponents.GreaterThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.LessThanOperator) ? "/*" : "")}
		public static bool operator <({idTypeName} left, {idTypeName} right) => left.CompareTo(right) < 0;
		{(existingComponents.HasFlags(IdTypeComponents.LessThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.GreaterEqualsOperator) ? "/*" : "")}
		public static bool operator >=({idTypeName} left, {idTypeName} right) => left.CompareTo(right) >= 0;
		{(existingComponents.HasFlags(IdTypeComponents.GreaterEqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.LessEqualsOperator) ? "/*" : "")}
		public static bool operator <=({idTypeName} left, {idTypeName} right) => left.CompareTo(right) <= 0;
		{(existingComponents.HasFlags(IdTypeComponents.LessEqualsOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "/*" : "")}
		public static implicit operator {idTypeName}({(underlyingType.IsValueType ? "" : nullInputAnnotation)}{underlyingTypeFullyQualifiedName} value) => new {idTypeName}(value);
		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType || isNonNullString ? "" : mayReturnNullAnnotation)}
		public static implicit operator {underlyingTypeFullyQualifiedName}({idTypeName} id) => id.Value;
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "/*" : "")}
		[return: MaybeNull, NotNullIfNotNull(""value"")]
		public static implicit operator {idTypeName}?({(underlyingType.IsValueType ? "" : "[AllowNull] ")}{underlyingTypeFullyQualifiedName}{(underlyingType.IsValueType ? "?" : "")} value) => value is null ? ({idTypeName}?)null : new {idTypeName}(value{(underlyingType.IsValueType ? ".Value" : "")});
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType || isNonNullString ? @"[return: MaybeNull, NotNullIfNotNull(""id"")]" : "[return: MaybeNull]")}
		public static implicit operator {underlyingTypeFullyQualifiedName}{(underlyingType.IsValueType ? "?" : "")}({idTypeName}? id) => id?.Value;
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
		private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<{idTypeName}>
		{{
			public override {idTypeName} Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
			{{
				{longNumericTypeComment}
				#nullable disable
				{(underlyingTypeIsNumericUnsuitableForJson
				? $@"return reader.TokenType == System.Text.Json.JsonTokenType.Number ? reader.Get{underlyingType.Name}() : ({idTypeName}){underlyingType.ContainingNamespace}.{underlyingType.Name}.Parse(reader.GetString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);"
				: $@"return ({idTypeName})System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeFullyQualifiedName}>(ref reader, options);")}
				#nullable enable
			}}

			public override void Write(System.Text.Json.Utf8JsonWriter writer, [AllowNull] {idTypeName} value, System.Text.Json.JsonSerializerOptions options)
			{{
				{longNumericTypeComment}
				{(underlyingTypeIsNumericUnsuitableForJson
				? "writer.WriteStringValue(value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));"
				: "System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);")}
			}}
		}}
		{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
		private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
		{{
			public override bool CanConvert(Type objectType)
			{{
				return objectType == typeof({idTypeName}) || objectType == typeof({idTypeName}?);
			}}

			public override void WriteJson(Newtonsoft.Json.JsonWriter writer, [AllowNull] object value, Newtonsoft.Json.JsonSerializer serializer)
			{{
				{longNumericTypeComment}
				if (value is null) serializer.Serialize(writer, null);
				else {(underlyingTypeIsNumericUnsuitableForJson
				? $"serializer.Serialize(writer, (({idTypeName})value).Value.ToString(System.Globalization.CultureInfo.InvariantCulture));"
				: $"serializer.Serialize(writer, (({idTypeName})value).Value);")}
			}}

			[return: MaybeNull]
			public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, [AllowNull] object existingValue, Newtonsoft.Json.JsonSerializer serializer)
			{{
				{longNumericTypeComment}
				#nullable disable
				if (objectType == typeof({idTypeName})) // Non-nullable
					{(underlyingTypeIsNumericUnsuitableForJson
					? $@"return reader.TokenType == Newtonsoft.Json.JsonToken.Integer ? ({idTypeName}?)serializer.Deserialize<{underlyingTypeFullyQualifiedName}?>(reader) : ({idTypeName}){underlyingType.ContainingNamespace}.{underlyingType.Name}.Parse(serializer.Deserialize<string>(reader), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);"
					: $@"return ({idTypeName})serializer.Deserialize<{underlyingTypeFullyQualifiedName}>(reader);")}
				else // Nullable
					{(underlyingTypeIsNumericUnsuitableForJson
					? $@"return reader.Value is null ? null : reader.TokenType == Newtonsoft.Json.JsonToken.Integer ? ({idTypeName}?)serializer.Deserialize<{underlyingTypeFullyQualifiedName}?>(reader) : ({idTypeName}?){underlyingType.ContainingNamespace}.{underlyingType.Name}.Parse(serializer.Deserialize<string>(reader), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);"
					: $@"return ({idTypeName}?)serializer.Deserialize<{underlyingTypeFullyQualifiedName}{(underlyingType.IsValueType ? "?" : "")}>(reader);")}
				#nullable enable
			}}
		}}
		{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}
	}}
}}
";

		AddSource(context, source, idTypeName, containingNamespace);
	}

	[Flags]
	private enum IdTypeComponents : ulong
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
		SerializableAttribute = 1 << 17,
		NewtonsoftJsonConverter = 1 << 18,
		SystemTextJsonConverter = 1 << 19,
		StringComparison = 1 << 20,
	}

	private sealed record Generatable : IGeneratable
	{
		public bool IdTypeExists { get; set; }
		public string EntityTypeName { get; set; } = null!;
		public bool IsIIdentity { get; set; }
		public string ContainingNamespace { get; set; } = null!;
		public string IdTypeName { get; set; } = null!;
		public string UnderlyingTypeFullyQualifiedName { get; set; } = null!;
		public Accessibility Accessibility { get; set; }
		public bool IsGeneric { get; set; }
		public bool IsNested { get; set; }
		public IdTypeComponents ExistingComponents { get; set; }
	}
}
