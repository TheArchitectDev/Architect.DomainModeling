using Architect.DomainModeling.Generator.Common;
using Architect.DomainModeling.Generator.Configurators;
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

		var aggregatedProvider = provider
			.Collect()
			.Combine(EntityFrameworkConfigurationGenerator.CreateMetadataProvider(context));

		context.RegisterSourceOutput(aggregatedProvider, DomainModelConfiguratorGenerator.GenerateSourceForIdentities!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Struct or class or record
		if (node is TypeDeclarationSyntax tds && tds is StructDeclarationSyntax or ClassDeclarationSyntax or RecordDeclarationSyntax)
		{
			// With relevant attribute
			if (tds.HasAttributeWithPrefix("IdentityValueObject"))
				return true;
		}

		// Non-generic class with any inherited/implemented types
		if (node is ClassDeclarationSyntax cds && cds.Arity == 0 && cds.BaseList is not null)
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
		var result = new Generatable();

		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol(tds);

		if (type is null)
			return null;

		ITypeSymbol underlyingType;
		var isBasedOnEntity = type.IsOrInheritsClass(baseType => baseType.Name == Constants.EntityTypeName, out _);

		// Path A: An Entity subclass that might be an Entity<TId, TUnderlying> for which TId may have to be generated
		if (isBasedOnEntity)
		{
			// Only an actual Entity<TId, TUnderlying>
			if (!type.IsOrInheritsClass(baseType => baseType.Arity == 2 && baseType.IsType(Constants.EntityTypeName, Constants.DomainModelingNamespace), out var entityType))
				return null;

			var idType = entityType.TypeArguments[0];
			underlyingType = entityType.TypeArguments[1];
			result.EntityTypeName = type.Name;
			result.EntityTypeLocation = type.Locations.FirstOrDefault();

			// The ID type exists if it is not of TypeKind.Error
			result.IdTypeExists = idType.TypeKind != TypeKind.Error;

			if (result.IdTypeExists)
				return result;

			result.IsStruct = true;
			result.ContainingNamespace = type.ContainingNamespace.ToString();
			result.IdTypeName = idType.Name;

			// We do not support combining with a manual definition, so we honor the entity's accessibility
			// The entity could be a private nested type (for example), and a private non-nested ID type would have insufficient accessibility, so then we need at least "internal"
			result.Accessibility = type.DeclaredAccessibility.AtLeast(Accessibility.Internal);
		}
		// Path B: An annotated type for which a partial may need to be generated
		else
		{
			// Only with the attribute
			if (type.GetAttribute("IdentityValueObjectAttribute", Constants.DomainModelingNamespace, arity: 1) is not AttributeData { AttributeClass: not null } attribute)
				return null;

			underlyingType = attribute.AttributeClass.TypeArguments[0];

			result.IdTypeExists = true;
			result.IdTypeLocation = type.Locations.FirstOrDefault();
			result.IsIIdentity = type.IsOrImplementsInterface(interf => interf.IsType(Constants.IdentityInterfaceTypeName, Constants.DomainModelingNamespace, arity: 1), out _);
			result.IsSerializableDomainObject = type.IsOrImplementsInterface(type => type.IsType(Constants.SerializableDomainObjectInterfaceTypeName, Constants.DomainModelingNamespace, arity: 2), out _);
			result.IsPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword);
			result.IsRecord = type.IsRecord;
			result.IsStruct = type.TypeKind == TypeKind.Struct;
			result.IsAbstract = type.IsAbstract;
			result.IsGeneric = type.IsGenericType;
			result.IsNested = type.IsNested();

			result.ContainingNamespace = type.ContainingNamespace.ToString();
			result.IdTypeName = type.Name;
			result.Accessibility = type.DeclaredAccessibility;

			var members = type.GetMembers();

			var existingComponents = IdTypeComponents.None;

			existingComponents |= IdTypeComponents.Value.If(members.Any(member => member.Name == "Value"));

			existingComponents |= IdTypeComponents.UnsettableValue.If(members.Any(member => member.Name == "Value" && member is not IFieldSymbol && member is not IPropertySymbol { SetMethod: not null }));

			existingComponents |= IdTypeComponents.Constructor.If(type.Constructors.Any(ctor =>
				!ctor.IsStatic && ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			// Records override this, but our implementation is superior
			existingComponents |= IdTypeComponents.ToStringOverride.If(!result.IsRecord && members.Any(member =>
				member.Name == nameof(ToString) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 0));

			// Records override this, but our implementation is superior
			existingComponents |= IdTypeComponents.GetHashCodeOverride.If(!result.IsRecord && members.Any(member =>
				member.Name == nameof(GetHashCode) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 0));

			// Records irrevocably and correctly override this, checking the type and delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.EqualsOverride.If(members.Any(member =>
				member.Name == nameof(Equals) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.IsType<object>()));

			// Records override this, but our implementation is superior
			existingComponents |= IdTypeComponents.EqualsMethod.If(!result.IsRecord && members.Any(member =>
				member.Name == nameof(Equals) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.CompareToMethod.If(members.Any(member =>
				member.Name == nameof(IComparable.CompareTo) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.EqualsOperator.If(members.Any(member =>
				member.Name == "op_Equality" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.NotEqualsOperator.If(members.Any(member =>
				member.Name == "op_Inequality" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.GreaterThanOperator.If(members.Any(member =>
				member.Name == "op_GreaterThan" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.LessThanOperator.If(members.Any(member =>
				member.Name == "op_LessThan" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
				member.Name == "op_GreaterThanOrEqual" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.LessEqualsOperator.If(members.Any(member =>
				member.Name == "op_LessThanOrEqual" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ConvertToOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ConvertFromOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.NullableConvertToOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.ReturnType.IsType(nameof(Nullable<int>), "System") && method.ReturnType.HasSingleGenericTypeArgument(type) &&
				(underlyingType.IsReferenceType
					? method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)
					: method.Parameters[0].Type.IsType(nameof(Nullable<int>), "System") && method.Parameters[0].Type.HasSingleGenericTypeArgument(underlyingType))));

			existingComponents |= IdTypeComponents.NullableConvertFromOperator.If(members.Any(member =>
				(member.Name == "op_Implicit" || member.Name == "op_Explicit") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				(underlyingType.IsReferenceType
					? method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default)
					: method.ReturnType.IsType(nameof(Nullable<int>), "System") && method.ReturnType.HasSingleGenericTypeArgument(underlyingType)) &&
				method.Parameters[0].Type.IsType(nameof(Nullable<int>), "System") && method.Parameters[0].Type.HasSingleGenericTypeArgument(type)));

			existingComponents |= IdTypeComponents.SerializeToUnderlying.If(members.Any(member =>
				member.Name.EndsWith($".{Constants.SerializeDomainObjectMethodName}") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 0));

			existingComponents |= IdTypeComponents.DeserializeFromUnderlying.If(members.Any(member =>
				member.Name.EndsWith($".{Constants.DeserializeDomainObjectMethodName}") && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 1 &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

			existingComponents |= IdTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft.Json") == true));

			existingComponents |= IdTypeComponents.StringComparison.If(members.Any(member =>
				member.Name == "StringComparison"));

			existingComponents |= IdTypeComponents.FormattableToStringOverride.If(members.Any(member =>
				member.Name == nameof(IFormattable.ToString) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>()));

			existingComponents |= IdTypeComponents.ParsableTryParseMethod.If(members.Any(member =>
				member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
				method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>() && method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.ParsableParseMethod.If(members.Any(member =>
				member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>()));

			existingComponents |= IdTypeComponents.SpanFormattableTryFormatMethod.If(members.Any(member =>
				member.Name == "TryFormat" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 4 &&
				method.Parameters[0].Type.IsType(typeof(Span<char>)) &&
				method.Parameters[1].Type.IsType<int>() && method.Parameters[1].RefKind == RefKind.Out &&
				method.Parameters[2].Type.IsType(typeof(ReadOnlySpan<char>)) &&
				method.Parameters[3].Type.IsType<IFormatProvider>()));

			existingComponents |= IdTypeComponents.SpanParsableTryParseMethod.If(members.Any(member =>
				member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
				method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<char>)) &&
				method.Parameters[1].Type.IsType(typeof(IFormatProvider)) &&
				method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.SpanParsableParseMethod.If(members.Any(member =>
				member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<char>)) &&
				method.Parameters[1].Type.IsType(typeof(IFormatProvider))));

			existingComponents |= IdTypeComponents.Utf8SpanFormattableTryFormatMethod.If(members.Any(member =>
				member.Name == "TryFormat" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 4 &&
				method.Parameters[0].Type.IsType(typeof(Span<byte>)) &&
				method.Parameters[1].Type.IsType<int>() && method.Parameters[1].RefKind == RefKind.Out &&
				method.Parameters[2].Type.IsType(typeof(ReadOnlySpan<char>)) &&
				method.Parameters[3].Type.IsType<IFormatProvider>()));

			existingComponents |= IdTypeComponents.Utf8SpanParsableTryParseMethod.If(members.Any(member =>
				member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
				method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<byte>)) &&
				method.Parameters[1].Type.IsType(typeof(IFormatProvider)) &&
				method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.Utf8SpanParsableParseMethod.If(members.Any(member =>
				member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
				method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<byte>)) &&
				method.Parameters[1].Type.IsType(typeof(IFormatProvider))));

			result.ExistingComponents = existingComponents;
		}

		result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
		result.UnderlyingTypeIsToStringNullable = underlyingType.IsToStringNullable();
		result.UnderlyingTypeIsINumber = underlyingType.IsOrImplementsInterface(interf => interf.IsType("INumber", "System.Numerics", arity: 1), out _);
		result.UnderlyingTypeIsString = underlyingType.IsType<string>();
		result.UnderlyingTypeIsNonNullString = result.UnderlyingTypeIsString && underlyingType.NullableAnnotation != NullableAnnotation.Annotated;
		result.UnderlyingTypeIsNumericUnsuitableForJson = underlyingType.IsType<decimal>() || underlyingType.IsType<ulong>() || underlyingType.IsType<long>() || underlyingType.IsType<System.Numerics.BigInteger>() ||
			underlyingType.IsType("UInt128", "System") || underlyingType.IsType("Int128", "System");
		result.UnderlyingTypeIsStruct = underlyingType.IsValueType;
		result.ToStringExpression = underlyingType.CreateStringExpression("Value");
		result.HashCodeExpression = underlyingType.CreateHashCodeExpression("Value", stringVariant: "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))");
		result.EqualityExpression = underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)");
		result.ComparisonExpression = underlyingType.CreateComparisonExpression("Value", stringVariant: "String.Compare(this.{0}, other.{0}, this.StringComparison)");

		return result;
	}
	
	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var containingNamespace = generatable.ContainingNamespace;
		var idTypeName = generatable.IdTypeName;
		var underlyingTypeFullyQualifiedName = generatable.UnderlyingTypeFullyQualifiedName;
		var entityTypeName = generatable.EntityTypeName;
		var underlyingTypeIsStruct = generatable.UnderlyingTypeIsStruct;
		var isRecord = generatable.IsRecord;
		var isINumber = generatable.UnderlyingTypeIsINumber;
		var isString = generatable.UnderlyingTypeIsString;
		var isToStringNullable = generatable.UnderlyingTypeIsToStringNullable;
		var toStringExpression = generatable.ToStringExpression;
		var hashCodeExpression = generatable.HashCodeExpression;
		var equalityExpression = generatable.EqualityExpression;
		var comparisonExpression = generatable.ComparisonExpression;

		var accessibility = generatable.Accessibility;
		var existingComponents = generatable.ExistingComponents;
		var hasIdentityValueObjectAttribute = generatable.IdTypeExists;

		if (generatable.IdTypeExists)
		{
			// Entity<TId, TUnderlying> was needlessly used, with a preexisting TId
			if (entityTypeName is not null)
			{
				context.ReportDiagnostic("EntityIdentityTypeAlreadyExists", "Entity identity type already exists",
					"Base class Entity<TId, TIdPrimitive> is intended to generate source for TId, but TId refers to an existing type. To use an existing identity type, inherit from Entity<TId> instead.", DiagnosticSeverity.Warning, generatable.EntityTypeLocation);
				return;
			}

			// Require the expected inheritance
			if (!generatable.IsPartial && !generatable.IsIIdentity)
			{
				context.ReportDiagnostic("IdentityGeneratorUnexpectedInheritance", "Unexpected interface",
					"Type marked as identity value object lacks IIdentity<T> interface. Did you forget the 'partial' keyword and elude source generation?", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}

			// Require ISerializableDomainObject
			if (!generatable.IsPartial && !generatable.IsSerializableDomainObject)
			{
				context.ReportDiagnostic("IdentityGeneratorMissingSerializableDomainObject", "Missing interface",
					"Type marked as identity value object lacks ISerializableDomainObject<TModel, TUnderlying> interface.", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}

			// No source generation, only above analyzers
			if (!generatable.IsPartial)
				return;

			// Only if struct
			if (!generatable.IsStruct)
			{
				context.ReportDiagnostic("IdentityGeneratorReferenceType", "Source-generated reference-typed identity",
					"The type was not source-generated because it is a class, while a struct was expected. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}

			// Only if non-abstract
			if (generatable.IsAbstract)
			{
				context.ReportDiagnostic("IdentityGeneratorAbstractType", "Source-generated abstract type",
					"The type was not source-generated because it is abstract. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}

			// Only if non-generic
			if (generatable.IsGeneric)
			{
				context.ReportDiagnostic("IdentityGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}

			// Only if non-nested
			if (generatable.IsNested)
			{
				context.ReportDiagnostic("IdentityGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.IdTypeLocation);
				return;
			}
		}

		var summary = entityTypeName is null ? null : $@"
	/// <summary>
	/// The identity type used for the <see cref=""{entityTypeName}""/> entity.
	/// </summary>";

		// Special case for strings, unless they are explicitly annotated as nullable
		// An ID wrapping a null string (such as a default instance) acts as if it contains an empty string instead
		// This allows strings to be used as a primitive without any null troubles
		// Conversions are carefree this way, and null inputs simply get converted to empty string equivalents, which tend not to match any valid ID
		var isNonNullString = generatable.UnderlyingTypeIsNonNullString;
		var nonNullStringSummary = !isNonNullString ? null : $@"
		/// <summary>
		/// A default <see cref=""{idTypeName}""/> instance always produces an empty string, not null.
		/// </summary>";

		// JavaScript (and arguably, by extent, JSON) have insufficient numeric capacity to properly hold the longer numeric types
		var underlyingTypeIsNumericUnsuitableForJson = generatable.UnderlyingTypeIsNumericUnsuitableForJson;

		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {Constants.DomainModelingNamespace};
using {Constants.DomainModelingNamespace}.Conversions;

#nullable enable

namespace {containingNamespace}
{{
	{summary}

	{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
	{JsonSerializationGenerator.WriteJsonConverterAttribute(idTypeName)}
	{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

	{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
	{JsonSerializationGenerator.WriteNewtonsoftJsonConverterAttribute(idTypeName)}
	{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}

	{(hasIdentityValueObjectAttribute ? "" : $"[IdentityValueObject<{underlyingTypeFullyQualifiedName}>]")}
	{(entityTypeName is null ? "/* Generated */ " : "")}{accessibility.ToCodeString()} readonly{(entityTypeName is null ? " partial" : "")}{(isRecord ? " record" : "")} struct {idTypeName}
		: {Constants.IdentityInterfaceTypeName}<{underlyingTypeFullyQualifiedName}>,
		IEquatable<{idTypeName}>,
		IComparable<{idTypeName}>,
#if NET7_0_OR_GREATER
		ISpanFormattable,
		ISpanParsable<{idTypeName}>,
#endif
#if NET8_0_OR_GREATER
		IUtf8SpanFormattable,
		IUtf8SpanParsable<{idTypeName}>,
#endif
		{Constants.SerializableDomainObjectInterfaceTypeName}<{idTypeName}, {underlyingTypeFullyQualifiedName}>
	{{
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "/*" : "")}
		{nonNullStringSummary}
		{(isNonNullString ? "[AllowNull] public" : "public")} {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct || isNonNullString ? "" : "?")} Value {(isNonNullString ? @"{ get => this._value ?? """"; private init => this._value = value ?? """"; }" : "{ get; private init; }")}
		{(isNonNullString ? "private readonly string? _value;" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.Constructor) ? "/*" : "")}
		public {idTypeName}({underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
		{{
			this.Value = value;
		}}
		{(existingComponents.HasFlags(IdTypeComponents.Constructor) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.StringComparison) ? "/*" : "")}
		{(isString
		? @"private StringComparison StringComparison => StringComparison.Ordinal;"
		: "")}
		{(existingComponents.HasFlags(IdTypeComponents.StringComparison) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ToStringOverride) ? "/*" : "")}{nonNullStringSummary}
		public override string{(isNonNullString || !isToStringNullable ? "" : "?")} ToString()
		{{

			return {(isINumber
				? """this.Value.ToString("0.#")"""
				: toStringExpression)};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			return {hashCodeExpression};
#pragma warning restore RS1024 // Compare symbols correctly
		}}
		{(existingComponents.HasFlags(IdTypeComponents.GetHashCodeOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.EqualsOverride) ? "/*" : "")}
		public override bool Equals(object? other)
		{{
			return other is {idTypeName} otherId && this.Equals(otherId);
		}}
		{(existingComponents.HasFlags(IdTypeComponents.EqualsOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.EqualsMethod) ? "/*" : "")}
		public bool Equals({idTypeName} other)
		{{
			return {equalityExpression};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.EqualsMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.CompareToMethod) ? "/*" : "")}
		public int CompareTo({idTypeName} other)
		{{
			return {comparisonExpression};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.CompareToMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SerializeToUnderlying) ? "/*" : "")}
		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		{underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct || isNonNullString ? "" : "?")} {Constants.SerializableDomainObjectInterfaceTypeName}<{idTypeName}, {underlyingTypeFullyQualifiedName}>.Serialize()
		{{
			return this.Value;
		}}
		{(existingComponents.HasFlags(IdTypeComponents.SerializeToUnderlying) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.DeserializeFromUnderlying) ? "/*" : "")}
#if NET7_0_OR_GREATER
		/// <summary>
		/// Deserializes a plain value back into a domain object, without any validation.
		/// </summary>
		static {idTypeName} {Constants.SerializableDomainObjectInterfaceTypeName}<{idTypeName}, {underlyingTypeFullyQualifiedName}>.Deserialize({underlyingTypeFullyQualifiedName} value)
		{{
			{(existingComponents.HasFlag(IdTypeComponents.UnsettableValue) ? "// To instead get safe syntax, make the Value property '{ get; private init; }' (or let the source generator implement it)" : "")}
			{(existingComponents.HasFlag(IdTypeComponents.UnsettableValue) ? $"return System.Runtime.CompilerServices.Unsafe.As<{underlyingTypeFullyQualifiedName}, {idTypeName}>(ref value);" : "")}
			{(existingComponents.HasFlag(IdTypeComponents.UnsettableValue) ? "//" : "")}return new {idTypeName}() {{ Value = value }};
		}}
#endif
		{(existingComponents.HasFlags(IdTypeComponents.DeserializeFromUnderlying) ? "*/" : "")}

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
		public static implicit operator {idTypeName}({underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value) => new {idTypeName}(value);
		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "/*" : "")}{nonNullStringSummary}
		public static implicit operator {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct || isNonNullString ? "" : "?")}({idTypeName} id) => id.Value;
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "/*" : "")}
		[return: NotNullIfNotNull(""value"")]
		public static implicit operator {idTypeName}?({underlyingTypeFullyQualifiedName}? value) => value is null ? ({idTypeName}?)null : new {idTypeName}(value{(underlyingTypeIsStruct ? ".Value" : "")});
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "/*" : "")}{nonNullStringSummary}
		{(underlyingTypeIsStruct || isNonNullString ? @"[return: NotNullIfNotNull(""id"")]" : "")}
		public static implicit operator {underlyingTypeFullyQualifiedName}?({idTypeName}? id) => id?.Value;
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "*/" : "")}

		#region Formatting & Parsing

#if NET7_0_OR_GREATER

		{(existingComponents.HasFlags(IdTypeComponents.FormattableToStringOverride) ? "/*" : "")}
		public string ToString(string? format, IFormatProvider? formatProvider) =>
			FormattingHelper.ToString(this.Value, format, formatProvider);
		{(existingComponents.HasFlags(IdTypeComponents.FormattableToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);
		{(existingComponents.HasFlags(IdTypeComponents.SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(IdTypeComponents.ParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(IdTypeComponents.SpanParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.ParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(string s, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(existingComponents.HasFlags(IdTypeComponents.ParsableParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SpanParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(existingComponents.HasFlags(IdTypeComponents.SpanParsableParseMethod) ? "*/" : "")}

#endif

#if NET8_0_OR_GREATER

		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);
		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(utf8Text, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(utf8Text, provider);
		{(existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableParseMethod) ? "*/" : "")}

#endif

		#endregion

		{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
		{JsonSerializationGenerator.WriteJsonConverter(idTypeName, underlyingTypeFullyQualifiedName, numericAsString: underlyingTypeIsNumericUnsuitableForJson)}
		{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
		{JsonSerializationGenerator.WriteNewtonsoftJsonConverter(idTypeName, underlyingTypeFullyQualifiedName, isStruct: true, numericAsString: underlyingTypeIsNumericUnsuitableForJson)}
		{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}
	}}
}}
";

		AddSource(context, source, idTypeName, containingNamespace);
	}

	[Flags]
	internal enum IdTypeComponents : ulong
	{
		None = 0,

		Value = 1UL << 0,
		Constructor = 1UL << 1,
		ToStringOverride = 1UL << 2,
		GetHashCodeOverride = 1UL << 3,
		EqualsOverride = 1UL << 4,
		EqualsMethod = 1UL << 5,
		CompareToMethod = 1UL << 6,
		EqualsOperator = 1UL << 7,
		NotEqualsOperator = 1UL << 8,
		GreaterThanOperator = 1UL << 9,
		LessThanOperator = 1UL << 10,
		GreaterEqualsOperator = 1UL << 11,
		LessEqualsOperator = 1UL << 12,
		ConvertToOperator = 1UL << 13,
		ConvertFromOperator = 1UL << 14,
		NullableConvertToOperator = 1UL << 15,
		NullableConvertFromOperator = 1UL << 16,
		NewtonsoftJsonConverter = 1UL << 17,
		SystemTextJsonConverter = 1UL << 18,
		StringComparison = 1UL << 19,
		SerializeToUnderlying = 1UL << 20,
		DeserializeFromUnderlying = 1UL << 21,
		UnsettableValue = 1UL << 22,

		FormattableToStringOverride = 1UL << 24,
		ParsableTryParseMethod = 1UL << 25,
		ParsableParseMethod = 1UL << 26,
		SpanFormattableTryFormatMethod = 1UL << 27,
		SpanParsableTryParseMethod = 1UL << 28,
		SpanParsableParseMethod = 1UL << 29,
		Utf8SpanFormattableTryFormatMethod = 1UL << 30,
		Utf8SpanParsableTryParseMethod = 1UL << 31,
		Utf8SpanParsableParseMethod = 1UL << 32,
	}

	internal sealed record Generatable : IGeneratable
	{
		private uint _bits;
		public bool IdTypeExists { get => this._bits.GetBit(0); set => this._bits.SetBit(0, value); }
		public string EntityTypeName { get; set; } = null!;
		public bool IsIIdentity { get => this._bits.GetBit(1); set => this._bits.SetBit(1, value); }
		public bool IsPartial { get => this._bits.GetBit(2); set => this._bits.SetBit(2, value); }
		public bool IsRecord { get => this._bits.GetBit(3); set => this._bits.SetBit(3, value); }
		public bool IsStruct { get => this._bits.GetBit(4); set => this._bits.SetBit(4, value); }
		public bool IsAbstract { get => this._bits.GetBit(5); set => this._bits.SetBit(5, value); }
		public bool IsGeneric { get => this._bits.GetBit(6); set => this._bits.SetBit(6, value); }
		public bool IsNested { get => this._bits.GetBit(7); set => this._bits.SetBit(7, value); }
		public string ContainingNamespace { get; set; } = null!;
		public string IdTypeName { get; set; } = null!;
		public string UnderlyingTypeFullyQualifiedName { get; set; } = null!;
		public bool UnderlyingTypeIsToStringNullable { get => this._bits.GetBit(8); set => this._bits.SetBit(8, value); }
		public bool UnderlyingTypeIsINumber { get => this._bits.GetBit(9); set => this._bits.SetBit(9, value); }
		public bool UnderlyingTypeIsString { get => this._bits.GetBit(10); set => this._bits.SetBit(10, value); }
		public bool UnderlyingTypeIsNonNullString { get => this._bits.GetBit(11); set => this._bits.SetBit(11, value); }
		public bool UnderlyingTypeIsNumericUnsuitableForJson { get => this._bits.GetBit(12); set => this._bits.SetBit(12, value); }
		public bool UnderlyingTypeIsStruct { get => this._bits.GetBit(13); set => this._bits.SetBit(13, value); }
		public bool IsSerializableDomainObject { get => this._bits.GetBit(14); set => this._bits.SetBit(14, value); }
		public Accessibility Accessibility { get; set; }
		public IdTypeComponents ExistingComponents { get; set; }
		public string ToStringExpression { get; set; } = null!;
		public string HashCodeExpression { get; set; } = null!;
		public string EqualityExpression { get; set; } = null!;
		public string ComparisonExpression { get; set; } = null!;
		public SimpleLocation? EntityTypeLocation { get; set; }
		public SimpleLocation? IdTypeLocation { get; set; }
	}
}
