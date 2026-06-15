using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Tests.IdentityTestTypes;
using Architect.DomainModeling.Tests.WrapperValueObjectTestTypes;

namespace Architect.DomainModeling.Tests.Analyzers;

#pragma warning disable CounterintuitiveNullHandlingOnLiftedValueObjectComparison // Comparisons between null and non-null might produce unintended results -- Irrelevant here and interferes with our tests

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
[SuppressMessage("Usage", "ComparisonBetweenUnrelatedValueObjects:Comparison between unrelated value objects", Justification = "Testing presence of warning.")]
public class ValueObjectImplicitConversionOnBinaryOperatorAnalyzerTests
{
	// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
	// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

	public static void CompareUnrelatedIdentities_Always_ShouldWarn()
	{
		_ = (IntId)0 == (FullySelfImplementedIdentity)0;
		_ = (IntId)0 != (FullySelfImplementedIdentity)0;
		_ = (IntId)0 < (FullySelfImplementedIdentity)0;
		_ = (IntId)0 <= (FullySelfImplementedIdentity)0;
		_ = (IntId)0 > (FullySelfImplementedIdentity)0;
		_ = (IntId)0 >= (FullySelfImplementedIdentity)0;

		_ = (IntId?)0 == (FullySelfImplementedIdentity)0;
		_ = (IntId?)0 != (FullySelfImplementedIdentity)0;
		_ = (IntId?)0 < (FullySelfImplementedIdentity)0;
		_ = (IntId?)0 <= (FullySelfImplementedIdentity)0;
		_ = (IntId?)0 > (FullySelfImplementedIdentity)0;
		_ = (IntId?)0 >= (FullySelfImplementedIdentity)0;

		_ = (IntId)0 == (FullySelfImplementedIdentity?)0;
		_ = (IntId)0 != (FullySelfImplementedIdentity?)0;
		_ = (IntId)0 < (FullySelfImplementedIdentity?)0;
		_ = (IntId)0 <= (FullySelfImplementedIdentity?)0;
		_ = (IntId)0 > (FullySelfImplementedIdentity?)0;
		_ = (IntId)0 >= (FullySelfImplementedIdentity?)0;

		_ = (IntId?)0 == (FullySelfImplementedIdentity?)0;
		_ = (IntId?)0 != (FullySelfImplementedIdentity?)0;
		_ = (IntId?)0 < (FullySelfImplementedIdentity?)0;
		_ = (IntId?)0 <= (FullySelfImplementedIdentity?)0;
		_ = (IntId?)0 > (FullySelfImplementedIdentity?)0;
		_ = (IntId?)0 >= (FullySelfImplementedIdentity?)0;
	}

	public static void CompareUnrelatedWrapperValueObjects_Always_ShouldWarn()
	{
		_ = (IntValue)0 == (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue)0 != (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue)0 < (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue)0 <= (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue)0 > (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue)0 >= (FullySelfImplementedWrapperValueObject)0;

#pragma warning disable CS8604 // Possible null reference argument. -- True, but we just want to use this to test an analyzer"
		_ = (IntValue?)0 == (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue?)0 != (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue?)0 < (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue?)0 <= (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue?)0 > (FullySelfImplementedWrapperValueObject)0;
		_ = (IntValue?)0 >= (FullySelfImplementedWrapperValueObject)0;

		_ = (IntValue)0 == (FullySelfImplementedWrapperValueObject?)0;
		_ = (IntValue)0 != (FullySelfImplementedWrapperValueObject?)0;
		_ = (IntValue)0 < (FullySelfImplementedWrapperValueObject?)0;
		_ = (IntValue)0 <= (FullySelfImplementedWrapperValueObject?)0;
		_ = (IntValue)0 > (FullySelfImplementedWrapperValueObject?)0;
		_ = (IntValue)0 >= (FullySelfImplementedWrapperValueObject?)0;
#pragma warning restore CS8604 // Possible null reference argument.
	}

	public static void CompareUnrelatedWrapperValueObjectToCoreType_Always_ShouldWarn()
	{
		_ = (IntValue)0 == 0;
		_ = (IntValue)0 != 0;
		_ = (IntValue)0 < 0;
		_ = (IntValue)0 <= 0;
		_ = (IntValue)0 > 0;
		_ = (IntValue)0 >= 0;

#pragma warning disable CS8604 // Possible null reference argument. -- True, but we just want to use this to test an analyzer"
		_ = (IntValue?)0 == 0;
		_ = (IntValue?)0 != 0;
		_ = (IntValue?)0 < 0;
		_ = (IntValue?)0 <= 0;
		_ = (IntValue?)0 > 0;
		_ = (IntValue?)0 >= 0;
#pragma warning restore CS8604 // Possible null reference argument.

		_ = (IntValue)0 == (int?)0;
		_ = (IntValue)0 != (int?)0;
		_ = (IntValue)0 < (int?)0;
		_ = (IntValue)0 <= (int?)0;
		_ = (IntValue)0 > (int?)0;
		_ = (IntValue)0 >= (int?)0;

		_ = (IntValue?)0 == (int?)0;
		_ = (IntValue?)0 != (int?)0;
		_ = (IntValue?)0 < (int?)0;
		_ = (IntValue?)0 <= (int?)0;
		_ = (IntValue?)0 > (int?)0;
		_ = (IntValue?)0 >= (int?)0;
	}
}
