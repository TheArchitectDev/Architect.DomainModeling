using Architect.DomainModeling.Tests.IdentityTestTypes;
using Architect.DomainModeling.Tests.WrapperValueObjectTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests;

public class ValueObjectExtensionsTests
{
	[Fact]
	public void IsDefault_WithDefaultEquivalent_ShouldReturnExpectedResult()
	{
		var result1 = default(StringId).IsDefault();
		var result2 = new StringId("").IsDefault();

		Assert.True(result1);
		Assert.True(result2);
	}

	[Fact]
	public void IsDefault_WithoutDefaultEquivalent_ShouldReturnExpectedResult()
	{
		var result = new StringValue("A").IsDefault();

		Assert.False(result);
	}
}
