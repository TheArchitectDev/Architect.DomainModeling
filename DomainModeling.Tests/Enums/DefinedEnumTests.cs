using System.Diagnostics.CodeAnalysis;
using Xunit;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.DomainModeling.Tests;

public class DefinedEnumTests
{
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "We need to test the behavior under these circumstances.")]
	private enum SimpleSByte : sbyte
	{
		Zero = 0,
		One = 1,
		AlsoOne = 1,
		MinusOne = -1,
	}
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "We need to test the behavior under these circumstances.")]
	private enum AdvancedSByte : sbyte
	{
		Zero = 0,
		One = 1,
		AlsoOne = 1,
		Two = 2,
		MinusSixFive = -65, // (sbyte)191
		MinusTwo = -2,
		MinusOne = -1,
	}
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "We need to test the behavior under these circumstances.")]
	private enum SimpleShort : short
	{
		Zero = 0,
		One = 1,
		AlsoOne = 1,
		Two = 2,
		OneNineOne = 191,
		MinusTwo = -2,
		MinusOne = -1,
	}
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "We need to test the behavior under these circumstances.")]
	private enum AdvancedShort : short
	{
		Zero = 0,
		One = 1,
		AlsoOne = 1,
		Two = 2,
		OneNineOne = 191,
		MinusOneSixThreeNineSeven = -16397, // (short)49139
		MinusTwo = -2,
		MinusOne = -1,
	}
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "We need to test the behavior under these circumstances.")]
	[Flags]
	private enum ExampleFlags : uint
	{
		Zero = 0,
		One = 1,
		AlsoOne = 1,
		Two = 2,
		Four = 4,
		Sixteen = 16,
	}

	static DefinedEnumTests()
	{
		DefinedEnum.ExceptionFactoryForUndefinedInput = (type, numericValue, errorState) => new InvalidOperationException($"{type.Name} expects a defined value.");
	}

	[Fact]
	public void UndefinedValue_Regularly_ShouldReturnExpectedResult()
	{
		Assert.Equal(~(LazyThreadSafetyMode)(1 | 2), DefinedEnum.UndefinedValues<LazyThreadSafetyMode>.UndefinedValue);
		Assert.Equal((SimpleSByte)(-65), DefinedEnum.UndefinedValues<SimpleSByte>.UndefinedValue);
		Assert.Equal((AdvancedSByte)3, DefinedEnum.UndefinedValues<AdvancedSByte>.UndefinedValue);
		Assert.Equal((SimpleShort)(-16397), DefinedEnum.UndefinedValues<SimpleShort>.UndefinedValue);
		Assert.Equal((AdvancedShort)3, DefinedEnum.UndefinedValues<AdvancedShort>.UndefinedValue);
		Assert.Equal(~(ExampleFlags)(1 | 2 | 4 | 16), DefinedEnum.UndefinedValues<ExampleFlags>.UndefinedValue);
	}

	[Fact]
	public void ThrowUndefinedInput_WithNongenericOverload_ShouldThrowConfiguredException()
	{
		Assert.Throws<InvalidOperationException>(() => DefinedEnum.ThrowUndefinedInput(typeof(LazyThreadSafetyMode), numericValue: 999, errorState: null));
	}

	[Fact]
	public void ThrowUndefinedInput_WithGenericOverload_ShouldThrowConfiguredException()
	{
		Assert.Throws<InvalidOperationException>(() => DefinedEnum.ThrowUndefinedInput((LazyThreadSafetyMode)999, errorState: null));
	}
}
