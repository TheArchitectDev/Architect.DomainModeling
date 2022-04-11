using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator
{
	[Generator]
	public class IdentityGenerator : SourceGenerator
	{
		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<StructDeclarationSyntax> CandidatePartialIdentityStructs { get; } = new List<StructDeclarationSyntax>();
			public List<ClassDeclarationSyntax> CandidateEntityClasses { get; } = new List<ClassDeclarationSyntax>();

			public void OnVisitSyntaxNode(SyntaxNode node)
			{
				// Partial struct with some interface
				if (node is StructDeclarationSyntax sds && sds.Modifiers.Any(SyntaxKind.PartialKeyword) && sds.BaseList is not null)
				{
					// Consider any type with SOME 1-param generic "IIdentity" inheritance/implementation
					foreach (var baseType in sds.BaseList.Types)
					{
						if (baseType.Type is not GenericNameSyntax genericName) continue;

						if (genericName.Arity == 1 && genericName.Identifier.ValueText == Constants.IdentityInterfaceTypeName)
						{
							this.CandidatePartialIdentityStructs.Add(sds);
							return;
						}
					}
				}

				// Concrete, non-generic classes
				if (node is ClassDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword) && cds.TypeParameterList is null && cds.BaseList is not null)
				{
					// Consider any type with SOME 2-param generic "Entity" inheritance/implementation
					foreach (var baseType in cds.BaseList.Types)
					{
						if (baseType.Type is not GenericNameSyntax genericName) continue;

						if (genericName.Arity == 2 && genericName.Identifier.ValueText == Constants.EntityTypeName)
						{
							this.CandidateEntityClasses.Add(cds);
							return;
						}
					}
				}
			}
		}

		public override void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public override void Execute(GeneratorExecutionContext context)
		{
			// Work only with our own syntax receiver
			if (context.SyntaxReceiver is not SyntaxReceiver receiver)
				return;

			// Complete partial identity structs
			foreach (var sds in receiver.CandidatePartialIdentityStructs)
			{
				var model = context.Compilation.GetSemanticModel(sds.SyntaxTree);
				var type = model.GetDeclaredSymbol(sds)!;

				// Only with the attribute
				if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
					continue;
				// Only with the intended inheritance
				if (!type.Interfaces.Any(interf => interf.Name == Constants.IdentityInterfaceTypeName && interf.ContainingNamespace.HasFullName(Constants.DomainModelingNamespace) && interf.IsGenericType && interf.TypeParameters.Length == 1))
				{
					context.ReportDiagnostic("IdentityGeneratorUnexpectedInheritance", "Unexpected interface",
						"The type marked as source-generated has an unexpected base class or interface. Did you mean IIdentity<T>?", DiagnosticSeverity.Warning, type);
					continue;
				}
				// Only if non-generic
				if (type.IsGenericType)
				{
					context.ReportDiagnostic("IdentityGeneratorGenericType", "Source-generated generic type",
						"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, type);
					continue;
				}
				// Only if non-nested
				if (sds.Parent is not NamespaceDeclarationSyntax && sds.Parent is not FileScopedNamespaceDeclarationSyntax)
				{
					context.ReportDiagnostic("IdentityGeneratorNestedType", "Source-generated nested type",
						"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, type);
					continue;
				}

				AddPartialIdentityStructForExisting(context, type: type);
			}

			// Provide identity structs for entities with source-generated ID types
			foreach (var cds in receiver.CandidateEntityClasses)
			{
				var model = context.Compilation.GetSemanticModel(cds.SyntaxTree);
				var type = model.GetDeclaredSymbol(cds)!;

				INamedTypeSymbol? entityType = null;
				var baseType = (INamedTypeSymbol?)type;
				while ((baseType = baseType!.BaseType) is not null)
				{
					// End of inheritance chain
					if (baseType.IsType<object>())
						break;

					if (baseType.Arity == 2 &&
						baseType.IsType(Constants.EntityTypeName, Constants.DomainModelingNamespace) &&
						baseType.TypeArguments[0] is INamedTypeSymbol)
					{
						entityType = baseType;
						break;
					}
				}

				if (entityType is null) continue;

				// The type exists if it is not of TypeKind.Error
				var idTypeExists = baseType!.TypeArguments[0].TypeKind != TypeKind.Error;

				if (idTypeExists) // No need to generate the ID type
				{
					context.ReportDiagnostic("EntityIdentityTypeAlreadyExists", "Entity identity type already exists",
						"Base class Entity<TId, TIdPrimitive> is intended to generate source for TId, but TId refers to an existing type. To use an existing identity type, inherit from Entity<TId> instead.", DiagnosticSeverity.Warning, type);
					continue;
				}

				AddIdentityStruct(context, type.ContainingNamespace, type, baseType.TypeArguments[0], baseType.TypeArguments[1]);
			}
		}

		/// <summary>
		/// Adds a partial identity struct that completes the given existing one.
		/// </summary>
		private static void AddPartialIdentityStructForExisting(GeneratorExecutionContext context, INamedTypeSymbol type)
		{
			var interf = type.Interfaces.Single(interf =>
				interf.ContainingNamespace.HasFullName(Constants.DomainModelingNamespace) && interf.IsGenericType && interf.TypeParameters.Length == 1);

			var underlyingType = interf.TypeArguments[0];

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

			AddIdentityStruct(context, type.ContainingNamespace, entityType: null, type, underlyingType, existingComponents);
		}

		/// <summary>
		/// Adds an identity struct based on the given parameters.
		/// </summary>
		private static void AddIdentityStruct(GeneratorExecutionContext context,
			INamespaceSymbol containingNamespace, ITypeSymbol? entityType, ITypeSymbol idType, ITypeSymbol underlyingType,
			IdTypeComponents existingComponents = IdTypeComponents.None)
		{
			var entityTypeName = entityType?.Name;
			var idTypeName = idType.Name;
			var underlyingTypeName = underlyingType.ToString();

			// If the ID type is manually declared (not from an entity), then we follow its accessibility
			// Otherwise, we do not support combining with a manual definition, so we honor the entity's accessibility
			// The entity could be a private nested type (for example), and a private non-nested ID type would have insufficient accessibility, so then we need at least "internal"
			var accessibility = entityType is null
				? idType.DeclaredAccessibility
				: entityType.DeclaredAccessibility.AtLeast(Accessibility.Internal);

			// The outcommented block could be used in the future to follow the developer's nullability specification, if that is considered beneficial
			//var nullInputAnnotation = "";
			//var nullPropertyAnnotation = "";
			//var mayReturnNullAnnotation = "";
			//switch (underlyingType.NullableAnnotation)
			//{
			//	case NullableAnnotation.NotAnnotated: // Not nullable
			//		nullInputAnnotation = "[DisallowNull] ";
			//		nullPropertyAnnotation = "[DisallowNull, NotNull]";
			//		mayReturnNullAnnotation = "[return: NotNull]";
			//		break;
			//	case NullableAnnotation.Annotated: // Nullable
			//		nullInputAnnotation = "[AllowNull] ";
			//		nullPropertyAnnotation = "[AllowNull, MaybeNull]";
			//		mayReturnNullAnnotation = "[return: MaybeNull]";
			//		break;
			//}
			var nullInputAnnotation = "[AllowNull] ";
			var nullPropertyAnnotation = "[AllowNull, MaybeNull]";
			var mayReturnNullAnnotation = "[return: MaybeNull]";

			var summary = entityTypeName is null ? null : $@"
	/// <summary>
	/// The identity type used for the <see cref=""{entityTypeName}""/> entity.
	/// </summary>";

			var hasSourceGeneratedAttribute = idType.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace);

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
	{accessibility.ToCodeString()} readonly{(entityType is null ? " partial" : "")} struct {idTypeName} : {Constants.IdentityInterfaceTypeName}<{underlyingTypeName}>, IEquatable<{idTypeName}>, IComparable<{idTypeName}>
	{{
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "/*" : "")}
		{nonNullStringSummary}
		{(underlyingType.IsValueType ? "" : isNonNullString ? "[NotNull]" : nullPropertyAnnotation)}
		public {underlyingTypeName} Value {(isNonNullString ? @"=> this._value ?? """";" : "{ get; }")}
		{(isNonNullString ? "private readonly string _value;" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.Constructor) ? "/*" : "")}
		public {idTypeName}({(underlyingType.IsValueType ? "" : nullInputAnnotation)}{underlyingTypeName} value)
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
		public static implicit operator {idTypeName}({(underlyingType.IsValueType ? "" : nullInputAnnotation)}{underlyingTypeName} value) => new {idTypeName}(value);
		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType || isNonNullString ? "" : mayReturnNullAnnotation)}
		public static implicit operator {underlyingTypeName}({idTypeName} id) => id.Value;
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "/*" : "")}
		[return: MaybeNull, NotNullIfNotNull(""value"")]
		public static implicit operator {idTypeName}?({(underlyingType.IsValueType ? "" : "[AllowNull] ")}{underlyingTypeName}{(underlyingType.IsValueType ? "?" : "")} value) => value is null ? ({idTypeName}?)null : new {idTypeName}(value{(underlyingType.IsValueType ? ".Value" : "")});
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "/*" : "")}
		{(underlyingType.IsValueType || isNonNullString ? @"[return: MaybeNull, NotNullIfNotNull(""id"")]" : "[return: MaybeNull]")}
		public static implicit operator {underlyingTypeName}{(underlyingType.IsValueType ? "?" : "")}({idTypeName}? id) => id?.Value;
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
					: $@"return ({idTypeName})System.Text.Json.JsonSerializer.Deserialize<{underlyingTypeName}>(ref reader, options);")}
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
						? $@"return reader.TokenType == Newtonsoft.Json.JsonToken.Integer ? ({idTypeName}?)serializer.Deserialize<{underlyingTypeName}?>(reader) : ({idTypeName}){underlyingType.ContainingNamespace}.{underlyingType.Name}.Parse(serializer.Deserialize<string>(reader), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);"
						: $@"return ({idTypeName})serializer.Deserialize<{underlyingTypeName}>(reader);")}
				else // Nullable
					{(underlyingTypeIsNumericUnsuitableForJson
						? $@"return reader.Value is null ? null : reader.TokenType == Newtonsoft.Json.JsonToken.Integer ? ({idTypeName}?)serializer.Deserialize<{underlyingTypeName}?>(reader) : ({idTypeName}?){underlyingType.ContainingNamespace}.{underlyingType.Name}.Parse(serializer.Deserialize<string>(reader), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture);"
						: $@"return ({idTypeName}?)serializer.Deserialize<{underlyingTypeName}{(underlyingType.IsValueType ? "?" : "")}>(reader);")}
				#nullable enable
			}}
		}}
		{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}
	}}
}}
";

			AddSource(context, source, idType);
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
	}
}
