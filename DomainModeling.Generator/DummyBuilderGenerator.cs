using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator
{
	[Generator]
	public class DummyBuilderGenerator : SourceGenerator
	{
		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<ClassDeclarationSyntax> BuilderClasses { get; } = new List<ClassDeclarationSyntax>();
			/// <summary>
			/// Builders applicable for source generation.
			/// </summary>
			public List<ClassDeclarationSyntax> CandidateBuilderClasses { get; } = new List<ClassDeclarationSyntax>();

			public void OnVisitSyntaxNode(SyntaxNode node)
			{
				// Subclass
				if (node is ClassDeclarationSyntax cds && cds.BaseList is not null)
				{
					// Consider any type with SOME 2-param generic "DummyBuilder" inheritance/implementation
					foreach (var baseType in cds.BaseList.Types)
					{
						if (baseType.Type is not GenericNameSyntax genericName) continue;

						if (genericName.Arity == 2 && genericName.Identifier.ValueText == Constants.DummyBuilderTypeName)
						{
							this.BuilderClasses.Add(cds);

							if (cds.Modifiers.Any(SyntaxKind.PartialKeyword))
								this.CandidateBuilderClasses.Add(cds);
							break;
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

			var builderTypes = new List<INamedTypeSymbol>();
			var builderTypesWithSourceGeneration = new List<INamedTypeSymbol>();

			foreach (var cds in receiver.BuilderClasses)
			{
				var model = context.Compilation.GetSemanticModel(cds.SyntaxTree);
				var type = model.GetDeclaredSymbol(cds)!;

				// Only with the intended inheritance
				if (type.BaseType?.IsType(Constants.DummyBuilderTypeName, Constants.DomainModelingNamespace) != true)
					continue;
				// Only if non-abstract
				if (type.IsAbstract)
					continue;
				// Only if non-generic
				if (type.IsGenericType)
					continue;

				builderTypes.Add(type);
			}

			var builderTypesByModel = builderTypes.ToDictionary<INamedTypeSymbol, ITypeSymbol>(builderType => builderType.BaseType!.TypeArguments[0], SymbolEqualityComparer.Default);

			// Complete partial DummyBuilder subtypes
			foreach (var cds in receiver.CandidateBuilderClasses)
			{
				var model = context.Compilation.GetSemanticModel(cds.SyntaxTree);
				var type = model.GetDeclaredSymbol(cds)!;

				// Only with the attribute
				if (!type.HasAttribute(Constants.SourceGeneratedAttributeName, Constants.DomainModelingNamespace))
					continue;
				// Only with the intended inheritance
				if (type.BaseType?.IsType(Constants.DummyBuilderTypeName, Constants.DomainModelingNamespace) != true)
				{
					context.ReportDiagnostic("DummyBuilderGeneratorUnexpectedInheritance", "Unexpected base class",
						"The type marked as source-generated has an unexpected base class. Did you mean DummyBuilder<TModel, TModelBuilder>?", DiagnosticSeverity.Warning, type);
					continue;
				}
				// Only if non-abstract
				if (type.IsAbstract)
				{
					context.ReportDiagnostic("DummyBuilderGeneratorAbstractType", "Source-generated abstract type",
						"The type was not source-generated because it is abstract.", DiagnosticSeverity.Warning, type);
					continue;
				}
				// Only if non-generic
				if (type.IsGenericType)
				{
					context.ReportDiagnostic("DummyBuilderGeneratorGenericType", "Source-generated generic type",
						"The type was not source-generated because it is generic.", DiagnosticSeverity.Warning, type);
					continue;
				}
				// Only if non-nested
				if (cds.Parent is not NamespaceDeclarationSyntax && cds.Parent is not FileScopedNamespaceDeclarationSyntax)
				{
					context.ReportDiagnostic("DummyBuilderGeneratorNestedType", "Source-generated nested type",
						"The type was not source-generated because it is a nested type. To get source generation, avoid nesting it inside another type.", DiagnosticSeverity.Warning, type);
					continue;
				}

				builderTypesWithSourceGeneration.Add(type);
			}

			// Remove models for which multiple builders exist
			{
				var buildersWithDuplicateModel = builderTypesWithSourceGeneration
					.GroupBy<INamedTypeSymbol, ITypeSymbol>(builderType => builderType.BaseType!.TypeArguments[0], SymbolEqualityComparer.Default)
					.Where(group => group.Count() > 1)
					.ToList();

				foreach (var group in buildersWithDuplicateModel)
				{
					foreach (var type in group)
						builderTypesWithSourceGeneration.Remove(type);

					context.ReportDiagnostic("DummyBuilderGeneratorDuplicateBuilders", "Duplicate builders",
						$"Multiple dummy builders exist for {group.Key.Name}. Source generation for these builders was skipped.", DiagnosticSeverity.Warning, group.Last());
				}
			}

			foreach (var type in builderTypesWithSourceGeneration)
			{
				var typeName = type.Name; // Non-generic
				var containingNamespace = type.ContainingNamespace.ToString();
				var fullTypeName = $"{containingNamespace}.{typeName}";
				var membersByName = type.GetMembers().ToLookup(member => member.Name, StringComparer.OrdinalIgnoreCase);

				if (type.BaseType!.TypeArguments[0] is not INamedTypeSymbol modelType || modelType.IsValueType) return;

				var hasBuildMethod = membersByName["Build"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 0);

				var suitableCtor = modelType.Constructors
					.OrderByDescending(ctor => ctor.DeclaredAccessibility) // Most accessible first
					.ThenBy(ctor => ctor.Parameters.Length > 0 ? 0 : 1) // Prefer a non-default ctor
					.ThenBy(ctor => ctor.Parameters.Length) // Shortest first (the most basic non-default option)
					.FirstOrDefault();

				if (suitableCtor is null) return;

				var ctorParams = suitableCtor.Parameters;

				var modelCtorParams = String.Join($",{Environment.NewLine}				", ctorParams.Select(param => $"{param.Name}: this.{param.Name.ToTitleCase()}").ToList());

				var components = new List<string>();
				foreach (var param in ctorParams)
				{
					var componentBuilder = new StringBuilder();

					var memberName = param.Name.ToTitleCase();

					// Add the property, with a field initializer
					// The field initializer may use other builders to instantiate objects
					// Obviously it may not use our own, which could cause infinite recursion (instantly crashing the compiler)
					{
						builderTypesByModel.Remove(modelType);

						if (membersByName[memberName].Any(member => member is IPropertySymbol || member is IFieldSymbol))
							componentBuilder.Append("// ");
						componentBuilder.AppendLine($"		private {param.Type.WithNullableAnnotation(NullableAnnotation.None)} {memberName} {{ get; set; }} = {param.Type.CreateDummyInstantiationExpression(param.Name == "value" ? param.ContainingType.Name : param.Name, builderTypesByModel.Keys, type => $"new {builderTypesByModel[type]}().Build()")};");

						builderTypesByModel.Add(modelType, type);
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
						componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value) => this.With{memberName}(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));");
					}
					if (param.Type.IsNullable(out var underlyingType) && (underlyingType.IsType<DateTime>() || underlyingType.IsType<DateTimeOffset>()))
					{
						if (membersByName[$"With{memberName}"].Any(member => member is IMethodSymbol method && method.Parameters.Length == 1 && method.Parameters[0].Type.IsType<string>()))
							componentBuilder.Append("// ");
						componentBuilder.AppendLine($"		public {typeName} With{memberName}(System.String value, bool _ = false) => this.With{memberName}(value is null ? null : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal));");
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
	public partial class {typeName}
	{{
#nullable disable

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

				AddSource(context, source, type);
			}
		}
	}
}
