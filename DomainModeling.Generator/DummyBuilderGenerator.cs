using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

[Generator]
public class DummyBuilderGenerator : SourceGenerator
{
	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
			.Where(builder => builder is not null)
			.DeduplicatePartials()
			.Collect();

		context.RegisterSourceOutput(provider.Combine(context.CompilationProvider), GenerateSource!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Struct or class or record
		if (node is TypeDeclarationSyntax tds && tds is StructDeclarationSyntax or ClassDeclarationSyntax or RecordDeclarationSyntax)
		{
			// With relevant attribute
			if (tds.HasAttributeWithPrefix("DummyBuilder"))
				return true;
		}

		return false;
	}

	private static Builder? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node);

		if (type is null)
			return null;

		// Only with the attribute
		if (type.GetAttribute("DummyBuilderAttribute", Constants.DomainModelingNamespace, arity: 1) is not AttributeData { AttributeClass: not null } attribute)
			return null;

		var modelType = attribute.AttributeClass.TypeArguments[0];

		var result = new Builder()
		{
			TypeFullyQualifiedName = type.ToString(),
			ModelTypeFullyQualifiedName = modelType.ToString(),
			IsPartial = tds.Modifiers.Any(SyntaxKind.PartialKeyword),
			IsRecord = type.IsRecord,
			IsClass = type.TypeKind == TypeKind.Class,
			IsAbstract = type.IsAbstract,
			IsGeneric = type.IsGenericType,
			IsNested = type.IsNested(),
		};

		// Manually implemented
		if (!result.IsPartial) // Do not generate source, but be aware of existence, for potential invocation from newly generated builders
		{
			// Only if non-abstract
			if (type.IsAbstract)
				return null;

			// Only if non-generic
			if (type.IsGenericType)
				return null;

			return result;
		}

		var members = type.GetMembers();

		result.HasSuitableConstructor = GetSuitableConstructor(modelType) is not null;
		result.HasBuildMethod = members.Any(member => member.Name == "Build" && member is IMethodSymbol method && method.Parameters.Length == 0);
		result.Checksum = Convert.ToBase64String([.. context.Node.GetText().GetChecksum()]); // Many kinds of changes in the file may warrant changes in the generated source, so rely on the source's checksum

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, (ImmutableArray<Builder> Builders, Compilation Compilation) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var builders = input.Builders.ToList();
		var compilation = input.Compilation;

		var concreteBuilderTypesByModel = builders
			.Where(builder => !builder.IsAbstract && !builder.IsGeneric) // Concrete only
			.GroupBy(builder => builder.ModelTypeFullyQualifiedName) // Deduplicate
			.Select(group => new KeyValuePair<ITypeSymbol?, string>(compilation.GetTypeByMetadataName(group.Key), group.First().TypeFullyQualifiedName))
			.Where(pair => pair.Key is not null)
			.ToDictionary<KeyValuePair<ITypeSymbol?, string>, ITypeSymbol, string>(pair => pair.Key!, pair => pair.Value, SymbolEqualityComparer.Default);

		// Remove models for which multiple builders exist
		{
			var buildersWithDuplicateModel = builders
				.GroupBy(builder => builder.ModelTypeFullyQualifiedName)
				.Where(group => group.Count() > 1)
				.ToList();

			// Remove models for which multiple builders exist
			foreach (var group in buildersWithDuplicateModel)
			{
				foreach (var type in group)
					builders.Remove(type);

				context.ReportDiagnostic("DummyBuilderGeneratorDuplicateBuilders", "Duplicate builders",
					$"Multiple dummy builders exist for {group.Key}. Source generation for these builders was skipped.", DiagnosticSeverity.Warning, compilation.GetTypeByMetadataName(group.Last().TypeFullyQualifiedName));
			}
		}

		foreach (var builder in builders)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			var type = compilation.GetTypeByMetadataName(builder.TypeFullyQualifiedName);
			var modelType = type?.GetAttribute("DummyBuilderAttribute", Constants.DomainModelingNamespace, arity: 1) is AttributeData { AttributeClass: not null } attribute
				? attribute.AttributeClass.TypeArguments[0]
				: null;

			// No source generation, only above analyzers
			if (!builder.IsPartial)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorNonPartialType", "Non-partial dummy builder type",
					"Type marked as dummy builder is not marked as 'partial'. To get source generation, add the 'partial' keyword.", DiagnosticSeverity.Info, type);
				continue;
			}

			// Require being able to find the builder type
			if (type is null)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorUnexpectedType", "Unexpected type",
					$"Type marked as dummy builder has unexpected type '{builder.TypeFullyQualifiedName}'.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Require being able to find the model type
			if (modelType is null)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorUnexpectedModelType", "Unexpected model type",
					$"Type marked as dummy builder has unexpected model type '{builder.ModelTypeFullyQualifiedName}'.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Only if class
			if (!builder.IsClass)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorValueType", "Source-generated struct value object",
					"The type was not source-generated because it is a struct, while a class was expected. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Only if non-abstract
			if (builder.IsAbstract)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorAbstractType", "Source-generated abstract type",
					"The type was not source-generated because it is abstract. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Only if non-generic
			if (builder.IsGeneric)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Only if non-nested
			if (builder.IsNested)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type. To disable source generation, remove the 'partial' keyword.", DiagnosticSeverity.Warning, type);
				continue;
			}

			// Only with a suitable constructor
			if (!builder.HasSuitableConstructor)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorNoSuitableConstructor", "No suitable constructor",
					$"{type.Name} could not find a suitable constructor on {modelType.Name}.", DiagnosticSeverity.Warning, type);
			}

			var typeName = type.Name; // Non-generic
			var containingNamespace = type.ContainingNamespace.ToString();
			var membersByName = type.GetMembers().ToLookup(member => member.Name, StringComparer.OrdinalIgnoreCase);

			var hasBuildMethod = builder.HasBuildMethod;

			var suitableCtor = GetSuitableConstructor(modelType);

			if (suitableCtor is null)
				continue;

			var ctorParams = suitableCtor.Parameters;

			var modelCtorParams = String.Join($",{Environment.NewLine}				", ctorParams.Select(param => $"{param.Name}: this.{param.Name.ToTitleCase()}").ToList());

			var components = new List<string>();
			foreach (var param in ctorParams)
			{
				var componentBuilder = new StringBuilder();

				var memberName = param.Name.ToTitleCase();

				// Add a property for the ctor param, with a field initializer
				// The field initializer may use other builders to instantiate objects
				// Obviously it may not use our own, which could cause infinite recursion (crashing the compiler)
				{
					concreteBuilderTypesByModel.Remove(modelType);

					if (membersByName[memberName].Any(member => member is IPropertySymbol || member is IFieldSymbol))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		private {param.Type.WithNullableAnnotation(NullableAnnotation.None)} {memberName} {{ get; set; }} = {param.Type.CreateDummyInstantiationExpression(param.Name == "value" ? param.ContainingType.Name : param.Name, concreteBuilderTypesByModel.Keys, type => $"new {concreteBuilderTypesByModel[type]}().Build()")};");

					concreteBuilderTypesByModel.Add(modelType, builder.TypeFullyQualifiedName);
				}

				if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.Equals(param.Type, SymbolEqualityComparer.Default)))
					componentBuilder.Append("// ");
				componentBuilder.AppendLine($"		public {typeName} With{memberName}({param.Type.WithNullableAnnotation(NullableAnnotation.None)} value) => this.With(b => b.{memberName} = value);");

				foreach (var primitiveType in param.Type.GetAvailableConversionsFromPrimitives(skipForSystemTypes: true))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType(primitiveType)))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}({primitiveType} value, bool _ = false) => this.With{memberName}(({param.Type.WithNullableAnnotation(NullableAnnotation.None)})value);");
				}

				if (param.Type.IsType<DateTime>() || param.Type.IsType<DateTimeOffset>())
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value) => this.With{memberName}(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));");
				}
				if (param.Type.IsNullable(out var underlyingType) && (underlyingType.IsType<DateTime>() || underlyingType.IsType<DateTimeOffset>()))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value, bool _ = false) => this.With{memberName}(value is null ? null : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));");
				}

				if (param.Type.IsType("DateOnly", "System"))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value) => this.With{memberName}(DateOnly.Parse(value, CultureInfo.InvariantCulture));");
				}
				if (param.Type.IsNullable(out underlyingType) && underlyingType.IsType("DateOnly", "System"))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value, bool _ = false) => this.With{memberName}(value is null ? null : DateOnly.Parse(value, CultureInfo.InvariantCulture));");
				}

				if (param.Type.IsType("TimeOnly", "System"))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value) => this.With{memberName}(TimeOnly.Parse(value, CultureInfo.InvariantCulture));");
				}
				if (param.Type.IsNullable(out underlyingType) && underlyingType.IsType("TimeOnly", "System"))
				{
					if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
						componentBuilder.Append("// ");
					componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value, bool _ = false) => this.With{memberName}(value is null ? null : TimeOnly.Parse(value, CultureInfo.InvariantCulture));");
				}

				components.Add(componentBuilder.ToString());
			}
			var joinedComponents = String.Join($"{Environment.NewLine}", components);

			var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable disable

