using Architect.DomainModeling.Generator.Common;
using Architect.DomainModeling.Generator.Configurators;
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

		var aggregatedProvider = provider
			.Collect()
			.Combine(EntityFrameworkConfigurationGenerator.CreateMetadataProvider(context));

		context.RegisterSourceOutput(aggregatedProvider, DomainModelConfiguratorGenerator.GenerateSourceForWrapperValueObjects!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Struct or class or record
		if (node is TypeDeclarationSyntax tds && tds is StructDeclarationSyntax or ClassDeclarationSyntax or RecordDeclarationSyntax)
		{
			// With relevant attribute
			if (tds.HasAttributeWithPrefix("WrapperValueObject"))
				return true;
		}

		return false;
	}

	private static Generatable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol(tds);

		if (type is null)
			return null;

		// Only with the attribute
		if (type.GetAttribute("WrapperValueObjectAttribute", Constants.DomainModelingNamespace, arity: 1) is not AttributeData { AttributeClass: not null } attribute)
			return null;

		var underlyingType = attribute.AttributeClass.TypeArguments[0];

		var result = new Generatable();
		result.TypeLocation = type.Locations.FirstOrDefault();
		result.IsWrapperValueObject = type.IsOrImplementsInterface(type => type.IsType(Constants.WrapperValueObjectInterfaceTypeName, Constants.DomainModelingNamespace, arity: 1), out _);
		result.IsSerializableDomainObject = type.IsOrImplementsInterface(type => type.IsType(Constants.SerializableDomainObjectInterfaceTypeName, Constants.DomainModelingNamespace, arity: 2), out _);
		result.IsPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword);
		result.IsRecord = type.IsRecord;
		result.IsClass = type.TypeKind == TypeKind.Class;
		result.IsAbstract = type.IsAbstract;
		result.IsGeneric = type.IsGenericType;
		result.IsNested = type.IsNested();
		result.Accessibility = type.DeclaredAccessibility;

		result.TypeName = type.Name; // Will be non-generic if we pass the conditions to proceed with generation
		result.ContainingNamespace = type.ContainingNamespace.ToString();

		result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
		result.UnderlyingTypeKind = underlyingType.TypeKind;
		result.UnderlyingTypeIsStruct = underlyingType.IsValueType;
		result.UnderlyingTypeIsNullable = underlyingType.IsNullable();
		result.UnderlyingTypeIsString = underlyingType.IsType<string>();
		result.UnderlyingTypeHasNullableToString = underlyingType.IsToStringNullable();

		result.ValueFieldName = type.GetMembers().FirstOrDefault(member => member is IFieldSymbol field && (field.Name == "<Value>k__BackingField" || field.Name.Equals("value") || field.Name.Equals("_value")))?.Name ??
			"_value";
		// IComparable is implemented on-demand, if the type implements IComparable against itself and the underlying type is self-comparable
		// It is also implemented if the underlying type is an annotated identity
		result.IsComparable = type.AllInterfaces.Any(interf => interf.IsType("IComparable", "System", arity: 1) && interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default)) &&
			underlyingType.IsComparable(seeThroughNullable: true);
		result.IsComparable |= underlyingType.GetAttribute("IdentityValueObjectAttribute", Constants.DomainModelingNamespace, arity: 1) is not null;

		var members = type.GetMembers();

		var existingComponents = WrapperValueObjectTypeComponents.None;

		existingComponents |= WrapperValueObjectTypeComponents.Value.If(members.Any(member => member.Name == "Value"));

		existingComponents |= WrapperValueObjectTypeComponents.UnsettableValue.If(members.Any(member => member.Name == "Value" && member is not IFieldSymbol && member is not IPropertySymbol { SetMethod: not null }));

		existingComponents |= WrapperValueObjectTypeComponents.Constructor.If(type.Constructors.Any(ctor =>
			!ctor.IsStatic && ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.DefaultConstructor.If(type.Constructors.Any(ctor =>
			!ctor.IsStatic && ctor.Parameters.Length == 0 && ctor.DeclaringSyntaxReferences.Length > 0));

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.ToStringOverride.If(!result.IsRecord && members.Any(member =>
			member.Name == nameof(ToString) && member is IMethodSymbol method && method.Parameters.Length == 0));

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.GetHashCodeOverride.If(!result.IsRecord && members.Any(member =>
			member.Name == nameof(GetHashCode) && member is IMethodSymbol method && method.Parameters.Length == 0));

		// Records irrevocably and correctly override this, checking the type and delegating to IEquatable<T>.Equals(T)
		existingComponents |= WrapperValueObjectTypeComponents.EqualsOverride.If(members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.IsType<object>()));

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.EqualsMethod.If(!result.IsRecord && members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.CompareToMethod.If(members.Any(member =>
			member.Name == nameof(IComparable.CompareTo) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
		existingComponents |= WrapperValueObjectTypeComponents.EqualsOperator.If(members.Any(member =>
			member.Name == "op_Equality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
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

		existingComponents |= WrapperValueObjectTypeComponents.SerializeToUnderlying.If(members.Any(member =>
			member.Name.EndsWith($".{Constants.SerializeDomainObjectMethodName}") && member is IMethodSymbol method && method.Parameters.Length == 0 &&
			method.Arity == 0));

		existingComponents |= WrapperValueObjectTypeComponents.DeserializeFromUnderlying.If(members.Any(member =>
			member.Name.EndsWith($".{Constants.DeserializeDomainObjectMethodName}") && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default) &&
			method.Arity == 0));

		existingComponents |= WrapperValueObjectTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsType("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

		existingComponents |= WrapperValueObjectTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft.Json") == true));

		existingComponents |= WrapperValueObjectTypeComponents.StringComparison.If(members.Any(member =>
			member.Name == "StringComparison" && member.IsOverride));

		existingComponents |= WrapperValueObjectTypeComponents.FormattableToStringOverride.If(members.Any(member =>
			member.Name == nameof(IFormattable.ToString) && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>()));

		existingComponents |= WrapperValueObjectTypeComponents.ParsableTryParseMethod.If(members.Any(member =>
			member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
			method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>() && method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.ParsableParseMethod.If(members.Any(member =>
			member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.IsType<string>() && method.Parameters[1].Type.IsType<IFormatProvider>()));

		existingComponents |= WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod.If(members.Any(member =>
			member.Name == "TryFormat" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 4 &&
			method.Parameters[0].Type.IsType(typeof(Span<char>)) &&
			method.Parameters[1].Type.IsType<int>() && method.Parameters[1].RefKind == RefKind.Out &&
			method.Parameters[2].Type.IsType(typeof(ReadOnlySpan<char>)) &&
			method.Parameters[3].Type.IsType<IFormatProvider>()));

		existingComponents |= WrapperValueObjectTypeComponents.SpanParsableTryParseMethod.If(members.Any(member =>
			member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
			method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<char>)) &&
			method.Parameters[1].Type.IsType(typeof(IFormatProvider)) &&
			method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.SpanParsableParseMethod.If(members.Any(member =>
			member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<char>)) &&
			method.Parameters[1].Type.IsType(typeof(IFormatProvider))));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod.If(members.Any(member =>
			member.Name == "TryFormat" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 4 &&
			method.Parameters[0].Type.IsType(typeof(Span<byte>)) &&
			method.Parameters[1].Type.IsType<int>() && method.Parameters[1].RefKind == RefKind.Out &&
			method.Parameters[2].Type.IsType(typeof(ReadOnlySpan<char>)) &&
			method.Parameters[3].Type.IsType<IFormatProvider>()));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod.If(members.Any(member =>
			member.Name == "TryParse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 3 &&
			method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<byte>)) &&
			method.Parameters[1].Type.IsType(typeof(IFormatProvider)) &&
			method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod.If(members.Any(member =>
			member.Name == "Parse" && member is IMethodSymbol method && method.Arity == 0 && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.IsType(typeof(ReadOnlySpan<byte>)) &&
			method.Parameters[1].Type.IsType(typeof(IFormatProvider))));

		result.ExistingComponents = existingComponents;
		result.ToStringExpression = underlyingType.CreateStringExpression("Value");
		result.HashCodeExpression = underlyingType.CreateHashCodeExpression("Value", "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))");
		result.EqualityExpression = underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)");
		result.ComparisonExpression = underlyingType.CreateComparisonExpression("Value", "String.Compare(this.{0}, other.{0}, this.StringComparison)");
		result.ValueMemberLocation = members.FirstOrDefault(member => member.Name == "Value" && member is IFieldSymbol or IPropertySymbol)?.Locations.FirstOrDefault();

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Require the expected inheritance
		if (!generatable.IsPartial && !generatable.IsWrapperValueObject)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorUnexpectedInheritance", "Unexpected inheritance",
				"Type marked as wrapper value object lacks IWrapperValueObject<TValue> interface. Did you forget the 'partial' keyword and elude source generation?", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// Require ISerializableDomainObject
		if (!generatable.IsPartial && !generatable.IsSerializableDomainObject)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorMissingSerializableDomainObject", "Missing interface",
				"Type marked as wrapper value object lacks ISerializableDomainObject<TModel, TUnderlying> interface.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// No source generation, only above analyzers
		if (!generatable.IsPartial)
			return;

		// Only if class
		if (!generatable.IsClass)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorValueType", "Source-generated struct wrapper value object",
				"The type was not source-generated because it is a struct, while a class was expected. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// Only if non-record
		if (generatable.IsRecord)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorRecordType", "Source-generated record wrapper value object",
				"The type was not source-generated because it is a record, which cannot inherit from a non-record base class. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// Only if non-abstract
		if (generatable.IsAbstract)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorAbstractType", "Source-generated abstract type",
				"The type was not source-generated because it is abstract. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// Only if non-generic
		if (generatable.IsGeneric)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorGenericType", "Source-generated generic type",
				"The type was not source-generated because it is generic. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		// Only if non-nested
		if (generatable.IsNested)
		{
			context.ReportDiagnostic("WrapperValueObjectGeneratorNestedType", "Source-generated nested type",
				"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}

		var typeName = generatable.TypeName;
		var containingNamespace = generatable.ContainingNamespace;
		var underlyingTypeFullyQualifiedName = generatable.UnderlyingTypeFullyQualifiedName;
		var valueFieldName = generatable.ValueFieldName;
		var isComparable = generatable.IsComparable;
		var existingComponents = generatable.ExistingComponents;

		// Warn if Value is not settable
		if (existingComponents.HasFlag(WrapperValueObjectTypeComponents.UnsettableValue))
			context.ReportDiagnostic("WrapperValueObjectGeneratorUnsettableValue", "WrapperValueObject has Value property without init",
				"The WrapperValueObject's Value property is missing 'private init' and is using a workaround to be deserializable. To support deserialization more cleanly, use '{ get; private init; }' or let the source generator implement the property.",
				DiagnosticSeverity.Warning, generatable.ValueMemberLocation);

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
	{JsonSerializationGenerator.WriteJsonConverterAttribute(typeName)}
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
	{JsonSerializationGenerator.WriteNewtonsoftJsonConverterAttribute(typeName)}
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}

	/* Generated */ {generatable.Accessibility.ToCodeString()} sealed partial{(generatable.IsRecord ? " record" : "")} class {typeName}
		: {Constants.WrapperValueObjectTypeName}<{underlyingTypeFullyQualifiedName}>,
		IEquatable<{typeName}>,
		{(isComparable ? "" : "/*")}IComparable<{typeName}>,{(isComparable ? "" : "*/")}
#if NET7_0_OR_GREATER
		ISpanFormattable,
		ISpanParsable<{typeName}>,
#endif
#if NET8_0_OR_GREATER
		IUtf8SpanFormattable,
		IUtf8SpanParsable<{typeName}>,
#endif
		{Constants.SerializableDomainObjectInterfaceTypeName}<{typeName}, {underlyingTypeFullyQualifiedName}>
	{{
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.StringComparison) ? "/*" : "")}
		{(generatable.UnderlyingTypeIsString ? "" : @"protected sealed override StringComparison StringComparison => throw new NotSupportedException(""This operation applies to string-based value objects only."");")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.StringComparison) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "/*" : "")}
		public {underlyingTypeFullyQualifiedName} Value {{ get; private init; }}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "/*" : "")}
		public {typeName}({underlyingTypeFullyQualifiedName} value)
		{{
			this.Value = value{(generatable.UnderlyingTypeIsStruct ? "" : " ?? throw new ArgumentNullException(nameof(value))")};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DefaultConstructor) ? "/*" : "")}
