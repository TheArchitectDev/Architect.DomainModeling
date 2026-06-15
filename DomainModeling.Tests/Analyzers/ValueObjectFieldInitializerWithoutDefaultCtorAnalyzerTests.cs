namespace Architect.DomainModeling.Tests.Analyzers;

#pragma warning disable IDE0079 // Remove unnecessary suppression -- False positive
#pragma warning disable ValueObjectFieldInitializerWithoutDefaultConstructor // ValueObject has field initializers but no default constructor -- Testing presence of warning

// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

[ValueObject]
public partial class ValueObjectImplicitConversionOnBinaryOperatorAnalyzerTestValueObject(int one)
{
	public int Zero { get; private init; } // Fine, because no field initializer
	public int One { get; private init; } = one > 0 ? 1 : 0; // Fine, because field initializer uses primary ctor param
	public int Two { get; private init; } = 2; // Warning, because field initializer will not be run during deserialization, for lack of a default ctor (because of primary ctor)
	public int Three = 3; // Warning, because field initializer will not be run during deserialization, for lack of a default ctor (because of primary ctor)
}

[WrapperValueObject<string>]
public partial class ValueObjectImplicitConversionOnBinaryOperatorAnalyzerTestWrapperValueObject : Uri
{
	public StringComparison StringComparisonAcceptable => StringComparison.Ordinal; // Fine, because no field initializer
	public StringComparison StringComparison { get; } = StringComparison.Ordinal; // Warning, because field initializer will not be run during deserialization, for lack of a default ctor (because base has none)

	public ValueObjectImplicitConversionOnBinaryOperatorAnalyzerTestWrapperValueObject(string value)
		: base(value)
	{
		this.Value = value ?? throw new ArgumentNullException(nameof(value));
	}
}