namespace {containingNamespace}
{{
	/// <summary>
	/// <para>
	/// Implements the Builder pattern to construct <see cref=""{modelType.ToString().Replace("<", "{").Replace(">", "}")}""/> objects for testing purposes.
	/// </para>
	/// <para>
	/// Where production code relies on the type's constructor, test code can rely on this builder.
	/// That way, if the constructor changes, only the builder needs to be adjusted, rather than lots of test methods.
	/// </para>
	/// </summary>
	/* Generated */ {type.DeclaredAccessibility.ToCodeString()} partial{(builder.IsRecord ? " record" : "")} class {typeName}
	{{
{joinedComponents}

		private {typeName} With(Action<{typeName}> assignment)
		{{
			assignment(this);
			return this;
		}}

		{(hasBuildMethod ? "/*" : "")}
		public {modelType} Build()
		{{
			var result = new {modelType}(
				{modelCtorParams});
			return result;
		}}
		{(hasBuildMethod ? "*/" : "")}
	}}
}}
";

			AddSource(context, source, typeName, containingNamespace);
		}
	}

	private static IMethodSymbol? GetSuitableConstructor(ITypeSymbol modelType)
	{
		if (modelType is not INamedTypeSymbol namedTypeSymbol)
			return null;

		var result = namedTypeSymbol.Constructors
			.OrderByDescending(ctor => ctor.DeclaredAccessibility) // Most accessible first
			.ThenBy(ctor => ctor.Parameters.Length > 0 ? 0 : 1) // Prefer a non-default ctor
			.ThenBy(ctor => ctor.Parameters.Length) // Shortest first (the most basic non-default option)
			.FirstOrDefault();

		return result;
	}

	private sealed record Builder : IGeneratable
	{
		public string TypeFullyQualifiedName { get; set; } = null!;
		public string ModelTypeFullyQualifiedName { get; set; } = null!;
		public bool IsPartial { get; set; }
		public bool IsRecord { get; set; }
		public bool IsClass { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsGeneric { get; set; }
		public bool IsNested { get; set; }
		public bool HasBuildMethod { get; set; }
		public bool HasSuitableConstructor { get; set; }
		public string? Checksum { get; set; }
	}
}
