using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Conversions;
using Xunit;
using Enum = Architect.DomainModeling.DefinedEnum<System.Threading.LazyThreadSafetyMode>;
using IntEnum = Architect.DomainModeling.DefinedEnum<System.Threading.LazyThreadSafetyMode, int>;
using IntWrapper = Architect.DomainModeling.IValueWrapper<Architect.DomainModeling.DefinedEnum<System.Threading.LazyThreadSafetyMode, int>, int>;
using StringEnum = Architect.DomainModeling.DefinedEnum<System.Threading.LazyThreadSafetyMode, string>;
using StringWrapper = Architect.DomainModeling.IValueWrapper<Architect.DomainModeling.DefinedEnum<System.Threading.LazyThreadSafetyMode, string>, string>;

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
		DefinedEnum.ExceptionFactoryForNullInput = type => new InvalidOperationException($"{type.Name} expects a non-null value.");
		DefinedEnum.ExceptionFactoryForUndefinedInput = (type, numericValue) => new InvalidOperationException($"{type.Name} expects a defined value.");
	}

	private static Enum ToEnum(int value)
	{
		return new Enum((LazyThreadSafetyMode)value);
	}

	private static IntEnum ToIntEnum(int value)
	{
		return new IntEnum((LazyThreadSafetyMode)value);
	}

	private static StringEnum ToStringEnum(int value)
	{
		return new StringEnum((LazyThreadSafetyMode)value);
	}

	private static Enum? ToEnum(int? value)
	{
		return value is null ? null : new Enum((LazyThreadSafetyMode)value);
	}

	private static IntEnum? ToIntEnum(int? value)
	{
		return value is null ? null : new IntEnum((LazyThreadSafetyMode)value);
	}

	private static StringEnum? ToStringEnum(int? value)
	{
		return value is null ? null : new StringEnum((LazyThreadSafetyMode)value);
	}

	[Fact]
	public void UndefinedValue_Regularly_ShouldReturnExpectedResult()
	{
		Assert.Equal(~(LazyThreadSafetyMode)(1 | 2), Enum.UndefinedValue);
		Assert.Equal((SimpleSByte)(-65), DefinedEnum<SimpleSByte>.UndefinedValue);
		Assert.Equal((AdvancedSByte)3, DefinedEnum<AdvancedSByte>.UndefinedValue);
		Assert.Equal((SimpleShort)(-16397), DefinedEnum<SimpleShort>.UndefinedValue);
		Assert.Equal((AdvancedShort)3, DefinedEnum<AdvancedShort>.UndefinedValue);
		Assert.Equal(~(ExampleFlags)(1 | 2 | 4 | 16), DefinedEnum<ExampleFlags>.UndefinedValue);
	}

	[Fact]
	public void ThrowUndefinedInput_Regularly_ShouldThrow()
	{
		Assert.Throws<InvalidOperationException>(() => Enum.ThrowUndefinedInput());
	}

	[Fact]
	public void Construct_WithNull_ShouldThrowConfiguredException()
	{
		Assert.Throws<InvalidOperationException>(() => new Enum(null!));
		Assert.Throws<InvalidOperationException>(() => new IntEnum(null!));
		Assert.Throws<InvalidOperationException>(() => new StringEnum(null!));
	}

	[Fact]
	public void Construct_WithUndefinedValue_ShouldThrowConfiguredException()
	{
		Assert.Throws<InvalidOperationException>(() => new Enum((LazyThreadSafetyMode)(-1)));
		Assert.Throws<InvalidOperationException>(() => new IntEnum((LazyThreadSafetyMode)(-1)));
		Assert.Throws<InvalidOperationException>(() => new StringEnum((LazyThreadSafetyMode)(-1)));
	}

	[Fact]
	public void Construct_WithUndefinedBitForFlags_ShouldThrowConfiguredException()
	{
		Assert.Throws<InvalidOperationException>(() => new DefinedEnum<ExampleFlags>((ExampleFlags)8));
		Assert.Throws<InvalidOperationException>(() => new DefinedEnum<ExampleFlags, uint>((ExampleFlags)8));
		Assert.Throws<InvalidOperationException>(() => new DefinedEnum<ExampleFlags, string>((ExampleFlags)8));
	}

	[Fact]
	public void Construct_Regularly_ShouldHaveExpectedValue()
	{
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new Enum(LazyThreadSafetyMode.PublicationOnly).Value);
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new IntEnum(LazyThreadSafetyMode.PublicationOnly).Value);
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new StringEnum(LazyThreadSafetyMode.PublicationOnly).Value);
	}

	[Fact]
	public void Construct_FromNullableValue_ShouldHaveExpectedValue()
	{
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new Enum((LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly).Value);
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new IntEnum((LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly).Value);
		Assert.Equal(LazyThreadSafetyMode.PublicationOnly, new StringEnum((LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly).Value);
	}

	[Fact]
	public void Construct_WithValidFlags_ShouldHaveExpectedValue()
	{
		Assert.Equal(ExampleFlags.One | ExampleFlags.Sixteen, new DefinedEnum<ExampleFlags>(ExampleFlags.Zero | ExampleFlags.One | ExampleFlags.Sixteen).Value);
		Assert.Equal(ExampleFlags.One | ExampleFlags.Sixteen, new DefinedEnum<ExampleFlags, uint>(ExampleFlags.Zero | ExampleFlags.One | ExampleFlags.Sixteen).Value);
		Assert.Equal(ExampleFlags.One | ExampleFlags.Sixteen, new DefinedEnum<ExampleFlags, string>(ExampleFlags.Zero | ExampleFlags.One | ExampleFlags.Sixteen).Value);
	}

	[Fact]
	public void ToString_Regularly_ShouldReturnExpectedResult()
	{
		Assert.Equal("PublicationOnly", ToEnum(1).ToString());
		Assert.Equal("PublicationOnly", ToIntEnum(1).ToString());
		Assert.Equal("PublicationOnly", ToStringEnum(1).ToString());
	}

	[Fact]
	public void GetHashCode_Regulary_ShouldReturnExpectedResult()
	{
		Assert.Equal(1.GetHashCode(), ToEnum(1).GetHashCode());
		Assert.Equal(1.GetHashCode(), ToIntEnum(1).GetHashCode());
		Assert.Equal(1.GetHashCode(), ToStringEnum(1).GetHashCode());
	}

	[Fact]
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "WrapperValueObjectDefaultExpression:Default expression instantiating unvalidated value object", Justification = "Needs to work, despite being bad practice.")]
	public void GetHashCode_WithDefaultInstance_ShouldReturnExpectedResult()
	{
		var instance = default(Enum);
		Assert.Equal(0, instance.GetHashCode());
		var intInstance = default(IntEnum);
		Assert.Equal(0, intInstance.GetHashCode());
		var stringInstance = default(StringEnum);
		Assert.Equal(0, stringInstance.GetHashCode());
	}

	[Theory]
	[InlineData(0, 0, true)]
	[InlineData(0, 1, false)]
	[InlineData(1, 2, false)]
	public void Equals_Regularly_ShouldReturnExpectedResult(int one, int two, bool expectedResult)
	{
		Assert.Equal(expectedResult, ToEnum(one).Equals(ToEnum(two)));
		Assert.Equal(expectedResult, ToIntEnum(one).Equals(ToIntEnum(two)));
		Assert.Equal(expectedResult, ToStringEnum(one).Equals(ToStringEnum(two)));
	
		Assert.Equal(expectedResult, ToEnum(one).Equals(ToIntEnum(two)));
		Assert.Equal(expectedResult, ToEnum(one).Equals(ToStringEnum(two)));
		Assert.Equal(expectedResult, ToIntEnum(one).Equals(ToEnum(two)));
		Assert.Equal(expectedResult, ToIntEnum(one).Equals(ToStringEnum(two)));
		Assert.Equal(expectedResult, ToStringEnum(one).Equals(ToEnum(two)));
		Assert.Equal(expectedResult, ToStringEnum(one).Equals(ToIntEnum(two)));
	}

	[Fact]
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "WrapperValueObjectDefaultExpression:Default expression instantiating unvalidated value object", Justification = "Needs to work, despite being bad practice.")]
	public void Equals_WithDefaultInstance_ShouldReturnExpectedResult()
	{
		Assert.NotEqual(default, ToEnum(1));
		Assert.NotEqual(ToEnum(1), default);

		Assert.NotEqual(default, ToIntEnum(1));
		Assert.NotEqual(ToIntEnum(1), default);

		Assert.NotEqual(default, ToStringEnum(1));
		Assert.NotEqual(ToStringEnum(1), default);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(0, 1)]
	[InlineData(1, 2)]
	public void EqualityOperator_Regularly_ShouldMatchEquals(int one, int two)
	{
		Assert.Equal(ToEnum(one).Equals(ToEnum(two)), ToEnum(one) == ToEnum(two));
		Assert.Equal(ToEnum(one).Equals(ToEnum(two)), ToEnum(one) == ToEnum(two).Value);
		Assert.Equal(ToEnum(one).Equals(ToEnum(two)), ToEnum(one).Value == ToEnum(two));

		Assert.Equal(ToIntEnum(one).Equals(ToIntEnum(two)), ToIntEnum(one) == ToIntEnum(two));
		Assert.Equal(ToIntEnum(one).Equals(ToIntEnum(two)), ToIntEnum(one) == ToIntEnum(two).Value);
		Assert.Equal(ToIntEnum(one).Equals(ToIntEnum(two)), ToIntEnum(one).Value == ToIntEnum(two));

		Assert.Equal(ToStringEnum(one).Equals(ToStringEnum(two)), ToStringEnum(one) == ToStringEnum(two));
		Assert.Equal(ToStringEnum(one).Equals(ToStringEnum(two)), ToStringEnum(one) == ToStringEnum(two).Value);
		Assert.Equal(ToStringEnum(one).Equals(ToStringEnum(two)), ToStringEnum(one).Value == ToStringEnum(two));

		Assert.Equal(ToEnum(one).Equals(ToIntEnum(two)), ToEnum(one) == ToIntEnum(two));
		Assert.Equal(ToEnum(one).Equals(ToStringEnum(two)), ToEnum(one) == ToStringEnum(two));
		Assert.Equal(ToIntEnum(one).Equals(ToEnum(two)), ToIntEnum(one) == ToEnum(two));
		Assert.Equal(ToStringEnum(one).Equals(ToEnum(two)), ToStringEnum(one) == ToEnum(two));

		// Cannot compare directly between different primitive representations - can simply take their values for such comparisons
		//Assert.Equal(ToIntEnum(one).Equals(ToStringEnum(two)), ToIntEnum(one) == ToStringEnum(two));
		//Assert.Equal(ToStringEnum(one).Equals(ToIntEnum(two)), ToStringEnum(one) == ToIntEnum(two));
	}

	[Fact]
	public void EqualityOperator_WithNullables_ShouldReturnExpectedResult()
	{
#pragma warning disable IDE0079 // Remove unnecessary suppression -- Suppression below is falsely flagged as unnecessary
#pragma warning disable IDE0004 // Remove unnecessary cast -- Deliberate casts to test specific operators
#pragma warning disable CS8073 // The result of the expression is always 'false' (or 'true') -- Deliberate casts to test specific operators
#pragma warning disable xUnit2024 // Do not use boolean asserts for simple equality tests -- Deliberate test of specific operators
		Assert.True((Enum?)null == (Enum?)null);
		Assert.True((IntEnum?)null == (IntEnum?)null);
		Assert.True((StringEnum?)null == (StringEnum?)null);

		Assert.False((Enum?)null == LazyThreadSafetyMode.PublicationOnly);
		Assert.False((IntEnum?)null == LazyThreadSafetyMode.PublicationOnly);
		Assert.False((StringEnum?)null == LazyThreadSafetyMode.PublicationOnly);
		Assert.True((Enum?)ToEnum(1) == LazyThreadSafetyMode.PublicationOnly);
		Assert.True((IntEnum?)ToIntEnum(1) == LazyThreadSafetyMode.PublicationOnly);
		Assert.True((StringEnum?)ToStringEnum(1) == LazyThreadSafetyMode.PublicationOnly);

		// Analyzers disallows implicit conversion to DefinedEnum from non-constant
		//Assert.False((Enum?)null == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.False((IntEnum?)null == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.False((StringEnum?)null == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.True((Enum?)ToEnum(1) == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.True((IntEnum?)ToIntEnum(1) == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.True((StringEnum?)ToStringEnum(1) == (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);

		Assert.False((Enum?)null == (Enum?)ToEnum(0));
		Assert.False((IntEnum?)null == (IntEnum?)ToIntEnum(0));
		Assert.False((StringEnum?)null == (StringEnum?)ToStringEnum(0));
		Assert.False((Enum?)ToEnum(0) == (Enum?)null);
		Assert.False((IntEnum?)ToIntEnum(0) == (IntEnum?)null);
		Assert.False((StringEnum?)ToStringEnum(0) == (StringEnum?)null);

		Assert.True((Enum?)ToEnum(0) == (Enum?)ToEnum(0));
		Assert.True((IntEnum?)ToIntEnum(0) == (IntEnum?)ToIntEnum(0));
		Assert.True((StringEnum?)ToStringEnum(0) == (StringEnum?)ToStringEnum(0));

		// Comparisons between nullables of different enum-related types are ambiguous, which is easy enough to work around by using their values
		//Assert.True((Enum?)ToEnum(0) == (IntEnum?)ToIntEnum(0));
		//Assert.True((Enum?)ToEnum(0) == (StringEnum?)ToStringEnum(0));
		//Assert.True((IntEnum?)ToIntEnum(0) == (Enum?)ToEnum(0));
		//Assert.True((StringEnum?)ToStringEnum(0) == (Enum?)ToEnum(0));
		//Assert.True((IntEnum?)ToIntEnum(0) == (StringEnum?)ToStringEnum(0));
		//Assert.True((StringEnum?)ToStringEnum(0) == (IntEnum?)ToIntEnum(0));
#pragma warning restore xUnit2024
#pragma warning restore CS8073
#pragma warning restore IDE0004
#pragma warning restore IDE0079
	}

	[Fact]
	public void InequalityOperator_WithNullables_ShouldReturnExpectedResult()
	{
#pragma warning disable IDE0079 // Remove unnecessary suppression -- Suppression below is falsely flagged as unnecessary
#pragma warning disable IDE0004 // Remove unnecessary cast -- Deliberate casts to test specific operators
#pragma warning disable CS8073 // The result of the expression is always 'false' (or 'true') -- Deliberate casts to test specific operators
#pragma warning disable xUnit2024 // Do not use boolean asserts for simple equality tests -- Deliberate test of specific operators
		Assert.False((Enum?)null != (Enum?)null);
		Assert.False((IntEnum?)null != (IntEnum?)null);
		Assert.False((StringEnum?)null != (StringEnum?)null);

		Assert.True((Enum?)null != LazyThreadSafetyMode.PublicationOnly);
		Assert.True((IntEnum?)null != LazyThreadSafetyMode.PublicationOnly);
		Assert.True((StringEnum?)null != LazyThreadSafetyMode.PublicationOnly);
		Assert.False((Enum?)ToEnum(1) != LazyThreadSafetyMode.PublicationOnly);
		Assert.False((IntEnum?)ToIntEnum(1) != LazyThreadSafetyMode.PublicationOnly);
		Assert.False((StringEnum?)ToStringEnum(1) != LazyThreadSafetyMode.PublicationOnly);

		// Analyzers disallows implicit conversion to DefinedEnum from non-constant
		//Assert.True((Enum?)null != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.True((IntEnum?)null != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.True((StringEnum?)null != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.False((Enum?)ToEnum(1) != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.False((IntEnum?)ToIntEnum(1) != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);
		//Assert.False((StringEnum?)ToStringEnum(1) != (LazyThreadSafetyMode?)LazyThreadSafetyMode.PublicationOnly);

		Assert.True((Enum?)null != (Enum?)ToEnum(0));
		Assert.True((IntEnum?)null != (IntEnum?)ToIntEnum(0));
		Assert.True((StringEnum?)null != (StringEnum?)ToStringEnum(0));
		Assert.True((Enum?)ToEnum(0) != (Enum?)null);
		Assert.True((IntEnum?)ToIntEnum(0) != (IntEnum?)null);
		Assert.True((StringEnum?)ToStringEnum(0) != (StringEnum?)null);

		Assert.False((Enum?)ToEnum(0) != (Enum?)ToEnum(0));
		Assert.False((IntEnum?)ToIntEnum(0) != (IntEnum?)ToIntEnum(0));
		Assert.False((StringEnum?)ToStringEnum(0) != (StringEnum?)ToStringEnum(0));

		// Comparisons between nullables of different enum-related types are ambiguous, which is easy enough to work around by using their values
		//Assert.False((Enum?)ToEnum(0) != (IntEnum?)ToIntEnum(0));
		//Assert.False((Enum?)ToEnum(0) != (StringEnum?)ToStringEnum(0));
		//Assert.False((IntEnum?)ToIntEnum(0) != (Enum?)ToEnum(0));
		//Assert.False((StringEnum?)ToStringEnum(0) != (Enum?)ToEnum(0));
		//Assert.False((IntEnum?)ToIntEnum(0) != (StringEnum?)ToStringEnum(0));
		//Assert.False((StringEnum?)ToStringEnum(0) != (IntEnum?)ToIntEnum(0));
#pragma warning restore xUnit2024
#pragma warning restore CS8073
#pragma warning restore IDE0004
#pragma warning restore IDE0079
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(0, 1)]
	[InlineData(1, 0)]
	public void CompareTo_WithEqualValues_ShouldHaveEqualityMatchingEquality(int one, int two)
	{
		Assert.Equal(one.Equals(two), ToEnum(one).CompareTo(ToEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToIntEnum(one).CompareTo(ToIntEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToStringEnum(one).CompareTo(ToStringEnum(two)) == 0);

		Assert.Equal(one.Equals(two), ToEnum(one).CompareTo(ToIntEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToEnum(one).CompareTo(ToStringEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToIntEnum(one).CompareTo(ToEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToIntEnum(one).CompareTo(ToStringEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToStringEnum(one).CompareTo(ToEnum(two)) == 0);
		Assert.Equal(one.Equals(two), ToStringEnum(one).CompareTo(ToIntEnum(two)) == 0);
	}

	[Theory]
	[InlineData(null, null, 0)]
	[InlineData(null, 0, -1)]
	[InlineData(0, null, +1)]
	[InlineData(0, 0, 0)]
	[InlineData(0, 1, -1)]
	[InlineData(1, 0, +1)]
	public void CompareTo_Regularly_ShouldReturnExpectedResult(int? one, int? two, int expectedResult)
	{
		Assert.Equal(expectedResult, Comparer<Enum?>.Default.Compare(ToEnum(one), ToEnum(two)));
		Assert.Equal(-expectedResult, Comparer<Enum?>.Default.Compare(ToEnum(two), ToEnum(one)));
		Assert.Equal(expectedResult, Comparer<IntEnum?>.Default.Compare(ToIntEnum(one), ToIntEnum(two)));
		Assert.Equal(-expectedResult, Comparer<IntEnum?>.Default.Compare(ToIntEnum(two), ToIntEnum(one)));
		Assert.Equal(expectedResult, Comparer<StringEnum?>.Default.Compare(ToStringEnum(one), ToStringEnum(two)));
		Assert.Equal(-expectedResult, Comparer<StringEnum?>.Default.Compare(ToStringEnum(two), ToStringEnum(one)));

		Assert.Equal(expectedResult, Comparer<Enum?>.Default.Compare(ToEnum(one), ToIntEnum(two)));
		Assert.Equal(expectedResult, Comparer<Enum?>.Default.Compare(ToEnum(one), ToStringEnum(two)));

		Assert.Equal(expectedResult, Comparer<IntEnum?>.Default.Compare(ToIntEnum(one), ToEnum(two)));
		Assert.Equal(expectedResult, Comparer<StringEnum?>.Default.Compare(ToStringEnum(one), ToEnum(two)));
	}

	[Theory]
	[InlineData(0, 0, 0)]
	[InlineData(0, 1, -1)]
	[InlineData(1, 0, +1)]
	public void GreaterThan_Regularly_ShouldReturnExpectedResult(int one, int two, int expectedResult)
	{
		Assert.Equal(expectedResult > 0, ToEnum(one) > ToEnum(two));
		Assert.Equal(expectedResult > 0, ToEnum(one) > ToEnum(two).Value);
		Assert.Equal(expectedResult > 0, ToEnum(one).Value > ToEnum(two));
		Assert.Equal(expectedResult <= 0, ToEnum(one) <= ToEnum(two));
		Assert.Equal(expectedResult <= 0, ToEnum(one) <= ToEnum(two).Value);
		Assert.Equal(expectedResult <= 0, ToEnum(one).Value <= ToEnum(two));

		Assert.Equal(expectedResult > 0, ToIntEnum(one) > ToIntEnum(two));
		Assert.Equal(expectedResult > 0, ToIntEnum(one) > ToIntEnum(two).Value);
		Assert.Equal(expectedResult > 0, ToIntEnum(one).Value > ToIntEnum(two));
		Assert.Equal(expectedResult <= 0, ToIntEnum(one) <= ToIntEnum(two));
		Assert.Equal(expectedResult <= 0, ToIntEnum(one) <= ToIntEnum(two).Value);
		Assert.Equal(expectedResult <= 0, ToIntEnum(one).Value <= ToIntEnum(two));

		Assert.Equal(expectedResult > 0, ToStringEnum(one) > ToStringEnum(two));
		Assert.Equal(expectedResult > 0, ToStringEnum(one) > ToStringEnum(two).Value);
		Assert.Equal(expectedResult > 0, ToStringEnum(one).Value > ToStringEnum(two));
		Assert.Equal(expectedResult <= 0, ToStringEnum(one) <= ToStringEnum(two));
		Assert.Equal(expectedResult <= 0, ToStringEnum(one) <= ToStringEnum(two).Value);
		Assert.Equal(expectedResult <= 0, ToStringEnum(one).Value <= ToStringEnum(two));

		Assert.Equal(expectedResult > 0, ToEnum(one) > ToIntEnum(two));
		Assert.Equal(expectedResult > 0, ToEnum(one) > ToStringEnum(two));
		Assert.Equal(expectedResult > 0, ToIntEnum(one) > ToEnum(two));
		Assert.Equal(expectedResult > 0, ToStringEnum(one) > ToEnum(two));

		// Cannot compare directly between different primitive representations - can simply take their values for such comparisons
		//Assert.Equal(expectedResult > 0, ToIntEnum(one) > ToStringEnum(two));
		//Assert.Equal(expectedResult > 0, ToStringEnum(one) > ToIntEnum(two));
	}

	[Theory]
	[InlineData(0, 0, 0)]
	[InlineData(0, 1, -1)]
	[InlineData(1, 0, +1)]
	public void LessThan_Regularly_ShouldReturnExpectedResult(int one, int two, int expectedResult)
	{
		Assert.Equal(expectedResult < 0, ToEnum(one) < ToEnum(two));
		Assert.Equal(expectedResult < 0, ToEnum(one) < ToEnum(two).Value);
		Assert.Equal(expectedResult < 0, ToEnum(one).Value < ToEnum(two));
		Assert.Equal(expectedResult >= 0, ToEnum(one) >= ToEnum(two));
		Assert.Equal(expectedResult >= 0, ToEnum(one) >= ToEnum(two).Value);
		Assert.Equal(expectedResult >= 0, ToEnum(one).Value >= ToEnum(two));

		Assert.Equal(expectedResult < 0, ToIntEnum(one) < ToIntEnum(two));
		Assert.Equal(expectedResult < 0, ToIntEnum(one) < ToIntEnum(two).Value);
		Assert.Equal(expectedResult < 0, ToIntEnum(one).Value < ToIntEnum(two));
		Assert.Equal(expectedResult >= 0, ToIntEnum(one) >= ToIntEnum(two));
		Assert.Equal(expectedResult >= 0, ToIntEnum(one) >= ToIntEnum(two).Value);
		Assert.Equal(expectedResult >= 0, ToIntEnum(one).Value >= ToIntEnum(two));

		Assert.Equal(expectedResult < 0, ToStringEnum(one) < ToStringEnum(two));
		Assert.Equal(expectedResult < 0, ToStringEnum(one) < ToStringEnum(two).Value);
		Assert.Equal(expectedResult < 0, ToStringEnum(one).Value < ToStringEnum(two));
		Assert.Equal(expectedResult >= 0, ToStringEnum(one) >= ToStringEnum(two));
		Assert.Equal(expectedResult >= 0, ToStringEnum(one) >= ToStringEnum(two).Value);
		Assert.Equal(expectedResult >= 0, ToStringEnum(one).Value >= ToStringEnum(two));

		Assert.Equal(expectedResult < 0, ToEnum(one) < ToIntEnum(two));
		Assert.Equal(expectedResult < 0, ToEnum(one) < ToStringEnum(two));
		Assert.Equal(expectedResult < 0, ToIntEnum(one) < ToEnum(two));
		Assert.Equal(expectedResult < 0, ToStringEnum(one) < ToEnum(two));

		// Cannot compare directly between different primitive representations - can simply take their values for such comparisons
		//Assert.Equal(expectedResult < 0, ToIntEnum(one) < ToStringEnum(two));
		//Assert.Equal(expectedResult < 0, ToStringEnum(one) < ToIntEnum(two));
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastToUnderlyingType_Regularly_ShouldReturnExpectedResultOrThrowConfiguredException(int? value, int? expectedResult)
	{
		var intInstance = ToIntEnum(value);
		var stringInstance = ToStringEnum(value);

		if (expectedResult is null)
		{
			// Configured exception
			Assert.Throws<InvalidOperationException>(() => (int)(LazyThreadSafetyMode)intInstance!);
			Assert.Throws<InvalidOperationException>(() => (int)(LazyThreadSafetyMode)stringInstance!);
		}
		else
		{
			Assert.Equal(expectedResult, (int)(LazyThreadSafetyMode)intInstance!);
			Assert.Equal(expectedResult, (int)(LazyThreadSafetyMode)stringInstance!);
		}
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastToNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
	{
		var intInstance = ToIntEnum(value);
		var stringInstance = ToStringEnum(value);

		Assert.Equal(expectedResult, (int?)(LazyThreadSafetyMode?)intInstance);
		Assert.Equal(expectedResult, (int?)(LazyThreadSafetyMode?)stringInstance);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastFromUnderlyingType_Regularly_ShouldReturnExpectedResult(int value, int expectedResult)
	{
		Assert.Equal(ToIntEnum(expectedResult), (IntEnum)(LazyThreadSafetyMode)value);
		Assert.Equal(ToStringEnum(expectedResult), (StringEnum)(LazyThreadSafetyMode)value);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastFromNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
	{
		Assert.Equal((LazyThreadSafetyMode?)expectedResult, ((IntEnum?)(LazyThreadSafetyMode?)value)?.Value);
		Assert.Equal((LazyThreadSafetyMode?)expectedResult, ((StringEnum?)(LazyThreadSafetyMode?)value)?.Value);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastToNumericType_Regularly_ShouldReturnExpectedResult(int value, int expectedResult)
	{
		Assert.Equal(expectedResult, (Int128)ToIntEnum(value));
		Assert.Equal(expectedResult, (Int128)ToStringEnum(value));
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastToNullableCoreType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
	{
		Assert.Equal(expectedResult, (Int128?)ToIntEnum(value));
		Assert.Equal(expectedResult, (Int128?)ToStringEnum(value));
	}

	[Fact]
	public void CastBetweenWithAndWithoutPrimitive_Regularly_ShouldReturnExpectedResult()
	{
		Assert.Equal(ToEnum(1), (Enum)ToIntEnum(1));
		Assert.Equal(ToEnum(1), (Enum?)ToIntEnum(1));
		Assert.Equal(ToEnum(1), (Enum)ToStringEnum(1));
		Assert.Equal(ToEnum(1), (Enum?)ToStringEnum(1));

		Assert.Equal(ToIntEnum(1), (IntEnum)ToEnum(1));
		Assert.Equal(ToIntEnum(1), (IntEnum?)ToEnum(1));
		Assert.Equal(ToStringEnum(1), (StringEnum)ToEnum(1));
		Assert.Equal(ToStringEnum(1), (StringEnum?)ToEnum(1));
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void Value_ViaCoreValueInterface_ShouldReturnExpectedResult(int value, string stringValue)
	{
		IntWrapper intInstance = ToIntEnum(value);
		Assert.IsType<int>(intInstance.Value);
		Assert.Equal(value, intInstance.Value);

		StringWrapper stringInstance = ToStringEnum(value);
		Assert.IsType<string>(stringInstance.Value);
		Assert.Equal(stringValue, stringInstance.Value);
	}

	/// <summary>
	/// Helper to access abstract statics.
	/// </summary>
	private static TWrapper CreateFromCoreValue<TWrapper, TValue>(TValue value)
		where TWrapper : ICoreValueWrapper<TWrapper, TValue>
	{
		return TWrapper.Create(value);
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void Create_ViaCoreValueInterface_ShouldReturnExpectedResult(int value, string stringValue)
	{
		Assert.IsType<IntEnum>(CreateFromCoreValue<IntEnum, int>(value));
		Assert.Equal((LazyThreadSafetyMode)value, CreateFromCoreValue<IntEnum, int>(value).Value);

		Assert.IsType<StringEnum>(CreateFromCoreValue<StringEnum, string>(stringValue));
		Assert.Equal((LazyThreadSafetyMode)value, CreateFromCoreValue<StringEnum, string>(value.ToString()).Value);
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void Serialize_ToCoreType_ShouldReturnExpectedResult(int value, string stringValue)
	{
		IntWrapper intInstance = ToIntEnum(value);
		Assert.IsType<int>(intInstance.Serialize());
		Assert.Equal(value, intInstance.Serialize());

		StringWrapper stringInstance = ToStringEnum(value);
		Assert.IsType<string>(stringInstance.Serialize());
		Assert.Equal(stringValue, stringInstance.Serialize());
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void SerializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int? value, string? stringValue)
	{
		var intInstance = ToIntEnum(value);
		Assert.Equal(value?.ToString() ?? "null", System.Text.Json.JsonSerializer.Serialize(intInstance));

		var stringInstance = ToStringEnum(value);
		Assert.Equal(value is null ? "null" : $@"""{stringValue}""", System.Text.Json.JsonSerializer.Serialize(stringInstance));
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void SerializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(int? value, string? stringValue)
	{
		var intInstance = ToIntEnum(value);
		Assert.Equal(value?.ToString() ?? "null", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance));

		var stringInstance = ToStringEnum(value);
		Assert.Equal(value is null ? "null" : $@"""{stringValue}""", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance));
	}

	/// <summary>
	/// Helper to access abstract statics.
	/// </summary>
	private static TWrapper Deserialize<TWrapper, TValue>(TValue value)
		where TWrapper : IValueWrapper<TWrapper, TValue>
	{
		return TWrapper.Deserialize(value);
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void Deserialize_FromCoreType_ShouldReturnExpectedResult(int value, string stringValue)
	{
		Assert.IsType<IntEnum>(Deserialize<IntEnum, int>(value));
		Assert.Equal(value, (int)Deserialize<IntEnum, int>(value).Value);

		Assert.IsType<StringEnum>(Deserialize<StringEnum, string>(stringValue));
		Assert.Equal(value, (int)Deserialize<StringEnum, string>(stringValue).Value);
	}

	/// <summary>
	/// Deserialization bypasses validation.
	/// </summary>
	[Fact]
	public void Deserialize_FromUndefinedCoreValue_ShouldReturnExpectedResult()
	{
		// From numeric we get that particular value, undefined though it may be
		Assert.Equal((LazyThreadSafetyMode)(-1), Deserialize<IntEnum, int>(-1).Value);

		// From string we get our default undefined value
		Assert.Equal(~(LazyThreadSafetyMode)(1 | 2), Deserialize<StringEnum, string>("Nonexistent").Value);
		Assert.Equal((SimpleSByte)(-65), Deserialize<DefinedEnum<SimpleSByte, string>, string>("Nonexistent").Value); // Preferred
		Assert.Equal((AdvancedSByte)3, Deserialize<DefinedEnum<AdvancedSByte, string>, string>("Nonexistent").Value); // Found by scanning
		Assert.Equal((SimpleShort)(-16397), Deserialize<DefinedEnum<SimpleShort, string>, string>("Nonexistent").Value); // Fallback when preferred not available
		Assert.Equal((AdvancedShort)3, Deserialize<DefinedEnum<AdvancedShort, string>, string>("Nonexistent").Value); // Found by scanning
	}

	[Theory]
	[InlineData("null", null, null)]
	[InlineData("0", 0, "None")]
	[InlineData("1", 1, "PublicationOnly")]
	public void DeserializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int? value, string? stringValue)
	{
		Assert.Equal(value, (int?)System.Text.Json.JsonSerializer.Deserialize<IntEnum?>(json)?.Value);

		json = json == "null" ? json : $@"""{stringValue}""";
		Assert.Equal(value, (int?)System.Text.Json.JsonSerializer.Deserialize<StringEnum?>(json)?.Value);
	}

	[Theory]
	[InlineData("null", null, null)]
	[InlineData("0", 0, "None")]
	[InlineData("1", 1, "PublicationOnly")]
	public void DeserializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(string json, int? value, string? stringValue)
	{
		if (value is not null) // To also test deserialization NOT wrapped in a nullable
			Assert.Equal(value, (int)Newtonsoft.Json.JsonConvert.DeserializeObject<IntEnum>(json).Value);

		json = json == "null" ? json : $@"""{stringValue}""";
		Assert.Equal(value, (int?)Newtonsoft.Json.JsonConvert.DeserializeObject<StringEnum?>(json)?.Value);
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void ReadAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int value, string stringValue)
	{
		Assert.Equal(KeyValuePair.Create(ToIntEnum(value), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<IntEnum, bool>>($$"""{ "{{value}}": true }""")?.Single());

		Assert.Equal(KeyValuePair.Create(ToStringEnum(value), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<StringEnum, bool>>($$"""{ "{{stringValue}}": true }""")?.Single());
	}

	[Theory]
	[InlineData(0, "None")]
	[InlineData(1, "PublicationOnly")]
	public void WriteAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int value, string stringValue)
	{
		Assert.Equal($$"""{"{{value}}":true}""", System.Text.Json.JsonSerializer.Serialize(new Dictionary<IntEnum, bool>() { [ToIntEnum(value)] = true }));

		Assert.Equal($$"""{"{{stringValue}}":true}""", System.Text.Json.JsonSerializer.Serialize(new Dictionary<StringEnum, bool>() { [ToStringEnum(value)] = true }));
	}

	[Fact]
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Design", "WrapperValueObjectDefaultExpression:Default expression instantiating unvalidated value object", Justification = "Needs to work, despite being bad practice.")]
	public void FormattableToString_InAllScenarios_ShouldReturnExpectedResult()
	{
		Assert.Equal("None", default(Enum).ToString(format: null, formatProvider: null));
		Assert.Equal("None", default(IntEnum).ToString(format: null, formatProvider: null));
		Assert.Equal("None", default(StringEnum).ToString(format: null, formatProvider: null));
		Assert.Equal("PublicationOnly", ToEnum(1).ToString(format: null, formatProvider: null));
		Assert.Equal("PublicationOnly", ToIntEnum(1).ToString(format: null, formatProvider: null));
		Assert.Equal("PublicationOnly", ToStringEnum(1).ToString(format: null, formatProvider: null));

		Assert.Equal("-1", DomainObjectSerializer.Deserialize<IntEnum, int>(-1).ToString(format: null, formatProvider: null));
	}

	[Fact]
	public void SpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
	{
		Span<char> result = stackalloc char[15];

		Assert.True(ToEnum(1).TryFormat(result, out var charsWritten, format: null, provider: null));
		Assert.Equal(15, charsWritten);
		Assert.Equal("PublicationOnly".AsSpan(), result);

		Assert.True(ToIntEnum(1).TryFormat(result, out charsWritten, format: null, provider: null));
		Assert.Equal(15, charsWritten);
		Assert.Equal("PublicationOnly".AsSpan(), result);

		Assert.True(ToStringEnum(1).TryFormat(result, out charsWritten, format: null, provider: null));
		Assert.Equal(15, charsWritten);
		Assert.Equal("PublicationOnly".AsSpan(), result);

		Assert.True(DomainObjectSerializer.Deserialize<IntEnum, int>(-1).TryFormat(result, out charsWritten, format: null, provider: null));
		Assert.Equal("-1", result[..2]);
	}

	[Fact]
	public void UtfSpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
	{
		Span<byte> result = stackalloc byte[15];

		Assert.True(ToIntEnum(1).TryFormat(result, out var bytesWritten, format: null, provider: null));
		Assert.Equal(15, bytesWritten);
		Assert.Equal("PublicationOnly"u8, result);

		Assert.True(ToEnum(1).TryFormat(result, out bytesWritten, format: null, provider: null));
		Assert.Equal(15, bytesWritten);
		Assert.Equal("PublicationOnly"u8, result);

		Assert.True(ToStringEnum(1).TryFormat(result, out bytesWritten, format: null, provider: null));
		Assert.Equal(15, bytesWritten);
		Assert.Equal("PublicationOnly"u8, result);

		Assert.True(DomainObjectSerializer.Deserialize<IntEnum, int>(-1).TryFormat(result, out bytesWritten, format: null, provider: null));
		Assert.Equal("-1"u8, result[..2]);
	}

	[Fact]
	public void ParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
	{
		var input = "publicationonly";

		Assert.True(Enum.TryParse(input, provider: null, out var result1));
		Assert.Equal((LazyThreadSafetyMode)1, result1.Value);
		Assert.Equal(result1, Enum.Parse(input, provider: null));

		Assert.True(IntEnum.TryParse(input, provider: null, out var result2));
		Assert.Equal((LazyThreadSafetyMode)1, result2.Value);
		Assert.Equal(result2, IntEnum.Parse(input, provider: null));

		Assert.True(StringEnum.TryParse(input, provider: null, out var result3));
		Assert.Equal((LazyThreadSafetyMode)1, result3.Value);
		Assert.Equal(result3, StringEnum.Parse(input, provider: null));
	}

	[Fact]
	public void SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
	{
		var input = "publicationonly".AsSpan();

		Assert.True(Enum.TryParse(input, provider: null, out var result1));
		Assert.Equal((LazyThreadSafetyMode)1, result1.Value);
		Assert.Equal(result1, Enum.Parse(input, provider: null));

		Assert.True(IntEnum.TryParse(input, provider: null, out var result2));
		Assert.Equal((LazyThreadSafetyMode)1, result2.Value);
		Assert.Equal(result2, IntEnum.Parse(input, provider: null));

		Assert.True(StringEnum.TryParse(input, provider: null, out var result3));
		Assert.Equal((LazyThreadSafetyMode)1, result3.Value);
		Assert.Equal(result3, StringEnum.Parse(input, provider: null));
	}

	[Fact]
	public void Utf8SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
	{
		var input = "publicationonly"u8;

		Assert.True(Enum.TryParse(input, provider: null, out var result1));
		Assert.Equal((LazyThreadSafetyMode)1, result1.Value);
		Assert.Equal(result1, Enum.Parse(input, provider: null));

		Assert.True(IntEnum.TryParse(input, provider: null, out var result2));
		Assert.Equal((LazyThreadSafetyMode)1, result2.Value);
		Assert.Equal(result2, IntEnum.Parse(input, provider: null));

		Assert.True(StringEnum.TryParse(input, provider: null, out var result3));
		Assert.Equal((LazyThreadSafetyMode)1, result3.Value);
		Assert.Equal(result3, StringEnum.Parse(input, provider: null));
	}

	[Fact]
	public void ParsabilityAndFormattability_InAllScenarios_ShouldBeGeneratedAccordingToTransitiveAvailability()
	{
		var interfaces = typeof(Enum).GetInterfaces();
		Assert.Contains(interfaces, interf => interf.Name == "ISpanFormattable");
		Assert.Contains(interfaces, interf => interf.Name == "ISpanParsable`1");
		Assert.Contains(interfaces, interf => interf.Name == "IUtf8SpanFormattable");
		Assert.Contains(interfaces, interf => interf.Name == "IUtf8SpanParsable`1");

		interfaces = typeof(StringEnum).GetInterfaces();
		Assert.Contains(interfaces, interf => interf.Name == "ISpanFormattable");
		Assert.Contains(interfaces, interf => interf.Name == "ISpanParsable`1");
		Assert.Contains(interfaces, interf => interf.Name == "IUtf8SpanFormattable");
		Assert.Contains(interfaces, interf => interf.Name == "IUtf8SpanParsable`1");
	}
}
