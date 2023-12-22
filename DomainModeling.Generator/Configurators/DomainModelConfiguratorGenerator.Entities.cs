using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator.Configurators;

public partial class DomainModelConfiguratorGenerator
{
	internal static void GenerateSourceForEntities(SourceProductionContext context, (ImmutableArray<EntityGenerator.Generatable> Generatables, (bool HasConfigureConventions, string AssemblyName) Metadata) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Generate the method only if we have any generatables, or if we are an assembly in which ConfigureConventions() is called
		if (!input.Generatables.Any() && !input.Metadata.HasConfigureConventions)
			return;

		var targetNamespace = input.Metadata.AssemblyName;

		var configurationText = String.Join($"{Environment.NewLine}\t\t\t", input.Generatables.Select(generatable =>
			$"configurator.ConfigureEntity<{generatable.ContainingNamespace}.{generatable.TypeName}>({Environment.NewLine}				new {Constants.DomainModelingNamespace}.Configuration.IEntityConfigurator.Args() {{ HasDefaultConstructor = {(generatable.ExistingComponents.HasFlag(EntityGenerator.EntityTypeComponents.DefaultConstructor) ? "true" : "false")} }});"));

		var source = $@"
using {Constants.DomainModelingNamespace};

#nullable enable

namespace {targetNamespace}
{{
	public static class EntityDomainModelConfigurator
	{{
		/// <summary>
		/// <para>
		/// Invokes a callback on the given <paramref name=""configurator""/> for each marked <see cref=""IEntity""/> type in the current assembly.
		/// </para>
		/// <para>
		/// For example, this can be used to have Entity Framework configure a convention for every matching type in the domain model, in a trim-safe way.
		/// </para>
		/// </summary>
		public static void ConfigureEntities({Constants.DomainModelingNamespace}.Configuration.IEntityConfigurator configurator)
		{{
			{configurationText}
		}}
	}}
}}
";

		AddSource(context, source, "EntityDomainModelConfigurator", targetNamespace);
	}
}
