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

		context.RegisterSourceOutput(provider, GenerateSource!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Subclass
		if (node is not ClassDeclarationSyntax cds || cds.BaseList is null)
			return false;

		// Consider any type with SOME 2-param generic "DummyBuilder" inheritance/implementation
		foreach (var baseType in cds.BaseList.Types)
		{
			if (baseType.Type.HasArityAndName(2, Constants.DummyBuilderTypeName))
				return true;
		}

		return false;
	}

	private static Builder? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var model = context.SemanticModel;
		var cds = (ClassDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node);

		if (type is null)
			return null;

		var result = new Builder();
		result.SetAssociatedData(type);

		var isManuallyImplementedBuilder = !cds.Modifiers.Any(SyntaxKind.PartialKeyword);

		if (isManuallyImplementedBuilder) // Do not generate source, but be aware of existence, for potential invocation from newly generated builders
		{
			// Only with the intended inheritance
			if (type.BaseType?.IsType(Constants.DummyBuilderTypeName, Constants.DomainModelingNamespace) != true)
				return null;
			// Only if non-abstract
			if (type.IsAbstract)
				return null;
			// Only if non-generic
			if (type.IsGenericType)
				return null;

			result.TypeFullyQualifiedName = type.ToString();
			result.IsManuallyImplemented = true;
		}
		else // Prepare to generate source
		{
			// Only with the attribute
			if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
				return null;

			// Only with a usable model type
			if (type.BaseType!.TypeArguments[0] is not INamedTypeSymbol modelType)
				return null;

			result.TypeFullyQualifiedName = type.ToString();
			result.IsDummyBuilder = type.BaseType?.IsType(Constants.DummyBuilderTypeName, Constants.DomainModelingNamespace) == true;
			result.IsAbstract = type.IsAbstract;
			result.IsGeneric = type.IsGenericType;
			result.IsNested = type.IsNested();

			var members = type.GetMembers();

			result.HasBuildMethod = members.Any(member => member.Name == "Build" && member is IMethodSymbol method && method.Parameters.Length == 0);

			var suitableCtor = GetSuitableConstructor(modelType);

			result.HasSuitableConstructor = suitableCtor is not null;
			result.Checksum = Convert.ToBase64String(context.Node.GetText().GetChecksum().ToArray()); // Many kinds of changes in the file may warrant changes in the generated source, so rely on the source's checksum
		}

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, ImmutableArray<Builder> builders)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var buildersWithSourceGeneration = builders.Where(builder => !builder.IsManuallyImplemented).ToList();
		var concreteBuilderTypesByModel = builders
			.Where(builder => !builder.IsAbstract && !builder.IsGeneric) // Concrete only
			.Where(builder => builder.IsManuallyImplemented || builder.IsDummyBuilder) // Manually implemented or with the correct inheritance for generation only
			.GroupBy<Builder, INamedTypeSymbol>(builder => builder.ModelType(), SymbolEqualityComparer.Default) // Deduplicate
			.ToDictionary<IGrouping<INamedTypeSymbol, Builder>, ITypeSymbol, INamedTypeSymbol>(group => group.Key, group => group.First().TypeSymbol(), SymbolEqualityComparer.Default);

		// Remove models for which multiple builders exist
		{
			var buildersWithDuplicateModel = buildersWithSourceGeneration
				.GroupBy<Builder, ITypeSymbol>(builder => builder.ModelType(), SymbolEqualityComparer.Default)
				.Where(group => group.Count() > 1);

			// Remove models for which multiple builders exist
			foreach (var group in buildersWithDuplicateModel)
			{
				foreach (var type in group)
					buildersWithSourceGeneration.Remove(type);

				context.ReportDiagnostic("DummyBuilderGeneratorDuplicateBuilders", "Duplicate builders",
					$"Multiple dummy builders exist for {group.Key.Name}. Source generation for these builders was skipped.", DiagnosticSeverity.Warning, group.Last().TypeSymbol());
			}
		}

		foreach (var builder in buildersWithSourceGeneration)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			var type = builder.TypeSymbol();
			var modelType = builder.ModelType();

			// Only with a suitable constructor
			if (!builder.HasSuitableConstructor)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorNoSuitableConstructor", "No suitable constructor",
					$"{type.Name} could not find a suitable constructor on {modelType.Name}.", DiagnosticSeverity.Warning, type);
			}
			// Only with the intended inheritance
			if (!builder.IsDummyBuilder)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorUnexpectedInheritance", "Unexpected base class",
					"The type marked as source-generated has an unexpected base class. Did you mean DummyBuilder<TModel, TModelBuilder>?", DiagnosticSeverity.Warning, type);
				continue;
			}
			// Only if non-abstract
			if (builder.IsAbstract)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorAbstractType", "Source-generated abstract type",
					"The type was not source-generated because it is abstract.", DiagnosticSeverity.Warning, type);
				continue;
			}
			// Only if non-generic
			if (builder.IsGeneric)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorGenericType", "Source-generated generic type",
					"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, type);
				continue;
			}
			// Only if non-nested
			if (builder.IsNested)
			{
				context.ReportDiagnostic("DummyBuilderGeneratorNestedType", "Source-generated nested type",
					"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, type);
				continue;
			}

			var typeName = type.Name; // Non-generic
			var containingNamespace = type.ContainingNamespace.ToString();
			var membersByName = type.GetMembers().ToLookup(member => member.Name, StringComparer.OrdinalIgnoreCase);

			var hasBuildMethod = builder.HasBuildMethod;

			var suitableCtor = GetSuitableConstructor(modelType);

			if (suitableCtor is null)
				return;

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

					concreteBuilderTypesByModel.Add(modelType, type);
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
	/* Generated */ public partial class {typeName}
	{{
{joinedComponents}

		{(hasBuildMethod ? "/*" : "")}
		public override {modelType} Build()
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

	private static IMethodSymbol? GetSuitableConstructor(INamedTypeSymbol modelType)
	{
		var result = modelType.Constructors
			.OrderByDescending(ctor => ctor.DeclaredAccessibility) // Most accessible first
			.ThenBy(ctor => ctor.Parameters.Length > 0 ? 0 : 1) // Prefer a non-default ctor
			.ThenBy(ctor => ctor.Parameters.Length) // Shortest first (the most basic non-default option)
			.FirstOrDefault();

		return result;
	}

	private sealed record Builder : IGeneratable
	{
		public string TypeFullyQualifiedName { get; set; } = null!;
		public bool IsDummyBuilder { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsGeneric { get; set; }
		public bool IsNested { get; set; }
		public bool IsManuallyImplemented { get; set; }
		public bool HasBuildMethod { get; set; }
		public bool HasSuitableConstructor { get; set; }
		public string? Checksum { get; set; }

		public INamedTypeSymbol TypeSymbol()
		{
			return this.GetAssociatedData<INamedTypeSymbol>();
		}

		public INamedTypeSymbol ModelType()
		{
			return (INamedTypeSymbol)this.TypeSymbol().BaseType!.TypeArguments[0];
		}
	}
}
