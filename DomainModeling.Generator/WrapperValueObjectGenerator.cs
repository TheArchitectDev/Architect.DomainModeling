using System.Collections.Immutable;
using Architect.DomainModeling.Generator.Common;
using Architect.DomainModeling.Generator.Configurators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

public class WrapperValueObjectGenerator : SourceGenerator
{
	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// We are invoked from another source generator
		// This lets us combine knowledge of various value wrapper kinds
	}

	/// <summary>
	/// Intializes a provider containing only the basic info of the wrapper type and underlying type.
	/// This one should not change often, making it suitable for use with Collect().
	/// </summary>
	internal void InitializeBasicProvider(IncrementalGeneratorInitializationContext context, out IncrementalValuesProvider<ValueWrapperGenerator.BasicGeneratable> provider)
	{
		provider = context.SyntaxProvider
			.CreateSyntaxProvider(
				FilterSyntaxNode,
				(context, ct) => context.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node) switch
				{
					INamedTypeSymbol type when HasRequiredAttribute(type, out var attribute) && attribute.TypeArguments[0] is ITypeSymbol underlyingType =>
						GetFirstProblem((TypeDeclarationSyntax)context.Node, type, underlyingType) is { }
							? default
							: new ValueWrapperGenerator.BasicGeneratable(
								isIdentity: false,
								containingNamespace: type.ContainingNamespace.ToString(),
								wrapperType: type,
								underlyingType: underlyingType,
								customCoreType: type.AllInterfaces.FirstOrDefault(interf => interf.IsType("ICoreValueWrapper", "Architect", "DomainModeling", arity: 2))?.TypeArguments[1]),
					_ => default,
				})
			.Where(generatable => generatable != default)
			.DeduplicatePartials()!;
	}

	/// <summary>
	/// Takes general info of all wrapper value objects, and of all nodes of all kinds of value wrappers (including wrapper value objects).
	/// Additionally gathers detailed info per individual wrapper value object.
	/// Generates source based on all of the above.
	/// </summary>
	internal void Generate(
		IncrementalGeneratorInitializationContext context,
		IncrementalValueProvider<ImmutableArray<ValueWrapperGenerator.BasicGeneratable>> valueWrappers)
	{
		var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
			.Where(generatable => generatable is not null)
			.DeduplicatePartials();

		context.RegisterSourceOutput(provider.Combine(valueWrappers), GenerateSource!);

		var aggregatedProvider = valueWrappers.Combine(EntityFrameworkConfigurationGenerator.CreateMetadataProvider(context));

		context.RegisterSourceOutput(aggregatedProvider, DomainModelConfiguratorGenerator.GenerateSourceForWrapperValueObjects);
	}

	private static bool HasRequiredAttribute(INamedTypeSymbol type, out INamedTypeSymbol attributeType)
	{
		attributeType = null!;
		if (type.GetAttribute(attr => attr.IsOrInheritsClass("WrapperValueObjectAttribute", "Architect", "DomainModeling", arity: 1, out _)) is { } attribute)
			attributeType = attribute;
		return attributeType is not null;
	}

	private static Diagnostic? GetFirstProblem(TypeDeclarationSyntax tds, INamedTypeSymbol type, ITypeSymbol underlyingType)
	{
		var isPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword);

		// Require the expected inheritance
		if (!isPartial && !type.IsOrImplementsInterface(type => type.IsType("IWrapperValueObject", "Architect", "DomainModeling", arity: 1), out _))
			return CreateDiagnostic("WrapperValueObjectGeneratorMissingInterface", "Missing IWrapperValueObject<TValue> interface",
				"Type marked as wrapper value object lacks IWrapperValueObject<TValue> interface. Did you forget the 'partial' keyword and elude source generation?", DiagnosticSeverity.Warning);

		// Require IDirectValueWrapper
		var hasDirectValueWrapperInterface = type.AllInterfaces.Any(interf =>
			interf.IsType("IDirectValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default) && interf.TypeArguments[1].Equals(underlyingType, SymbolEqualityComparer.Default));
		if (!isPartial && !hasDirectValueWrapperInterface)
			return CreateDiagnostic("WrapperValueObjectGeneratorMissingDirectValueWrapper", "Missing interface",
				$"Type marked as identity value object lacks IDirectValueWrapper<{type.Name}, {underlyingType.Name}> interface.", DiagnosticSeverity.Warning);

		// Require ICoreValueWrapper
		var hasCoreValueWrapperInterface = type.AllInterfaces.Any(interf =>
			interf.IsType("ICoreValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default));
		if (!isPartial && !hasCoreValueWrapperInterface)
			return CreateDiagnostic("WrapperValueObjectGeneratorMissingCoreValueWrapper", "Missing interface",
				$"Type marked as identity value object lacks ICoreValueWrapper<{type.Name}, {underlyingType.Name}> interface.", DiagnosticSeverity.Warning);

		if (isPartial)
		{
			// Only if non-abstract
			if (type.IsAbstract)
				return CreateDiagnostic("WrapperValueObjectGeneratorAbstractType", "Source-generated abstract type",
					"The type was not source-generated because it is abstract. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);

			// Only if non-generic
			if (type.IsGeneric())
				return CreateDiagnostic("WrapperValueObjectGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);

			// Only if non-nested
			if (type.IsNested())
				return CreateDiagnostic("WrapperValueObjectGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);
		}

		return null;

		// Local shorthand to create a diagnostic
		Diagnostic CreateDiagnostic(string id, string title, string description, DiagnosticSeverity severity)
		{
			return Diagnostic.Create(
				new DiagnosticDescriptor(id, title, description, "Design", severity, isEnabledByDefault: true),
				type.Locations.FirstOrDefault());
		}
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Struct or class or record
		if (node is TypeDeclarationSyntax tds && tds is StructDeclarationSyntax or ClassDeclarationSyntax or RecordDeclarationSyntax)
		{
			// With relevant attribute
			if (tds.HasAttributeWithInfix("Wrapper"))
				return true;
		}

		return false;
	}

	private static Generatable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol(tds);

		if (type is null)
			return null;

		// Only with the attribute
		if (!HasRequiredAttribute(type, out var attribute))
			return null;

		var underlyingType = attribute.TypeArguments[0];

		var result = new Generatable();
		result.IsWrapperValueObject = type.IsOrImplementsInterface(type => type.IsType("IWrapperValueObject", "Architect", "DomainModeling", arity: 1), out _);
		result.IsPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword);
		result.IsRecord = type.IsRecord;
		result.IsClass = type.TypeKind == TypeKind.Class;
		result.IsAbstract = type.IsAbstract;
		result.IsGeneric = type.IsGenericType;
		result.IsNested = type.IsNested();
		result.Accessibility = type.DeclaredAccessibility;

		result.TypeName = type.Name; // Will be non-generic if we pass the conditions to proceed with generation
		result.ContainingNamespace = type.ContainingNamespace.ToString();

		result.ToStringExpression = underlyingType.CreateValueToStringExpression();
		result.HashCodeExpression = underlyingType.CreateHashCodeExpression("Value", "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))");
		result.EqualityExpression = underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)");
		result.ComparisonExpression = underlyingType.CreateComparisonExpression("Value", "String.Compare(this.{0}, other.{0}, this.StringComparison)");
		result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
		result.UnderlyingTypeIsStruct = underlyingType.IsValueType;
		result.UnderlyingTypeIsNullable = underlyingType.IsNullable();
		result.UnderlyingTypeIsString = underlyingType.SpecialType == SpecialType.System_String;
		result.UnderlyingTypeIsInterface = underlyingType.TypeKind == TypeKind.Interface;

		result.ValueFieldName = type.GetMembers().FirstOrDefault(member => member is IFieldSymbol { Name: "<Value>k__BackingField" or "value" or "_value" })?.Name ?? "_value";
		// IComparable is implemented on-demand, if the type implements IComparable against itself and the underlying type is self-comparable
		// It is also implemented if the underlying type is an annotated identity
		result.IsComparable = type.AllInterfaces.Any(interf => interf.IsSystemType("IComparable", arity: 1) && interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default)) &&
			underlyingType.IsComparable(seeThroughNullable: true);
		result.IsComparable |= underlyingType.GetAttribute(attr => attr.IsOrInheritsClass("IdentityValueObjectAttribute", "Architect", "DomainModeling", arity: 1, out _)) is not null;
		result.LacksDefaultConstructor = !type.IsValueType && (!type.BasePermitsDefaultConstruction() || type.HasPrimaryConstructor());

		var members = type.GetMembers();

		var existingComponents = WrapperValueObjectTypeComponents.None;

		existingComponents |= WrapperValueObjectTypeComponents.Value.If(members.Any(member => member.Name == "Value"));

		existingComponents |= WrapperValueObjectTypeComponents.UnsettableValue.If(members.Any(member => member.Name == "Value" && member is not IFieldSymbol && member is not IPropertySymbol { SetMethod: not null }));

		existingComponents |= WrapperValueObjectTypeComponents.Constructor.If(type.InstanceConstructors.Any(ctor =>
			ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.NullableConstructor.If(underlyingType.IsValueType && type.InstanceConstructors.Any(ctor =>
			ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.IsNullableOf(underlyingType)));

		existingComponents |= WrapperValueObjectTypeComponents.DefaultConstructor.If(
			type.InstanceConstructors.Any(ctor => ctor.Parameters.Length == 0 && ctor.DeclaringSyntaxReferences.Length > 0) ||
			!type.BasePermitsDefaultConstruction() ||
			type.HasPrimaryConstructor());

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.ToStringOverride.If(members.Any(member =>
			member is IMethodSymbol { Name: nameof(ToString), IsImplicitlyDeclared: false, IsOverride: true, Parameters.Length: 0, }));

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.GetHashCodeOverride.If(members.Any(member =>
			member is IMethodSymbol { Name: nameof(GetHashCode), IsImplicitlyDeclared: false, IsOverride: true, Parameters.Length: 0, }));

		// Records irrevocably and correctly override this, checking the type and delegating to IEquatable<T>.Equals(T)
		existingComponents |= WrapperValueObjectTypeComponents.EqualsOverride.If(members.Any(member =>
			member is IMethodSymbol { Name: nameof(Equals), IsOverride: true, Parameters.Length: 1, } method &&
			method.Parameters[0].Type.SpecialType == SpecialType.System_Object));

		// Records override this, but our implementation is superior
		existingComponents |= WrapperValueObjectTypeComponents.EqualsMethod.If(members.Any(member =>
			member.HasNameOrExplicitInterfaceImplementationName(nameof(Equals)) && member is IMethodSymbol { IsImplicitlyDeclared: false, IsOverride: false, Parameters.Length: 1, } method &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.CompareToMethod.If(members.Any(member =>
			member.HasNameOrExplicitInterfaceImplementationName(nameof(IComparable.CompareTo)) && member is IMethodSymbol { Arity: 0, Parameters.Length: 1, } method &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
		existingComponents |= WrapperValueObjectTypeComponents.EqualsOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.EqualityOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
		existingComponents |= WrapperValueObjectTypeComponents.NotEqualsOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.InequalityOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.GreaterThanOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.GreaterThanOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
			method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

		existingComponents |= WrapperValueObjectTypeComponents.LessThanOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.LessThanOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
			method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

		existingComponents |= WrapperValueObjectTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.GreaterThanOrEqualOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
			method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

		existingComponents |= WrapperValueObjectTypeComponents.LessEqualsOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.LessThanOrEqualOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
			method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

		existingComponents |= WrapperValueObjectTypeComponents.ConvertToOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
			method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.ConvertFromOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
			method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default) &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.NullableConvertToOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
			method.ReturnType.IsNullableOrReferenceOf(type) &&
			method.Parameters[0].Type.IsNullableOrReferenceOf(underlyingType)));

		existingComponents |= WrapperValueObjectTypeComponents.NullableConvertFromOperator.If(members.Any(member =>
			member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
			method.ReturnType.IsNullableOrReferenceOf(underlyingType) &&
			method.Parameters[0].Type.IsNullableOrReferenceOf(type)));

		existingComponents |= WrapperValueObjectTypeComponents.SerializeToUnderlying.If(members.Any(member =>
			member.HasNameOrExplicitInterfaceImplementationName("Serialize") && member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 0, } method &&
			method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.DeserializeFromUnderlying.If(members.Any(member =>
			member.HasNameOrExplicitInterfaceImplementationName("Deserialize") && member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 1, } method &&
			method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default) &&
			method.ReturnType.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsTypeWithNamespace("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

		existingComponents |= WrapperValueObjectTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
			attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft", "Json") == true));

		existingComponents |= WrapperValueObjectTypeComponents.StringComparison.If(members.Any(member =>
			member is IPropertySymbol { Name: "StringComparison", IsImplicitlyDeclared: false, } prop));

		existingComponents |= WrapperValueObjectTypeComponents.FormattableToStringOverride.If(members.Any(member =>
			member.HasNameOrExplicitInterfaceImplementationName("ToString") && member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 2, } method &&
			method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.ParsableTryParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
			method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider") && method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.ParsableParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
			method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 4, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("TryFormat") &&
			method.Parameters[0].Type.IsSpanOfSpecialType(SpecialType.System_Char) &&
			method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 && method.Parameters[1].RefKind == RefKind.Out &&
			method.Parameters[2].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
			method.Parameters[3].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.SpanParsableTryParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
			method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
			method.Parameters[1].Type.IsSystemType("IFormatProvider") &&
			method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.SpanParsableParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
			method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
			method.Parameters[1].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 4, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("TryFormat") &&
			method.Parameters[0].Type.IsSpanOfSpecialType(SpecialType.System_Byte) &&
			method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 && method.Parameters[1].RefKind == RefKind.Out &&
			method.Parameters[2].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
			method.Parameters[3].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
			method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Byte) &&
			method.Parameters[1].Type.IsSystemType("IFormatProvider") &&
			method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

		existingComponents |= WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
			method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Byte) &&
			method.Parameters[1].Type.IsSystemType("IFormatProvider")));

		existingComponents |= WrapperValueObjectTypeComponents.CreateMethod.If(members.Any(member =>
			member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 1, } method &&
			member.HasNameOrExplicitInterfaceImplementationName("Create") &&
			method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default) &&
			method.ReturnType.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.DirectValueWrapperInterface.If(type.AllInterfaces.Any(interf =>
			interf.IsType("IDirectValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default) && interf.TypeArguments[1].Equals(underlyingType, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.CoreValueWrapperInterface.If(type.AllInterfaces.Any(interf =>
			interf.IsType("ICoreValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= WrapperValueObjectTypeComponents.WrapperBaseClass.If(type.IsOrInheritsClass("WrapperValueObject", "Architect", "DomainModeling", arity: 1, out _));

		result.ExistingComponents = existingComponents;
		result.ValueMemberLocation = members.FirstOrDefault(member => member.Name == "Value" && member is IFieldSymbol or IPropertySymbol)?.Locations.FirstOrDefault();
		result.IsToStringNullable = underlyingType.IsToStringNullable() || result.ToStringExpression.Contains('?');
		if (result.UnderlyingTypeIsString && result.ValueMemberLocation is not null) // Special-case string wrappers with a hand-written Value member
			result.IsToStringNullable = !members.Any(member =>
				member.Name == "Value" && member is IPropertySymbol { GetMethod.ReturnType: { SpecialType: SpecialType.System_String, NullableAnnotation: NullableAnnotation.NotAnnotated, } });

		result.Problem = GetFirstProblem(tds, type, underlyingType);

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, (Generatable Generatable, ImmutableArray<ValueWrapperGenerator.BasicGeneratable> ValueWrappers) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var generatable = input.Generatable;
		var valueWrappers = input.ValueWrappers;

		if (generatable.Problem is not null)
			context.ReportDiagnostic(generatable.Problem);

		if (generatable.Problem is not null || !generatable.IsPartial)
			return;

		var typeName = generatable.TypeName;
		var containingNamespace = generatable.ContainingNamespace;
		var valueFieldName = generatable.ValueFieldName;
		var isComparable = generatable.IsComparable;
		var existingComponents = generatable.ExistingComponents;

		var directParentOfCore = ValueWrapperGenerator.GetDirectParentOfCoreType(valueWrappers, generatable.TypeName, generatable.ContainingNamespace);
		var coreTypeFullyQualifiedName = directParentOfCore.CoreTypeFullyQualifiedName ?? generatable.UnderlyingTypeFullyQualifiedName;
		var coreTypeIsStruct = directParentOfCore.CoreTypeIsStruct;

		(var coreValueIsNonNull, var isSpanFormattable, var isSpanParsable, var isUtf8SpanFormattable, var isUtf8SpanParsable) = ValueWrapperGenerator.GetFormattabilityAndParsabilityRecursively(
			valueWrappers,
			typeName: generatable.TypeName, containingNamespace: generatable.ContainingNamespace);

		var underlyingTypeFullyQualifiedNameForAlias = generatable.UnderlyingTypeFullyQualifiedName;
		var coreTypeFullyQualifiedNameForAlias = coreTypeFullyQualifiedName;
		var underlyingTypeFullyQualifiedName = Char.IsUpper(underlyingTypeFullyQualifiedNameForAlias[0]) && !underlyingTypeFullyQualifiedNameForAlias.Contains('<')
			? underlyingTypeFullyQualifiedNameForAlias.Split('.').Last()
			: underlyingTypeFullyQualifiedNameForAlias;
		coreTypeFullyQualifiedName = coreTypeFullyQualifiedNameForAlias == underlyingTypeFullyQualifiedNameForAlias
			? underlyingTypeFullyQualifiedName
			: Char.IsUpper(coreTypeFullyQualifiedName[0]) && !coreTypeFullyQualifiedName.Contains('<')
				? coreTypeFullyQualifiedName.Split('.').Last()
				: coreTypeFullyQualifiedName;

		var stringComparisonProperty = (existingComponents.HasFlags(WrapperValueObjectTypeComponents.WrapperBaseClass), generatable.UnderlyingTypeIsString, existingComponents.HasFlags(WrapperValueObjectTypeComponents.StringComparison)) switch
		{
			(false, false, _) => @"", // No strings
			(false, true, false) => @"private StringComparison StringComparison => StringComparison.Ordinal;",
			(false, true, true) => @"//private StringComparison StringComparison => StringComparison.Ordinal;",
			(true, false, false) => @"protected sealed override StringComparison StringComparison => throw new NotSupportedException(""This operation applies to string-based value objects only."");",
			(true, false, true) => @"//protected sealed override StringComparison StringComparison => throw new NotSupportedException(""This operation applies to string-based value objects only."");",
			(true, true, _) => @"", // Compiler will indicate that override is required
		};

		var formattableParsableWrapperSuffix = generatable.UnderlyingTypeIsString
			? $"StringWrapper<{typeName}>"
			: $"Wrapper<{typeName}, {underlyingTypeFullyQualifiedName}>";

		// Warn if Value is not settable
		if (existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue))
			context.ReportDiagnostic("WrapperValueObjectGeneratorUnsettableValue", "WrapperValueObject has Value property without init",
				"The WrapperValueObject's Value property is missing 'private init' and is using a workaround to be deserializable. To support deserialization more cleanly, use '{ get; private init; }' or let the source generator implement the property.",
				DiagnosticSeverity.Warning, generatable.ValueMemberLocation);

		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Architect.DomainModeling;
using Architect.DomainModeling.Conversions;
{(underlyingTypeFullyQualifiedName != underlyingTypeFullyQualifiedNameForAlias ? $"using {underlyingTypeFullyQualifiedName} = {underlyingTypeFullyQualifiedNameForAlias};" : "")}
{(coreTypeFullyQualifiedName != coreTypeFullyQualifiedNameForAlias && coreTypeFullyQualifiedName != underlyingTypeFullyQualifiedName ? $"using {coreTypeFullyQualifiedName} = {coreTypeFullyQualifiedNameForAlias};" : "")}

#nullable enable

namespace {containingNamespace}
{{
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SystemTextJsonConverter) ? "//" : "")}{JsonSerializationGenerator.WriteJsonConverterAttribute(typeName, underlyingTypeFullyQualifiedName)}
	{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NewtonsoftJsonConverter) ? "//" : "")}{JsonSerializationGenerator.WriteNewtonsoftJsonConverterAttribute(typeName, underlyingTypeFullyQualifiedName)}
	[DebuggerDisplay(""{{ToString(){(coreTypeFullyQualifiedName == "string" ? "" : ",nq")}}}"")]
	[CompilerGenerated] {generatable.Accessibility.ToCodeString()} {(generatable.IsClass ? "sealed" : "readonly")} partial {(generatable.IsRecord ? "record " : "")}{(generatable.IsClass ? "class" : "struct")} {typeName} :
		IWrapperValueObject<{underlyingTypeFullyQualifiedName}>,
		IEquatable<{typeName}>,
		{(isComparable ? "" : "//")}IComparable<{typeName}>,
		{(isSpanFormattable ? "" : "//")}ISpanFormattable, ISpanFormattable{formattableParsableWrapperSuffix},
		{(isSpanParsable ? "" : "//")}ISpanParsable<{typeName}>, ISpanParsable{formattableParsableWrapperSuffix},
		{(isUtf8SpanFormattable ? "" : "//")}IUtf8SpanFormattable, IUtf8SpanFormattable{formattableParsableWrapperSuffix},
		{(isUtf8SpanParsable ? "" : "//")}IUtf8SpanParsable<{typeName}>, IUtf8SpanParsable{formattableParsableWrapperSuffix},
		IDirectValueWrapper<{typeName}, {underlyingTypeFullyQualifiedName}>,
		ICoreValueWrapper<{typeName}, {coreTypeFullyQualifiedName}>
	{{
		{stringComparisonProperty}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "/*" : "")}
		public {underlyingTypeFullyQualifiedName} Value {{ get; private init; }}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Value) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "/*" : "")}
		public {typeName}({underlyingTypeFullyQualifiedName} value)
		{{
			this.Value = value{(generatable.UnderlyingTypeIsStruct ? "" : " ?? throw new ArgumentNullException(nameof(value))")};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.Constructor) ? "*/" : "")}

		{(generatable.UnderlyingCanBeNull || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConstructor) ? "/*" : "")}
		/// <summary>
		/// Accepts a nullable parameter, but throws for null values.
		/// For example, this is useful for a mandatory request input where omission must lead to rejection.
		/// </summary>
		public {typeName}([DisallowNull] {underlyingTypeFullyQualifiedName}? value)
			: this(value ?? throw new ArgumentNullException(nameof(value)))
		{{
		}}
		{(generatable.UnderlyingCanBeNull || existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConstructor) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DefaultConstructor) ? "/*" : "")}