#pragma warning disable CS8618 // Deserialization constructor
		[Obsolete(""This constructor exists for deserialization purposes only."")]
		private {typeName}()
		{{
		}}
#pragma warning restore CS8618
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DefaultConstructor) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "/*" : "")}
		public sealed override string{(generatable.UnderlyingTypeHasNullableToString ? "?" : "")} ToString()
		{{
			{(generatable.ToStringExpression.Contains('?') ? "// Null-safety protects instances produced by GetUninitializedObject()" : "")}
			return {generatable.ToStringExpression};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public sealed override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			{(generatable.HashCodeExpression.Contains('?') ? "// Null-safety protects instances produced by GetUninitializedObject()" : "")}
			return {generatable.HashCodeExpression};
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
				: {generatable.EqualityExpression};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsMethod) ? " */" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) ? "/*" : "")}
		{(isComparable ? "" : "/*")}
		public int CompareTo({typeName}? other)
		{{
			return other is null
				? +1
				: {generatable.ComparisonExpression};
		}}
		{(isComparable ? "" : "*/")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SerializeToUnderlying) ? "/*" : "")}
		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		{underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} {Constants.SerializableDomainObjectInterfaceTypeName}<{typeName}, {underlyingTypeFullyQualifiedName}>.Serialize()
		{{
			return this.Value;
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SerializeToUnderlying) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DeserializeFromUnderlying) ? "/*" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? $@"
#if NET8_0_OR_GREATER
		[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = ""{valueFieldName}"")]
		private static extern ref {underlyingTypeFullyQualifiedName} GetValueFieldReference({typeName} instance);
