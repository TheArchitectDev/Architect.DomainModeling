using System.Collections.Immutable;
using Architect.DomainModeling.Generator.Configurators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

public class IdentityGenerator : SourceGenerator
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
					INamedTypeSymbol type when LooksLikeEntity(type) && IsEntity(type, out var entityInterface) && entityInterface.TypeArguments[0].TypeKind == TypeKind.Error &&
						entityInterface.TypeArguments[1] is ITypeSymbol underlyingType =>
						new ValueWrapperGenerator.BasicGeneratable(
							isIdentity: true,
							containingNamespace: type.ContainingNamespace.ToString(),
							wrapperType: entityInterface.TypeArguments[0],
							underlyingType: underlyingType,
							customCoreType: null),
					INamedTypeSymbol type when HasRequiredAttribute(type, out var attribute) && attribute.AttributeClass!.TypeArguments[0] is ITypeSymbol underlyingType =>
						GetFirstProblem((TypeDeclarationSyntax)context.Node, type, underlyingType) is { }
							? default
							: new ValueWrapperGenerator.BasicGeneratable(
								isIdentity: true,
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
	/// Takes general info of all identities, and of all nodes of all kinds of value wrappers (including identities).
	/// Additionally gathers detailed info per individual identity.
	/// Generates source based on all of the above.
	/// </summary>
	internal void Generate(
		IncrementalGeneratorInitializationContext context,
		IncrementalValueProvider<ImmutableArray<ValueWrapperGenerator.BasicGeneratable>> valueWrappers)
	{
		var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
			.Where(generatable => generatable is not null)
			.DeduplicatePartials()!;

		context.RegisterSourceOutput(provider.Combine(valueWrappers), GenerateSource!);

		var aggregatedProvider = valueWrappers.Combine(EntityFrameworkConfigurationGenerator.CreateMetadataProvider(context));

		context.RegisterSourceOutput(aggregatedProvider, DomainModelConfiguratorGenerator.GenerateSourceForIdentities);
	}

	private static bool LooksLikeEntity(INamedTypeSymbol type)
	{
		var result = type.IsOrInheritsClass(baseType => baseType.Name == "Entity", out _);
		return result;
	}

	private static bool IsEntity(INamedTypeSymbol type, out INamedTypeSymbol entityInterface)
	{
		var result = type.IsOrInheritsClass(baseType => baseType.Arity == 2 && baseType.IsType("Entity", "Architect", "DomainModeling"), out entityInterface);
		return result;
	}

	private static bool HasRequiredAttribute(INamedTypeSymbol type, out AttributeData attribute)
	{
		attribute = null!;
		if (type.GetAttribute("IdentityValueObjectAttribute", "Architect.DomainModeling", arity: 1) is AttributeData { AttributeClass: not null } attributeOutput)
			attribute = attributeOutput;
		return attribute != null;
	}

	private static Diagnostic? GetFirstProblem(TypeDeclarationSyntax tds, INamedTypeSymbol type, ITypeSymbol underlyingType)
	{
		var isPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword);

		// Require the expected inheritance
		if (!isPartial && !type.IsOrImplementsInterface(interf => interf.IsType("IIdentity", "Architect", "DomainModeling", arity: 1), out _))
			return CreateDiagnostic("IdentityGeneratorMissingInterface", "Missing IIdentity<T> interface",
				"Type marked as identity value object lacks IIdentity<T> interface. Did you forget the 'partial' keyword and elude source generation?", DiagnosticSeverity.Warning);

		// Require IDirectValueWrapper
		var hasDirectValueWrapperInterface = type.AllInterfaces.Any(interf =>
			interf.IsType("IDirectValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default) && interf.TypeArguments[1].Equals(underlyingType, SymbolEqualityComparer.Default));
		if (!isPartial && !hasDirectValueWrapperInterface)
			return CreateDiagnostic("IdentityGeneratorMissingDirectValueWrapper", "Missing interface",
				$"Type marked as identity value object lacks IDirectValueWrapper<{type.Name}, {underlyingType.Name}> interface.", DiagnosticSeverity.Warning);

		// Require ICoreValueWrapper
		var hasCoreValueWrapperInterface = type.AllInterfaces.Any(interf =>
			interf.IsType("ICoreValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
			interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default));
		if (!isPartial && !hasCoreValueWrapperInterface)
			return CreateDiagnostic("IdentityGeneratorMissingCoreValueWrapper", "Missing interface",
				$"Type marked as identity value object lacks ICoreValueWrapper<{type.Name}, {underlyingType.Name}> interface.", DiagnosticSeverity.Warning);

		// No source generation, only above analyzers
		if (isPartial)
		{
			// Only if struct
			if (type.TypeKind != TypeKind.Struct)
				return CreateDiagnostic("IdentityGeneratorReferenceType", "Source-generated reference-typed identity",
					"The type was not source-generated because it is a class, while a struct was expected. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);

			// Only if non-abstract
			if (type.IsAbstract)
				return CreateDiagnostic("IdentityGeneratorAbstractType", "Source-generated abstract type",
					"The type was not source-generated because it is abstract. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);

			// Only if non-generic
			if (type.IsGeneric())
				return CreateDiagnostic("IdentityGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);

			// Only if non-nested
			if (type.IsNested())
				return CreateDiagnostic("IdentityGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning);
		}

		return null;

		// Local shorthand to create a diagnostic
		Diagnostic CreateDiagnostic(string id, string title, string description, DiagnosticSeverity severity)
		{
			return Diagnostic.Create(
				new DiagnosticDescriptor(id, title, description, "Architect.DomainModeling", severity, isEnabledByDefault: true),
				type.Locations.FirstOrDefault());
		}
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
				if (baseType.Type.HasArityAndName(2, "Entity"))
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

		ITypeSymbol underlyingType;
		var isBasedOnEntity = LooksLikeEntity(type);

		// Path A: An Entity subclass that might be an Entity<TId, TUnderlying> for which TId may have to be generated
		if (isBasedOnEntity)
		{
			// Only an actual Entity<TId, TUnderlying>
			if (!IsEntity(type, out var entityInterface))
				return null;

			var idType = entityInterface.TypeArguments[0];
			underlyingType = entityInterface.TypeArguments[1];
			result.EntityTypeName = type.Name;

			// The ID type exists if it is not of TypeKind.Error
			result.IdTypeExists = idType.TypeKind != TypeKind.Error;

			if (result.IdTypeExists)
			{
				// Entity<TId, TUnderlying> was needlessly used, with a preexisting TId
				result.Problem = Diagnostic.Create(new DiagnosticDescriptor("EntityIdentityTypeAlreadyExists", "Entity identity type already exists", "Architect.DomainModeling",
					"Base class Entity<TId, TIdPrimitive> is intended to generate source for TId, but TId refers to an existing type. To use an existing identity type, inherit from Entity<TId> instead.",
					DiagnosticSeverity.Warning, isEnabledByDefault: true), type.Locations.FirstOrDefault());
				return result;
			}

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
			if (!HasRequiredAttribute(type, out var attribute))
				return null;

			underlyingType = attribute.AttributeClass!.TypeArguments[0];

			result.IdTypeExists = true;
			result.IsIIdentity = type.IsOrImplementsInterface(interf => interf.IsType("IIdentity", "Architect", "DomainModeling", arity: 1), out _);
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
			existingComponents |= IdTypeComponents.ToStringOverride.If(members.Any(member =>
				member is IMethodSymbol { Name: nameof(ToString), IsImplicitlyDeclared: false, IsOverride: true, Arity: 0, Parameters.Length: 0, }));

			// Records override this, but our implementation is superior
			existingComponents |= IdTypeComponents.GetHashCodeOverride.If(members.Any(member =>
				member is IMethodSymbol { Name: nameof(GetHashCode), IsImplicitlyDeclared: false, IsOverride: true, Arity: 0, Parameters.Length: 0, }));

			// Records irrevocably and correctly override this, checking the type and delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.EqualsOverride.If(members.Any(member =>
				member is IMethodSymbol { Name: nameof(Equals), IsOverride: true, Arity: 0, Parameters.Length: 1, } method &&
				method.Parameters[0].Type.SpecialType == SpecialType.System_Object));

			// Records override this, but our implementation is superior
			existingComponents |= IdTypeComponents.EqualsMethod.If(members.Any(member =>
				member.HasNameOrExplicitInterfaceImplementationName(nameof(Equals)) && member is IMethodSymbol { IsImplicitlyDeclared: false, IsOverride: false, Arity: 0, Parameters.Length: 1, } method &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.CompareToMethod.If(members.Any(member =>
				member.HasNameOrExplicitInterfaceImplementationName(nameof(IComparable.CompareTo)) && member is IMethodSymbol { Arity: 0, Parameters.Length: 1, } method &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.EqualsOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.EqualityOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			// Records irrevocably and correctly override this, delegating to IEquatable<T>.Equals(T)
			existingComponents |= IdTypeComponents.NotEqualsOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.InequalityOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.GreaterThanOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.GreaterThanOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
				method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

			existingComponents |= IdTypeComponents.LessThanOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.LessThanOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
				method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

			existingComponents |= IdTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.GreaterThanOrEqualOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
				method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

			existingComponents |= IdTypeComponents.LessEqualsOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Name: WellKnownMemberNames.LessThanOrEqualOperatorName, IsStatic: true, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.IsNullableOfOrEqualTo(type) &&
				method.Parameters[1].Type.IsNullableOfOrEqualTo(type)));

			existingComponents |= IdTypeComponents.ConvertToOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
				method.ReturnType.Equals(type, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.ConvertFromOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
				method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default) &&
				method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.NullableConvertToOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
				method.ReturnType.IsNullableOf(type) &&
				method.Parameters[0].Type.IsNullableOrReferenceOf(underlyingType)));

			existingComponents |= IdTypeComponents.NullableConvertFromOperator.If(members.Any(member =>
				member is IMethodSymbol { MethodKind: MethodKind.Conversion, IsStatic: true, Parameters.Length: 1, } method &&
				method.ReturnType.IsNullableOrReferenceOf(underlyingType) &&
				method.Parameters[0].Type.IsNullableOf(type)));

			existingComponents |= IdTypeComponents.SerializeToUnderlying.If(members.Any(member =>
				member.HasNameOrExplicitInterfaceImplementationName("Serialize") && member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 0, } method &&
				method.ReturnType.Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.DeserializeFromUnderlying.If(members.Any(member =>
				member.HasNameOrExplicitInterfaceImplementationName("Deserialize") && member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 1, } method &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default) &&
				method.ReturnType.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.SystemTextJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsTypeWithNamespace("JsonConverterAttribute", "System.Text.Json.Serialization") == true));

			existingComponents |= IdTypeComponents.NewtonsoftJsonConverter.If(type.GetAttributes().Any(attribute =>
				attribute.AttributeClass?.IsType("JsonConverterAttribute", "Newtonsoft", "Json") == true));

			existingComponents |= IdTypeComponents.StringComparison.If(members.Any(member =>
				member is IPropertySymbol { Name: "StringComparison", IsImplicitlyDeclared: false, } prop));

			existingComponents |= IdTypeComponents.FormattableToStringOverride.If(members.Any(member =>
				member.HasNameOrExplicitInterfaceImplementationName("ToString") && member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 2, } method &&
				method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.ParsableTryParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
				method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider") && method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.ParsableParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
				method.Parameters[0].Type.SpecialType == SpecialType.System_String && method.Parameters[1].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.SpanFormattableTryFormatMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 4, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("TryFormat") &&
				method.Parameters[0].Type.IsSpanOfSpecialType(SpecialType.System_Char) &&
				method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 && method.Parameters[1].RefKind == RefKind.Out &&
				method.Parameters[2].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
				method.Parameters[3].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.SpanParsableTryParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
				method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
				method.Parameters[1].Type.IsSystemType("IFormatProvider") &&
				method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.SpanParsableParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
				method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
				method.Parameters[1].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.Utf8SpanFormattableTryFormatMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: false, Parameters.Length: 4, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("TryFormat") &&
				method.Parameters[0].Type.IsSpanOfSpecialType(SpecialType.System_Byte) &&
				method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 && method.Parameters[1].RefKind == RefKind.Out &&
				method.Parameters[2].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Char) &&
				method.Parameters[3].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.Utf8SpanParsableTryParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 3, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("TryParse") &&
				method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Byte) &&
				method.Parameters[1].Type.IsSystemType("IFormatProvider") &&
				method.Parameters[2].Type.Equals(type, SymbolEqualityComparer.Default) && method.Parameters[2].RefKind == RefKind.Out));

			existingComponents |= IdTypeComponents.Utf8SpanParsableParseMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 2, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("Parse") &&
				method.Parameters[0].Type.IsReadOnlySpanOfSpecialType(SpecialType.System_Byte) &&
				method.Parameters[1].Type.IsSystemType("IFormatProvider")));

			existingComponents |= IdTypeComponents.CreateMethod.If(members.Any(member =>
				member is IMethodSymbol { Arity: 0, IsStatic: true, Parameters.Length: 1, } method &&
				member.HasNameOrExplicitInterfaceImplementationName("Create") &&
				method.Parameters[0].Type.Equals(underlyingType, SymbolEqualityComparer.Default) &&
				method.ReturnType.Equals(type, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.DirectValueWrapperInterface.If(type.AllInterfaces.Any(interf =>
				interf.IsType("IDirectValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
				interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default) && interf.TypeArguments[1].Equals(underlyingType, SymbolEqualityComparer.Default)));

			existingComponents |= IdTypeComponents.CoreValueWrapperInterface.If(type.AllInterfaces.Any(interf =>
				interf.IsType("ICoreValueWrapper", "Architect", "DomainModeling", arity: 2) && !interf.IsImplicitlyDeclared &&
				interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default)));

			result.ExistingComponents = existingComponents;

			result.Problem = GetFirstProblem(tds, type, underlyingType);
		}

		result.ToStringExpression = underlyingType.CreateValueToStringExpression();
		result.HashCodeExpression = underlyingType.CreateHashCodeExpression("Value", stringVariant: "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))");
		result.EqualityExpression = underlyingType.CreateEqualityExpression("Value", stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)");
		result.ComparisonExpression = underlyingType.CreateComparisonExpression("Value", stringVariant: "String.Compare(this.{0}, other.{0}, this.StringComparison)");
		result.UnderlyingTypeFullyQualifiedName = underlyingType.ToString();
		result.IsToStringNullable = underlyingType.IsToStringNullable() || result.ToStringExpression.Contains('?');
		result.UnderlyingTypeIsINumber = underlyingType.IsOrImplementsInterface(interf => interf.IsSystemType("INumber", "Numerics", arity: 1), out _);
		result.UnderlyingTypeIsString = underlyingType.SpecialType == SpecialType.System_String;
		result.UnderlyingTypeIsNonNullString = result.UnderlyingTypeIsString && underlyingType.NullableAnnotation != NullableAnnotation.Annotated;
		result.UnderlyingTypeIsNumericUnsuitableForJson = underlyingType.SpecialType is SpecialType.System_Decimal or SpecialType.System_UInt64 or SpecialType.System_Int64 ||
			underlyingType.IsSystemType("BigInteger", "Numerics") || underlyingType.IsSystemType("UInt128") || underlyingType.IsSystemType("Int128");
		result.UnderlyingTypeIsStruct = underlyingType.IsValueType;
		result.UnderlyingTypeIsInterface = underlyingType.TypeKind == TypeKind.Interface;

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, (Generatable Generatable, ImmutableArray<ValueWrapperGenerator.BasicGeneratable> ValueWrappers) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var generatable = input.Generatable;
		var valueWrappers = input.ValueWrappers;

		if (generatable.Problem is not null)
			context.ReportDiagnostic(generatable.Problem);

		if (generatable.Problem is not null || (!generatable.IsPartial && generatable.IdTypeExists))
			return;

		var containingNamespace = generatable.ContainingNamespace;
		var idTypeName = generatable.IdTypeName;
		var entityTypeName = generatable.EntityTypeName;
		var underlyingTypeIsStruct = generatable.UnderlyingTypeIsStruct;
		var isRecord = generatable.IsRecord;
		var isINumber = generatable.UnderlyingTypeIsINumber;
		var isString = generatable.UnderlyingTypeIsString;
		var isToStringNullable = generatable.IsToStringNullable;
		var toStringExpression = generatable.ToStringExpression;
		var hashCodeExpression = generatable.HashCodeExpression;
		var equalityExpression = generatable.EqualityExpression;
		var comparisonExpression = generatable.ComparisonExpression;

		var accessibility = generatable.Accessibility;
		var existingComponents = generatable.ExistingComponents;
		var hasIdentityValueObjectAttribute = generatable.IdTypeExists;

		var directParentOfCore = ValueWrapperGenerator.GetDirectParentOfCoreType(valueWrappers, idTypeName, containingNamespace);
		var coreTypeFullyQualifiedName = directParentOfCore.CoreTypeFullyQualifiedName ?? generatable.UnderlyingTypeFullyQualifiedName;
		var coreTypeIsStruct = directParentOfCore.CoreTypeIsStruct;

		(var coreValueIsNonNull, var isSpanFormattable, var isSpanParsable, var isUtf8SpanFormattable, var isUtf8SpanParsable) = ValueWrapperGenerator.GetFormattabilityAndParsabilityRecursively(
			valueWrappers, typeName: idTypeName, containingNamespace: containingNamespace);

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

		var formattableParsableWrapperSuffix = generatable.UnderlyingTypeIsString
			? $"StringWrapper<{idTypeName}>"
			: $"Wrapper<{idTypeName}, {underlyingTypeFullyQualifiedName}>";

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
	{summary}

	{(existingComponents.HasFlags(IdTypeComponents.SystemTextJsonConverter) ? "//" : "")}{JsonSerializationGenerator.WriteJsonConverterAttribute(idTypeName, underlyingTypeFullyQualifiedName, numericAsString: underlyingTypeIsNumericUnsuitableForJson)}
	{(existingComponents.HasFlags(IdTypeComponents.NewtonsoftJsonConverter) ? "//" : "")}{JsonSerializationGenerator.WriteNewtonsoftJsonConverterAttribute(idTypeName, underlyingTypeFullyQualifiedName, numericAsString: underlyingTypeIsNumericUnsuitableForJson)}
	{(hasIdentityValueObjectAttribute ? "" : $"[IdentityValueObject<{underlyingTypeFullyQualifiedName}>]")}
	[DebuggerDisplay(""{{ToString(){(coreTypeFullyQualifiedName == "string" ? "" : ",nq")}}}"")]
	[CompilerGenerated] {accessibility.ToCodeString()} readonly{(entityTypeName is null ? " partial" : "")}{(isRecord ? " record" : "")} struct {idTypeName} :
		IIdentity<{underlyingTypeFullyQualifiedName}>,
		IEquatable<{idTypeName}>,
		IComparable<{idTypeName}>,
		{(isSpanFormattable ? "" : "//")}ISpanFormattable, ISpanFormattable{formattableParsableWrapperSuffix},
		{(isSpanParsable ? "" : "//")}ISpanParsable<{idTypeName}>, ISpanParsable{formattableParsableWrapperSuffix},
		{(isUtf8SpanFormattable ? "" : "//")}IUtf8SpanFormattable, IUtf8SpanFormattable{formattableParsableWrapperSuffix},
		{(isUtf8SpanParsable ? "" : "//")}IUtf8SpanParsable<{idTypeName}>, IUtf8SpanParsable{formattableParsableWrapperSuffix},
		IDirectValueWrapper<{idTypeName}, {underlyingTypeFullyQualifiedName}>,
		ICoreValueWrapper<{idTypeName}, {coreTypeFullyQualifiedName}>
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

		{(existingComponents.HasFlags(IdTypeComponents.EqualsOperator) ? "//" : "")}public static bool operator ==({idTypeName} left, {idTypeName} right) => left.Equals(right);
		{(existingComponents.HasFlags(IdTypeComponents.NotEqualsOperator) ? "//" : "")}public static bool operator !=({idTypeName} left, {idTypeName} right) => !(left == right);

		{(existingComponents.HasFlags(IdTypeComponents.GreaterThanOperator) ? "//" : "")}public static bool operator >({idTypeName} left, {idTypeName} right) => left.CompareTo(right) > 0;
		{(existingComponents.HasFlags(IdTypeComponents.LessThanOperator) ? "//" : "")}public static bool operator <({idTypeName} left, {idTypeName} right) => left.CompareTo(right) < 0;
		{(existingComponents.HasFlags(IdTypeComponents.GreaterEqualsOperator) ? "//" : "")}public static bool operator >=({idTypeName} left, {idTypeName} right) => !(left < right);
		{(existingComponents.HasFlags(IdTypeComponents.LessEqualsOperator) ? "//" : "")}public static bool operator <=({idTypeName} left, {idTypeName} right) => !(left > right);

		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: $@"public static implicit operator {idTypeName}({underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value) => new {idTypeName}(value);")}
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: $@"public static implicit operator {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct || isNonNullString ? "" : "?")}({idTypeName} id) => id.Value;")}

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: @"[return: NotNullIfNotNull(nameof(value))]")}
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: $@"public static implicit operator {idTypeName}?({underlyingTypeFullyQualifiedName}? value) => value is {{ }} actual ? new {idTypeName}(actual) : ({idTypeName}?)null;")}
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: underlyingTypeIsStruct || isNonNullString ? @"[return: NotNullIfNotNull(nameof(id))]" : "[return: MaybeNull]")}
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "//" : "")}{(generatable is { UnderlyingTypeIsInterface: true }
			? ""
			: $@"public static implicit operator {underlyingTypeFullyQualifiedName}?({idTypeName}? id) => id?.Value;")}

		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "/* For nested wrapper types only" : "")}
		{(existingComponents.HasFlags(IdTypeComponents.ConvertToOperator) ? "//" : "")}public static implicit operator {idTypeName}({coreTypeFullyQualifiedName} value) => ValueWrapperUnwrapper.Wrap<{idTypeName}, {coreTypeFullyQualifiedName}>(value);
		{(existingComponents.HasFlags(IdTypeComponents.ConvertFromOperator) ? "//" : "")}public static implicit operator {coreTypeFullyQualifiedName}{(coreTypeIsStruct || coreValueIsNonNull ? "" : "?")}({idTypeName} id) => ValueWrapperUnwrapper.Unwrap<{idTypeName}, {coreTypeFullyQualifiedName}>(id){(coreValueIsNonNull ? "!" : "")};

		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "//" : "")}[return: NotNullIfNotNull(nameof(value))]
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertToOperator) ? "//" : "")}public static implicit operator {idTypeName}?({coreTypeFullyQualifiedName}? value) => value is {{ }} actual ? ValueWrapperUnwrapper.Wrap<{idTypeName}, {coreTypeFullyQualifiedName}>(actual) : ({idTypeName}?)null;
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "//" : "")}[return: {(coreTypeIsStruct || coreValueIsNonNull ? @"NotNullIfNotNull(nameof(id))" : @"MaybeNull")}]
		{(existingComponents.HasFlags(IdTypeComponents.NullableConvertFromOperator) ? "//" : "")}public static implicit operator {coreTypeFullyQualifiedName}?({idTypeName}? id) => id is {{ }} actual ? ValueWrapperUnwrapper.Unwrap<{idTypeName}, {coreTypeFullyQualifiedName}>(actual) : ({coreTypeFullyQualifiedName}?)null;
		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "*/" : "")}

		#region Wrapping & Serialization

		{(existingComponents.HasFlags(IdTypeComponents.CreateMethod) ? "/*" : "")}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {idTypeName} IValueWrapper<{idTypeName}, {underlyingTypeFullyQualifiedName}>.Create({underlyingTypeFullyQualifiedName} value)
		{{
			return new {idTypeName}(value);
		}}
		{(existingComponents.HasFlags(IdTypeComponents.CreateMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.SerializeToUnderlying) ? "/*" : "")}
		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		{underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct || isNonNullString ? "" : "?")} IValueWrapper<{idTypeName}, {underlyingTypeFullyQualifiedName}>.Serialize()
		{{
			return this.Value;
		}}
		{(existingComponents.HasFlags(IdTypeComponents.SerializeToUnderlying) ? "*/" : "")}

		{(existingComponents.HasFlags(IdTypeComponents.DeserializeFromUnderlying) ? "/*" : "")}
		/// <summary>
		/// Deserializes a plain value back into a domain object, without using a parameterized constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {idTypeName} IValueWrapper<{idTypeName}, {underlyingTypeFullyQualifiedName}>.Deserialize({underlyingTypeFullyQualifiedName} value)
		{{
			{(existingComponents.HasFlags(IdTypeComponents.UnsettableValue) ? "// To instead get safe syntax, make the Value property '{ get; private init; }' (or let the source generator implement it)" : "")}
			{(existingComponents.HasFlags(IdTypeComponents.UnsettableValue) ? $"return Unsafe.As<{underlyingTypeFullyQualifiedName}, {idTypeName}>(ref value);" : "")}
			{(existingComponents.HasFlags(IdTypeComponents.UnsettableValue) ? "//" : "")}return new {idTypeName}() {{ Value = value }};
		}}
		{(existingComponents.HasFlags(IdTypeComponents.DeserializeFromUnderlying) ? "*/" : "")}

		{(generatable.ExistingComponents.HasFlags(IdTypeComponents.CoreValueWrapperInterface) ? "/* Up to developer because core type was customized" : coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "/* For nested wrapper types only" : "")}
		[MaybeNull]
		{coreTypeFullyQualifiedName} IValueWrapper<{idTypeName}, {coreTypeFullyQualifiedName}>.Value => this.Value is {{ }} actual ? ValueWrapperUnwrapper.Unwrap<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(actual) : default;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {idTypeName} IValueWrapper<{idTypeName}, {coreTypeFullyQualifiedName}>.Create({coreTypeFullyQualifiedName} value)
		{{
			var intermediateValue = ValueWrapperUnwrapper.Wrap<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(value);
			return ValueWrapperUnwrapper.Wrap<{idTypeName}, {underlyingTypeFullyQualifiedName}>(intermediateValue);
		}}

		/// <summary>
		/// Serializes a domain object as a plain value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: MaybeNull]
		{coreTypeFullyQualifiedName} IValueWrapper<{idTypeName}, {coreTypeFullyQualifiedName}>.Serialize()
		{{
			var intermediateValue = DomainObjectSerializer.Serialize<{idTypeName}, {underlyingTypeFullyQualifiedName}>(this);
			return DomainObjectSerializer.Serialize<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(intermediateValue);
		}}

		/// <summary>
		/// Deserializes a plain value back into a domain object, without using a parameterized constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static {idTypeName} IValueWrapper<{idTypeName}, {coreTypeFullyQualifiedName}>.Deserialize({coreTypeFullyQualifiedName} value)
		{{
			var intermediateValue = DomainObjectSerializer.Deserialize<{underlyingTypeFullyQualifiedName}, {coreTypeFullyQualifiedName}>(value);
			return DomainObjectSerializer.Deserialize<{idTypeName}, {underlyingTypeFullyQualifiedName}>(intermediateValue);
		}}
		{(coreTypeFullyQualifiedName == underlyingTypeFullyQualifiedName ? "*/" : generatable.ExistingComponents.HasFlags(IdTypeComponents.CoreValueWrapperInterface) ? "*/" : "")}

		#endregion

		#region Formatting & Parsing

