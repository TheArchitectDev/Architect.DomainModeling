using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Tests.IdentityTestTypes;
using Architect.DomainModeling.Tests.WrapperValueObjectTestTypes;

namespace Architect.DomainModeling.Tests.Analyzers;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
[SuppressMessage("Usage", "CounterintuitiveNullHandlingOnLiftedValueObjectComparison:Comparisons between null and non-null might produce unintended results", Justification = "Testing presence of warning.")]
public class ValueObjectLiftingOnComparisonOperatorAnalyzerTests
{
	// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
	// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

	public static void CompareUnrelatedValueObjects_WithLifting_ShouldWarn()
	{
#pragma warning disable CS0464 // Comparing with null of struct type always produces 'false' -- We still want to test our analyzer on this syntax

		_ = new IntId(1) > null;
		_ = null > new IntId(1);
		_ = new DecimalValue(1) > null;
		_ = new IntValue(1) > null;
		//_ = new Tests.ValueObjectTestTypes.StringValue("A", "B") > null; // This one was already rejected simply based on the disallowed null

#pragma warning restore CS0464 // Comparing with null of struct type always produces 'false'
	}
}