#elif NET7_0_OR_GREATER
		private static readonly System.Reflection.FieldInfo ValueFieldInfo = typeof({typeName}).GetField(""{valueFieldName}"", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
#endif" : "")}
#if NET7_0_OR_GREATER
		/// <summary>
		/// Deserializes a plain value back into a domain object, without any validation.
		/// </summary>
		static {typeName} {Constants.SerializableDomainObjectInterfaceTypeName}<{typeName}, {underlyingTypeFullyQualifiedName}>.Deserialize({underlyingTypeFullyQualifiedName} value)
		{{
			{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? $@"
			// To instead get syntax that is safe at compile time, make the Value property '{{ get; private init; }}' (or let the source generator implement it)
#if NET8_0_OR_GREATER
			var result = new {typeName}(); GetValueFieldReference(result) = value; return result;
#else
			var result = new {typeName}(); ValueFieldInfo.SetValue(result, value); return result;
#endif" : "")}
#pragma warning disable CS0618 // Obsolete constructor is intended for us
			{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? "//" : "")}return new {typeName}() {{ Value = value }};
#pragma warning restore CS0618
		}}
#endif
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DeserializeFromUnderlying) ? "*/" : "")}

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

		{(generatable.UnderlyingTypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "/*" : "")}
		{(generatable.UnderlyingTypeIsStruct ? "" : @"[return: NotNullIfNotNull(""value"")]")}
		public static explicit operator {typeName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")}({underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value) => {(generatable.UnderlyingTypeIsStruct ? "" : "value is null ? null : ")}new {typeName}(value);
		{(generatable.UnderlyingTypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "*/" : "")}

		{(generatable.UnderlyingTypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "/*" : "")}
		{(generatable.UnderlyingTypeIsStruct ? "" : @"[return: NotNullIfNotNull(""instance"")]")}
		public static implicit operator {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")}({typeName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} instance) => instance{(generatable.UnderlyingTypeIsStruct ? "" : "?")}.Value;
		{(generatable.UnderlyingTypeKind == TypeKind.Interface || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "*/" : "")}

		{(generatable.UnderlyingTypeIsNullable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "/*" : "")}
		{(generatable.UnderlyingTypeIsStruct ? @"[return: NotNullIfNotNull(""value"")]" : "")}
		{(generatable.UnderlyingTypeIsStruct ? $"public static explicit operator {typeName}?({underlyingTypeFullyQualifiedName}? value) => value is null ? null : new {typeName}(value.Value);" : "")}
		{(generatable.UnderlyingTypeIsNullable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "*/" : "")}

		{(generatable.UnderlyingTypeIsNullable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "/*" : "")}
		{(generatable.UnderlyingTypeIsStruct ? @"[return: NotNullIfNotNull(""instance"")]" : "")}
		{(generatable.UnderlyingTypeIsStruct ? $"public static implicit operator {underlyingTypeFullyQualifiedName}?({typeName}? instance) => instance?.Value;" : "")}
		{(generatable.UnderlyingTypeIsNullable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "*/" : "")}

		#region Formatting & Parsing

#if NET7_0_OR_GREATER

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.FormattableToStringOverride) ? "/*" : "")}
		public string ToString(string? format, IFormatProvider? formatProvider) =>
			FormattingHelper.ToString(this.Value, format, formatProvider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.FormattableToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(string s, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableParseMethod) ? "*/" : "")}