//#if !NET10_0_OR_GREATER // Starting with .NET 10, these operations are provided by default implementations and extension methods

		{(!isSpanFormattable || existingComponents.HasFlags(IdTypeComponents.FormattableToStringOverride) ? "/*" : "")}
		public string ToString(string? format, IFormatProvider? formatProvider) =>
			FormattingHelper.ToString(this.Value, format, formatProvider);
		{(!isSpanFormattable || existingComponents.HasFlags(IdTypeComponents.FormattableToStringOverride) ? "*/" : "")}

		{(!isSpanFormattable || existingComponents.HasFlags(IdTypeComponents.SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);
		{(!isSpanFormattable || existingComponents.HasFlags(IdTypeComponents.SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.ParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.ParsableTryParseMethod) ? "*/" : "")}

		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(s, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.SpanParsableTryParseMethod) ? "*/" : "")}

		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.ParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(string s, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.ParsableParseMethod) ? "*/" : "")}

		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.SpanParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(s, provider);
		{(!isSpanParsable || existingComponents.HasFlags(IdTypeComponents.SpanParsableParseMethod) ? "*/" : "")}

		{(!isUtf8SpanFormattable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "/*" : "")}
		public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
			FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);
		{(!isUtf8SpanFormattable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanFormattableTryFormatMethod) ? "*/" : "")}

		{(!isUtf8SpanParsable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableTryParseMethod) ? "/*" : "")}
		public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out {idTypeName} result) =>
			ParsingHelper.TryParse(utf8Text, provider, out {underlyingTypeFullyQualifiedName}{(underlyingTypeIsStruct ? "" : "?")} value)
				? (result = ({idTypeName})value) is var _
				: !((result = default) is var _);
		{(!isUtf8SpanParsable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableTryParseMethod) ? "*/" : "")}

		{(!isUtf8SpanParsable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableParseMethod) ? "/*" : "")}
		public static {idTypeName} Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
			({idTypeName})ParsingHelper.Parse<{underlyingTypeFullyQualifiedName}>(utf8Text, provider);
		{(!isUtf8SpanParsable || existingComponents.HasFlags(IdTypeComponents.Utf8SpanParsableParseMethod) ? "*/" : "")}

