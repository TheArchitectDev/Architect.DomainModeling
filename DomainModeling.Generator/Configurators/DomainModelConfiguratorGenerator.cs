using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator.Configurators;

/// <summary>
/// Generates DomainModelConfigurators, types intended to configure miscellaneous components when it comes to certain types of domain objects.
/// </summary>
public partial class DomainModelConfiguratorGenerator : SourceGenerator
{
	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Only invoked from other generators
	}
}
