using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Tests.Analyzers
{
	namespace WrapperValueObjectTestTypes
	{
		[SuppressMessage("Design", "WrapperValueObjectGeneratorMissingStringComparison:WrapperValueObject has string members but no StringComparison property", Justification = "Testing presence of warning.")]
		[WrapperValueObject<string>]
		internal sealed partial class StringWrapperValueObject
		{
		}
	}
}
