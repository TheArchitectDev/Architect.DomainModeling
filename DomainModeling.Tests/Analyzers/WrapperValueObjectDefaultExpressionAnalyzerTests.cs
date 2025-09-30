using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Tests.WrapperValueObjectTestTypes;

namespace Architect.DomainModeling.Tests.Analyzers;

[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
[SuppressMessage("Design", "WrapperValueObjectDefaultExpression:Default expression instantiating unvalidated value object", Justification = "Testing presence of warning.")]
public class WrapperValueObjectDefaultExpressionAnalyzerTests
{
	// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
	// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

	public static void UseDefaultExpressionOnWrapperValueObjectStruct_Always_ShouldWarn()
	{
		_ = default(DecimalValue);
	}

	public static void UseDefaultLiteralOnWrapperValueObjectStruct_Always_ShouldWarn()
	{
		DecimalValue value = default;
		_ = value;
	}
}
