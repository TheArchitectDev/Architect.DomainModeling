using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

[Generator]
public class ValueObjectGenerator : SourceGenerator
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
		// Partial subclass with any inherited/implemented types
		if (node is not ClassDeclarationSyntax cds || !cds.Modifiers.Any(SyntaxKind.PartialKeyword) || cds.BaseList is null)
			return false;

		foreach (var baseType in cds.BaseList.Types)
		{
			// Consider any type with SOME non-generic "ValueObject" inheritance/implementation
			if (baseType.Type.HasArityAndName(0, Constants.ValueObjectTypeName))
				return true;
		}

		/* Supporting records has the following issues:
		 * - Cannot inherit from a non-record class (and vice versa).
		 * - Promotes the use of "positional" (automatic) properties. This generates a constructor and init-properties, stimulating non-validated ValueObjects, an antipattern.
		 * - Provides multiple nearly-identical solutions, reducing standardization.
		// Partial record with some interface/base
		if (node is RecordDeclarationSyntax rds && rds.Modifiers.Any(SyntaxKind.PartialKeyword) && rds.BaseList is not null)
		{
			foreach (var baseType in rds.BaseList.Types)
			{
				// Consider any type with SOME non-generic "IValueObject" inheritance/implementation
				if (baseType.Type.HasArityAndName(0, Constants.ValueObjectInterfaceTypeName))
				{
					this.CandidateValueObjectTypes.Add(rds);
					return;
				}
			}
		}*/

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

		// Only with the attribute
		if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
			return null;

		result.SetAssociatedData(type);
		result.IsClass = tds is ClassDeclarationSyntax;
		result.IsRecord = tds is RecordDeclarationSyntax;
		result.IsValueObject = type.BaseType?.IsType(Constants.ValueObjectTypeName, Constants.DomainModelingNamespace) == true;
		result.IsIValueObject = !type.AllInterfaces.Any(interf => interf.IsType(Constants.ValueObjectInterfaceTypeName, Constants.DomainModelingNamespace));
		result.IsAbstract = type.IsAbstract;
		result.IsGeneric = type.IsGenericType;
		result.IsNested = type.IsNested();

		result.TypeName = type.Name; // Will be non-generic if we pass the conditions to proceed with generation
		result.ContainingNamespace = type.ContainingNamespace.ToString();

		var members = type.GetMembers();

		var existingComponents = ValueObjectTypeComponents.None;

		existingComponents |= ValueObjectTypeComponents.ToStringOverride.If(members.Any(member =>
			member.Name == nameof(ToString) && member is IMethodSymbol method && method.Parameters.Length == 0));

		existingComponents |= ValueObjectTypeComponents.GetHashCodeOverride.If(members.Any(member =>
			member.Name == nameof(GetHashCode) && member is IMethodSymbol method && method.Parameters.Length == 0));

		existingComponents |= ValueObjectTypeComponents.EqualsOverride.If(members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.IsType<object>()));

		existingComponents |= ValueObjectTypeComponents.EqualsMethod.If(members.Any(member =>
			member.Name == nameof(Equals) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.CompareToMethod.If(members.Any(member =>
			member.Name == nameof(IComparable.CompareTo) && member is IMethodSymbol method && method.Parameters.Length == 1 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.EqualsOperator.If(members.Any(member =>
			member.Name == "op_Equality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.NotEqualsOperator.If(members.Any(member =>
			member.Name == "op_Inequality" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.GreaterThanOperator.If(members.Any(member =>
			member.Name == "op_GreaterThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.LessThanOperator.If(members.Any(member =>
			member.Name == "op_LessThan" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.GreaterEqualsOperator.If(members.Any(member =>
			member.Name == "op_GreaterThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.LessEqualsOperator.If(members.Any(member =>
			member.Name == "op_LessThanOrEqual" && member is IMethodSymbol method && method.Parameters.Length == 2 &&
			method.Parameters[0].Type.Equals(type, SymbolEqualityComparer.Default) &&
			method.Parameters[1].Type.Equals(type, SymbolEqualityComparer.Default)));

		existingComponents |= ValueObjectTypeComponents.StringComparison.If(members.Any(member =>
			member.Name == "StringComparison" && member.IsOverride));

		result.ExistingComponents = existingComponents;

		var dataMembers = GetFieldsAndPropertiesWithBackingField(type, out _, out _);

		// We need a fast to way to check whether the member names or types have changed
		// Technically the solution here is incomplete, since we do not detect changes to the interfaces implemented by the members' types, even though these could affect the generated source
		// In order to get good performance, we accept that a rebuild is required for this niche scenario
		var dataMemberHashCode = "".GetStableHashCode64();
		foreach (var tuple in dataMembers)
		{
			dataMemberHashCode = tuple.Member.Name.GetStableHashCode64(dataMemberHashCode);
			dataMemberHashCode = ":".GetStableHashCode64(dataMemberHashCode);
			dataMemberHashCode = (tuple.Type.ContainingNamespace?.ToString() ?? "").GetStableHashCode64(dataMemberHashCode); // Arrays have no namespace
			dataMemberHashCode = ".".GetStableHashCode64(dataMemberHashCode);
			dataMemberHashCode = tuple.Type.Name.GetStableHashCode64(dataMemberHashCode);
			dataMemberHashCode = "&".GetStableHashCode64(dataMemberHashCode);
		}
		result.DataMemberHashCode = dataMemberHashCode;

		// IComparable is implemented on-demand, if the type implements IComparable against itself and all data members are self-comparable
		result.IsComparable = type.AllInterfaces.Any(interf => interf.IsType("IComparable", "System", generic: true) && interf.TypeArguments[0].Equals(type, SymbolEqualityComparer.Default));
		result.IsComparable = result.IsComparable && dataMembers.All(tuple => tuple.Type.IsComparable(seeThroughNullable: true));

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var type = generatable.GetAssociatedData<INamedTypeSymbol>();

		// Only with the intended inheritance
		if (generatable.IsClass && !generatable.IsValueObject)
		{
			context.ReportDiagnostic("ValueObjectGeneratorUnexpectedInheritance", "Unexpected base class",
				"The type marked as source-generated has an unexpected base class. Did you mean ValueObject?", DiagnosticSeverity.Warning, type);
			return;
		}
		if (generatable.IsRecord && !generatable.IsIValueObject)
		{
			context.ReportDiagnostic("ValueObjectGeneratorUnexpectedInheritance", "Missing interface",
				"The type marked as source-generated has an unexpected base class or interface. Did you mean IValueObject?", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-abstract
		if (generatable.IsAbstract)
		{
			context.ReportDiagnostic("ValueObjectGeneratorAbstractType", "Source-generated abstract type",
				"The type was not source-generated because it is abstract.", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-generic
		if (generatable.IsGeneric)
		{
			context.ReportDiagnostic("ValueObjectGeneratorGenericType", "Source-generated generic type",
				"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, type);
			return;
		}
		// Only if non-nested
		if (generatable.IsNested)
		{
			context.ReportDiagnostic("ValueObjectGeneratorNestedType", "Source-generated nested type",
				"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, type);
			return;
		}

		var isRecord = generatable.IsRecord;

		var typeName = type.Name; // Non-generic
		var containingNamespace = type.ContainingNamespace.ToString();
		var isComparable = generatable.IsComparable;
		var existingComponents = generatable.ExistingComponents;

		var dataMembers = GetFieldsAndPropertiesWithBackingField(type, out _, out _);

		var toStringExpressions = dataMembers
			.Select(tuple => $"{tuple.Member.Name}={{this.{tuple.Member.Name}}}")
			.ToList();
		var toStringBody = $@"return $""{{{{{{nameof({typeName})}} {String.Join(" ", toStringExpressions)}}}}}"";";
		if (dataMembers.Count == 0) toStringBody = $@"return $""{{{{{typeName}}}}}"";";

		var getHashCodeExpressions = dataMembers
			.Select(tuple => (Type: tuple.Type, MemberName: tuple.Member.Name, Expression: tuple.Type.CreateHashCodeExpression(tuple.Member.Name, stringVariant: "(this.{0} is null ? 0 : String.GetHashCode(this.{0}, this.StringComparison))")))
			.Select(tuple => tuple.Expression == $"this.{tuple.MemberName}.GetHashCode()" || tuple.Expression == $"(this.{tuple.MemberName}?.GetHashCode() ?? 0)"
				? $"this.{tuple.MemberName}" // Simplify, since HashCode.Add() will call GetHashCode() for us
				: tuple.Expression)
			.Select(expression => $"hashCode.Add({expression});")
			.ToList();
		var getHashCodeBody = $"var hashCode = new HashCode();{Environment.NewLine}			{String.Join($"{Environment.NewLine}			", getHashCodeExpressions)}{Environment.NewLine}			return hashCode.ToHashCode();";
		if (dataMembers.Count == 0) getHashCodeBody = $"return typeof({typeName}).GetHashCode();";

		var equalityExpressions = dataMembers.Select(tuple => tuple.Type.CreateEqualityExpression(tuple.Member.Name, stringVariant: "String.Equals(this.{0}, other.{0}, this.StringComparison)")).ToList();
		var equalsBodyIfInstanceNonNull = $"return{Environment.NewLine}				{String.Join($" &&{Environment.NewLine}				", equalityExpressions)}";
		if (dataMembers.Count == 0) equalsBodyIfInstanceNonNull = "return true;";

		var comparisonExpressions = dataMembers
			.Select(tuple => $"comparison = {tuple.Type.CreateComparisonExpression(tuple.Member.Name, stringVariant: "String.Compare(this.{0}, other.{0}, this.StringComparison)")};").ToList();
		var compareToBodyIfInstanceNonNull = $"int comparison;{Environment.NewLine}			{String.Join($"{Environment.NewLine}			if (comparison != 0) return comparison;{Environment.NewLine}			", comparisonExpressions)}{Environment.NewLine}			return comparison;";
		if (dataMembers.Count == 0) compareToBodyIfInstanceNonNull = "return 0;";

		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {Constants.DomainModelingNamespace};

#nullable enable

namespace {containingNamespace}
{{
	/* Generated */ {type.DeclaredAccessibility.ToCodeString()} sealed partial {(isRecord ? "record" : "class")} {typeName} : IEquatable<{typeName}>{(isComparable ? "" : "/*")}, IComparable<{typeName}>{(isComparable ? "" : "*/")}
	{{
		{(isRecord || existingComponents.HasFlags(ValueObjectTypeComponents.StringComparison) ? "/*" : "")}
		{(dataMembers.Any(member => member.Type.IsType<string>())
	? @"protected sealed override StringComparison StringComparison => StringComparison.Ordinal;"
	: @"protected sealed override StringComparison StringComparison => throw new NotSupportedException(""This operation applies to string-based value objects only."");")}
		{(isRecord || existingComponents.HasFlags(ValueObjectTypeComponents.StringComparison) ? "*/" : "")}

		{(!isRecord && existingComponents.HasFlags(ValueObjectTypeComponents.ToStringOverride) ? "/*" : "")}
		public sealed override string ToString()
		{{
			{toStringBody}
		}}
		{(!isRecord && existingComponents.HasFlags(ValueObjectTypeComponents.ToStringOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(ValueObjectTypeComponents.GetHashCodeOverride) ? "/*" : "")}
		public sealed override int GetHashCode()
		{{
#pragma warning disable RS1024 // Compare symbols correctly
			{getHashCodeBody}
#pragma warning restore RS1024 // Compare symbols correctly
		}}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.GetHashCodeOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsOverride) ? "/*" : "")}
		public sealed override bool Equals(object? other)
		{{
			return other is {typeName} otherValue && this.Equals(otherValue);
		}}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsOverride) ? "*/" : "")}

		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsMethod) ? "/*" : "")}
		public bool Equals({typeName}? other)
		{{
			if (other is null) return false;

			{equalsBodyIfInstanceNonNull};
		}}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsMethod) ? " */" : "")}

		/// <summary>
		/// Provides type inference when comparing types that are entirely source-generated. The current code's source generator does not know the appropriate namespace, because the type is being generated at the same time, thus necessitating type inference.
		/// </summary>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool Equals<T>(T left, T right)
		{{
			return EqualityComparer<T>.Default.Equals(left, right);
		}}

		{(existingComponents.HasFlags(ValueObjectTypeComponents.CompareToMethod) ? "/*" : "")}
		{(isComparable ? "" : "/*")}
		// This method is generated only if the ValueObject implements IComparable<T> against its own type and each data member implements IComparable<T> against its own type
		public int CompareTo({typeName}? other)
		{{
			if (other is null) return +1;

			{compareToBodyIfInstanceNonNull}
		}}

		/// <summary>
		/// Provides type inference when comparing types that are entirely source-generated. The current code's source generator does not know the appropriate namespace, because the type is being generated at the same time, thus necessitating type inference.
		/// </summary>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int Compare<T>(T left, T right)
		{{
			return Comparer<T>.Default.Compare(left, right);
		}}
		{(isComparable ? "" : "*/")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.CompareToMethod) ? "*/" : "")}

		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsOperator) ? "/*" : "")}
		public static bool operator ==({typeName}? left, {typeName}? right) => left is null ? right is null : left.Equals(right);
		{(existingComponents.HasFlags(ValueObjectTypeComponents.EqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.NotEqualsOperator) ? "/*" : "")}
		public static bool operator !=({typeName}? left, {typeName}? right) => !(left == right);
		{(existingComponents.HasFlags(ValueObjectTypeComponents.NotEqualsOperator) ? "*/" : "")}

		{(isComparable ? "" : "/*")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.GreaterThanOperator) ? "/*" : "")}
		public static bool operator >({typeName}? left, {typeName}? right) => left is null ? false : left.CompareTo(right) > 0;
		{(existingComponents.HasFlags(ValueObjectTypeComponents.GreaterThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.LessThanOperator) ? "/*" : "")}
		public static bool operator <({typeName}? left, {typeName}? right) => left is null ? right is not null : left.CompareTo(right) < 0;
		{(existingComponents.HasFlags(ValueObjectTypeComponents.LessThanOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.GreaterEqualsOperator) ? "/*" : "")}
		public static bool operator >=({typeName}? left, {typeName}? right) => !(left < right);
		{(existingComponents.HasFlags(ValueObjectTypeComponents.GreaterEqualsOperator) ? "*/" : "")}
		{(existingComponents.HasFlags(ValueObjectTypeComponents.LessEqualsOperator) ? "/*" : "")}
		public static bool operator <=({typeName}? left, {typeName}? right) => !(left > right);
		{(existingComponents.HasFlags(ValueObjectTypeComponents.LessEqualsOperator) ? "*/" : "")}
		{(isComparable ? "" : "*/")}
	}}
}}
";

		AddSource(context, source, typeName, containingNamespace);
	}

	private static IReadOnlyList<(ISymbol Member, ITypeSymbol Type)> GetFieldsAndPropertiesWithBackingField(INamedTypeSymbol type,
		out IReadOnlyList<IFieldSymbol> explicitFields, out IReadOnlyList<IPropertySymbol> propertiesWithBackingField)
	{
		var members = type.GetMembers();

		explicitFields = members.OfType<IFieldSymbol>().Where(field => !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared).ToList();

		var backingFields = members.OfType<IFieldSymbol>().Where(field => !field.IsStatic && !field.IsConst && field.IsImplicitlyDeclared && field.Name.EndsWith(">k__BackingField")).ToList();
		var properties = members.OfType<IPropertySymbol>().Where(property => !property.IsStatic && !property.IsIndexer && !property.IsWriteOnly).ToList();
		propertiesWithBackingField = properties
			.Select(property => (Property: property, BackingFieldName: $"<{property.Name}>k__BackingField"))
			.Where(pair => backingFields.Any(field => field.Name == pair.BackingFieldName)) // Quadratic, but there should not be many elements, and these types warn on ToDictionary()
			.Select(pair => pair.Property)
			.ToList();

		var result = explicitFields
			.Cast<ISymbol>()
			.Concat(propertiesWithBackingField)
			.Select(member => (Member: member, Type: (member as IFieldSymbol)?.Type ?? ((IPropertySymbol)member).Type))
			.ToList();

		return result;
	}

	[Flags]
	private enum ValueObjectTypeComponents : ulong
	{
		None = 0,

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
		StringComparison = 1 << 13,
	}

	private sealed record Generatable : IGeneratable
	{
		public bool IsClass { get; set; }
		public bool IsRecord { get; set; }
		public bool IsValueObject { get; set; }
		public bool IsIValueObject { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsGeneric { get; set; }
		public bool IsNested { get; set; }
		public bool IsComparable { get; set; }
		public string TypeName { get; set; } = null!;
		public string ContainingNamespace { get; set; } = null!;
		public ValueObjectTypeComponents ExistingComponents { get; set; }
		public ulong DataMemberHashCode { get; set; }
	}
}
