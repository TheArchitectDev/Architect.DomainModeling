using Architect.DomainModeling.Enums;
using Xunit;

namespace Architect.DomainModeling.Tests.Enums;

public class InternalEnumExtensionsTests
{
	private enum ByteEnum : byte
	{
		One = 1,
	}
	private enum LongEnum : long
	{
		MinusOne = -1,
		Min = Int64.MinValue,
	}
	private enum UlongEnum : ulong
	{
		Max = UInt64.MaxValue,
	}

	[Fact]
	public void GetNumericValue_WithByte_ShouldReturnExpectedResult()
	{
		Assert.Equal((Int128)1, ByteEnum.One.GetNumericValue());
	}

	[Fact]
	public void GetNumericValue_WithLong_ShouldReturnExpectedResult()
	{
		Assert.Equal((Int128)(-1), LongEnum.MinusOne.GetNumericValue());
		Assert.Equal((Int128)(Int64.MinValue), LongEnum.Min.GetNumericValue());
	}

	[Fact]
	public void GetNumericValue_WithUlong_ShouldReturnExpectedResult()
	{
		Assert.Equal((Int128)UInt64.MaxValue, UlongEnum.Max.GetNumericValue());
	}

	[Fact]
	public void GetBinaryValue_WithByte_ShouldCastAndGetEnumValueToUnderlyingTypeCorrectly()
	{
		var result = ByteEnum.One.GetBinaryValue();
		Assert.Equal(1, (byte)result);
		Assert.Equal(ByteEnum.One, (ByteEnum)result);
		Assert.Equal(ByteEnum.One, InternalEnumExtensions.GetEnumValue<ByteEnum>(result));
	}

	[Fact]
	public void GetBinaryValue_WithLong_ShouldCastAndGetEnumValueToUnderlyingTypeCorrectly()
	{
		var result1 = LongEnum.MinusOne.GetBinaryValue();
		var result2 = LongEnum.Min.GetBinaryValue();

		Assert.Equal(-1, (long)result1);
		Assert.Equal(LongEnum.MinusOne, (LongEnum)result1);
		Assert.Equal(LongEnum.MinusOne, InternalEnumExtensions.GetEnumValue<LongEnum>(result1));

		Assert.Equal(Int64.MinValue, (long)result2);
		Assert.Equal(LongEnum.Min, (LongEnum)result2);
		Assert.Equal(LongEnum.Min, InternalEnumExtensions.GetEnumValue<LongEnum>(result2));
	}

	[Fact]
	public void GetBinaryValue_WithUlong_ShouldCastAndGetEnumValueToUnderlyingTypeCorrectly()
	{
		var result = UlongEnum.Max.GetBinaryValue();
		Assert.Equal(UInt64.MaxValue, result); // Already the type we would cast to
		Assert.Equal(UlongEnum.Max, (UlongEnum)result);
		Assert.Equal(UlongEnum.Max, InternalEnumExtensions.GetEnumValue<UlongEnum>(result));
	}
}