//#endif

		#endregion
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
		CreateMethod = 1UL << 33,
		DirectValueWrapperInterface = 1UL << 34,
		CoreValueWrapperInterface = 1UL << 35,
	}

	private sealed record Generatable
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
		public string ToStringExpression { get; set; } = null!;
		public string HashCodeExpression { get; set; } = null!;
		public string EqualityExpression { get; set; } = null!;
		public string ComparisonExpression { get; set; } = null!;
		public string UnderlyingTypeFullyQualifiedName { get; set; } = null!;
		public bool IsToStringNullable { get => this._bits.GetBit(8); set => this._bits.SetBit(8, value); }
		public bool UnderlyingTypeIsINumber { get => this._bits.GetBit(9); set => this._bits.SetBit(9, value); }
		public bool UnderlyingTypeIsString { get => this._bits.GetBit(10); set => this._bits.SetBit(10, value); }
		public bool UnderlyingTypeIsNonNullString { get => this._bits.GetBit(11); set => this._bits.SetBit(11, value); }
		public bool UnderlyingTypeIsNumericUnsuitableForJson { get => this._bits.GetBit(12); set => this._bits.SetBit(12, value); }
		public bool UnderlyingTypeIsStruct { get => this._bits.GetBit(13); set => this._bits.SetBit(13, value); }
		public bool UnderlyingTypeIsInterface { get => this._bits.GetBit(14); set => this._bits.SetBit(14, value); }
		public Accessibility Accessibility { get; set; }
		public IdTypeComponents ExistingComponents { get; set; }
		public Diagnostic? Problem { get; set; }
	}
}
