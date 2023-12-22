using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator.Configurators;

public partial class DomainModelConfiguratorGenerator
{
	internal static void GenerateSourceForDomainEvents(SourceProductionContext context, (ImmutableArray<DomainEventGenerator.Generatable> Generatables, (bool HasConfigureConventions, string AssemblyName) Metadata) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Generate the method only if we have any generatables, or if we are an assembly in which ConfigureConventions() is called
		if (!input.Generatables.Any() && !input.Metadata.HasConfigureConventions)
			return;

		var targetNamespace = input.Metadata.AssemblyName;

		var configurationText = String.Join($"{Environment.NewLine}\t\t\t", input.Generatables.Select(generatable =>
			$"configurator.ConfigureDomainEvent<{generatable.ContainingNamespace}.{generatable.TypeName}>({Environment.NewLine}				new {Constants.DomainModelingNamespace}.Configuration.IDomainEventConfigurator.Args() {{ HasDefaultConstructor = {(generatable.ExistingComponents.HasFlag(DomainEventGenerator.DomainEventTypeComponents.DefaultConstructor) ? "true" : "false")} }});"));

		var source = $@"
using {Constants.DomainModelingNamespace};

#nullable enable

namespace {targetNamespace}
{{
	public static class DomainEventDomainModelConfigurator
	{{
		/// <summary>
		/// <para>
		/// Invokes a callback on the given <paramref name=""configurator""/> for each marked domain event type in the current assembly.
		/// </para>
		/// <para>
		/// For example, this can be used to have Entity Framework configure a convention for every matching type in the domain model, in a trim-safe way.
		/// </para>
		/// </summary>
		public static void ConfigureDomainEvents({Constants.DomainModelingNamespace}.Configuration.IDomainEventConfigurator configurator)
		{{
			{configurationText}
		}}
	}}
}}
";

		AddSource(context, source, "DomainEventDomainModelConfigurator", targetNamespace);
	}
}
