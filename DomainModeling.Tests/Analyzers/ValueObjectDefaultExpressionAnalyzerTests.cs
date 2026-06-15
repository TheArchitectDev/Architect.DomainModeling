using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Tests.Analyzers;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
[SuppressMessage("Design", "ValueObjectDefaultExpression:Default expression instantiating unvalidated value object", Justification = "Testing presence of warning.")]
public class ValueObjectDefaultExpressionAnalyzerTests
{
	// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
	// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

	public static void UseDefaultExpressionOnWrapperValueObjectStruct_Always_ShouldWarn()
	{
		_ = default(Tests.WrapperValueObjectTestTypes.DecimalValue);
	}

	public static void UseDefaultExpressionOnValueObjectStruct_Always_ShouldWarn()
	{
		_ = default(Tests.ValueObjectTestTypes.CustomCollectionValueObject);
	}

	public static void UseDefaultLiteralOnWrapperValueObjectStruct_Always_ShouldWarn()
	{
		Tests.WrapperValueObjectTestTypes.DecimalValue value = default;
		_ = value;
	}

	public static void UseDefaultLiteralOnValueObjectStruct_Always_ShouldWarn()
	{
		Tests.WrapperValueObjectTestTypes.DecimalValue value = default;
		_ = value;
	}
}
