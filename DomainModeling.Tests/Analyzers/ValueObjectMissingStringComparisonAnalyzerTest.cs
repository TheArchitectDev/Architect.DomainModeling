using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Tests.Analyzers
{
	namespace ValueObjectTestTypes
	{
		[SuppressMessage("Design", "ValueObjectGeneratorMissingStringComparison:ValueObject has string members but no StringComparison property", Justification = "Testing presence of warning.")]
		[ValueObject]
		internal sealed partial class StringValueObject
		{
			public string? One { get; private init; }
		}
	}
}