#pragma warning disable CS8618 // Deserialization constructor
		/// <summary>
		/// <strong>Obsolete:</strong> This constructor exists for deserialization purposes only.
		/// </summary>
		[Obsolete(""This constructor exists for deserialization purposes only."")]
		{(generatable.IsClass ? "private" : "public")} {typeName}()
		{{
		}}
#pragma warning restore CS8618
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DefaultConstructor) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "/*" : "")}
		public {(generatable.IsClass ? "sealed " : "")}override string{(generatable.IsToStringNullable ? "?" : "")} ToString()
		{{
			{(generatable.ToStringExpression.Contains('?') ? "// Null-safety protects instances produced by GetUninitializedObject()" : "")}
			return {generatable.ToStringExpression};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public {(generatable.IsClass ? "sealed " : "")} override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			{(generatable.HashCodeExpression.Contains('?') ? "// Null-safety protects instances produced by GetUninitializedObject()" : "")}
			return {generatable.HashCodeExpression};
#pragma warning restore RS1024 // Compare symbols correctly
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GetHashCodeOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOverride) ? "/*" : "")}
		public {(generatable.IsClass ? "sealed " : "")} override bool Equals(object? other)
		{{
			return other is {typeName} otherValue && this.Equals(otherValue);
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsMethod) ? "/*" : "")}
		public bool Equals({typeName}{(generatable.IsClass ? "?" : "")} other)
		{{
			return {(!generatable.IsClass ? "" : @"other is null
				? false
				: ")}{generatable.EqualityExpression};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsMethod) ? " */" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) || !isComparable ? "/*" : "")}
		public int CompareTo({typeName}{(generatable.IsClass ? "?" : "")} other)
		{{
			return {(!generatable.IsClass ? "" : @"other is null
				? +1
				: ")}{generatable.ComparisonExpression};
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CompareToMethod) || !isComparable ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.EqualsOperator) ? "//" : "")}public static bool operator ==({typeName}{(generatable.IsClass ? "?" : "")} left, {typeName}{(generatable.IsClass ? "?" : "")} right) => {(generatable.IsClass ? "left is null ? right is null : left.Equals(right)" : "left.Equals(right)")};
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NotEqualsOperator) ? "//" : "")}public static bool operator !=({typeName}{(generatable.IsClass ? "?" : "")} left, {typeName}{(generatable.IsClass ? "?" : "")} right) => !(left == right);

		{(isComparable ? "" : "/*")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterThanOperator) ? "//" : "")}public static bool operator >({typeName} left, {typeName} right) => left.CompareTo(right) > 0;
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessThanOperator) ? "//" : "")}public static bool operator <({typeName} left, {typeName} right) => left.CompareTo(right) < 0;
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.GreaterEqualsOperator) ? "//" : "")}public static bool operator >=({typeName} left, {typeName} right) => !(left < right);
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.LessEqualsOperator) ? "//" : "")}public static bool operator <=({typeName} left, {typeName} right) => !(left > right);
		{(isComparable ? "" : "*/")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { IsClass: true, UnderlyingCanBeNull: true, }
			? ""
			: $"public static explicit operator {typeName}({underlyingTypeFullyQualifiedName} value) => new {typeName}(value);")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { IsClass: true, UnderlyingCanBeNull: true, }
			? ""
			: $"public static implicit operator {underlyingTypeFullyQualifiedName}({typeName} instance) => instance.Value;")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { UnderlyingTypeIsNullable: true }
			? ""
			: @"[return: NotNullIfNotNull(nameof(value))]")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { UnderlyingTypeIsNullable: true }
			? ""
			: $@"public static explicit operator {typeName}?({underlyingTypeFullyQualifiedName}? value) => value is {{ }} actual ? new {typeName}(actual) : ({typeName}?)null;")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { UnderlyingTypeIsNullable: true }
			? ""
			: @"[return: NotNullIfNotNull(nameof(instance))]")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true } or { UnderlyingTypeIsNullable: true }
			? ""
			: $@"public static implicit operator {underlyingTypeFullyQualifiedName}?({typeName}? instance) => instance?.Value;")}

		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "/* For nested wrapper types only" : "")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertToOperator) ? "//" : "")}{(generatable.IsClass && !coreTypeIsStruct
			? ""
			: $"public static explicit operator {typeName}({coreTypeFullyQualifiedName} value) => ValueWrapperUnwrapper.Wrap<{typeName}, {coreTypeFullyQualifiedName}>(value);")}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.ConvertFromOperator) ? "//" : "")}{(generatable.IsClass && !coreTypeIsStruct
			? ""
			: $"public static implicit operator {coreTypeFullyQualifiedName}{(coreTypeIsStruct || coreValueIsNonNull ? "" : "?")}({typeName} instance) => ValueWrapperUnwrapper.Unwrap<{typeName}, {coreTypeFullyQualifiedName}>(instance){(coreValueIsNonNull ? "!" : "")};")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "//" : "")}[return: NotNullIfNotNull(nameof(value))]
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertToOperator) ? "//" : "")}public static explicit operator {typeName}?({coreTypeFullyQualifiedName}? value) => value is {{ }} actual ? ValueWrapperUnwrapper.Wrap<{typeName}, {coreTypeFullyQualifiedName}>(actual) : ({typeName}?)null;
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "//" : "")}[return: {(coreTypeIsStruct || coreValueIsNonNull ? @"NotNullIfNotNull(nameof(instance))" : @"MaybeNull")}]
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.NullableConvertFromOperator) ? "//" : "")}public static implicit operator {coreTypeFullyQualifiedName}?({typeName}? instance) => instance is {{ }} actual ? ValueWrapperUnwrapper.Unwrap<{typeName}, {coreTypeFullyQualifiedName}>(actual) : ({coreTypeFullyQualifiedName}?)null;
		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "*/" : "")}

		#region Wrapping & Serialization

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CreateMethod) ? "/*" : "")}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {typeName} IValueWrapper<{typeName}, {underlyingTypeFullyQualifiedName}>.Create({underlyingTypeFullyQualifiedName} value)
		{{
			return new {typeName}(value);
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.CreateMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SerializeToUnderlying) ? "/*" : "")}
		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: MaybeNull]
		{underlyingTypeFullyQualifiedName} IValueWrapper<{typeName}, {underlyingTypeFullyQualifiedName}>.Serialize()
		{{
			return this.Value;
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.SerializeToUnderlying) ? "*/" : "")}

		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DeserializeFromUnderlying) ? "/*" : "")}
		{(generatable.LacksDefaultConstructor || existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? $@"
		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = ""{valueFieldName}"")]
		private static extern ref {underlyingTypeFullyQualifiedName} GetValueFieldReference({typeName} instance);" : "")}

		/// <summary>
		/// Deserializes a plain value back into a domain object, without using a parameterized constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {typeName} IValueWrapper<{typeName}, {underlyingTypeFullyQualifiedName}>.Deserialize({underlyingTypeFullyQualifiedName} value)
		{{
			{(generatable.LacksDefaultConstructor ? $@"
			// To instead get syntax that is safe at compile time, use a value type or ensure that a default constructor is available
			var result = ObjectInstantiator<{typeName}>.Instantiate(); GetValueFieldReference(result) = value; return result;" : "")}
			{(!generatable.LacksDefaultConstructor && existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? $@"
			// To instead get syntax that is safe at compile time, make the Value property '{{ get; private init; }}' (or let the source generator implement it)
			var result = new {typeName}(); GetValueFieldReference(result) = value; return result;" : "")}
#pragma warning disable CS0618 // Obsolete constructor is intended for us
			{(generatable.LacksDefaultConstructor || existingComponents.HasFlags(WrapperValueObjectTypeComponents.UnsettableValue) ? "//" : "")}return new {typeName}() {{ Value = value }};
#pragma warning restore CS0618
		}}
		{(existingComponents.HasFlags(WrapperValueObjectTypeComponents.DeserializeFromUnderlying) ? "*/" : "")}

		{(generatable.ExistingComponents.HasFlags(WrapperValueObjectTypeComponents.CoreValueWrapperInterface) ? "/* Up to developer because core type was customized" : coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "/* For nested wrapper types only" : "")}
		[MaybeNull]
		{coreTypeFullyQualifiedName} IValueWrapper<{typeName}, {coreTypeFullyQualifiedName}>.Value => this.Value is {{ }} actual ? ValueWrapperUnwrapper.Unwrap<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(actual) : default;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {typeName} IValueWrapper<{typeName}, {coreTypeFullyQualifiedName}>.Create({coreTypeFullyQualifiedName} value)
		{{
			var intermediateValue = ValueWrapperUnwrapper.Wrap<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(value);
			return ValueWrapperUnwrapper.Wrap<{typeName}, {underlyingTypeFullyQualifiedName}>(intermediateValue);
		}}

		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		[return: MaybeNull]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		{coreTypeFullyQualifiedName} IValueWrapper<{typeName}, {coreTypeFullyQualifiedName}>.Serialize()
		{{
			var intermediateValue = DomainObjectSerializer.Serialize<{typeName}, {underlyingTypeFullyQualifiedName}>(this);
			return DomainObjectSerializer.Serialize<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(intermediateValue);
		}}

		/// <summary>
		/// Deserializes a plain value back into a domain object, without using a parameterized constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {typeName} IValueWrapper<{typeName}, {coreTypeFullyQualifiedName}>.Deserialize({coreTypeFullyQualifiedName} value)
		{{
			var intermediateValue = DomainObjectSerializer.Deserialize<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(value);
			return DomainObjectSerializer.Deserialize<{typeName}, {underlyingTypeFullyQualifiedName}>(intermediateValue);
		}}
		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "*/" : generatable.ExistingComponents.HasFlags(WrapperValueObjectTypeComponents.CoreValueWrapperInterface) ? "*/" : "")}

		#endregion

		#region Formatting & Parsing

//#if !NET10_0_OR_GREATER // Starting with .NET 10, these operations are provided by default implementations and extension methods

		{(!isSpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.FormattableToStringOverride) ? "/*" : "")}
		public string ToString(string? format, IFormatProvider? formatProvider) =>
			FormattingHelper.ToString(this.Value, format, formatProvider);
		{(!isSpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.FormattableToStringOverride) ? "*/" : "")}
		
		{(!isSpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);
		{(!isSpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanFormattableTryFormatMethod) ? "*/" : "")}
		
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(!isSpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableTryParseMethod) ? "*/" : "")}
		
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableTryParseMethod) ? "*/" : "")}
		
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(string s, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.ParsableParseMethod) ? "*/" : "")}
		
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(!isSpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.SpanParsableParseMethod) ? "*/" : "")}
		
		{(!isUtf8SpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);
		{(!isUtf8SpanFormattable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "*/" : "")}
		
		{(!isUtf8SpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out {typeName} result) =>
			ParsingHelper.TryParse(utf8Text, provider, out {underlyingTypeFullyQualifiedName}{(generatable.UnderlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({typeName})value) is var _
				: !((result = default) is var _);
		{(!isUtf8SpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableTryParseMethod) ? "*/" : "")}
		
		{(!isUtf8SpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod) ? "/*" : "")}
		public static {typeName} Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
			({typeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(utf8Text, provider);
		{(!isUtf8SpanParsable || existingComponents.HasFlags(WrapperValueObjectTypeComponents.Utf8SpanParsableParseMethod) ? "*/" : "")}

//#endif

		#endregion
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
		NullableConstructor = 1UL << 2,
		ToStringOverride = 1UL << 3,
		GetHashCodeOverride = 1UL << 4,
		EqualsOverride = 1UL << 5,
		EqualsMethod = 1UL << 6,
		CompareToMethod = 1UL << 7,
		EqualsOperator = 1UL << 8,
		NotEqualsOperator = 1UL << 9,
		GreaterThanOperator = 1UL << 10,
		LessThanOperator = 1UL << 11,
		GreaterEqualsOperator = 1UL << 12,
		LessEqualsOperator = 1UL << 13,
		ConvertToOperator = 1UL << 14,
		ConvertFromOperator = 1UL << 15,
		NullableConvertToOperator = 1UL << 16,
		NullableConvertFromOperator = 1UL << 17,
		NewtonsoftJsonConverter = 1UL << 18,
		SystemTextJsonConverter = 1UL << 19,
		StringComparison = 1UL << 20,
		SerializeToUnderlying = 1UL << 21,
		DeserializeFromUnderlying = 1UL << 22,
		UnsettableValue = 1UL << 23,
		DefaultConstructor = 1UL << 24,
		FormattableToStringOverride = 1UL << 25,
		ParsableTryParseMethod = 1UL << 26,
		ParsableParseMethod = 1UL << 27,
		SpanFormattableTryFormatMethod = 1UL << 28,
		SpanParsableTryParseMethod = 1UL << 29,
		SpanParsableParseMethod = 1UL << 30,
		Utf8SpanFormattableTryFormatMethod = 1UL << 31,
		Utf8SpanParsableTryParseMethod = 1UL << 32,
		Utf8SpanParsableParseMethod = 1UL << 33,
		CreateMethod = 1UL << 34,
		DirectValueWrapperInterface = 1UL << 35,
		CoreValueWrapperInterface = 1UL << 36,
		WrapperBaseClass = 1UL << 37,
	}

	private sealed record Generatable
	{
		private uint _bits;
		public bool IsWrapperValueObject { get => this._bits.GetBit(0); set => this._bits.SetBit(0, value); }
		public bool IsPartial { get => this._bits.GetBit(1); set => this._bits.SetBit(1, value); }
		public bool IsRecord { get => this._bits.GetBit(2); set => this._bits.SetBit(2, value); }
		public bool IsClass { get => this._bits.GetBit(3); set => this._bits.SetBit(3, value); }
		public bool IsAbstract { get => this._bits.GetBit(4); set => this._bits.SetBit(4, value); }
		public bool IsGeneric { get => this._bits.GetBit(5); set => this._bits.SetBit(5, value); }
		public bool IsNested { get => this._bits.GetBit(6); set => this._bits.SetBit(6, value); }
		public bool IsComparable { get => this._bits.GetBit(7); set => this._bits.SetBit(7, value); }
		public string TypeName { get; set; } = null!;
		public string ContainingNamespace { get; set; } = null!;
		public string UnderlyingTypeFullyQualifiedName { get; set; } = null!;
		public TypeKind UnderlyingTypeKind { get; set; }
		public bool UnderlyingTypeIsStruct { get => this._bits.GetBit(8); set => this._bits.SetBit(8, value); }
		public bool UnderlyingTypeIsNullable { get => this._bits.GetBit(9); set => this._bits.SetBit(9, value); }
		public bool UnderlyingTypeIsString { get => this._bits.GetBit(10); set => this._bits.SetBit(10, value); }
		public bool IsToStringNullable { get => this._bits.GetBit(11); set => this._bits.SetBit(11, value); }
		public bool UnderlyingTypeIsInterface { get => this._bits.GetBit(12); set => this._bits.SetBit(12, value); }
		public bool LacksDefaultConstructor { get => this._bits.GetBit(13); set => this._bits.SetBit(13, value); }
		public string ValueFieldName { get; set; } = null!;
		public Accessibility Accessibility { get; set; }
		public WrapperValueObjectTypeComponents ExistingComponents { get; set; }
		public string ToStringExpression { get; set; } = null!;
		public string HashCodeExpression { get; set; } = null!;
		public string EqualityExpression { get; set; } = null!;
		public string ComparisonExpression { get; set; } = null!;
		public SimpleLocation? ValueMemberLocation { get; set; }
		public Diagnostic? Problem { get; set; }

		public bool IsStruct => !this.IsClass;
		public bool UnderlyingCanBeNull => !this.UnderlyingTypeIsStruct || this.UnderlyingTypeIsNullable;
	}
}
