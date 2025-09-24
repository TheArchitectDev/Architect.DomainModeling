using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator.Configurators;

public partial class DomainModelConfiguratorGenerator
{
	internal static void GenerateSourceForIdentities(
		SourceProductionContext context,
		(ImmutableArray<ValueWrapperGenerator.BasicGeneratable> ValueWrappers, (bool HasConfigureConventions, string AssemblyName) Metadata) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Generate the method only if we have any value wrappers, or if we are an assembly in which ConfigureConventions() is called
		if (!input.ValueWrappers.Any() && !input.Metadata.HasConfigureConventions)
			return;

		var targetNamespace = input.Metadata.AssemblyName;

		var configurationText = String.Join($"{Environment.NewLine}\t\t\t", input.ValueWrappers
			.Where(generatable => generatable.IsIdentity)
			.Select(generatable => (Generatable: generatable, CoreTypeName: ValueWrapperGenerator.GetDirectParentOfCoreType(input.ValueWrappers, generatable.TypeName, generatable.ContainingNamespace).CoreTypeFullyQualifiedName))
			.Select(tuple => $$"""
				configurator.ConfigureIdentity<{{tuple.Generatable.ContainingNamespace}}.{{tuple.Generatable.TypeName}}, {{tuple.Generatable.UnderlyingTypeFullyQualifiedName}}, {{tuple.CoreTypeName}}>({{Environment.NewLine}}				new Architect.DomainModeling.Configuration.IIdentityConfigurator.Args());
				"""));

		var source = $@"
using Architect.DomainModeling;

#nullable enable

namespace {targetNamespace}
{{
	public static class IdentityDomainModelConfigurator
	{{
		/// <summary>
		/// <para>
		/// Invokes a callback on the given <paramref name=""configurator""/> for each marked <see cref=""IIdentity{{T}}""/> type in the current assembly.
		/// </para>
		/// <para>
		/// For example, this can be used to have Entity Framework configure a convention for every matching type in the domain model, in a trim-safe way.
		/// </para>
		/// </summary>
		public static void ConfigureIdentities(Architect.DomainModeling.Configuration.IIdentityConfigurator configurator)
		{{
			{configurationText}
		}}
	}}
}}
";

		AddSource(context, source, "IdentityDomainModelConfigurator", targetNamespace);
	}
}
