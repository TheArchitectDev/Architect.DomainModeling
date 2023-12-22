using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator.Configurators;

public partial class DomainModelConfiguratorGenerator
{
	internal static void GenerateSourceForWrapperValueObjects(SourceProductionContext context, (ImmutableArray<WrapperValueObjectGenerator.Generatable> Generatables, (bool HasConfigureConventions, string AssemblyName) Metadata) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Generate the method only if we have any generatables, or if we are an assembly in which ConfigureConventions() is called
		if (!input.Generatables.Any() && !input.Metadata.HasConfigureConventions)
			return;

		var targetNamespace = input.Metadata.AssemblyName;

		var configurationText = String.Join($"{Environment.NewLine}\t\t\t", input.Generatables.Select(generatable => $"""
			configurator.ConfigureWrapperValueObject<{generatable.ContainingNamespace}.{generatable.TypeName}, {generatable.UnderlyingTypeFullyQualifiedName}>({Environment.NewLine}				new {Constants.DomainModelingNamespace}.Configuration.IWrapperValueObjectConfigurator.Args());
			"""));

		var source = $@"
using {Constants.DomainModelingNamespace};

#nullable enable

namespace {targetNamespace}
{{
	public static class WrapperValueObjectDomainModelConfigurator
	{{
		/// <summary>
		/// <para>
		/// Invokes a callback on the given <paramref name=""configurator""/> for each marked <see cref=""IWrapperValueObject{{TValue}}""/> type in the current assembly.
		/// </para>
		/// <para>
		/// For example, this can be used to have Entity Framework configure a convention for every matching type in the domain model, in a trim-safe way.
		/// </para>
		/// </summary>
		public static void ConfigureWrapperValueObjects({Constants.DomainModelingNamespace}.Configuration.IWrapperValueObjectConfigurator configurator)
		{{
			{configurationText}
		}}
	}}
}}
";

		AddSource(context, source, "WrapperValueObjectDomainModelConfigurator", targetNamespace);
	}
}