#endif

#if NET8_0_OR_GREATER

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(utf8Text, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(utf8Text, provider);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod) ? "*/" : "")}

#endif

		#endregion

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "/*" : "")}
		{JsonSerializationGenerator.WriteJsonConverter(typeName, underlyingTypeFullyQualifiedName, numericAsString: false)}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "/*" : "")}
		{JsonSerializationGenerator.WriteNewtonsoftJsonConverter(typeName, underlyingTypeFullyQualifiedName, isStruct: false, numericAsString: false)}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "*/" : "")}
	}}
}}
";

		AddSource(context, source, typeName, containingNamespace);
	}

	[Flags]
	internal enum WrapperValueObjectTypeComponents : ulong
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
		DefaultConstructor = 1UL << 23,
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
		public bool IsWrapperValueObject { get => this._bits.GetBit(0); set => this._bits.SetBit(0, value); }
		public bool IsSerializableDomainObject { get => this._bits.GetBit(1); set => this._bits.SetBit(1, value); }
		public bool IsPartial { get => this._bits.GetBit(2); set => this._bits.SetBit(2, value); }
		public bool IsRecord { get => this._bits.GetBit(3); set => this._bits.SetBit(3, value); }
		public bool IsClass { get => this._bits.GetBit(4); set => this._bits.SetBit(4, value); }
		public bool IsAbstract { get => this._bits.GetBit(5); set => this._bits.SetBit(5, value); }
		public bool IsGeneric { get => this._bits.GetBit(6); set => this._bits.SetBit(6, value); }
		public bool IsNested { get => this._bits.GetBit(7); set => this._bits.SetBit(7, value); }
		public bool IsComparable { get => this._bits.GetBit(8); set => this._bits.SetBit(8, value); }
		public string TypeName { get; set; } = null!;
		public string ContainingNamespace { get; set; } = null!;
		public string UnderlyingTypeFullyQualifiedName { get; set; } = null!;
		public TypeKind UnderlyingTypeKind { get; set; }
		public bool UnderlyingTypeIsStruct { get => this._bits.GetBit(9); set => this._bits.SetBit(9, value); }
		public bool UnderlyingTypeIsNullable { get => this._bits.GetBit(10); set => this._bits.SetBit(10, value); }
		public bool UnderlyingTypeIsString { get => this._bits.GetBit(11); set => this._bits.SetBit(11, value); }
		public bool UnderlyingTypeHasNullableToString { get => this._bits.GetBit(12); set => this._bits.SetBit(12, value); }
		public string ValueFieldName { get; set; } = null!;
		public Accessibility Accessibility { get; set; }
		public WrapperValueObjectTypeComponents ExistingComponents { get; set; }
		public string ToStringExpression { get; set; } = null!;
		public string HashCodeExpression { get; set; } = null!;
		public string EqualityExpression { get; set; } = null!;
		public string ComparisonExpression { get; set; } = null!;
		public SimpleLocation? TypeLocation { get; set; }
		public SimpleLocation? ValueMemberLocation { get; set; }
	}
}
